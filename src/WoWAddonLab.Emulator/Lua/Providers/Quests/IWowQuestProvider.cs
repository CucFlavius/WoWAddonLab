namespace WoWAddonLab.Emulator.Lua;

public interface IWowQuestProvider
{
    bool TryGetTitle(int questId, out string title);
}
