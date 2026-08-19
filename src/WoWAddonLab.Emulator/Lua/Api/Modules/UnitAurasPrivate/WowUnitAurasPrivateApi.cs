using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowUnitAurasPrivateApi : LuaApiModule
{
    private const string AddUpdateCallbackUsage =
        "Usage: C_UnitAurasPrivate.AddPrivateAuraUpdateCallback(unitToken, cb)";
    private const string AnchorPrivateAuraUsage =
        "Usage: C_UnitAurasPrivate.AnchorPrivateAura(auraFrame, icon, duration, anchorID)";
    private const string GetAllPrivateAurasUsage =
        "Usage: local aura = C_UnitAurasPrivate.GetAllPrivateAuras(unit)";
    private const string GetAppliedSoundsUsage =
        "Usage: local sounds = C_UnitAurasPrivate.GetAuraAppliedSoundsForSpell(unitToken, spellID)";
    private const string GetPrivateAuraUsage =
        "Usage: local aura = C_UnitAurasPrivate.GetAuraDataByAuraInstanceIDPrivate(unit, auraInstanceID)";
    private const string RemoveUpdateCallbackUsage =
        "Usage: C_UnitAurasPrivate.RemovePrivateAuraUpdateCallback(unitToken, cb)";
    private const string SetWarningFrameUsage =
        "Usage: C_UnitAurasPrivate.SetPrivateWarningTextFrame(frame)";

    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "AddPrivateAuraUpdateCallback", "AnchorPrivateAura",
        "GetAllPrivateAuras", "GetAuraAppliedSoundsForSpell",
        "GetAuraDataByAuraInstanceIDPrivate", "GetPrivateAuraAnchors",
        "RemovePrivateAuraUpdateCallback",
        "SetPrivateAuraAnchorAddedCallback", "SetPrivateAuraAnchorRemovedCallback",
        "SetPrivateRaidBossMessageCallback", "SetPrivateWarningTextFrame",
        "SetShowDispelTypeCallback"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_UnitAurasPrivate");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var auras = runtime.UnitAuras;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "SetPrivateAuraAnchorAddedCallback":
                if (!SetRequiredCallback(
                        runtime,
                        state,
                        ref auras.PrivateAuraAnchorAddedCallbackReference))
                    return luaL_error(
                        state,
                        "Usage: C_UnitAurasPrivate.SetPrivateAuraAnchorAddedCallback(cb)");
                return 0;
            case "SetPrivateAuraAnchorRemovedCallback":
                if (!SetRequiredCallback(
                        runtime,
                        state,
                        ref auras.PrivateAuraAnchorRemovedCallbackReference))
                    return luaL_error(
                        state,
                        "Usage: C_UnitAurasPrivate.SetPrivateAuraAnchorRemovedCallback(cb)");
                return 0;
            case "SetShowDispelTypeCallback":
                if (!SetRequiredCallback(
                        runtime,
                        state,
                        ref auras.ShowDispelTypeCallbackReference))
                    return luaL_error(
                        state,
                        "Usage: C_UnitAurasPrivate.SetShowDispelTypeCallback(cb)");
                return 0;
            case "SetPrivateRaidBossMessageCallback":
                if (!SetRequiredCallback(
                        runtime,
                        state,
                        ref auras.PrivateRaidBossMessageCallbackReference))
                    return luaL_error(
                        state,
                        "Usage: C_UnitAurasPrivate.SetPrivateRaidBossMessageCallback(cb)");
                return 0;
            case "SetPrivateWarningTextFrame":
            {
                var frame = LuaBindings.GetObject(runtime, 1);
                if (frame is null || !WowWidgetApi.IsFrameWidget(frame.ObjectType))
                    return luaL_error(state, SetWarningFrameUsage);
                auras.PrivateWarningTextFrameObjectId = frame.Id;
                return 0;
            }
            case "AddPrivateAuraUpdateCallback":
            {
                if (!TryReadRequiredString(state, 1, out var unit) ||
                    !WowFunctionContainersApi.TryCaptureCallback(
                        runtime,
                        state,
                        2,
                        out _,
                        out var callbackReference))
                {
                    return luaL_error(state, AddUpdateCallbackUsage);
                }
                if (!LuaBindings.IsRecognizedUnitToken(unit))
                {
                    runtime.ReleaseReference(callbackReference);
                    return 0;
                }
                if (!auras.PrivateAuraUpdateCallbackReferences.TryGetValue(
                        unit,
                        out var callbacks))
                {
                    callbacks = [];
                    auras.PrivateAuraUpdateCallbackReferences.Add(unit, callbacks);
                }
                callbacks.Add(callbackReference);
                return 0;
            }
            case "RemovePrivateAuraUpdateCallback":
            {
                if (!TryReadRequiredString(state, 1, out var unit) ||
                    !WowFunctionContainersApi.TryCaptureCallback(
                        runtime,
                        state,
                        2,
                        out _,
                        out var callbackReference))
                {
                    return luaL_error(state, RemoveUpdateCallbackUsage);
                }
                if (!LuaBindings.IsRecognizedUnitToken(unit) ||
                    !auras.PrivateAuraUpdateCallbackReferences.TryGetValue(
                        unit,
                        out var callbacks))
                {
                    runtime.ReleaseReference(callbackReference);
                    return 0;
                }
                lua_rawgeti(state, LUA_REGISTRYINDEX, callbackReference);
                for (var index = 0; index < callbacks.Count; index++)
                {
                    lua_rawgeti(state, LUA_REGISTRYINDEX, callbacks[index]);
                    var equal = lua_rawequal(state, -1, -2) != 0;
                    lua_pop(state, 1);
                    if (!equal)
                        continue;
                    runtime.ReleaseReference(callbacks[index]);
                    callbacks.RemoveAt(index);
                    if (callbacks.Count == 0)
                        auras.PrivateAuraUpdateCallbackReferences.Remove(unit);
                    break;
                }
                lua_pop(state, 1);
                runtime.ReleaseReference(callbackReference);
                return 0;
            }
            case "AnchorPrivateAura":
                return AnchorPrivateAura(runtime, state);
            case "GetPrivateAuraAnchors":
            {
                var anchors = auras.PrivateAuraAnchors.Values.OrderBy(value => value.Id).ToArray();
                lua_createtable(state, anchors.Length, 0);
                for (var index = 0; index < anchors.Length; index++)
                {
                    PushAnchor(runtime, state, anchors[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetAllPrivateAuras":
            {
                if (!TryReadRequiredString(state, 1, out var unit))
                    return luaL_error(state, GetAllPrivateAurasUsage);
                var entries = auras.Find(unit).Where(aura => aura.IsPrivate).ToArray();
                lua_createtable(state, entries.Length, 0);
                for (var index = 0; index < entries.Length; index++)
                {
                    WowUnitAuraApi.PushAura(state, entries[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetAuraDataByAuraInstanceIDPrivate":
            {
                if (!TryReadRequiredString(state, 1, out var unit) ||
                    !TryReadRequiredUInt32(state, 2, out var instanceId))
                    return luaL_error(state, GetPrivateAuraUsage);
                return WowUnitAuraApi.PushOptionalAura(
                    state,
                    auras.Find(unit).FirstOrDefault(value =>
                        value.AuraInstanceId == instanceId));
            }
            case "GetAuraAppliedSoundsForSpell":
                return GetAuraAppliedSoundsForSpell(runtime, state);
            default:
                return 0;
        }
    }

    internal static void NotifyAnchorAdded(
        LuaRuntime runtime,
        WowPrivateAuraAnchorState anchor)
    {
        if (runtime.UnitAuras.PrivateAuraAnchorAddedCallbackReference <= 0)
            return;
        var baseline = lua_gettop(runtime.State);
        PushAnchor(runtime, runtime.State, anchor);
        var reference = LuaRuntime.CaptureValue(runtime.State, -1);
        lua_settop(runtime.State, baseline);
        try
        {
            runtime.InvokeReference(
                runtime.UnitAuras.PrivateAuraAnchorAddedCallbackReference,
                null,
                new LuaRegistryValue(reference));
        }
        finally
        {
            runtime.ReleaseReference(reference);
        }
    }

    internal static void NotifyAnchorRemoved(LuaRuntime runtime, long anchorId)
    {
        if (runtime.UnitAuras.PrivateAuraAnchorRemovedCallbackReference > 0)
        {
            runtime.InvokeReference(
                runtime.UnitAuras.PrivateAuraAnchorRemovedCallbackReference,
                null,
                anchorId);
        }
    }

    private static bool SetRequiredCallback(
        LuaRuntime runtime,
        lua_State state,
        ref int reference)
    {
        if (!WowFunctionContainersApi.TryCaptureCallback(
                runtime,
                state,
                1,
                out _,
                out var capturedReference))
        {
            return false;
        }
        runtime.ReleaseReference(reference);
        reference = capturedReference;
        return true;
    }

    private static int AnchorPrivateAura(LuaRuntime runtime, lua_State state)
    {
        var auraFrame = LuaBindings.GetObject(runtime, 1);
        var icon = LuaBindings.GetObject(runtime, 2);
        var duration = LuaBindings.GetObject(runtime, 3);
        if (auraFrame is null ||
            !WowWidgetApi.IsFrameWidget(auraFrame.ObjectType) ||
            icon is null ||
            !IsRegion(icon) ||
            duration is null ||
            !IsRegion(duration) ||
            !TryReadRequiredUInt32(state, 4, out var anchorId))
            return luaL_error(state, AnchorPrivateAuraUsage);

        if (!runtime.UnitAuras.PrivateAuraAnchors.TryGetValue(anchorId, out var anchor))
            return 0;

        runtime.Ui.Reparent(auraFrame, anchor.ParentId);
        auraFrame.Anchors.Clear();
        auraFrame.AllPointsTargetId = anchor.ParentId;
        ApplyAnchor(icon, anchor.IconAnchor);
        ApplyAnchor(duration, anchor.DurationAnchor);
        runtime.UnitAuras.PrivateAuraBindings[auraFrame.Id] =
            new WowPrivateAuraBindingState(
                anchorId,
                auraFrame.Id,
                icon.Id,
                duration.Id);
        runtime.Ui.InvalidateLayout();
        return 0;
    }

    private static int GetAuraAppliedSoundsForSpell(
        LuaRuntime runtime,
        lua_State state)
    {
        if (!TryReadRequiredString(state, 1, out var unit) ||
            !TryReadRequiredInt32(state, 2, out var spellId))
            return luaL_error(state, GetAppliedSoundsUsage);

        var entries = LuaBindings.IsRecognizedUnitToken(unit)
            ? runtime.UnitAuras.PrivateAuraAppliedSounds
                .Where(entry =>
                    entry.UnitToken.Equals(unit, StringComparison.OrdinalIgnoreCase) &&
                    entry.SpellId == spellId)
                .ToArray()
            : [];
        lua_createtable(state, entries.Length, 0);
        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            lua_createtable(state, 0, 5);
            SetString(state, "unitToken", entry.UnitToken);
            SetNumber(state, "spellID", entry.SpellId);
            SetOptionalString(state, "soundFileName", entry.SoundFileName);
            SetOptionalNumber(state, "soundFileID", entry.SoundFileId);
            SetOptionalString(state, "outputChannel", entry.OutputChannel);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static bool IsRegion(UiObject value) =>
        value.IsRegion || WowWidgetApi.IsFrameWidget(value.ObjectType);

    private static void ApplyAnchor(
        UiObject value,
        WowAuraAnchorPointState? anchor)
    {
        value.Anchors.Clear();
        value.AllPointsTargetId = null;
        if (anchor is null)
            return;
        value.Anchors.Add(new UiAnchor(
            anchor.Point,
            anchor.RelativeToObjectId,
            anchor.RelativePoint,
            (float)anchor.OffsetX,
            (float)anchor.OffsetY));
    }

    private static void PushAnchor(
        LuaRuntime runtime,
        lua_State state,
        WowPrivateAuraAnchorState anchor)
    {
        lua_createtable(state, 0, 11);
        SetNumber(state, "anchorID", anchor.Id);
        SetString(state, "unitToken", anchor.UnitToken);
        SetNumber(state, "auraIndex", anchor.AuraIndex);
        SetBoolean(state, "isContainer", anchor.IsContainer);
        SetBoolean(state, "showCountdownFrame", anchor.ShowCountdownFrame);
        SetBoolean(state, "showCountdownNumbers", anchor.ShowCountdownNumbers);
        SetOptionalNumber(state, "iconWidth", anchor.IconWidth);
        SetOptionalNumber(state, "iconHeight", anchor.IconHeight);
        SetOptionalNumber(state, "borderScale", anchor.BorderScale);
        runtime.PushObject(runtime.Ui.Find(anchor.ParentId));
        lua_setfield(state, -2, "parent");
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string name,
        double? value)
    {
        if (value is null)
            return;
        SetNumber(state, name, value.Value);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string name,
        long? value)
    {
        if (value is not null)
            SetNumber(state, name, value.Value);
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalString(
        lua_State state,
        string name,
        string? value)
    {
        if (value is not null)
            SetString(state, name, value);
    }

    private static bool TryReadRequiredString(
        lua_State state,
        int index,
        out string value)
    {
        value = string.Empty;
        if (index > lua_gettop(state) || lua_isstring(state, index) == 0)
            return false;
        value = lua_tostring(state, index) ?? string.Empty;
        return true;
    }

    private static bool TryReadRequiredInt32(
        lua_State state,
        int index,
        out int value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < int.MinValue or > int.MaxValue)
            return false;
        value = (int)number;
        return true;
    }

    private static bool TryReadRequiredUInt32(
        lua_State state,
        int index,
        out uint value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < uint.MinValue or > uint.MaxValue)
            return false;
        value = (uint)number;
        return true;
    }
}
