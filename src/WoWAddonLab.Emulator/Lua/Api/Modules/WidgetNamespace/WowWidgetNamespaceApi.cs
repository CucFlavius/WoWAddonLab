using LuaNET.Lua51;
using WoWAddonLab.Emulator.UI;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowWidgetNamespaceApi : LuaApiModule
{
    private static readonly lua_CFunction IsWidgetCallback = IsWidget;
    private static readonly lua_CFunction IsFrameWidgetCallback = IsFrameWidget;
    private static readonly lua_CFunction IsRenderableWidgetCallback = IsRenderableWidget;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        lua_pushcclosure(state, IsWidgetCallback, 0);
        lua_setfield(state, -2, "IsWidget");
        lua_pushcclosure(state, IsFrameWidgetCallback, 0);
        lua_setfield(state, -2, "IsFrameWidget");
        lua_pushcclosure(state, IsRenderableWidgetCallback, 0);
        lua_setfield(state, -2, "IsRenderableWidget");
        lua_setglobal(state, "C_Widget");
    }

    private static int IsWidget(lua_State state) =>
        PushObjectPredicate(state, static value => WowWidgetApi.IsWidget(value.ObjectType));

    private static int IsFrameWidget(lua_State state)
        => PushObjectPredicate(state, static value => WowWidgetApi.IsFrameWidget(value.ObjectType));

    private static int IsRenderableWidget(lua_State state) =>
        PushObjectPredicate(state, static value => value.IsRegion);

    private static int PushObjectPredicate(
        lua_State state,
        Func<UiObject, bool> predicate)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var value = LuaBindings.GetObject(runtime, 1);
        lua_pushboolean(state, value is not null && predicate(value) ? 1 : 0);
        return 1;
    }
}
