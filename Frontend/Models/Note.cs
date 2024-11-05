using System.ComponentModel.DataAnnotations;
namespace Frontend.Models;

public class Note
{
    [MaxLength(36, ErrorMessage = "Id cannot be more than 36 characters")]
    public string? Id { get; set; }

    public string? Creator { get; set; }

    public required int PatientId { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? CreatedDate { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? LastUpdatedDate { get; set; }

    [MaxLength(256, ErrorMessage = "Title cannot be more than 256 characters")]
    public string? Title { get; set; }

    public string? Body { get; set; }
}