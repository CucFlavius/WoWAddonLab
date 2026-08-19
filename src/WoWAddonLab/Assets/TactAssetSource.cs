using DBCD.Providers;
using WoWAddonLab.Configuration;
using WoWAddonLab.Emulator.Lua;
using System.Collections;
using System.Numerics;
using TACTSharp;

namespace WoWAddonLab.Assets;

public sealed class TactAssetSource : IWowModelResourceProvider
{
    private readonly WowInstallation _installation;
    private readonly Jenkins96 _hasher = new();
    private readonly Dictionary<string, uint> _resolved = new(StringComparer.Ordinal);
    private readonly Dictionary<uint, WowModelResourceMetadata?> _modelMetadata = [];
    private readonly Dictionary<uint, string?> _filenames = [];
    private readonly Dictionary<uint, WowModelAnimationPayloadState>
        _animationPayloadStates = [];
    private BuildInstance? _build;
    private string? _listfilePath;
    private TactDatabase? _database;
    private Dictionary<ushort, WowAnimationFallback>? _animationFallbacks;
    private Dictionary<int, WowAnimationKitDefinition>? _animationKits;
    private Dictionary<uint, WowSpellVisualKitDefinition>? _spellVisualKits;
    private Dictionary<uint, WowShadowyEffectDefinition>? _shadowyEffects;
    private Dictionary<uint, WowDissolveEffectDefinition>? _dissolveEffects;
    private Dictionary<uint, WowEdgeGlowEffectDefinition>? _edgeGlowEffects;

    public TactAssetSource(WowInstallation installation)
    {
        _installation = installation;
    }

    public bool IsInitialized => _build is not null;
    internal string CatalogCacheIdentity(string build)
    {
        var definitions = DefinitionsDirectory is null
            ? new Db2DefinitionState()
            : Db2DefinitionCache.LoadState(DefinitionsDirectory);
        var revision = definitions.DownloadedUtc?.UtcDateTime.Ticks ?? 0;
        return $"{_installation.Product.ProductCode}-{build}-dbd{definitions.SchemaVersion}-{revision}";
    }
    public bool SimulateUnresolvedModels => true;
    public string Description => $"{_installation.Product.UninstallName} {_installation.Version} ({_installation.Product.ProductCode})";
    public string? ListfilePath => _listfilePath;

    public string? DefinitionsDirectory { get; set; }

    internal TactDatabase Database =>
        _database ??= new TactDatabase(
            new TactDbcProvider(this),
            new FilesystemDBDProvider(
                DefinitionsDirectory ??
                throw new InvalidOperationException("The DB2 definitions directory was not set.")));

    public void Initialize(string cacheDirectory)
    {
        Directory.CreateDirectory(cacheDirectory);
        var build = new BuildInstance();
        build.Settings.BaseDir = _installation.RootPath;
        build.Settings.Product = _installation.Product.ProductCode.ToLowerInvariant();
        build.Settings.CacheDir = cacheDirectory;
        var (buildConfig, cdnConfig) = DiscoverConfigFiles(
            _installation.RootPath,
            _installation.Product.ProductCode);
        build.LoadConfigs(buildConfig, cdnConfig);
        build.Load();

        var listfilePath = Path.Combine(cacheDirectory, "community-listfile.csv");

        if (!File.Exists(listfilePath))
            new Listfile().Initialize(build.cdn, build.Settings, listfilePath);

        _build = build;
        _listfilePath = listfilePath;
    }

    public byte[]? Read(string? assetPath, uint? fileDataId)
    {
        if (_build is null)
            return null;

        var id = fileDataId ?? ResolveFileDataId(assetPath);
        if (id == 0 || _build.Root?.FileExists(id) != true)
            return null;
        return _build.OpenFileByFDID(id);
    }

    public bool FileExists(uint fileDataId) =>
        _build?.Root?.FileExists(fileDataId) == true;

    public bool TryGetFilename(uint fileDataId, out string filename)
    {
        lock (_filenames)
        {
            if (_filenames.TryGetValue(fileDataId, out var cached))
            {
                filename = cached ?? string.Empty;
                return cached is not null;
            }
        }

        var resolved = _listfilePath is { } path && File.Exists(path)
            ? FindFilenameInSortedListfile(path, fileDataId)
            : null;
        lock (_filenames)
            _filenames[fileDataId] = resolved;
        filename = resolved ?? string.Empty;
        return resolved is not null;
    }

    public WowModelAnimationPayloadState GetAnimationSequencePayloadState(
        uint modelFileDataId,
        uint animationFileDataId)
    {
        lock (_animationPayloadStates)
        {
            if (_animationPayloadStates.TryGetValue(animationFileDataId, out var cached))
                return cached;
        }

        var state = Read(animationFileDataId) is { Length: > 0 }
            ? WowModelAnimationPayloadState.Resident
            : WowModelAnimationPayloadState.Failed;
        lock (_animationPayloadStates)
            _animationPayloadStates[animationFileDataId] = state;
        return state;
    }

    public WowModelResourceMetadata? GetMetadata(uint fileDataId)
    {
        lock (_modelMetadata)
        {
            if (_modelMetadata.TryGetValue(fileDataId, out var cached))
                return cached;
        }

        var metadata = WowM2MetadataReader.Read(
            Read(fileDataId),
            sidecarFileDataId => Read(sidecarFileDataId));
        lock (_modelMetadata)
            _modelMetadata[fileDataId] = metadata;
        return metadata;
    }

    public bool TryGetAnimationFallback(
        ushort animationId,
        out WowAnimationFallback fallback)
    {
        EnsureAnimationFallbacks();
        return _animationFallbacks!.TryGetValue(animationId, out fallback);
    }

    public bool TryGetAnimationKit(
        int animationKitId,
        out WowAnimationKitDefinition animationKit)
    {
        EnsureAnimationKits();
        return _animationKits!.TryGetValue(animationKitId, out animationKit!);
    }

    public bool TryGetSpellVisualKit(
        uint spellVisualKitId,
        out WowSpellVisualKitDefinition spellVisualKit)
    {
        EnsureSpellVisualKits();
        return _spellVisualKits!.TryGetValue(spellVisualKitId, out spellVisualKit!);
    }

    public bool TryGetShadowyEffect(
        uint shadowyEffectId,
        out WowShadowyEffectDefinition shadowyEffect)
    {
        EnsureShadowyEffects();
        return _shadowyEffects!.TryGetValue(shadowyEffectId, out shadowyEffect);
    }

    public bool TryGetEdgeGlowEffect(
        uint edgeGlowEffectId,
        out WowEdgeGlowEffectDefinition edgeGlowEffect)
    {
        EnsureEdgeGlowEffects();
        return _edgeGlowEffects!.TryGetValue(edgeGlowEffectId, out edgeGlowEffect);
    }

    public bool TryGetDissolveEffect(
        uint dissolveEffectId,
        out WowDissolveEffectDefinition dissolveEffect)
    {
        EnsureDissolveEffects();
        return _dissolveEffects!.TryGetValue(dissolveEffectId, out dissolveEffect);
    }

    public byte[]? Read(uint fileDataId)
    {
        if (_build is null || !FileExists(fileDataId))
            return null;
        return _build.OpenFileByFDID(fileDataId);
    }

    public uint ResolveFileDataId(string? assetPath)
    {
        if (string.IsNullOrWhiteSpace(assetPath) || _build?.Root is not { } root)
            return 0;
        var normalized = assetPath.Replace('\\', '/').TrimStart('/').ToLowerInvariant();

        lock (_resolved)
        {
            if (_resolved.TryGetValue(normalized, out var cached))
                return cached;

            var fileDataId = ResolveExactFileDataId(root, normalized);
            if (fileDataId == 0)
            {
                var fallback = WowFileAssetPath.WithDefaultBlpExtension(normalized);
                if (!_resolved.TryGetValue(fallback, out fileDataId))
                {
                    fileDataId = ResolveExactFileDataId(root, fallback);
                    _resolved[fallback] = fileDataId;
                }
            }
            _resolved[normalized] = fileDataId;
            return fileDataId;
        }
    }

    private uint ResolveExactFileDataId(RootInstance root, string normalized)
    {
        if (Db2FileDataIds.TryGet(normalized, out var fileDataId))
            return fileDataId;
        var entries = root.GetEntriesByLookup(_hasher.ComputeHash(normalized));
        return entries.Count > 0 ? entries[0].fileDataID : 0;
    }

    internal static string? FindFilenameInSortedListfile(
        string path,
        uint targetFileDataId)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            FileOptions.RandomAccess);
        long low = 0;
        long high = stream.Length - 1;
        while (low <= high)
        {
            var midpoint = low + ((high - low) / 2);
            var lineStart = midpoint;
            while (lineStart > 0)
            {
                stream.Position = lineStart - 1;
                if (stream.ReadByte() == '\n')
                    break;
                lineStart--;
            }

            stream.Position = lineStart;
            if (lineStart >= stream.Length)
            {
                high = midpoint - 1;
                continue;
            }

            uint fileDataId = 0;
            var digitCount = 0;
            int current;
            while ((current = stream.ReadByte()) >= 0 && current != ';' && current != '\n')
            {
                if (current is < '0' or > '9')
                    break;
                fileDataId = checked((fileDataId * 10) + (uint)(current - '0'));
                digitCount++;
            }
            if (digitCount == 0 || current != ';')
                return null;

            if (fileDataId == targetFileDataId)
            {
                using var name = new MemoryStream();
                while ((current = stream.ReadByte()) >= 0 && current != '\r' && current != '\n')
                    name.WriteByte((byte)current);
                return System.Text.Encoding.UTF8.GetString(name.GetBuffer(), 0, (int)name.Length);
            }

            if (fileDataId < targetFileDataId)
            {
                while (current >= 0 && current != '\n')
                    current = stream.ReadByte();
                low = stream.Position;
            }
            else
                high = lineStart - 1;
        }
        return null;
    }

    private static (string BuildConfig, string CdnConfig) DiscoverConfigFiles(
        string installPath,
        string product)
    {
        var buildInfoPath = Path.Combine(installPath, ".build.info");
        if (!File.Exists(buildInfoPath))
            throw new FileNotFoundException($".build.info was not found at {buildInfoPath}.");

        var lines = File.ReadAllLines(buildInfoPath);
        if (lines.Length < 2)
            throw new InvalidDataException(".build.info has insufficient data.");
        var headers = lines[0].Split('|');
        var headerMap = headers
            .Select((value, index) => (Name: value.Split('!')[0], Index: index))
            .ToDictionary(value => value.Name, value => value.Index, StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines.Skip(1))
        {
            var values = line.Split('|');
            if (!headerMap.TryGetValue("Product", out var productIndex) ||
                productIndex >= values.Length ||
                !values[productIndex].Equals(product, StringComparison.OrdinalIgnoreCase))
                continue;

            var buildKey = values[headerMap["Build Key"]];
            var cdnKey = values[headerMap["CDN Key"]];
            return (
                ConfigPath(installPath, buildKey),
                ConfigPath(installPath, cdnKey));
        }

        throw new InvalidDataException($"Product '{product}' was not found in .build.info.");
    }

    private static string ConfigPath(string installPath, string key)
    {
        var path = Path.Combine(installPath, "Data", "config", key[..2], key.Substring(2, 2), key);
        if (!File.Exists(path))
            throw new FileNotFoundException($"TACT config was not found: {path}");
        return path;
    }

    private void EnsureAnimationFallbacks()
    {
        if (_animationFallbacks is not null)
            return;

        var fallbacks = new Dictionary<ushort, WowAnimationFallback>();
        if (_installation.Version is { Length: > 0 } build)
        {
            foreach (dynamic row in Database.Load("AnimationData", build).Values)
            {
                var id = UnsignedField(row, "ID");
                if (id > ushort.MaxValue)
                    continue;
                fallbacks[(ushort)id] = new WowAnimationFallback(
                    (ushort)id,
                    unchecked((ushort)UnsignedField(row, "Fallback")),
                    UnsignedField(row, "Flags"));
            }
        }
        _animationFallbacks = fallbacks;
    }

    private void EnsureAnimationKits()
    {
        if (_animationKits is not null)
            return;

        var kits = new Dictionary<int, WowAnimationKitDefinition>();
        if (_installation.Version is not { Length: > 0 } build)
        {
            _animationKits = kits;
            return;
        }

        var configFlags = Database.Load("AnimKitConfig", build).Values
            .ToDictionary(
                row => IntegerField(row, "ID"),
                row => UnsignedField(row, "ConfigFlags"));
        var animationKitBoneSets = Database.Load("AnimKitBoneSet", build).Values
            .ToDictionary(
                row => unchecked((byte)UnsignedField(row, "ID")),
                row => new
                {
                    BoneDataId = UnsignedField(row, "BoneDataID"),
                    ParentId = unchecked((byte)UnsignedField(
                        row,
                        "ParentAnimKitBoneSetID")),
                    AlternateId = unchecked((byte)UnsignedField(
                        row,
                        "AltAnimKitBoneSetID")),
                    AlternateBoneDataId = UnsignedField(row, "AltBoneDataID")
                });
        var animationKitPriorities = Database.Load("AnimKitPriority", build).Values
            .ToDictionary(
                row => unchecked((ushort)UnsignedField(row, "ID")),
                row => unchecked((byte)UnsignedField(row, "Priority")));

        IReadOnlyList<WowAnimationKitBoneSetTrackCandidate>
            BuildBoneSetAvailabilityCandidates(byte boneSetId, uint flags)
        {
            var candidates = new List<WowAnimationKitBoneSetTrackCandidate>();
            var visited = new HashSet<byte>();
            var currentId = boneSetId;
            while (currentId != 0 && visited.Add(currentId) &&
                   animationKitBoneSets.TryGetValue(currentId, out var current))
            {
                var useTrackZeroWhenUnavailable = (flags & 0x4000) != 0;
                candidates.Add(new WowAnimationKitBoneSetTrackCandidate(
                    current.BoneDataId,
                    current.AlternateBoneDataId,
                    useTrackZeroWhenUnavailable,
                    currentId));
                if (useTrackZeroWhenUnavailable)
                    break;

                if (current.AlternateId != byte.MaxValue)
                {
                    if (current.AlternateId == 0)
                        break;
                    if (animationKitBoneSets.TryGetValue(
                            current.AlternateId,
                            out var alternate))
                    {
                        candidates.Add(new WowAnimationKitBoneSetTrackCandidate(
                            alternate.BoneDataId,
                            alternate.AlternateBoneDataId,
                            AnimationKitBoneSetId: current.AlternateId));
                    }
                }

                if ((flags & 0x20) == 0)
                    break;
                currentId = current.ParentId;
            }
            return candidates;
        }

        var boneSetsByConfig = Database.Load("AnimKitConfigBoneSet", build).Values
            .GroupBy(row => IntegerField(row, "ParentAnimKitConfigID"))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WowAnimationKitBoneSetDefinition>)group
                    .Select(row =>
                    {
                        var boneSetId = unchecked((byte)UnsignedField(
                            row,
                            "AnimKitBoneSetID"));
                        animationKitBoneSets.TryGetValue(
                            boneSetId,
                            out var boneSet);
                        var priorityId = unchecked((ushort)UnsignedField(
                            row,
                            "AnimKitPriorityID"));
                        animationKitPriorities.TryGetValue(
                            priorityId,
                            out var priority);
                        return new WowAnimationKitBoneSetDefinition(
                            boneSetId,
                            priorityId,
                            boneSet?.BoneDataId,
                            boneSet?.ParentId ?? 0,
                            boneSet?.AlternateId ?? 0,
                            boneSet?.AlternateBoneDataId,
                            priority);
                    })
                    .ToArray());
        var segmentsByKit = Database.Load("AnimKitSegment", build).Values
            .GroupBy(row => IntegerField(row, "ParentAnimKitID"))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WowAnimationKitSegmentDefinition>)group
                    .Select(row =>
                    {
                        var configId = unchecked((ushort)UnsignedField(
                            row,
                            "AnimKitConfigID"));
                        var overrideConfigFlags =
                            UnsignedField(row, "OverrideConfigFlags");
                        var availabilityFlags =
                            configFlags.GetValueOrDefault(configId) |
                            overrideConfigFlags;
                        var boneSets =
                            (boneSetsByConfig.GetValueOrDefault(configId) ?? [])
                            .Select(boneSet => boneSet with
                            {
                                AvailabilityCandidates =
                                    BuildBoneSetAvailabilityCandidates(
                                        boneSet.AnimationKitBoneSetId,
                                        availabilityFlags)
                            })
                            .ToArray();
                        return new WowAnimationKitSegmentDefinition(
                            IntegerField(row, "ID"),
                            unchecked((byte)UnsignedField(row, "OrderIndex")),
                            unchecked((ushort)UnsignedField(row, "AnimID")),
                            UnsignedField(row, "AnimStartTime"),
                            configId,
                            unchecked((byte)UnsignedField(row, "StartCondition")),
                            unchecked((byte)UnsignedField(row, "StartConditionParam")),
                            UnsignedField(row, "StartConditionDelay"),
                            unchecked((byte)UnsignedField(row, "EndCondition")),
                            UnsignedField(row, "EndConditionParam"),
                            UnsignedField(row, "EndConditionDelay"),
                            FloatField(row, "Speed"),
                            UnsignedField(row, "SegmentFlags"),
                            unchecked((byte)UnsignedField(row, "ForcedVariation")),
                            overrideConfigFlags,
                            unchecked((sbyte)IntegerField(row, "LoopToSegmentIndex")),
                            unchecked((ushort)UnsignedField(row, "BlendInTimeMs")),
                            unchecked((ushort)UnsignedField(row, "BlendOutTimeMs")),
                            configFlags.GetValueOrDefault(configId),
                            boneSets);
                    })
                    .OrderBy(segment => segment.OrderIndex)
                    .ThenBy(segment => segment.SegmentId)
                    .ToArray());

        foreach (dynamic row in Database.Load("AnimKit", build).Values)
        {
            int id = IntegerField(row, "ID");
            if (id < 0)
                continue;
            kits[id] = new WowAnimationKitDefinition(
                id,
                UnsignedField(row, "OneShotDuration"),
                unchecked((ushort)UnsignedField(row, "OneShotStopAnimKitID")),
                unchecked((ushort)UnsignedField(row, "LowDefAnimKitID")),
                segmentsByKit.GetValueOrDefault(id) ?? []);
        }

        _animationKits = kits;
    }

    private void EnsureSpellVisualKits()
    {
        if (_spellVisualKits is not null)
            return;

        var kits = new Dictionary<uint, WowSpellVisualKitDefinition>();
        if (_installation.Version is not { Length: > 0 } build)
        {
            _spellVisualKits = kits;
            return;
        }

        var effectsByKit = Database.Load("SpellVisualKitEffect", build).Values
            .GroupBy(row => UnsignedField(row, "ParentSpellVisualKitID"))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WowSpellVisualKitEffectDefinition>)group
                    .Select(row => new WowSpellVisualKitEffectDefinition(
                        IntegerField(row, "ID"),
                        UnsignedField(row, "Effect"),
                        UnsignedField(row, "EffectType")))
                    .ToArray());

        foreach (dynamic row in Database.Load("SpellVisualKit", build).Values)
        {
            uint id = UnsignedField(row, "ID");
            kits[id] = new WowSpellVisualKitDefinition(
                id,
                effectsByKit.GetValueOrDefault(id) ?? []);
        }

        _spellVisualKits = kits;
    }

    private void EnsureShadowyEffects()
    {
        if (_shadowyEffects is not null)
            return;

        var effects = new Dictionary<uint, WowShadowyEffectDefinition>();
        if (_installation.Version is not { Length: > 0 } build)
        {
            _shadowyEffects = effects;
            return;
        }

        foreach (dynamic row in Database.Load("ShadowyEffect", build).Values)
        {
            uint id = UnsignedField(row, "ID");
            effects[id] = new WowShadowyEffectDefinition(
                id,
                UnsignedField(row, "PrimaryColor"),
                UnsignedField(row, "SecondaryColor"),
                FloatField(row, "Value"),
                UnsignedField(row, "Flags"),
                FloatField(row, "InnerStrength"),
                FloatField(row, "OuterStrength"));
        }

        _shadowyEffects = effects;
    }

    private void EnsureEdgeGlowEffects()
    {
        if (_edgeGlowEffects is not null)
            return;

        var effects = new Dictionary<uint, WowEdgeGlowEffectDefinition>();
        if (_installation.Version is not { Length: > 0 } build)
        {
            _edgeGlowEffects = effects;
            return;
        }

        foreach (dynamic row in Database.Load("EdgeGlowEffect", build).Values)
        {
            uint id = UnsignedField(row, "ID");
            effects[id] = new WowEdgeGlowEffectDefinition(
                id,
                FloatField(row, "FresnelCoefficient"),
                new Vector4(
                    FloatField(row, "GlowRed"),
                    FloatField(row, "GlowGreen"),
                    FloatField(row, "GlowBlue"),
                    FloatField(row, "GlowAlpha")),
                FloatField(row, "GlowMultiplier"),
                UnsignedField(row, "Flags"));
        }

        _edgeGlowEffects = effects;
    }

    private void EnsureDissolveEffects()
    {
        if (_dissolveEffects is not null)
            return;

        var effects = new Dictionary<uint, WowDissolveEffectDefinition>();
        if (_installation.Version is not { Length: > 0 } build)
        {
            _dissolveEffects = effects;
            return;
        }

        var textureBlendSets = Database.Load("TextureBlendSet", build).Values
            .ToDictionary(
                row => UnsignedField(row, "ID"),
                row => new WowTextureBlendSetDefinition(
                    UnsignedField(row, "ID"),
                    UnsignedArrayField(row, "TextureFileDataID", 3),
                    unchecked((byte)UnsignedField(row, "SwizzleRed")),
                    unchecked((byte)UnsignedField(row, "SwizzleGreen")),
                    unchecked((byte)UnsignedField(row, "SwizzleBlue")),
                    unchecked((byte)UnsignedField(row, "SwizzleAlpha")),
                    UnsignedField(row, "Flags"),
                    Vector3Field(row, "TextureScrollRateU"),
                    Vector3Field(row, "TextureScrollRateV"),
                    Vector3Field(row, "TextureScaleU"),
                    Vector3Field(row, "TextureScaleV"),
                    Vector4Field(row, "ModX")));

        foreach (dynamic row in Database.Load("DissolveEffect", build).Values)
        {
            uint textureBlendSetId = UnsignedField(row, "TextureBlendSetID");
            if (!textureBlendSets.TryGetValue(textureBlendSetId, out var textureBlendSet))
                continue;

            uint id = UnsignedField(row, "ID");
            effects[id] = new WowDissolveEffectDefinition(
                id,
                FloatField(row, "Ramp"),
                FloatField(row, "StartValue"),
                FloatField(row, "EndValue"),
                FloatField(row, "FadeInTime"),
                FloatField(row, "FadeOutTime"),
                FloatField(row, "Duration"),
                unchecked((byte)UnsignedField(row, "AttachID")),
                unchecked((byte)UnsignedField(row, "ProjectionType")),
                textureBlendSet,
                FloatField(row, "Scale"),
                UnsignedField(row, "Flags"),
                UnsignedField(row, "CurveID"),
                UnsignedField(row, "Priority"),
                FloatField(row, "FresnelIntensity"),
                new Vector4(
                    FloatField(row, "Field_9_1_5_40496_014"),
                    FloatField(row, "Field_9_1_5_40496_015"),
                    FloatField(row, "Field_9_1_5_40496_016"),
                    FloatField(row, "Field_9_1_5_40496_017")),
                FloatField(row, "Field_9_1_5_40496_018"),
                IntegerField(row, "Field_10_0_0_44649_019"),
                IntegerField(row, "Field_10_0_0_44649_020"));
        }

        _dissolveEffects = effects;
    }

    private static IReadOnlyList<uint> UnsignedArrayField(
        dynamic row,
        string name,
        int count)
    {
        IReadOnlyList<object?> values = SequenceField((object)row, name);
        return Enumerable.Range(0, count)
            .Select(index => index < values.Count
                ? unchecked((uint)Convert.ToInt64(values[index] ?? 0))
                : 0)
            .ToArray();
    }

    private static Vector3 Vector3Field(dynamic row, string name)
    {
        float[] values = FloatArrayField((object)row, name, 3);
        return new Vector3(values[0], values[1], values[2]);
    }

    private static Vector4 Vector4Field(dynamic row, string name)
    {
        float[] values = FloatArrayField((object)row, name, 4);
        return new Vector4(values[0], values[1], values[2], values[3]);
    }

    private static float[] FloatArrayField(dynamic row, string name, int count)
    {
        IReadOnlyList<object?> values = SequenceField((object)row, name);
        return Enumerable.Range(0, count)
            .Select(index => index < values.Count
                ? Convert.ToSingle(values[index] ?? 0)
                : 0)
            .ToArray();
    }

    private static IReadOnlyList<object?> SequenceField(dynamic row, string name)
    {
        try
        {
            object? value = row[name];
            return value is IEnumerable sequence and not string
                ? sequence.Cast<object?>().ToArray()
                : [value];
        }
        catch
        {
            return [];
        }
    }

    private static uint UnsignedField(dynamic row, string name)
    {
        object? value;
        try
        {
            value = row[name];
        }
        catch
        {
            return 0;
        }

        if (value is IEnumerable sequence and not string)
            value = sequence.Cast<object?>().FirstOrDefault();
        return unchecked((uint)Convert.ToInt64(value ?? 0));
    }

    private static int IntegerField(dynamic row, string name)
    {
        object? value;
        try
        {
            value = row[name];
        }
        catch
        {
            return 0;
        }

        if (value is IEnumerable sequence and not string)
            value = sequence.Cast<object?>().FirstOrDefault();
        return unchecked((int)Convert.ToInt64(value ?? 0));
    }

    private static float FloatField(dynamic row, string name)
    {
        object? value;
        try
        {
            value = row[name];
        }
        catch
        {
            return 0;
        }

        if (value is IEnumerable sequence and not string)
            value = sequence.Cast<object?>().FirstOrDefault();
        return Convert.ToSingle(value ?? 0);
    }
}
