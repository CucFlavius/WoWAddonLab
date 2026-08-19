using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTextureApi : LuaApiModule
{
    private const string SetPortraitTextureUsage =
        "Usage: SetPortraitTexture(textureObject, unitToken [, disableMasking])";
    private static readonly lua_CFunction GlobalCallback = DispatchGlobal;
    private static readonly lua_CFunction NamespaceCallback = DispatchNamespace;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "SetPortraitTexture", GlobalCallback);

        lua_newtable(state);
        foreach (var operation in new[]
                 {
                     "ClearTitleIconTexture",
                     "GetAtlasElementID",
                     "GetAtlasElements",
                     "GetAtlasExists",
                     "GetAtlasID",
                     "GetAtlasInfo",
                     "GetFilenameFromFileDataID",
                     "GetTitleIconTexture",
                     "IsTitleIconTextureReady",
                     "SetTitleIconTexture",
                     "SetURLTexture"
                 })
        {
            lua_pushstring(state, operation);
            lua_pushcclosure(state, NamespaceCallback, 1);
            lua_setfield(state, -2, operation);
        }
        lua_setglobal(state, "C_Texture");
        RegisterEnums(state);
    }

    private static int DispatchGlobal(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var target = LuaBindings.GetObject(runtime, 1);
        if (target is null ||
            !target.ObjectType.Equals("Texture", StringComparison.OrdinalIgnoreCase) ||
            lua_isstring(state, 2) == 0)
        {
            return luaL_error(state, SetPortraitTextureUsage);
        }

        var unitToken = lua_tostring(state, 2) ?? string.Empty;
        var disableMasking = false;
        if (lua_gettop(state) >= 3 && lua_isnil(state, 3) == 0)
        {
            if (lua_type(state, 3) != LUA_TBOOLEAN)
                return luaL_error(state, SetPortraitTextureUsage);
            disableMasking = lua_toboolean(state, 3) != 0;
        }

        SetPortraitTexture(runtime, target, unitToken, disableMasking);
        return 0;
    }

    internal static void SetPortraitTexture(
        LuaRuntime runtime,
        UiObject target,
        string unitToken,
        bool disableMasking)
    {
        var texture = target.Texture ??= new UiTextureState();
        texture.PortraitUnitToken = unitToken;
        texture.PortraitDisableMasking = disableMasking;
        LuaBindings.ClearTextureAsset(texture);

        if (runtime.Units.Find(unitToken) is
            {
                PortraitAsset: not null
            } unitWithAsset)
        {
            texture.Asset = unitWithAsset.PortraitAsset;
            texture.FileDataId = unitWithAsset.PortraitFileDataId;
            texture.IsColor = false;
        }
        else if (runtime.Units.Find(unitToken) is
                 {
                     PortraitFileDataId: not null
                 } unitWithFileDataId)
        {
            texture.Asset = null;
            texture.FileDataId = unitWithFileDataId.PortraitFileDataId;
            texture.IsColor = false;
        }
    }

    private static int DispatchNamespace(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "ClearTitleIconTexture")
        {
            const string clearUsage =
                "Usage: C_Texture.ClearTitleIconTexture(texture)";
            if (!TryReadRequiredTexture(runtime, state, 1, out var target))
                return luaL_error(state, clearUsage);
            LuaBindings.ClearTextureAsset(target.Texture ??= new UiTextureState());
            return 0;
        }

        if (operation == "GetAtlasElements")
        {
            lua_newtable(state);
            var index = 1;
            if (runtime.AtlasProvider is { } atlasProvider)
            {
                foreach (var atlasInfo in atlasProvider.EnumerateAtlases())
                {
                    lua_pushstring(state, atlasInfo.Name);
                    lua_rawseti(state, -2, index++);
                }
            }
            return 1;
        }

        if (operation is "GetTitleIconTexture" or
            "IsTitleIconTextureReady" or
            "SetTitleIconTexture")
        {
            return DispatchTitleIcon(runtime, state, operation);
        }

        if (operation == "SetURLTexture")
        {
            const string urlUsage =
                "Usage: C_Texture.SetURLTexture(texture, url)";
            if (!TryReadRequiredTexture(runtime, state, 1, out _) ||
                lua_isstring(state, 2) == 0)
            {
                return luaL_error(state, urlUsage);
            }

            return 0;
        }

        if (operation == "GetFilenameFromFileDataID")
        {
            const string filenameUsage =
                "Usage: local filename = C_Texture.GetFilenameFromFileDataID(fileDataID)";
            if (!TryReadRequiredInt32(state, 1, out var signedFileDataId))
                return luaL_error(state, filenameUsage);
            var filename = signedFileDataId >= 0 &&
                           runtime.AtlasProvider is IWowFileDataNameProvider filenameProvider &&
                           filenameProvider.TryGetFilename((uint)signedFileDataId, out var resolved)
                ? resolved
                : string.Empty;
            lua_pushstring(state, filename);
            return 1;
        }

        var usage = operation switch
        {
            "GetAtlasElementID" =>
                "Usage: local elementID = C_Texture.GetAtlasElementID(atlas)",
            "GetAtlasExists" =>
                "Usage: local atlasExists = C_Texture.GetAtlasExists(atlas)",
            "GetAtlasID" =>
                "Usage: local atlasID = C_Texture.GetAtlasID(atlas)",
            _ => "Usage: local info = C_Texture.GetAtlasInfo(atlas)"
        };
        if (lua_isstring(state, 1) == 0)
            return luaL_error(state, usage);

        var atlasName = lua_tostring(state, 1) ?? string.Empty;
        WowAtlasInfo? atlas = null;
        var found = runtime.AtlasProvider is { } provider &&
                    provider.TryGetAtlas(atlasName, out atlas);
        if (operation is "GetAtlasElementID" or "GetAtlasID")
        {
            lua_pushnumber(
                state,
                found
                    ? operation == "GetAtlasElementID"
                        ? atlas!.ElementId
                        : atlas!.AtlasId
                    : 0);
            return 1;
        }
        if (operation == "GetAtlasExists")
        {
            lua_pushboolean(state, found ? 1 : 0);
            return 1;
        }
        if (!found)
            return 0;

        lua_newtable(state);
        SetString(state, "elementName", atlas!.Name);
        SetNumber(state, "width", atlas.Width);
        SetNumber(state, "height", atlas.Height);

        lua_newtable(state);
        SetNumber(state, "x", atlas.RawWidth);
        SetNumber(state, "y", atlas.RawHeight);
        lua_setfield(state, -2, "rawSize");

        SetNumber(state, "leftTexCoord", atlas.Left);
        SetNumber(state, "rightTexCoord", atlas.Right);
        SetNumber(state, "topTexCoord", atlas.Top);
        SetNumber(state, "bottomTexCoord", atlas.Bottom);
        SetBoolean(state, "tilesHorizontally", atlas.TilesHorizontally);
        SetBoolean(state, "tilesVertically", atlas.TilesVertically);
        if (atlas.FileDataId != 0)
            SetNumber(state, "file", atlas.FileDataId);
        if (atlas.Filename is not null)
            SetString(state, "filename", atlas.Filename);

        if (atlas.SliceData is { } slice)
        {
            lua_newtable(state);
            SetNumber(state, "marginLeft", slice.MarginLeft);
            SetNumber(state, "marginTop", slice.MarginTop);
            SetNumber(state, "marginRight", slice.MarginRight);
            SetNumber(state, "marginBottom", slice.MarginBottom);
            SetNumber(state, "sliceMode", (int)slice.Mode);
            lua_setfield(state, -2, "sliceData");
        }
        else
        {
            lua_pushnil(state);
            lua_setfield(state, -2, "sliceData");
        }
        return 1;
    }

    private static int DispatchTitleIcon(
        LuaRuntime runtime,
        lua_State state,
        string operation)
    {
        var usage = operation switch
        {
            "GetTitleIconTexture" =>
                "Usage: C_Texture.GetTitleIconTexture(titleID, version, callback)",
            "IsTitleIconTextureReady" =>
                "Usage: local ready = C_Texture.IsTitleIconTextureReady(titleID, version)",
            _ =>
                "Usage: C_Texture.SetTitleIconTexture(texture, titleID, version)"
        };

        var stringIndex = operation == "SetTitleIconTexture" ? 2 : 1;
        var versionIndex = stringIndex + 1;
        if ((operation != "SetTitleIconTexture" ||
             TryReadRequiredTexture(runtime, state, 1, out _)) &&
            lua_isstring(state, stringIndex) != 0 &&
            TryReadRequiredInt32(state, versionIndex, out var version) &&
            version is >= 0 and <= 2)
        {
            if (operation == "IsTitleIconTextureReady")
            {
                lua_pushboolean(state, 0);
                return 1;
            }

            if (operation == "GetTitleIconTexture")
            {
                if (lua_isfunction(state, 3) == 0)
                    return luaL_error(state, usage);
                lua_pushvalue(state, 3);
                lua_pushboolean(state, 0);
                lua_pushnumber(state, 0);
                if (lua_pcall(state, 2, 0, 0) != 0)
                    return lua_error(state);
                return 0;
            }

            var target = LuaBindings.GetObject(runtime, 1)!;
            LuaBindings.ClearTextureAsset(target.Texture ??= new UiTextureState());
            return 0;
        }

        return luaL_error(state, usage);
    }

    private static bool TryReadRequiredTexture(
        LuaRuntime runtime,
        lua_State state,
        int index,
        out UiObject target)
    {
        target = LuaBindings.GetObject(runtime, index)!;
        return target is not null &&
               target.ObjectType.Equals("Texture", StringComparison.OrdinalIgnoreCase);
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

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }

        SetEnum(
            state,
            "TitleIconVersion",
            ("Small", 0),
            ("Medium", 1),
            ("Large", 2));
        SetEnumMeta(state, "TitleIconVersionMeta", 3, 0, 2);
        SetEnum(
            state,
            "UrlTextureResult",
            ("Found", 1),
            ("NotFound", 2),
            ("Requested", 3),
            ("NotAllowed", 4));
        SetEnumMeta(state, "UrlTextureResultMeta", 4, 1, 4);
        lua_pop(state, 1);
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params (string Name, int Value)[] values)
    {
        lua_createtable(state, 0, values.Length);
        foreach (var value in values)
            SetNumber(state, value.Name, value.Value);
        lua_setfield(state, -2, name);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int count,
        int minimum,
        int maximum)
    {
        lua_createtable(state, 0, 3);
        SetNumber(state, "NumValues", count);
        SetNumber(state, "MinValue", minimum);
        SetNumber(state, "MaxValue", maximum);
        lua_setfield(state, -2, name);
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }
}
