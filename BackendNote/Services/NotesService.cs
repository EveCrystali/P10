using BackendNote.Models;
using Elasticsearch.Net;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Nest;
namespace BackendNote.Services;

public class NotesService
{

    private readonly ElasticClient _elasticClient;

    private readonly ILogger<NotesService> _logger;
    public readonly IMongoCollection<Note> NotesCollection;


    public NotesService(
        IOptions<NoteDatabaseSettings> noteDatabaseSettings, ILogger<NotesService> logger)
    {
        MongoClient mongoClient = new(
                                      noteDatabaseSettings.Value.ConnectionString);

        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(
                                                               noteDatabaseSettings.Value.DatabaseName);

        NotesCollection = mongoDatabase.GetCollection<Note>(
                                                            noteDatabaseSettings.Value.NotesCollectionName);

        CreateIndexes();

        ConnectionSettings settings = new ConnectionSettings(new Uri("http://elasticsearch:9200"))
                                      .DefaultIndex("notes_index")
                                      .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
                                      .DisablePing()
                                      .DisableDirectStreaming()
                                      .ThrowExceptions()
                                      .DefaultFieldNameInferrer(p => p);

        _elasticClient = new ElasticClient(settings);

        _logger = logger;

        _logger.LogInformation("ElasticsearchService initialized successfully.");
    }
    
    public async Task<Note?> GetAsync(string id) =>
        await NotesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<List<Note>?> GetFromPatientIdAsync(int patientId) =>
        await NotesCollection.Find(x => x.PatientId == patientId).ToListAsync();

    public async Task CreateAsync(Note newNote)
    {
        await NotesCollection.InsertOneAsync(newNote);
        await CreateAsyncElasticsearch(newNote);
    }

    public async Task UpdateAsync(string id, Note updatedNote)
    {
        _logger.LogInformation("Updating note with ID {Id}", id);

        // Get existing note to preserve PatientId
        Note existingNote = await GetAsync(id) ?? throw new InvalidOperationException($"Note with ID {id} not found");

        // Preserve PatientId

        updatedNote.PatientId = existingNote.PatientId;
        updatedNote.Id = existingNote.Id;

        FilterDefinition<Note> filter = Builders<Note>.Filter.Eq(field: note => note.Id, id);
        UpdateDefinition<Note> update = Builders<Note>.Update
                                                  .Set(field: note => note.Title, updatedNote.Title)
                                                  .Set(field: note => note.Body, updatedNote.Body)
                                                  .Set(field: note => note.LastUpdatedDate, updatedNote.LastUpdatedDate);
        UpdateResult? updateResult = await NotesCollection.UpdateOneAsync(filter, update);

        _logger.LogInformation("MongoDB update result: {MatchedCount} matched, {ModifiedCount} modified", updateResult.MatchedCount, updateResult.ModifiedCount);

        await RemoveAsyncElasticsearch(id);
        await CreateAsyncElasticsearch(updatedNote);
    }

    public async Task RemoveAsync(Note note)
    {
        _logger.LogInformation("Deleting note with ID {Id} from MongoDB and Elasticsearch", note.Id);
        if (note.Id != null)
        {
            await RemoveAsyncElasticsearch(note.Id);
        }
        _logger.LogInformation("Note with ID {Id} deleted successfully", note.Id);
    }

    private async Task RemoveAsyncElasticsearch(string id)
    {
        _logger.LogInformation("Deleting note with ID {Id} from Elasticsearch", id);
        DeleteResponse? deleteResponse = await _elasticClient.DeleteAsync<Note>(id);
        if (!deleteResponse.IsValid)
        {
            string errorMessage = $"Failed to delete document with ID {id} from Elasticsearch. Reason: {deleteResponse.ServerError?.Error.Reason}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException("Error deleting document from Elasticsearch" + errorMessage);
        }
        _logger.LogInformation("Note with ID {Id} deleted successfully in Elasticsearch", id);
    }

    private async Task CreateAsyncElasticsearch(Note note)
    {
        _logger.LogInformation("Creating note with ID {Id} in Elasticsearch", note.Id);
        CreateResponse? createResponse = await _elasticClient.CreateAsync(note, x => x.Index("notes_index"));
        if (!createResponse.IsValid)
        {
            string errorMessage = $"Failed to create document with ID {note.Id} in Elasticsearch. Reason: {createResponse.ServerError?.Error.Reason}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException("Error creating document in Elasticsearch" + errorMessage);
        }
        _logger.LogInformation("Note with ID {Id} created successfully in Elasticsearch", note.Id);
    }

    private void CreateIndexes()
    {
        IndexKeysDefinition<Note> indexKeys = Builders<Note>.IndexKeys.Text(note => note.Title).Text(note => note.Body);
        CreateIndexModel<Note> indexModel = new(indexKeys);
        NotesCollection.Indexes.CreateOne(indexModel);
    }
}