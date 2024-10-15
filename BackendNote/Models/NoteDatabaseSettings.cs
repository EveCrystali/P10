using System;

namespace BackendNote.Models;

public class NoteDatabaseSettings
{
    public string ConnectionString { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;

    public string NotesCollectionName { get; set; } = null!;
}
