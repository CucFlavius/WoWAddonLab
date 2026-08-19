namespace WoWAddonLab.Emulator.Lua;

public interface IWowItemClassProvider
{
    IReadOnlyDictionary<int, string> Classes { get; }

    IReadOnlyDictionary<(int ClassId, int SubClassId), WowItemSubClassData>
        SubClasses { get; }
}
