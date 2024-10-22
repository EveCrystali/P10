using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BackendDiabetesRiskPrediction.Models;

namespace BackendDiabetesRiskPrediction.Services;

public class ElasticsearchService
{
    private readonly HttpClient _httpClient;
    private readonly string _elasticsearchUrl = "http://localhost:7203";

    public ElasticsearchService()
    {
        _httpClient = new HttpClient();
    }

    public async Task IndexNoteAsync(NoteRiskInfo note)
    {
        var requestBody = new
        {
            index = "notes",
            id = note.Id,
            document = new
            {
                note.Title,
                note.Body,
                note.PatientId
            }
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_elasticsearchUrl}/notes/_doc/{note.Id}", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task<int> CountWordsInNotes(string patientId, List<string> wordsToCount)
    {
        // FUTURE: Add null check conditions

        // Make a string of all the words to count
        string? queryWords = string.Join(" ", wordsToCount);
        // Initialize the word count
        int wordCounts = 0;

        // Build the query
        var requestBody = new
        {
            query = new
            {
                // Needed to combine several condition
                @bool = new
                {
                    // conditions must all be true
                    must = new object[]
                    {
                        // Condition 1: search in the patient id
                        new { term = new { PatientId = patientId } },
                        // Condition 2: search in the body
                        // ? What is exactly custom_french_analyzer ? It refers to create_index.json and custom_analyzer.json
                        new { match = new { Body = new { query = queryWords, analyzer = "custom_french_analyzer" } } }
                    }
                }
            },
            // Aggregate to count word occurrences
            aggs = new
            {
                // name of the aggregation
                word_counts = new
                {
                    // Term type aggregation
                    terms = new
                    {
                        // name of the field
                        field = "Body.keyword",
                        // size of the aggregation : the maximum of terms that is returned
                        size = 10000
                    }
                }
            }
        };

        HttpResponseMessage response = await _httpClient.PostAsync($"{_elasticsearchUrl}/notes_index/_search",
                new StringContent(JObject.FromObject(requestBody).ToString(), Encoding.UTF8, "application/json"));

        response.EnsureSuccessStatusCode();

        string? responseBody = await response.Content.ReadAsStringAsync();
        JObject? jsonResponse = JObject.Parse(responseBody);

        wordCounts = jsonResponse["aggregations"]["word_counts"]["buckets"]
            .Sum(b => (int)b["doc_count"]);

        return wordCounts;
    }
}