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
                                      .DefaultFieldNameInferrer(p => p)
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
        IndexResponse response = await _elasticsearchClient.IndexDocumentAsync(note);

        if (!response.IsValid)
        {
            throw new Exception($"Failed to index note: {response.OriginalException.Message}");
        }
    }

    public async Task<int> CountUniqueWordsInNotes(int patientId, HashSet<string> wordsToCount)
    {
        _logger.LogInformation("CountUniqueWordsInNotes called with patientId: {patientId}", patientId);

        // Step 1: Analyze the trigger words using the same analyzer as the `Body` field
        HashSet<string> analyzedWords = new HashSet<string>();

        _logger.LogInformation("Analyzing words: {wordsToCount}", string.Join(", ", wordsToCount));

        foreach (string word in wordsToCount)
        {
            AnalyzeResponse analyzeResponse = await _elasticsearchClient.Indices.AnalyzeAsync(a => a
                .Index("notes_index")
                .Analyzer("custom_french_analyzer")
                .Text(word)
            );

            if (!analyzeResponse.IsValid)
            {
                _logger.LogError("Failed to analyze word: {word}. Reason: {reason}", word, analyzeResponse.OriginalException.Message);
                continue;
            }

            analyzedWords.UnionWith(analyzeResponse.Tokens.Select(token => token.Token));
        }

        _logger.LogInformation("Analyzed words are: {words}", string.Join(", ", analyzedWords));

        // Step 2: Query for documents matching PatientId
        _logger.LogInformation("Executing search query for patientId: {patientId}", patientId);

        ISearchResponse<NoteRiskInfo> response = await _elasticsearchClient.SearchAsync<NoteRiskInfo>(s => s
            .Index("notes_index")
            .Query(q => q
                .Term(t => t.Field("PatientId").Value(patientId))
            )
            .Source(src => src
                .Includes(i => i.Field("Body"))
            )
            .Size(1000) // Adjust size as needed
        );

        if (!response.IsValid)
        {
            _logger.LogError("Search query failed. Reason: {reason}", response.OriginalException.Message);
            throw new Exception($"Failed to search notes: {response.OriginalException.Message}");
        }

        if (response.HitsMetadata?.Total.Value == 0)
        {
            _logger.LogWarning("No notes found for PatientId: {patientId}", patientId);
            return 0;
        }

        _logger.LogInformation("Found {total} notes for PatientId: {patientId}", response.HitsMetadata.Total.Value, patientId);

        foreach (IHit<NoteRiskInfo> hit in response.Hits)
        {
            _logger.LogDebug("Note found: {note}", hit.Source.Body);
        }

        _logger.LogInformation("Search query executed successfully.");

        // Step 3: Analyze the Body text of each document and count unique matching words
        HashSet<string> uniqueWordsInNotes = new HashSet<string>();

        foreach (IHit<NoteRiskInfo>? hit in response.Hits)
        {
            if (!string.IsNullOrEmpty(hit.Source.Body))
            {
                AnalyzeResponse analyzeBodyResponse = await _elasticsearchClient.Indices.AnalyzeAsync(a => a
                .Index("notes_index")
                .Analyzer("custom_french_analyzer")
                .Text(hit.Source.Body)
            );

                if (!analyzeBodyResponse.IsValid)
                {
                    _logger.LogError("Failed to analyze Body for document {id}. Reason: {reason}", hit.Id, analyzeBodyResponse.OriginalException.Message);
                    continue;
                }

                IEnumerable<string> bodyTokens = analyzeBodyResponse.Tokens.Select(token => token.Token);

                uniqueWordsInNotes.UnionWith(bodyTokens.Intersect(analyzedWords));
            }
        }

        int uniqueWordCount = uniqueWordsInNotes.Count;

        _logger.LogInformation("Unique word count is: {uniqueWordCount}", uniqueWordCount);

        _logger.LogInformation("Unique words found: {uniqueWords}", string.Join(", ", uniqueWordsInNotes));

        return uniqueWordCount;
    }

    // if (response.HitsMetadata?.Total.Value == 0)
    // {
    //     _logger.LogWarning("NO PATIENTS FOUND WITH PATIENTID: {patientId}", patientId);
    // }

    // if (response.IsValid)
    // {
    //     _logger.LogInformation("Search query executed successfully.");
    // }
    // else
    // {
    //     _logger.LogError("Search query failed. Reason: {reason}", response.OriginalException.Message);
    // }

    // if (!response.IsValid)
    // {
    //     throw new Exception($"Failed to search notes: {response.OriginalException.Message}");
    // }

    // // Étape 3 : Extraire les termes uniques de l'agrégation
    // TermsAggregate<string> termsAgg = response.Aggregations.Terms("unique_terms");

    // if (termsAgg != null)
    // {
    //     _logger.LogInformation($"Unique word count is : {termsAgg.Buckets.Count}");
    //     return termsAgg.Buckets.Count;
    // }
    // else
    // {
    //     _logger.LogInformation("Unique word count is : 0");
    //     return 0;
    // }
    // }


    // public async Task<int> CountUniqueWordsInNotes(int patientId, HashSet<string> wordsToCount)
    // {
    //     _logger.LogInformation("CountWordsInNotes called");

    //     var response = await _elasticsearchClient.SearchAsync<NoteRiskInfo>(s => s
    //         .Query(q => q
    //             .Bool(b => b
    //                 .Must(m => m.Term("PatientId", patientId)) // Utilisation correcte du champ
    //                 .Should(wordsToCount.Select(word => (Func<QueryContainerDescriptor<NoteRiskInfo>, QueryContainer>)(m => m.Match(mt => mt
    //                     .Field("Body")
    //                     .Query(word)
    //                     .Analyzer("custom_french_analyzer"))))
    //                     .ToArray())
    //                 .MinimumShouldMatch(1)
    //             )
    //         )
    //         .Aggregations(a => a
    //             .Terms("unique_word_counts", t => t
    //                 .Field("Body.keyword")
    //                 .Size(10000)
    //             )
    //         )
    //     );

    //     if (!response.IsValid)
    //     {
    //         throw new Exception($"Failed to search notes: {response.OriginalException.Message}");
    //     }

    //     // Extraire les résultats d'agrégation
    //     var uniqueWordsFound = new HashSet<string>();
    //     var termsAgg = response.Aggregations.Terms("unique_word_counts");
    //     if (termsAgg != null)
    //     {
    //         foreach (var bucket in termsAgg.Buckets)
    //         {
    //             uniqueWordsFound.Add(bucket.Key.ToString().ToLowerInvariant());
    //         }
    //     }

    //     _logger.LogInformation($"Unique word count is : {uniqueWordsFound.Count}");
    //     return uniqueWordsFound.Count;
    // }

}