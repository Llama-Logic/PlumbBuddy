namespace PlumbBuddy.Services;

public record PersonalNotesRecord(FileInfo File, string FolderPath, string FileName, string? ManifestedName, string? Notes, DateTimeOffset? PersonalDate);
