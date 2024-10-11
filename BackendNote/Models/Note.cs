using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SharedLibrary.Extensions;

namespace BackendNote.Models;

public class Note : IValidatable
{
    [BsonId]
    [BsonRequired]
    [BsonRepresentation(BsonType.ObjectId)]
    [MaxLength(36, ErrorMessage = "Id cannot be more than 36 characters")]
    public required string Id { get; set; }

    [BsonRequired]
    [BsonRepresentation(BsonType.ObjectId)]
    [MaxLength(36, ErrorMessage = "Title cannot be more than 36 characters")]
    public required string UserId { get; set; }

    [BsonRequired]
    [BsonRepresentation(BsonType.ObjectId)]
    [MaxLength(36, ErrorMessage = "PatientId cannot be more than 36 characters")]
    public required string PatientId { get; set; }

    [BsonRequired]
    [DataType(DataType.DateTime)]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime CreatedDate { get; set; }

    [BsonRequired]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime? LastUpdatedDate { get; set; }

    [BsonRequired]
    [MaxLength(256, ErrorMessage = "Title cannot be more than 256 characters")]
    public string? Title { get; set; }
    
    public string? Body { get; set; }

    public void Validate()
    {
        ValidationExtensions.Validate(this);
    }
}
