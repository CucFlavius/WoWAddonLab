using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowStoreConfirmationInfo(
    int ProductId,
    string ConfirmationText,
    double CurrentDollars,
    double CurrentCents,
    double NormalDollars,
    double NormalCents);
