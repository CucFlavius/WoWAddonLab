using System.Numerics;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCurveApi : LuaApiModule
{
    private const string MetatableName = "LuaCurveObject";
    private const int StorageMagic = 0x43555256;
    private const int MaximumPointCount = 256;

    private static readonly lua_CFunction GarbageCollectCallback = GarbageCollect;
    private static readonly lua_CFunction IndexCallback = Index;
    private static readonly lua_CFunction NewIndexCallback = NewIndex;
    private static readonly lua_CFunction EqualCallback = Equal;
    private static readonly lua_CFunction ToStringCallback = ToStringValue;
    private static readonly lua_CFunction DumpCallback = Dump;
    private static readonly lua_CFunction CreateCurveCallback = CreateCurve;
    private static readonly lua_CFunction CreateColorCurveCallback = CreateColorCurve;
    private static readonly lua_CFunction EvaluateColorFromBooleanCallback =
        EvaluateColorFromBoolean;
    private static readonly lua_CFunction EvaluateColorValueFromBooleanCallback =
        EvaluateColorValueFromBoolean;
    private static readonly lua_CFunction EvaluateGameCurveCallback = EvaluateGameCurve;

    private static readonly IReadOnlyDictionary<string, lua_CFunction> Methods =
        new Dictionary<string, lua_CFunction>(StringComparer.Ordinal)
        {
            ["GetType"] = state => Dispatch(state, "GetType"),
            ["HasSecretValues"] = state => Dispatch(state, "HasSecretValues"),
            ["SetType"] = state => Dispatch(state, "SetType"),
            ["AddPoint"] = state => Dispatch(state, "AddPoint"),
            ["ClearPoints"] = state => Dispatch(state, "ClearPoints"),
            ["Copy"] = state => Dispatch(state, "Copy"),
            ["Evaluate"] = state => Dispatch(state, "Evaluate"),
            ["GetPoint"] = state => Dispatch(state, "GetPoint"),
            ["GetPointCount"] = state => Dispatch(state, "GetPointCount"),
            ["GetPoints"] = state => Dispatch(state, "GetPoints"),
            ["RemovePoint"] = state => Dispatch(state, "RemovePoint"),
            ["SetPoints"] = state => Dispatch(state, "SetPoints"),
            ["SetToDefaults"] = state => Dispatch(state, "SetToDefaults")
        };

    public override void Register(lua_State state)
    {
        RegisterMetatable(state);
        RegisterEnums(state);

        lua_createtable(state, 0, 5);
        SetFunction(state, "CreateColorCurve", CreateColorCurveCallback);
        SetFunction(state, "CreateCurve", CreateCurveCallback);
        SetFunction(
            state,
            "EvaluateColorFromBoolean",
            EvaluateColorFromBooleanCallback);
        SetFunction(
            state,
            "EvaluateColorValueFromBoolean",
            EvaluateColorValueFromBooleanCallback);
        SetFunction(state, "EvaluateGameCurve", EvaluateGameCurveCallback);
        lua_setglobal(state, "C_CurveUtil");
    }

    internal static bool TryRead(
        lua_State state,
        int index,
        out WowCurveState? curve)
    {
        curve = null;
        unsafe
        {
            if (!TryGetStorage(state, index, out var storage) ||
                storage->StateHandle == IntPtr.Zero)
            {
                return false;
            }

            curve = GCHandle.FromIntPtr(storage->StateHandle).Target as WowCurveState;
            return curve is not null;
        }
    }

    internal static float Evaluate(WowCurveState curve, float x)
    {
        var points = curve.Points;
        if (points.Count == 0)
            return 0;
        if (points.Count == 1)
            return points[0].Y;

        return curve.Type switch
        {
            WowCurveType.Step => EvaluateStep(points, x),
            WowCurveType.Cosine => EvaluateCosine(points, x),
            WowCurveType.Cubic when points.Count >= 4 => EvaluateCubic(points, x),
            WowCurveType.Cubic => EvaluateCosine(points, x),
            _ => EvaluateLinear(points, x)
        };
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

        lua_createtable(state, 0, 4);
        SetInteger(state, "Linear", 0);
        SetInteger(state, "Step", 1);
        SetInteger(state, "Cosine", 2);
        SetInteger(state, "Cubic", 3);
        lua_setfield(state, -2, "LuaCurveType");

        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", 4);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 3);
        lua_setfield(state, -2, "LuaCurveTypeMeta");
        lua_setglobal(state, "Enum");
    }

    private static int CreateCurve(lua_State state)
    {
        Push(state, new WowCurveState());
        return 1;
    }

    private static int CreateColorCurve(lua_State state)
    {
        lua_newuserdata(state, (UIntPtr)1);
        return 1;
    }

    private static int EvaluateColorFromBoolean(lua_State state)
    {
        const string usage =
            "Usage: local value = C_CurveUtil.EvaluateColorFromBoolean(boolean, valueIfTrue, valueIfFalse)";
        if (lua_gettop(state) != 3 ||
            !TryReadColorTable(state, 2) ||
            !TryReadColorTable(state, 3))
        {
            return luaL_error(state, usage);
        }

        lua_pushvalue(state, lua_toboolean(state, 1) != 0 ? 2 : 3);
        return 1;
    }

    private static int EvaluateColorValueFromBoolean(lua_State state)
    {
        const string usage =
            "Usage: local value = C_CurveUtil.EvaluateColorValueFromBoolean(boolean, valueIfTrue, valueIfFalse)";
        if (lua_gettop(state) != 3 ||
            !TryReadNormalizedByte(state, 2, out var ifTrue) ||
            !TryReadNormalizedByte(state, 3, out var ifFalse))
        {
            return luaL_error(state, usage);
        }

        var selected = lua_toboolean(state, 1) != 0 ? ifTrue : ifFalse;
        lua_pushnumber(state, selected / 255.0);
        return 1;
    }

    private static int EvaluateGameCurve(lua_State state)
    {
        const string usage =
            "Usage: local y = C_CurveUtil.EvaluateGameCurve(curveID, x)";
        if (lua_gettop(state) != 2 ||
            !TryReadInt32(state, 1, out _) ||
            !TryReadFloat(state, 2, out _))
        {
            return luaL_error(state, usage);
        }

        lua_pushnumber(state, 0);
        return 1;
    }

    private static int Dispatch(lua_State state, string operation)
    {
        var usage = Usage(operation);
        if (!TryRead(state, 1, out var curve))
            return luaL_error(state, usage);

        switch (operation)
        {
            case "GetType":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                lua_pushinteger(state, (int)curve!.Type);
                return 1;
            case "HasSecretValues":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                lua_pushboolean(state, 0);
                return 1;
            case "SetType":
                if (lua_gettop(state) != 2 ||
                    !TryReadCurveType(state, 2, out var curveType))
                {
                    return luaL_error(state, usage);
                }
                curve!.Type = curveType;
                return 0;
            case "AddPoint":
                if (lua_gettop(state) != 3 ||
                    !TryReadFloat(state, 2, out var x) ||
                    !TryReadFloat(state, 3, out var y))
                {
                    return luaL_error(state, usage);
                }
                if (curve!.Points.Count >= MaximumPointCount)
                {
                    return luaL_error(
                        state,
                        $"LuaCurveObject:AddPoint(): attempted to assign too many points to a curve (expected no more than {curve.Points.Count} points)");
                }
                InsertPoint(curve.Points, new Vector2(x, y));
                return 0;
            case "ClearPoints":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                curve!.Points.Clear();
                return 0;
            case "Copy":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                Push(state, curve!.Copy());
                return 1;
            case "Evaluate":
                if (lua_gettop(state) != 2 ||
                    !TryReadFloat(state, 2, out var value))
                {
                    return luaL_error(state, usage);
                }
                lua_pushnumber(state, Evaluate(curve!, value));
                return 1;
            case "GetPoint":
                if (lua_gettop(state) != 2 ||
                    !TryReadOneBasedIndex(state, 2, out var pointIndex))
                {
                    return luaL_error(state, usage);
                }
                if (pointIndex >= curve!.Points.Count)
                {
                    lua_pushnil(state);
                    return 1;
                }
                PushVector2(state, curve.Points[(int)pointIndex]);
                return 1;
            case "GetPointCount":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                lua_pushnumber(state, curve!.Points.Count);
                return 1;
            case "GetPoints":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                lua_createtable(state, curve!.Points.Count, 0);
                for (var index = 0; index < curve.Points.Count; index++)
                {
                    PushVector2(state, curve.Points[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "RemovePoint":
                if (lua_gettop(state) != 2 ||
                    !TryReadOneBasedIndex(state, 2, out var removeIndex))
                {
                    return luaL_error(state, usage);
                }
                if (removeIndex >= curve!.Points.Count)
                {
                    return luaL_error(
                        state,
                        $"LuaCurveObject:RemovePoint(): attempted to remove an invalid curve point (index {removeIndex} out of range)");
                }
                curve.Points.RemoveAt((int)removeIndex);
                return 0;
            case "SetPoints":
                if (lua_gettop(state) != 2 ||
                    !TryReadPointArray(state, 2, out var points))
                {
                    return luaL_error(state, usage);
                }
                if (points.Count > MaximumPointCount)
                {
                    return luaL_error(
                        state,
                        $"LuaCurveObject:SetPoints(): attempted to assign too many points to a curve (expected no more than {points.Count} points)");
                }
                points.Sort(static (left, right) => left.X.CompareTo(right.X));
                curve!.Points.Clear();
                curve.Points.AddRange(points);
                return 0;
            case "SetToDefaults":
                if (lua_gettop(state) != 1)
                    return luaL_error(state, usage);
                curve!.Points.Clear();
                curve.Type = WowCurveType.Linear;
                return 0;
            default:
                return 0;
        }
    }

    private static unsafe void Push(lua_State state, WowCurveState curve)
    {
        var runtime = LuaBindings.GetRuntime(state);
        lua_newtable(state);
        var propertyTableReference = LuaRuntime.CaptureValue(state, -1);
        lua_pop(state, 1);

        var storage = (CurveStorage*)lua_newuserdata(state, (UIntPtr)sizeof(CurveStorage));
        storage->PropertyTableReference = propertyTableReference;
        storage->StateHandle = GCHandle.ToIntPtr(GCHandle.Alloc(curve));
        storage->Magic = StorageMagic;
        luaL_getmetatable(state, MetatableName);
        lua_setmetatable(state, -2);
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
                        TryGetStorage(state, 2, out var right) &&
                        left == right;
            lua_pushboolean(state, equal ? 1 : 0);
            return 1;
        }
    }

    private static int ToStringValue(lua_State state)
    {
        lua_pushstring(
            state,
            $"LuaCurveObject: 0x{lua_topointer(state, 1).ToUInt64():X}");
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
        out CurveStorage* storage)
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
        storage = (CurveStorage*)lua_touserdata(state, index);
        return storage is not null && storage->Magic == StorageMagic;
    }

    private static float EvaluateLinear(IReadOnlyList<Vector2> points, float x)
    {
        var right = LowerBound(points, x, 0, points.Count);
        if (right <= 0)
            return points[0].Y;
        if (right >= points.Count)
            return points[^1].Y;

        var leftPoint = points[right - 1];
        var rightPoint = points[right];
        var width = rightPoint.X - leftPoint.X;
        if (width <= 0)
            return rightPoint.Y;
        var amount = (x - leftPoint.X) / width;
        return amount * (rightPoint.Y - leftPoint.Y) + leftPoint.Y;
    }

    private static float EvaluateStep(IReadOnlyList<Vector2> points, float x)
    {
        var right = LowerBound(points, x, 0, points.Count);
        if (right <= 0)
            return points[0].Y;
        return right < points.Count ? points[right - 1].Y : points[^1].Y;
    }

    private static float EvaluateCosine(IReadOnlyList<Vector2> points, float x)
    {
        var right = LowerBound(points, x, 0, points.Count);
        if (right <= 0)
            return points[0].Y;
        if (right >= points.Count)
            return points[^1].Y;

        var leftPoint = points[right - 1];
        var rightPoint = points[right];
        var width = rightPoint.X - leftPoint.X;
        if (width <= 0)
            return rightPoint.Y;
        var amount = (1 - MathF.Cos((x - leftPoint.X) / width * MathF.PI)) * 0.5f;
        return amount * (rightPoint.Y - leftPoint.Y) + leftPoint.Y;
    }

    private static float EvaluateCubic(IReadOnlyList<Vector2> points, float x)
    {
        var right = LowerBound(points, x, 1, points.Count - 1);
        if (right <= 1)
            return points[1].Y;
        if (right >= points.Count - 1)
            return points[^2].Y;

        var p0 = points[right - 2];
        var p1 = points[right - 1];
        var p2 = points[right];
        var p3 = points[right + 1];
        var width = p2.X - p1.X;
        if (width <= 0)
            return p1.Y;
        var t = (x - p1.X) / width;
        return (((1.5f * p1.Y - 0.5f * p0.Y - 1.5f * p2.Y + 0.5f * p3.Y) * t +
                 (p0.Y - 2.5f * p1.Y + 2 * p2.Y - 0.5f * p3.Y)) * t * t) +
               (0.5f * p2.Y - 0.5f * p0.Y) * t + p1.Y;
    }

    private static int LowerBound(
        IReadOnlyList<Vector2> points,
        float x,
        int begin,
        int end)
    {
        while (begin < end)
        {
            var middle = begin + (end - begin) / 2;
            if (x <= points[middle].X)
                end = middle;
            else
                begin = middle + 1;
        }
        return begin;
    }

    private static void InsertPoint(List<Vector2> points, Vector2 point)
    {
        var index = LowerBound(points, point.X, 0, points.Count);
        points.Insert(index, point);
    }

    private static bool TryReadPointArray(
        lua_State state,
        int index,
        out List<Vector2> points)
    {
        points = [];
        if (lua_istable(state, index) == 0)
            return false;

        var absolute = AbsoluteIndex(state, index);
        var count = checked((int)lua_objlen(state, absolute));
        points.Capacity = count;
        for (var item = 1; item <= count; item++)
        {
            lua_rawgeti(state, absolute, item);
            var valid = TryReadVector2Table(state, -1, out var point);
            lua_pop(state, 1);
            if (!valid)
                return false;
            points.Add(point);
        }
        return true;
    }

    private static bool TryReadVector2Table(
        lua_State state,
        int index,
        out Vector2 point)
    {
        point = default;
        if (lua_istable(state, index) == 0)
            return false;
        var absolute = AbsoluteIndex(state, index);
        lua_getfield(state, absolute, "x");
        var validX = TryReadFloat(state, -1, out var x);
        lua_pop(state, 1);
        lua_getfield(state, absolute, "y");
        var validY = TryReadFloat(state, -1, out var y);
        lua_pop(state, 1);
        if (!validX || !validY)
            return false;
        point = new Vector2(x, y);
        return true;
    }

    private static void PushVector2(lua_State state, Vector2 point)
    {
        lua_createtable(state, 0, 2);
        lua_pushnumber(state, point.X);
        lua_setfield(state, -2, "x");
        lua_pushnumber(state, point.Y);
        lua_setfield(state, -2, "y");
        ApplyMixinToTopTable(state, "Vector2DMixin");
    }

    private static void ApplyMixinToTopTable(lua_State state, string mixinName)
    {
        var target = lua_gettop(state);
        lua_getglobal(state, mixinName);
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        var mixin = lua_gettop(state);
        lua_pushnil(state);
        while (lua_next(state, mixin) != 0)
        {
            lua_pushvalue(state, -2);
            lua_pushvalue(state, -2);
            lua_settable(state, target);
            lua_pop(state, 1);
        }
        lua_pop(state, 1);
    }

    private static bool TryReadCurveType(
        lua_State state,
        int index,
        out WowCurveType type)
    {
        type = WowCurveType.Linear;
        if (!TryReadInt32(state, index, out var value) || value is < 0 or > 3)
            return false;
        type = (WowCurveType)value;
        return true;
    }

    private static bool TryReadInt32(lua_State state, int index, out int value)
    {
        value = 0;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < int.MinValue or > int.MaxValue)
            return false;
        value = (int)number;
        return true;
    }

    private static bool TryReadOneBasedIndex(
        lua_State state,
        int index,
        out uint value)
    {
        value = 0;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < 0 or > uint.MaxValue)
            return false;
        value = number == 0 ? uint.MaxValue : (uint)(number - 1);
        return true;
    }

    private static bool TryReadFloat(lua_State state, int index, out float value)
    {
        value = 0;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (double.IsNaN(number) || number is < -float.MaxValue or > float.MaxValue)
            return false;
        value = (float)number;
        return true;
    }

    private static bool TryReadNormalizedByte(
        lua_State state,
        int index,
        out byte value)
    {
        value = 0;
        if (!TryReadFloat(state, index, out var number) || number is < 0 or > 1)
            return false;
        value = (byte)(number * 255);
        return true;
    }

    private static bool TryReadColorTable(lua_State state, int index)
    {
        if (lua_istable(state, index) == 0)
            return false;
        var absolute = AbsoluteIndex(state, index);
        foreach (var field in new[] { "r", "g", "b", "a" })
        {
            lua_getfield(state, absolute, field);
            var valid = field == "a" && lua_isnil(state, -1) != 0 ||
                        TryReadFloat(state, -1, out _);
            lua_pop(state, 1);
            if (!valid)
                return false;
        }
        return true;
    }

    private static void SetFunction(
        lua_State state,
        string name,
        lua_CFunction callback)
    {
        lua_pushcfunction(state, callback);
        lua_setfield(state, -2, name);
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
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
        "GetType" => "Usage: local curveType = self:GetType()",
        "HasSecretValues" =>
            "Usage: local hasSecretValues = self:HasSecretValues()",
        "SetType" => "Usage: self:SetType(type)",
        "AddPoint" => "Usage: self:AddPoint(point)",
        "ClearPoints" => "Usage: self:ClearPoints()",
        "Copy" => "Usage: local curve = self:Copy()",
        "Evaluate" => "Usage: local y = self:Evaluate(x)",
        "GetPoint" => "Usage: local point = self:GetPoint(index)",
        "GetPointCount" => "Usage: local count = self:GetPointCount()",
        "GetPoints" => "Usage: local point = self:GetPoints()",
        "RemovePoint" => "Usage: self:RemovePoint(index)",
        "SetPoints" => "Usage: self:SetPoints(point)",
        _ => "Usage: self:SetToDefaults()"
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct CurveStorage
    {
        public int Magic;
        public int PropertyTableReference;
        public IntPtr StateHandle;
    }
}
