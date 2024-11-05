using BackendNote.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
namespace BackendNote.Services;

public class NotesService
{
    public readonly IMongoCollection<Note> _notesCollection;


    public NotesService(
        IOptions<NoteDatabaseSettings> noteDatabaseSettings)
    {
        MongoClient mongoClient = new(
                                      noteDatabaseSettings.Value.ConnectionString);

        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(
                                                               noteDatabaseSettings.Value.DatabaseName);

        _notesCollection = mongoDatabase.GetCollection<Note>(
                                                             noteDatabaseSettings.Value.NotesCollectionName);


        CreateIndexes();
    }

    public async Task<List<Note>> GetAsync() =>
        await _notesCollection.Find(_ => true).ToListAsync();

    public async Task<Note?> GetAsync(string id) =>
        await _notesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<List<Note>?> GetFromPatientIdAsync(int PatientId) =>
        await _notesCollection.Find(x => x.PatientId == PatientId).ToListAsync();

    public async Task CreateAsync(Note newNote)
    {
        await _notesCollection.InsertOneAsync(newNote);
    }

    public async Task UpdateAsync(string id, Note updatedNote)
    {
        FilterDefinition<Note> filter = Builders<Note>.Filter.Eq(field: note => note.Id, id);
        UpdateDefinition<Note> update = Builders<Note>.Update
                                                      .Set(field: note => note.Title, updatedNote.Title)
                                                      .Set(field: note => note.Body, updatedNote.Body)
                                                      .Set(field: note => note.LastUpdatedDate, updatedNote.LastUpdatedDate);
        await _notesCollection.UpdateOneAsync(filter, update);
    }

    public async Task RemoveAsync(string id) =>
        await _notesCollection.DeleteOneAsync(x => x.Id == id);

    private void CreateIndexes()
    {
        IndexKeysDefinition<Note> indexKeys = Builders<Note>.IndexKeys.Text(note => note.Title).Text(note => note.Body);
        CreateIndexModel<Note> indexModel = new(indexKeys);
        _notesCollection.Indexes.CreateOne(indexModel);
    }
}