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
                                      .ThrowExceptions()
                                      .EnableDebugMode()
                                      .PrettyJson()
                                      .OnRequestCompleted(response =>
                                      {
                                          Console.WriteLine($"Request: {response.DebugInformation}");
                                      });


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

    public async Task<int> CountUniqueWordsInNotes(int patientId, HashSet<string> wordsToCount)
    {
        _logger.LogInformation("CountWordsInNotes called");

        var response = await _elasticsearchClient.SearchAsync<NoteRiskInfo>(s => s
            .Query(q => q
                .Bool(b => b
                    .Must(m => m.Term("PatientId", patientId)) // Utilisation correcte du champ
                    .Should(wordsToCount.Select(word => (Func<QueryContainerDescriptor<NoteRiskInfo>, QueryContainer>)(m => m.Match(mt => mt
                        .Field("Body")
                        .Query(word)
                        .Analyzer("custom_french_analyzer"))))
                        .ToArray())
                    .MinimumShouldMatch(1)
                )
            )
            .Aggregations(a => a
                .Terms("unique_word_counts", t => t
                    .Field("Body")
                    .Size(10000)
                )
            )
        );

        if (!response.IsValid)
        {
            throw new Exception($"Failed to search notes: {response.OriginalException.Message}");
        }

        // Extraire les résultats d'agrégation
        var uniqueWordsFound = new HashSet<string>();
        var termsAgg = response.Aggregations.Terms("unique_word_counts");
        if (termsAgg != null)
        {
            foreach (var bucket in termsAgg.Buckets)
            {
                uniqueWordsFound.Add(bucket.Key.ToString().ToLowerInvariant());
            }
        }

        _logger.LogInformation($"Unique word count is : {uniqueWordsFound.Count}");
        return uniqueWordsFound.Count;
    }

}