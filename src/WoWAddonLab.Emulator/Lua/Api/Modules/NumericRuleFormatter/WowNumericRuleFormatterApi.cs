using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal static class WowNumericRuleFormatterApi
{
    private const string MetatableName = "NumericRuleFormatter";
    private const int StorageMagic = 0x4E52464D;

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
            ["SetBreakpoints"] = state => Dispatch(state, "SetBreakpoints")
        };

    public static void Register(lua_State state)
    {
        RegisterMetatable(state);
        RegisterEnums(state);
    }

    public static void RegisterFactory(lua_State state)
    {
        lua_pushcfunction(state, CreateCallback);
        lua_setfield(state, -2, "CreateNumericRuleFormatter");
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
                as WowNumericRuleFormatterState;
            return formatter is not null;
        }
    }

    internal static string Format(
        lua_State state,
        WowNumericRuleFormatterState formatter,
        double value)
    {
        WowNumericRuleFormatBreakpoint? selected = null;
        foreach (var breakpoint in formatter.Breakpoints)
        {
            if (value < breakpoint.Threshold)
                continue;
            selected = breakpoint;
            break;
        }

        if (selected is null)
            return value.ToString("G14", CultureInfo.InvariantCulture);

        var rule = selected;
        var formattedValue = ApplyRounding(value, rule.Step, rule.Rounding);
        if (rule.Minimum.HasValue)
            formattedValue = Math.Max(rule.Minimum.Value, formattedValue);
        if (rule.Maximum.HasValue)
            formattedValue = Math.Min(rule.Maximum.Value, formattedValue);

        var components = rule.Components.Count == 0
            ? [formattedValue]
            : rule.Components.Select(component =>
            {
                var componentValue = formattedValue;
                if (component.Divisor > 0)
                    componentValue /= component.Divisor;
                if (component.Modulus > 0)
                    componentValue %= component.Modulus;
                return ApplyRounding(
                    componentValue,
                    component.Step,
                    component.Rounding);
            }).ToArray();

        return FormatWithLua(state, rule.Format, components);
    }

    private static double ApplyRounding(
        double value,
        double step,
        NumericRuleFormatRounding rounding)
    {
        if (step <= 0)
            return value;
        var remainder = value % step;
        return rounding switch
        {
            NumericRuleFormatRounding.Nearest =>
                value - remainder + (remainder < step * 0.5 ? 0 : step),
            NumericRuleFormatRounding.Up =>
                value - remainder + (remainder <= 0 ? 0 : step),
            NumericRuleFormatRounding.Down => value - remainder,
            _ => value
        };
    }

    private static string FormatWithLua(
        lua_State state,
        string format,
        IReadOnlyList<double> values)
    {
        var originalTop = lua_gettop(state);
        lua_getglobal(state, "string");
        lua_getfield(state, -1, "format");
        lua_remove(state, -2);
        lua_pushstring(state, format);
        foreach (var value in values)
            lua_pushnumber(state, value);

        if (lua_pcall(state, values.Count + 1, 1, 0) != 0)
        {
            var error = lua_tostring(state, -1) ?? "numeric rule formatting failed";
            lua_settop(state, originalTop);
            luaL_error(state, error);
            return string.Empty;
        }

        var result = lua_tostring(state, -1) ?? string.Empty;
        lua_settop(state, originalTop);
        return result;
    }

    private static void RegisterMetatable(lua_State state)
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

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }

        lua_createtable(state, 0, 3);
        SetInteger(state, "Nearest", 0);
        SetInteger(state, "Up", 1);
        SetInteger(state, "Down", 2);
        lua_setfield(state, -2, "NumericRuleFormatRounding");

        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", 3);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 2);
        lua_setfield(state, -2, "NumericRuleFormatRoundingMeta");
        lua_setglobal(state, "Enum");
    }

    private static int Create(lua_State state)
    {
        Push(state, new WowNumericRuleFormatterState());
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
                var number = lua_tonumber(state, 2);
                if (!double.IsFinite(number))
                    return luaL_error(state, usage);
                lua_pushstring(state, Format(state, formatter!, number));
                return 1;
            case "AddBreakpoint":
                if (lua_gettop(state) != 2 ||
                    !TryReadBreakpoint(state, 2, out var breakpoint))
                {
                    return luaL_error(state, usage);
                }
                formatter!.Breakpoints.Add(breakpoint!);
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
            case "SetBreakpoints":
                if (lua_gettop(state) != 2 ||
                    !TryReadBreakpoints(state, 2, out var breakpoints))
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
        out List<WowNumericRuleFormatBreakpoint> values)
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
            values.Add(value!);
        }
        return true;
    }

    private static bool TryReadBreakpoint(
        lua_State state,
        int index,
        out WowNumericRuleFormatBreakpoint? value)
    {
        value = null;
        if (lua_istable(state, index) == 0)
            return false;
        var table = AbsoluteIndex(state, index);
        if (!TryReadRequiredNumber(state, table, "threshold", out var threshold) ||
            !TryReadRequiredNumber(state, table, "step", out var step) ||
            !TryReadRounding(state, table, "rounding", out var rounding) ||
            !TryReadOptionalNumber(state, table, "min", out var minimum) ||
            !TryReadOptionalNumber(state, table, "max", out var maximum) ||
            !TryReadOptionalString(state, table, "format", out var format) ||
            !TryReadComponents(state, table, out var components))
        {
            return false;
        }

        value = new WowNumericRuleFormatBreakpoint(
            threshold,
            step,
            rounding,
            minimum,
            maximum,
            format,
            components);
        return true;
    }

    private static bool TryReadComponents(
        lua_State state,
        int table,
        out IReadOnlyList<WowNumericRuleFormatComponent> values)
    {
        values = [];
        lua_getfield(state, table, "components");
        if (lua_isnil(state, -1) != 0)
        {
            lua_pop(state, 1);
            return true;
        }
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            return false;
        }

        var componentTable = AbsoluteIndex(state, -1);
        var count = checked((int)lua_objlen(state, componentTable));
        var components = new List<WowNumericRuleFormatComponent>(count);
        for (var itemIndex = 1; itemIndex <= count; itemIndex++)
        {
            lua_rawgeti(state, componentTable, itemIndex);
            var valid = TryReadComponent(state, -1, out var component);
            lua_pop(state, 1);
            if (!valid)
            {
                lua_pop(state, 1);
                return false;
            }
            components.Add(component);
        }
        lua_pop(state, 1);
        values = components;
        return true;
    }

    private static bool TryReadComponent(
        lua_State state,
        int index,
        out WowNumericRuleFormatComponent value)
    {
        value = default;
        if (lua_istable(state, index) == 0)
            return false;
        var table = AbsoluteIndex(state, index);
        if (!TryReadRequiredNumber(state, table, "div", out var divisor) ||
            !TryReadRequiredNumber(state, table, "mod", out var modulus) ||
            !TryReadRequiredNumber(state, table, "step", out var step) ||
            !TryReadRounding(state, table, "rounding", out var rounding))
        {
            return false;
        }

        value = new WowNumericRuleFormatComponent(divisor, modulus, step, rounding);
        return true;
    }

    private static bool TryReadRequiredNumber(
        lua_State state,
        int table,
        string name,
        out double value)
    {
        lua_getfield(state, table, name);
        var valid = lua_isnumber(state, -1) != 0;
        value = valid ? lua_tonumber(state, -1) : 0;
        lua_pop(state, 1);
        return valid && double.IsFinite(value);
    }

    private static bool TryReadOptionalNumber(
        lua_State state,
        int table,
        string name,
        out double? value)
    {
        lua_getfield(state, table, name);
        if (lua_isnil(state, -1) != 0)
        {
            lua_pop(state, 1);
            value = null;
            return true;
        }
        var valid = lua_isnumber(state, -1) != 0;
        var parsed = valid ? lua_tonumber(state, -1) : 0;
        lua_pop(state, 1);
        value = valid ? parsed : null;
        return valid && double.IsFinite(parsed);
    }

    private static bool TryReadOptionalString(
        lua_State state,
        int table,
        string name,
        out string value)
    {
        lua_getfield(state, table, name);
        if (lua_isnil(state, -1) != 0)
        {
            lua_pop(state, 1);
            value = string.Empty;
            return true;
        }
        var valid = lua_isstring(state, -1) != 0;
        value = valid ? lua_tostring(state, -1) ?? string.Empty : string.Empty;
        lua_pop(state, 1);
        return valid;
    }

    private static bool TryReadRounding(
        lua_State state,
        int table,
        string name,
        out NumericRuleFormatRounding value)
    {
        value = default;
        if (!TryReadRequiredNumber(state, table, name, out var raw) ||
            raw != Math.Truncate(raw) || raw is < 0 or > 2)
        {
            return false;
        }
        value = (NumericRuleFormatRounding)(int)raw;
        return true;
    }

    private static void PushBreakpoints(
        lua_State state,
        IReadOnlyList<WowNumericRuleFormatBreakpoint> breakpoints)
    {
        lua_createtable(state, breakpoints.Count, 0);
        for (var index = 0; index < breakpoints.Count; index++)
        {
            PushBreakpoint(state, breakpoints[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushBreakpoint(
        lua_State state,
        WowNumericRuleFormatBreakpoint value)
    {
        lua_createtable(state, 0, 7);
        SetNumber(state, "threshold", value.Threshold);
        SetNumber(state, "step", value.Step);
        SetInteger(state, "rounding", (int)value.Rounding);
        if (value.Minimum.HasValue)
            SetNumber(state, "min", value.Minimum.Value);
        if (value.Maximum.HasValue)
            SetNumber(state, "max", value.Maximum.Value);
        lua_pushstring(state, value.Format);
        lua_setfield(state, -2, "format");
        lua_createtable(state, value.Components.Count, 0);
        for (var index = 0; index < value.Components.Count; index++)
        {
            var component = value.Components[index];
            lua_createtable(state, 0, 4);
            SetNumber(state, "div", component.Divisor);
            SetNumber(state, "mod", component.Modulus);
            SetNumber(state, "step", component.Step);
            SetInteger(state, "rounding", (int)component.Rounding);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "components");
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static unsafe void Push(
        lua_State state,
        WowNumericRuleFormatterState formatter)
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
        out WowNumericRuleFormatterState? formatter)
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
                as WowNumericRuleFormatterState;
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
            $"NumericRuleFormatter: 0x{lua_topointer(state, 1).ToUInt64():X}");
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
