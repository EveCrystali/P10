using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BackendNote.Models;

public class Note
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string PatientId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastUpdatedDate { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
}
