using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowStoreVasPurchaseArguments(
    int ProductId,
    string Guid,
    string? NameChangeName,
    string? StubGuildName,
    string? StubCharacterGuid,
    int? DestinationRealm,
    string? DestinationWowAccount,
    string? DestinationBnetAccount,
    bool IsFactionBundle,
    bool IsGuildFollow);
