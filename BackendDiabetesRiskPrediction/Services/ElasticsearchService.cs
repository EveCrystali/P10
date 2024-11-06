using BackendDiabetesRiskPrediction.Models;
using Elasticsearch.Net;
using Nest;
namespace BackendDiabetesRiskPrediction.Services;

public class ElasticsearchService
{
    private readonly ElasticClient _elasticsearchClient;

    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(ILogger<ElasticsearchService> logger)
    {
        ConnectionSettings settings = new ConnectionSettings(new Uri("http://elasticsearch:9200"))
                                      .DefaultIndex("notes_index")
                                      .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
                                      .DisablePing()
                                      .DisableDirectStreaming()
                                      .ThrowExceptions();

        _elasticsearchClient = new ElasticClient(settings);

        _logger = logger;

        _logger.LogInformation("ElasticsearchService initialized successfully.");
    }

    public async Task IndexNoteAsync(NoteRiskInfo note)
    {
        var response = await _elasticsearchClient.IndexDocumentAsync(note);

        if (!response.IsValid)
        {
            throw new Exception($"Failed to index note: {response.OriginalException.Message}");
        }
    }

    public async Task<int> CountWordsInNotes(int patientId, HashSet<string> wordsToCount)
    {
        _logger.LogInformation("CountWordsInNotes called");
        var response = await _elasticsearchClient.SearchAsync<NoteRiskInfo>(s => s
                                                                                 .Query(q => q
                                                                                            .Bool(b => b
                                                                                                      .Must(
                                                                                                            m => m.Term(t => t.Field(f => f.PatientId).Value(patientId)),
                                                                                                            m => m.Match(mt => mt
                                                                                                                               .Field(f => f.Body)
                                                                                                                               .Query(string.Join(" ", wordsToCount))
                                                                                                                               .Analyzer("custom_french_analyzer")
                                                                                                                        )
                                                                                                           )
                                                                                                 )
                                                                                       )
                                                                                 .Aggregations(a => a
                                                                                                   .Terms("word_counts", t => t
                                                                                                                              .Field(f => f.Body.Suffix("keyword"))
                                                                                                                              .Size(10000)
                                                                                                         )
                                                                                              )
                                                                           );

        if (!response.IsValid)
        {
            throw new Exception($"Failed to search notes: {response.OriginalException.Message}");
        }

        // Extract word count from aggregation
        var wordCounts = response.Aggregations.Terms("word_counts").Buckets
                                 .Sum(b => (int)b.DocCount);

        _logger.LogInformation($"Word count is : {wordCounts}");
        return wordCounts;
    }
}