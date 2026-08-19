using System.Collections;
using DBCD.Providers;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactModelInfoCatalog : TactCatalog, IWowModelInfoProvider
{
    private readonly IReadOnlyDictionary<int, WowModelSceneDefinition> _scenes;
    private readonly IReadOnlyDictionary<int, WowModelSceneActorDefinition> _actors;
    private readonly IReadOnlyDictionary<int, WowModelSceneActorDisplayDefinition> _displays;
    private readonly IReadOnlyDictionary<int, WowModelSceneCameraDefinition> _cameras;

    private TactModelInfoCatalog(
        IReadOnlyDictionary<int, WowModelSceneDefinition> scenes,
        IReadOnlyDictionary<int, WowModelSceneActorDefinition> actors,
        IReadOnlyDictionary<int, WowModelSceneActorDisplayDefinition> displays,
        IReadOnlyDictionary<int, WowModelSceneCameraDefinition> cameras)
    {
        _scenes = scenes;
        _actors = actors;
        _displays = displays;
        _cameras = cameras;
    }

    public int SceneCount => _scenes.Count;
    public int ActorCount => _actors.Count;
    public int ActorDisplayCount => _displays.Count;
    public int CameraCount => _cameras.Count;

    public bool TryGetScene(int id, out WowModelSceneDefinition scene) =>
        _scenes.TryGetValue(id, out scene!);

    public bool TryGetActor(int id, out WowModelSceneActorDefinition actor) =>
        _actors.TryGetValue(id, out actor!);

    public bool TryGetActorDisplay(int id, out WowModelSceneActorDisplayDefinition display) =>
        _displays.TryGetValue(id, out display!);

    public bool TryGetCamera(int id, out WowModelSceneCameraDefinition camera) =>
        _cameras.TryGetValue(id, out camera!);

    public static TactModelInfoCatalog Load(TactAssetSource tact, string build)
    {
        var database = tact.Database;
        var actorRows = database.Load("UiModelSceneActor", build).Values.ToArray();
        var cameraRows = database.Load("UiModelSceneCamera", build).Values.ToArray();

        var actorsByScene = actorRows
            .GroupBy(row => Integer(row, "UiModelSceneID"))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)group
                    .Select(row => Integer(row, "ID"))
                    .Order()
                    .ToArray());
        var camerasByScene = cameraRows
            .GroupBy(row => Integer(row, "UiModelSceneID"))
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)group
                    .Select(row => Integer(row, "ID"))
                    .Order()
                    .ToArray());

        var scenes = database.Load("UiModelScene", build).Values.ToDictionary(
            row => Integer(row, "ID"),
            row =>
            {
                var id = Integer(row, "ID");
                return new WowModelSceneDefinition(
                    id,
                    Integer(row, "UiSystemType"),
                    Integer(row, "Flags"),
                    camerasByScene.GetValueOrDefault(id) ?? [],
                    actorsByScene.GetValueOrDefault(id) ?? []);
            });

        var actors = actorRows.ToDictionary(
            row => Integer(row, "ID"),
            row =>
            {
                var flags = Integer(row, "Flags");
                var normalizedScale = Number(row, "NormalizedScale");
                return new WowModelSceneActorDefinition(
                    Integer(row, "ID"),
                    Text(row, "ScriptTag"),
                    Vector(row, "Position"),
                    Number(row, "OrientationYaw"),
                    Number(row, "OrientationPitch"),
                    Number(row, "OrientationRoll"),
                    normalizedScale > 0 ? normalizedScale : null,
                    (flags & 2) != 0,
                    (flags & 4) != 0,
                    (flags & 8) != 0,
                    OptionalInteger(row, "UiModelSceneActorDisplayID"));
            });

        var displays = database.Load("UiModelSceneActorDisplay", build).Values.ToDictionary(
            row => Integer(row, "ID"),
            row => new WowModelSceneActorDisplayDefinition(
                Integer(row, "ID"),
                Integer(row, "AnimationID"),
                Integer(row, "SequenceVariation"),
                Number(row, "AnimSpeed"),
                OptionalInteger(row, "AnimKitID"),
                OptionalInteger(row, "SpellVisualKitID"),
                Number(row, "Alpha"),
                Number(row, "Scale")));

        var cameras = cameraRows.ToDictionary(
            row => Integer(row, "ID"),
            row => new WowModelSceneCameraDefinition(
                Integer(row, "ID"),
                Text(row, "ScriptTag"),
                Vector(row, "Target"),
                Number(row, "Yaw"),
                Number(row, "Pitch"),
                Number(row, "Roll"),
                Number(row, "ZoomDistance"),
                Number(row, "MinZoomDistance"),
                Number(row, "MaxZoomDistance"),
                Vector(row, "ZoomedTargetOffset"),
                Number(row, "ZoomedYawOffset"),
                Number(row, "ZoomedPitchOffset"),
                Number(row, "ZoomedRollOffset"),
                Integer(row, "Flags"),
                Integer(row, "CameraType")));

        return new TactModelInfoCatalog(scenes, actors, displays, cameras);
    }

    private static WowVector3 Vector(dynamic row, string name)
    {
        var value = Field(row, name);
        if (value is IEnumerable sequence and not string)
        {
            var numbers = sequence.Cast<object?>()
                .Select(item => Convert.ToDouble(item ?? 0))
                .Take(3)
                .ToArray();
            if (numbers.Length == 3)
                return new WowVector3(numbers[0], numbers[1], numbers[2]);
        }

        return new WowVector3(
            IndexedNumber(row, name, 0),
            IndexedNumber(row, name, 1),
            IndexedNumber(row, name, 2));
    }

    private static double IndexedNumber(dynamic row, string name, int index)
    {
        foreach (var candidate in new[]
                 {
                     $"{name}[{index}]",
                     $"{name}_{index}",
                     $"{name}{index}"
                 })
        {
            var value = Field(row, candidate);
            if (value is not null)
                return Convert.ToDouble(value);
        }
        return 0;
    }



    private static int? OptionalInteger(dynamic row, string name)
    {
        var value = Integer(row, name);
        return value == 0 ? null : value;
    }

    private static double Number(dynamic row, string name) =>
        Convert.ToDouble(Field(row, name) ?? 0);

}
