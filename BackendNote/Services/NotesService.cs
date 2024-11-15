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

    public async Task<List<Note>> GetAsync() =>
        await NotesCollection.Find(_ => true).ToListAsync();

    public async Task<Note?> GetAsync(string id) =>
        await NotesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<List<Note>?> GetFromPatientIdAsync(int patientId) =>
        await NotesCollection.Find(x => x.PatientId == patientId).ToListAsync();

    public async Task CreateAsync(Note newNote)
    {
        await NotesCollection.InsertOneAsync(newNote);
        await _elasticClient.IndexDocumentAsync(newNote);
    }

    public async Task UpdateAsync(string id, Note updatedNote)
    {
        _logger.LogInformation("Updating note with ID {id}", id);

        FilterDefinition<Note> filter = Builders<Note>.Filter.Eq(field: note => note.Id, id);
        UpdateDefinition<Note> update = Builders<Note>.Update
                                                      .Set(field: note => note.Title, updatedNote.Title)
                                                      .Set(field: note => note.Body, updatedNote.Body)
                                                      .Set(field: note => note.LastUpdatedDate, updatedNote.LastUpdatedDate);
        var updateResult = await NotesCollection.UpdateOneAsync(filter, update);

        _logger.LogInformation("MongoDB update result: {matchedCount} matched, {modifiedCount} modified", updateResult.MatchedCount, updateResult.ModifiedCount);

        var updateResponse = await _elasticClient.UpdateAsync<Note, Note>(id, u => u
                                                                                   .Doc(updatedNote)
                                                                                   .Index("notes_index"));
        if (updateResponse.IsValid)
        {
            _logger.LogInformation("Successfully updated document in Elasticsearch with ID {id}", id);
        }
        else
        {
            string errorMessage = $"Failed to update document in Elasticsearch. Reason: {updateResponse.ServerError?.Error.Reason}";
            _logger.LogError(errorMessage);
            throw new Exception(errorMessage);
        }
    }

    public async Task RemoveAsync(Note note)
    {
        _logger.LogInformation("Deleting note with ID {id} from MongoDB and Elasticsearch", note.Id);

        await NotesCollection.DeleteOneAsync(x => x.Id == note.Id);
        var deleteResponse = await _elasticClient.DeleteAsync<Note>(note.Id);

        if (!deleteResponse.IsValid)
        {
            string errorMessage = $"Failed to delete document with ID {note.Id} from Elasticsearch. Reason: {deleteResponse.ServerError?.Error.Reason}";
            _logger.LogError(errorMessage);
            throw new Exception(errorMessage);
        }

        _logger.LogInformation("Note with ID {id} deleted successfully", note.Id);
    }

    private void CreateIndexes()
    {
        IndexKeysDefinition<Note> indexKeys = Builders<Note>.IndexKeys.Text(note => note.Title).Text(note => note.Body);
        CreateIndexModel<Note> indexModel = new(indexKeys);
        NotesCollection.Indexes.CreateOne(indexModel);
    }
}