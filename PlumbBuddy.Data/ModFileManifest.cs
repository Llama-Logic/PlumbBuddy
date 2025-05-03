namespace PlumbBuddy.Data;

#pragma warning disable IDE0079

[SuppressMessage("Naming", "CA1724: Type names should not match namespaces")]
public class ModFileManifest(ModFileHash modFileHash, ModFileManifestHash inscribedModFileManifestHash, ModFileManifestHash calculatedModFileManifestHash)
{
    ModFileManifest() :
        this(null!, null!, null!)
    {
    }

    [Key]
    public long Id { get; set; }

    public long ModFileHashId { get; set; }

    [ForeignKey(nameof(ModFileHashId))]
    public ModFileHash ModFileHash { get; set; } = modFileHash;

    [Required]
    public required string Name { get; set; }

    public string? Description { get; set; }

    public ICollection<ModCreator> Creators { get; } = [];

    public string? Version { get; set; }

    public Uri? Url { get; set; }

    public long InscribedModFileManifestHashId { get; set; }

    [ForeignKey(nameof(InscribedModFileManifestHashId))]
    public ModFileManifestHash InscribedModFileManifestHash { get; set; } = inscribedModFileManifestHash;

    public ICollection<ModFileManifestResourceKey> HashResourceKeys { get; } = [];

    public long CalculatedModFileManifestHashId { get; set; }

    [ForeignKey(nameof(CalculatedModFileManifestHashId))]
    public ModFileManifestHash CalculatedModFileManifestHash { get; set; } = calculatedModFileManifestHash;

    public ICollection<ModFileManifestHash> SubsumedHashes { get; } = [];

    public ICollection<ModFeature> Features { get; } = [];

    public ICollection<ModExclusivity> Exclusivities { get; } = [];

    public ICollection<PackCode> RequiredPacks { get; } = [];

    public long? ElectronicArtsPromoCodeId { get; set; }

    [ForeignKey(nameof(ElectronicArtsPromoCodeId))]
    public ElectronicArtsPromoCode? ElectronicArtsPromoCode { get; set; }

    public ICollection<PackCode> IncompatiblePacks { get; } = [];

    public ICollection<RequiredMod> RequiredMods { get; } = [];

    public string? TuningName { get; set; }

    /// <summary>
    /// This value is bit-identical with the <see cref="ulong"/> full instance, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="long"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public long? TuningFullInstance { get; set; }

    [NotMapped]
    public ResourceKey? Key
    {
        get =>
            KeyType is { } keyType && KeyGroup is { } keyGroup && KeyFullInstance is { } keyFullInstance
                ? new((ResourceType)unchecked((uint)keyType), unchecked((uint)keyGroup), unchecked((ulong)keyFullInstance))
                : null;
        set
        {
            if (value is { } key)
            {
                KeyType = unchecked((int)(uint)key.Type);
                KeyGroup = unchecked((int)key.Group);
                KeyFullInstance = unchecked((long)key.FullInstance);
            }
            else
            {
                KeyType = null;
                KeyGroup = null;
                KeyFullInstance = null;
            }
        }
    }

    /// <summary>
    /// This value is bit-identical with the <see cref="ResourceType"/> (<see cref="uint"/>) type, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="int"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public int? KeyType { get; set; }

    /// <summary>
    /// This value is bit-identical with the <see cref="uint"/> group, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="int"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public int? KeyGroup { get; set; }

    /// <summary>
    /// This value is bit-identical with the <see cref="ulong"/> full instance, but since it must be signed in storage, may be negative.
    /// Do not forget to perform <see langword="unchecked"/> conversions to <see cref="long"/> of any literal values you intend to use in relation to this column in queries.
    /// </summary>
    public long? KeyFullInstance { get; set; }

    public string? ContactEmail { get; set; }

    public Uri? ContactUrl { get; set; }

    public string? MessageToTranslators { get; set; }

    public Uri? TranslationSubmissionUrl { get; set; }

    public ICollection<ModFileManifestRepurposedLanguage> RepurposedLanguages { get; } = [];

    public ICollection<ModFileManifestTranslator> Translators { get; } = [];

    /// <summary>
    /// Convert this entity to its LLP model equivalent
    /// </summary>
    public ModFileManifestModel ToModel()
    {
        var model = new ModFileManifestModel
        {
            ContactEmail = ContactEmail,
            ContactUrl = ContactUrl,
            Description = Description,
            ElectronicArtsPromoCode = ElectronicArtsPromoCode?.Code,
            Hash = (InscribedModFileManifestHash?.Sha256 ?? Enumerable.Empty<byte>()).ToImmutableArray(),
            MessageToTranslators = MessageToTranslators,
            Name = Name,
            TuningFullInstance = TuningFullInstance is null ? 0UL : unchecked((ulong)TuningFullInstance),
            TuningName = TuningName,
            Url = Url,
            Version = Version
        };
        static void addCollectionElements<TElement, TEntity>(ICollection<TEntity>? maybeNullEntityCollection, Collection<TElement> elementCollection, Func<TEntity, TElement> elementSelector)
        {
            if (maybeNullEntityCollection is { } entityCollection && entityCollection.Count is > 0)
                foreach (var entity in entityCollection)
                    elementCollection.Add(elementSelector(entity));
        }
        static void addHashSetElements<TElement, TEntity>(ICollection<TEntity>? maybeNullEntityCollection, HashSet<TElement> elementHashSet, Func<TEntity, TElement> elementSelector)
        {
            if (maybeNullEntityCollection is { } entityCollection && entityCollection.Count is > 0)
                foreach (var entity in entityCollection)
                    elementHashSet.Add(elementSelector(entity));
        }
        addCollectionElements(Creators, model.Creators, entity => entity.Name);
        addCollectionElements(Exclusivities, model.Exclusivities, entity => entity.Name);
        addCollectionElements(Features, model.Features, entity => entity.Name);
        addHashSetElements(HashResourceKeys, model.HashResourceKeys, entity => new ResourceKey(unchecked((ResourceType)(uint)entity.KeyType), unchecked((uint)entity.KeyGroup), unchecked((ulong)entity.KeyFullInstance)));
        addCollectionElements(IncompatiblePacks, model.IncompatiblePacks, entity => entity.Code);
        addCollectionElements(RepurposedLanguages, model.RepurposedLanguages, entity => new ModFileManifestModelRepurposedLanguage
        {
            ActualLocale = entity.ActualLocale,
            GameLocale = entity.GameLocale
        });
        addCollectionElements(RequiredMods, model.RequiredMods, entity =>
        {
            var requiredMod = new ModFileManifestModelRequiredMod
            {
                IgnoreIfHashAvailable = (entity.IgnoreIfHashAvailable?.Sha256 ?? Enumerable.Empty<byte>()).ToImmutableArray(),
                IgnoreIfHashUnavailable = (entity.IgnoreIfHashUnavailable?.Sha256 ?? Enumerable.Empty<byte>()).ToImmutableArray(),
                IgnoreIfPackAvailable = entity.IgnoreIfPackAvailable?.Code,
                IgnoreIfPackUnavailable = entity.IgnoreIfPackUnavailable?.Code,
                Name = entity.Name,
                RequirementIdentifier = entity.RequirementIdentifier?.Identifier,
                Url = entity.Url,
                Version = entity.Version,
            };
            addCollectionElements(entity.Creators, requiredMod.Creators, entity => entity.Name);
            addHashSetElements(entity.Hashes, requiredMod.Hashes, entity => [.. entity.Sha256]);
            addCollectionElements(entity.RequiredFeatures, requiredMod.RequiredFeatures, entity => entity.Name);
            return requiredMod;
        });
        addCollectionElements(RequiredPacks, model.RequiredPacks, entity => entity.Code);
        addHashSetElements(SubsumedHashes, model.SubsumedHashes, entity => [..entity.Sha256]);
        addCollectionElements(Translators, model.Translators, entity => new ModFileManifestModelTranslator
        {
            Language = entity.Language,
            Name = entity.Name
        });
        return model;
    }
}
