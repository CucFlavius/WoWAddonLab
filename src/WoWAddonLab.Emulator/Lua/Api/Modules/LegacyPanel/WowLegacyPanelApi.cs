using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowLegacyPanelApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "CloseGuildRegistrar", "CloseItemText", "CloseLoot", "ClosePetition",
        "CloseTabardCreation", "SendSubscriptionInterstitialResponse",
        "SetPortraitTextureFromCreatureDisplayID"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state) => 0;
}
