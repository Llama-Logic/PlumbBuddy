namespace PlumbBuddy.Data;

public enum ModsDirectoryFileType :
    int
{
    Ignored = 0,
    Package,
    ScriptArchive,
    TextFile,
    HtmlFile,
    ZipArchive,
    RarArchive,
    SevenZipArchive,
    CorruptPackage,
    CorruptScriptArchive
}
