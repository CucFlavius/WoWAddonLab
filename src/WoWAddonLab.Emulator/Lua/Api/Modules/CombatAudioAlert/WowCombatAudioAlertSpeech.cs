using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCombatAudioAlertSpeech(
    string Text,
    int Category,
    bool AllowOverlap);
