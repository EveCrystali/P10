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
    
    public async Task<int> CountUniqueWordsInNotes(int patientId, HashSet<string> wordsToCount)
    {
        _logger.LogInformation("CountUniqueWordsInNotes called with patientId: {PatientId}", patientId);

        HashSet<string> analyzedTriggerWords = await AnalyzeWords(wordsToCount);
        ISearchResponse<NoteRiskInfo> patientNotes = await GetPatientNotes(patientId);
        return await CountUniqueWordsInPatientNotes(patientNotes, analyzedTriggerWords);
    }

    private async Task<HashSet<string>> AnalyzeWords(HashSet<string> wordsToCount)
    {
        HashSet<string> analyzedWords = new();
        _logger.LogInformation("Analyzing words: {WordsToCount}", string.Join(", ", wordsToCount));

        foreach (string word in wordsToCount)
        {
            IEnumerable<string>? analyzedTokens = await AnalyzeText(word);
            if (analyzedTokens != null)
            {
                analyzedWords.UnionWith(analyzedTokens);
            }
        }

        _logger.LogInformation("Analyzed words are: {Words}", string.Join(", ", analyzedWords));
        return analyzedWords;
    }

    private async Task<IEnumerable<string>?> AnalyzeText(string text)
    {
        AnalyzeResponse analyzeResponse = await _elasticsearchClient.Indices.AnalyzeAsync(a => a
            .Index("notes_index")
            .Analyzer("custom_french_analyzer")
            .Text(text));

        if (!analyzeResponse.IsValid)
        {
            _logger.LogError("Failed to analyze text: {Text}. Reason: {Reason}", text, analyzeResponse.OriginalException.Message);
            return null;
        }

        return analyzeResponse.Tokens.Select(token => token.Token);
    }

    private async Task<ISearchResponse<NoteRiskInfo>> GetPatientNotes(int patientId)
    {
        _logger.LogInformation("Executing search query for patientId: {PatientId}", patientId);

        ISearchResponse<NoteRiskInfo> response = await _elasticsearchClient.SearchAsync<NoteRiskInfo>(s => s
            .Index("notes_index")
            .Query(q => q
                .Term(t => t.Field("PatientId").Value(patientId))
            )
            .Source(src => src
                .Includes(i => i.Field("Body"))
            )
            .Size(1000)
        );

        ValidateSearchResponse(response, patientId);
        return response;
    }

    private void ValidateSearchResponse(ISearchResponse<NoteRiskInfo> response, int patientId)
    {
        if (!response.IsValid)
        {
            _logger.LogError("Search query failed. Reason: {Reason}", response.OriginalException.Message);
            throw new InvalidOperationException($"Failed to search notes: {response.OriginalException.Message}");
        }

        if (response.HitsMetadata?.Total.Value == 0)
        {
            _logger.LogWarning("No notes found for PatientId: {PatientId}", patientId);
            return;
        }

        _logger.LogInformation("Found {Total} notes for PatientId: {PatientId}", response.HitsMetadata?.Total.Value, patientId);
    }

    private async Task<int> CountUniqueWordsInPatientNotes(ISearchResponse<NoteRiskInfo> response, HashSet<string> analyzedWords)
    {
        HashSet<string> uniqueWordsInNotes = new();

        if (response.Hits != null)
        {
            foreach (IHit<NoteRiskInfo> hit in response.Hits)
            {
                if (string.IsNullOrEmpty(hit.Source.Body)) continue;

                IEnumerable<string>? bodyTokens = await AnalyzeText(hit.Source.Body);
                if (bodyTokens == null) continue;

                List<string> commonWords = bodyTokens.Intersect(analyzedWords).ToList();
                _logger.LogInformation("Common words found in note {Id}: {CommonWords}", hit.Id, string.Join(", ", commonWords));

                uniqueWordsInNotes.UnionWith(commonWords);
            }
        }
        _logger.LogInformation("Unique words in notes are: {UniqueWordsInNotes}", string.Join(", ", uniqueWordsInNotes));

        int uniqueWordCount = uniqueWordsInNotes.Count;
        _logger.LogInformation("Unique word count is: {UniqueWordCount}", uniqueWordCount);

        return uniqueWordCount;
    }
}