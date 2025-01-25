namespace PlumbBuddy.Services;

public record CatalogModValue(ModFileManifestModel Manifest, IReadOnlyList<FileInfo> Files, IReadOnlyList<CatalogModKey> Dependencies, IReadOnlyList<CatalogModKey> Dependents);
