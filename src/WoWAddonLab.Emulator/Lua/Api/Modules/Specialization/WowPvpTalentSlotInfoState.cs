using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPvpTalentSlotInfoState(
    bool Enabled,
    int Level,
    int? SelectedTalentId,
    IReadOnlyList<int> AvailableTalentIds);
