using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal static class WowAbbreviatedNumberFormatterApi
{
    private const string MetatableName = "AbbreviatedNumberFormatter";
    private const int StorageMagic = 0x414E464D;

    private static readonly lua_CFunction CreateCallback = Create;
    private static readonly lua_CFunction GarbageCollectCallback = GarbageCollect;
    private static readonly lua_CFunction IndexCallback = Index;
    private static readonly lua_CFunction NewIndexCallback = NewIndex;
    private static readonly lua_CFunction EqualCallback = Equal;
    private static readonly lua_CFunction ToStringCallback = ToStringValue;
    private static readonly lua_CFunction DumpCallback = Dump;

    private static readonly IReadOnlyDictionary<string, lua_CFunction> Methods =
        new Dictionary<string, lua_CFunction>(StringComparer.Ordinal)
        {
            ["FormatNumber"] = state => Dispatch(state, "FormatNumber"),
            ["AddBreakpoint"] = state => Dispatch(state, "AddBreakpoint"),
            ["ClearBreakpoints"] = state => Dispatch(state, "ClearBreakpoints"),
            ["Copy"] = state => Dispatch(state, "Copy"),
            ["GetBreakpoints"] = state => Dispatch(state, "GetBreakpoints"),
            ["ResetBreakpoints"] = state => Dispatch(state, "ResetBreakpoints"),
            ["SetBreakpoints"] = state => Dispatch(state, "SetBreakpoints")
        };

    public static void Register(lua_State state)
    {
        if (luaL_newmetatable(state, MetatableName) == 0)
        {
            lua_pop(state, 1);
            return;
        }
        foreach (var (name, callback) in Methods)
        {
            lua_pushcfunction(state, callback);
            lua_setfield(state, -2, name);
        }
        lua_pushcfunction(state, GarbageCollectCallback);
        lua_setfield(state, -2, "__gc");
        lua_pushcfunction(state, IndexCallback);
        lua_setfield(state, -2, "__index");
        lua_pushcfunction(state, NewIndexCallback);
        lua_setfield(state, -2, "__newindex");
        lua_pushcfunction(state, EqualCallback);
        lua_setfield(state, -2, "__eq");
        lua_pushcfunction(state, ToStringCallback);
        lua_setfield(state, -2, "__tostring");
        lua_pushcfunction(state, DumpCallback);
        lua_setfield(state, -2, "__dump");
        lua_pushboolean(state, 0);
        lua_setfield(state, -2, "__metatable");
        lua_pop(state, 1);
    }

    public static void RegisterFactory(lua_State state)
    {
        lua_pushcfunction(state, CreateCallback);
        lua_setfield(state, -2, "CreateAbbreviatedNumberFormatter");
    }

    internal static bool TryRead(
        lua_State state,
        int index,
        out IWowNumericFormatterState? formatter)
    {
        formatter = null;
        unsafe
        {
            if (!TryGetStorage(state, index, out var storage) ||
                storage->StateHandle == IntPtr.Zero)
            {
                return false;
            }
            formatter = GCHandle.FromIntPtr(storage->StateHandle).Target
                as WowAbbreviatedNumberFormatterState;
            return formatter is not null;
        }
    }

    internal static string Format(
        LuaRuntime runtime,
        WowAbbreviatedNumberFormatterState formatter,
        double value)
    {
        foreach (var breakpoint in formatter.Breakpoints)
        {
            if (value < breakpoint.Breakpoint)
                continue;
            var significand = Math.Floor(value / breakpoint.SignificandDivisor) /
                              breakpoint.FractionDivisor;
            return significand.ToString("G14", CultureInfo.InvariantCulture) +
                   ResolveAbbreviation(runtime, breakpoint);
        }
        return value.ToString("G14", CultureInfo.InvariantCulture);
    }

    private static string ResolveAbbreviation(
        LuaRuntime runtime,
        WowNumberAbbreviationBreakpoint breakpoint)
    {
        if (!breakpoint.AbbreviationIsGlobal)
            return breakpoint.Abbreviation;
        lua_getglobal(runtime.State, breakpoint.Abbreviation);
        var value = lua_isstring(runtime.State, -1) != 0
            ? lua_tostring(runtime.State, -1) ?? string.Empty
            : breakpoint.Abbreviation switch
            {
                "FIRST_NUMBER_CAP_NO_SPACE" => "K",
                "SECOND_NUMBER_CAP_NO_SPACE" => "M",
                "THIRD_NUMBER_CAP_NO_SPACE" => "B",
                "FOURTH_NUMBER_CAP_NO_SPACE" => "T",
                _ => string.Empty
            };
        lua_pop(runtime.State, 1);
        return value;
    }

    private static int Create(lua_State state)
    {
        Push(state, WowAbbreviatedNumberFormatterState.CreateDefault());
        return 1;
    }

    private static int Dispatch(lua_State state, string operation)
    {
        var usage = Usage(operation);
        if (!TryReadConcrete(state, 1, out var formatter))
            return luaL_error(state, usage);

        switch (operation)
        {
            case "FormatNumber":
                if (lua_gettop(state) != 2 || lua_isnumber(state, 2) == 0)
                    return luaL_error(state, usage);
                lua_pushstring(
                    state,
                    Format(
                        LuaBindings.GetRuntime(state),
                        formatter!,
                        lua_tonumber(state, 2)));
                return 1;
            case "AddBreakpoint":
                if (lua_gettop(state) != 2 ||
                    !TryReadBreakpoint(state, 2, out var breakpoint) ||
                    !IsValid(breakpoint))
                {
                    return luaL_error(state, usage);
                }
                formatter!.Breakpoints.Add(breakpoint);
                formatter.Sort();
                return 0;
            case "ClearBreakpoints":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                formatter!.Breakpoints.Clear();
                return 0;
            case "Copy":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                Push(state, formatter!.Copy());
                return 1;
            case "GetBreakpoints":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                PushBreakpoints(state, formatter!.Breakpoints);
                return 1;
            case "ResetBreakpoints":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                formatter!.Reset();
                return 0;
            case "SetBreakpoints":
                if (lua_gettop(state) != 2 ||
                    !TryReadBreakpoints(state, 2, out var breakpoints) ||
                    breakpoints.Any(value => !IsValid(value)))
                {
                    return luaL_error(state, usage);
                }
                formatter!.Breakpoints.Clear();
                formatter.Breakpoints.AddRange(breakpoints);
                formatter.Sort();
                return 0;
            default:
                return 0;
        }
    }

    private static bool TryReadBreakpoints(
        lua_State state,
        int index,
        out List<WowNumberAbbreviationBreakpoint> values)
    {
        values = [];
        if (lua_istable(state, index) == 0)
            return false;
        var table = AbsoluteIndex(state, index);
        var count = checked((int)lua_objlen(state, table));
        for (var itemIndex = 1; itemIndex <= count; itemIndex++)
        {
            lua_rawgeti(state, table, itemIndex);
            var valid = TryReadBreakpoint(state, -1, out var value);
            lua_pop(state, 1);
            if (!valid)
                return false;
            values.Add(value);
        }
        return true;
    }

    private static bool TryReadBreakpoint(
        lua_State state,
        int index,
        out WowNumberAbbreviationBreakpoint value)
    {
        value = default;
        if (lua_istable(state, index) == 0)
            return false;
        var table = AbsoluteIndex(state, index);
        if (!TryReadRequiredNumberField(state, table, "breakpoint", out var threshold) ||
            !TryReadRequiredStringField(state, table, "abbreviation", out var abbreviation) ||
            !TryReadRequiredNumberField(
                state,
                table,
                "significandDivisor",
                out var significandDivisor) ||
            !TryReadRequiredNumberField(
                state,
                table,
                "fractionDivisor",
                out var fractionDivisor))
        {
            return false;
        }
        lua_getfield(state, table, "abbreviationIsGlobal");
        var abbreviationIsGlobal = lua_isnil(state, -1) != 0 ||
                                   lua_toboolean(state, -1) != 0;
        lua_pop(state, 1);
        value = new WowNumberAbbreviationBreakpoint(
            threshold,
            abbreviation,
            significandDivisor,
            fractionDivisor,
            abbreviationIsGlobal);
        return true;
    }

    private static bool TryReadRequiredNumberField(
        lua_State state,
        int table,
        string name,
        out double value)
    {
        lua_getfield(state, table, name);
        var valid = lua_isnumber(state, -1) != 0;
        value = valid ? lua_tonumber(state, -1) : 0;
        lua_pop(state, 1);
        return valid;
    }

    private static bool TryReadRequiredStringField(
        lua_State state,
        int table,
        string name,
        out string value)
    {
        lua_getfield(state, table, name);
        var valid = lua_isstring(state, -1) != 0;
        value = valid ? lua_tostring(state, -1) ?? string.Empty : string.Empty;
        lua_pop(state, 1);
        return valid;
    }

    private static bool IsValid(WowNumberAbbreviationBreakpoint value)
    {
        if (!double.IsFinite(value.Breakpoint) || value.Breakpoint <= 0 ||
            !double.IsFinite(value.SignificandDivisor) ||
            value.SignificandDivisor <= 0 ||
            !double.IsFinite(value.FractionDivisor) || value.FractionDivisor <= 0)
        {
            return false;
        }
        var exponent = Math.Log10(value.Breakpoint);
        return Math.Abs(exponent - Math.Floor(exponent)) < 4.440892098500626e-16;
    }

    private static void PushBreakpoints(
        lua_State state,
        IReadOnlyList<WowNumberAbbreviationBreakpoint> breakpoints)
    {
        lua_createtable(state, breakpoints.Count, 0);
        for (var index = 0; index < breakpoints.Count; index++)
        {
            var value = breakpoints[index];
            lua_createtable(state, 0, 5);
            SetNumber(state, "breakpoint", value.Breakpoint);
            lua_pushstring(state, value.Abbreviation);
            lua_setfield(state, -2, "abbreviation");
            SetNumber(state, "significandDivisor", value.SignificandDivisor);
            SetNumber(state, "fractionDivisor", value.FractionDivisor);
            lua_pushboolean(state, value.AbbreviationIsGlobal ? 1 : 0);
            lua_setfield(state, -2, "abbreviationIsGlobal");
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static unsafe void Push(
        lua_State state,
        WowAbbreviatedNumberFormatterState formatter)
    {
        var runtime = LuaBindings.GetRuntime(state);
        lua_newtable(state);
        var propertyTableReference = LuaRuntime.CaptureValue(state, -1);
        lua_pop(state, 1);
        var storage = (FormatterStorage*)lua_newuserdata(
            state,
            (UIntPtr)sizeof(FormatterStorage));
        storage->Magic = StorageMagic;
        storage->PropertyTableReference = propertyTableReference;
        storage->StateHandle = GCHandle.ToIntPtr(GCHandle.Alloc(formatter));
        luaL_getmetatable(state, MetatableName);
        lua_setmetatable(state, -2);
    }

    private static bool TryReadConcrete(
        lua_State state,
        int index,
        out WowAbbreviatedNumberFormatterState? formatter)
    {
        formatter = null;
        unsafe
        {
            if (!TryGetStorage(state, index, out var storage) ||
                storage->StateHandle == IntPtr.Zero)
            {
                return false;
            }
            formatter = GCHandle.FromIntPtr(storage->StateHandle).Target
                as WowAbbreviatedNumberFormatterState;
            return formatter is not null;
        }
    }

    private static int GarbageCollect(lua_State state)
    {
        unsafe
        {
            if (!TryGetStorage(state, 1, out var storage))
                return 0;
            if (LuaBindings.TryGetRuntime(state, out var runtime))
                runtime!.ReleaseReference(storage->PropertyTableReference);
            storage->PropertyTableReference = 0;
            if (storage->StateHandle != IntPtr.Zero)
            {
                var handle = GCHandle.FromIntPtr(storage->StateHandle);
                if (handle.IsAllocated)
                    handle.Free();
                storage->StateHandle = IntPtr.Zero;
            }
            storage->Magic = 0;
            return 0;
        }
    }

    private static int Index(lua_State state)
    {
        unsafe
        {
            if (!TryGetStorage(state, 1, out var storage))
            {
                lua_pushnil(state);
                return 1;
            }
            luaL_getmetatable(state, MetatableName);
            lua_pushvalue(state, 2);
            lua_rawget(state, -2);
            if (lua_isnil(state, -1) == 0)
            {
                lua_remove(state, -2);
                return 1;
            }
            lua_pop(state, 2);
            lua_rawgeti(state, LUA_REGISTRYINDEX, storage->PropertyTableReference);
            lua_pushvalue(state, 2);
            lua_rawget(state, -2);
            lua_remove(state, -2);
            return 1;
        }
    }

    private static int NewIndex(lua_State state)
    {
        unsafe
        {
            if (!TryGetStorage(state, 1, out var storage))
                return 0;
            luaL_getmetatable(state, MetatableName);
            lua_pushvalue(state, 2);
            lua_rawget(state, -2);
            var readOnly = lua_isnil(state, -1) == 0;
            lua_pop(state, 2);
            if (readOnly)
            {
                return luaL_error(
                    state,
                    $"Attempted to assign to read-only key {LuaKeyText(state, 2)}");
            }
            lua_rawgeti(state, LUA_REGISTRYINDEX, storage->PropertyTableReference);
            lua_pushvalue(state, 2);
            lua_pushvalue(state, 3);
            lua_rawset(state, -3);
            lua_pop(state, 1);
            return 0;
        }
    }

    private static int Equal(lua_State state)
    {
        unsafe
        {
            var equal = TryGetStorage(state, 1, out var left) &&
                        TryGetStorage(state, 2, out var right) && left == right;
            lua_pushboolean(state, equal ? 1 : 0);
            return 1;
        }
    }

    private static int ToStringValue(lua_State state)
    {
        lua_pushstring(
            state,
            $"AbbreviatedNumberFormatter: 0x{lua_topointer(state, 1).ToUInt64():X}");
        return 1;
    }

    private static int Dump(lua_State state)
    {
        lua_pushnil(state);
        return 1;
    }

    private static unsafe bool TryGetStorage(
        lua_State state,
        int index,
        out FormatterStorage* storage)
    {
        storage = null;
        if (lua_type(state, index) != LUA_TUSERDATA ||
            lua_getmetatable(state, index) == 0)
        {
            return false;
        }
        luaL_getmetatable(state, MetatableName);
        var matches = lua_rawequal(state, -1, -2) != 0;
        lua_pop(state, 2);
        if (!matches)
            return false;
        storage = (FormatterStorage*)lua_touserdata(state, index);
        return storage is not null && storage->Magic == StorageMagic;
    }

    private static int AbsoluteIndex(lua_State state, int index) =>
        index > 0 || index <= LUA_REGISTRYINDEX
            ? index
            : lua_gettop(state) + index + 1;

    private static string LuaKeyText(lua_State state, int index) =>
        lua_type(state, index) == LUA_TSTRING
            ? lua_tostring(state, index) ?? string.Empty
            : lua_typename(state, lua_type(state, index)) ?? "unknown";

    private static string Usage(string operation) => operation switch
    {
        "FormatNumber" => "Usage: local formattedNumber = self:FormatNumber(number)",
        "AddBreakpoint" => "Usage: self:AddBreakpoint(breakpoint)",
        "ClearBreakpoints" => "Usage: self:ClearBreakpoints()",
        "Copy" => "Usage: local copy = self:Copy()",
        "GetBreakpoints" => "Usage: local breakpoints = self:GetBreakpoints()",
        "ResetBreakpoints" => "Usage: self:ResetBreakpoints()",
        _ => "Usage: self:SetBreakpoints(breakpoints)"
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct FormatterStorage
    {
        public int Magic;
        public int PropertyTableReference;
        public IntPtr StateHandle;
    }
}
