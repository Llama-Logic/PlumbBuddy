namespace PlumbBuddy.Services;

public record PersonalNotesRecord(FileInfo File, string FolderPath, string FileName, DateTimeOffset LastWrite, string? ManifestedName, string? Notes, DateTimeOffset? PersonalDate);
