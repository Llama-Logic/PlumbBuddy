namespace PlumbBuddy.Services;

public record PersonalNotesRecord(FileInfo File, string ModsFolderPath, DateTimeOffset LastWrite, string? ManifestedName, string? Notes, DateTimeOffset? PersonalDate);
