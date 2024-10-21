using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class ElasticsearchService
{
    private readonly HttpClient _httpClient;
    private readonly string _elasticsearchUrl = "http://localhost:7203"; 

    public ElasticsearchService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<long> CountWordsInNotes(string patientId, List<string> wordsToCount)
    {
        var queryWords = string.Join(" ", wordsToCount);

        var requestBody = new
        {
            query = new
            {
                @bool = new
                {
                    must = new object[]
                    {
                        new { term = new { PatientId = patientId } },
                        new { match = new { Body = new { query = queryWords, analyzer = "custom_french_analyzer" } } }
                    }
                }
            },
            aggs = new
            {
                word_counts = new
                {
                    terms = new
                    {
                        field = "Body.keyword",
                        size = 10000
                    }
                }
            }
        };

        var response = await _httpClient.PostAsync(
            $"{_elasticsearchUrl}/notes_index/_search",
            new StringContent(JObject.FromObject(requestBody).ToString(), Encoding.UTF8, "application/json")
        );

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var jsonResponse = JObject.Parse(responseBody);

        var wordCounts = jsonResponse["aggregations"]["word_counts"]["buckets"]
            .Sum(b => (long)b["doc_count"]);

        return wordCounts;
    }
}
