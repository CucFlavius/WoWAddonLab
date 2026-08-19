using WoWAddonLab.Emulator.Addons;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAddOnApi : LuaApiModule
{
    private const string Insecure = "INSECURE";
    private const string Secure = "SECURE";
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "DisableAddOn", "DisableAllAddOns", "DoesAddOnExist",
                     "DoesAddOnHaveLoadError", "EnableAddOn", "EnableAllAddOns",
                     "GetAddOnDependencies", "GetAddOnEnableState", "GetAddOnInfo",
                     "GetAddOnInterfaceVersion", "GetAddOnLocalTable", "GetAddOnMetadata",
                     "GetAddOnName", "GetAddOnNotes", "GetAddOnOptionalDependencies",
                     "GetAddOnSecurity", "GetAddOnTitle", "GetNumAddOns",
                     "GetScriptsDisallowedForBeta", "IsAddOnDefaultEnabled",
                     "IsAddOnLoadOnDemand", "IsAddOnLoadable", "IsAddOnLoaded",
                     "IsAddonVersionCheckEnabled", "LoadAddOn", "ResetAddOns",
                     "ResetDisabledAddOns", "SaveAddOns", "SetAddonVersionCheck"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_AddOns");

        SetEnum(
            state,
            "AddOnEnableState",
            ("None", 0),
            ("Some", 1),
            ("All", 2));
        SetEnumMeta(state, "AddOnEnableStateMeta", 0, 2, 3);
        SetEnum(
            state,
            "AddOnSecurityStatus",
            ("Secure", 0),
            ("Insecure", 1),
            ("Banned", 2),
            ("NotAvailable", 3));
        SetEnumMeta(state, "AddOnSecurityStatusMeta", 0, 3, 4);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "GetNumAddOns":
                lua_pushnumber(state, runtime.AvailableManifests.Count);
                return 1;
            case "GetScriptsDisallowedForBeta":
                lua_pushboolean(state, 0);
                return 1;
            case "IsAddonVersionCheckEnabled":
                lua_pushboolean(state, runtime.AddonVersionCheckEnabled ? 1 : 0);
                return 1;
            case "SetAddonVersionCheck":
                runtime.AddonVersionCheckEnabled = RequiredBoolean(
                    state,
                    1,
                    "Usage: C_AddOns.SetAddonVersionCheck(enabled)");
                return 0;
            case "ResetAddOns":
                return 0;
            case "ResetDisabledAddOns":
                runtime.ResetDisabledAddonState();
                return 0;
            case "SaveAddOns":
                runtime.SaveAddonEnableState();
                return 0;
            case "DisableAllAddOns":
                OptionalString(
                    state,
                    1,
                    "Usage: C_AddOns.DisableAllAddOns([character])");
                foreach (var catalogEntry in runtime.AvailableManifests)
                {
                    if (!runtime.IsSecureAddon(catalogEntry.Name))
                        runtime.DisabledUserAddons.Add(catalogEntry.Name);
                }
                runtime.InvalidateAddonLoadability();
                return 0;
            case "EnableAllAddOns":
                OptionalString(
                    state,
                    1,
                    "Usage: C_AddOns.EnableAllAddOns([character])");
                foreach (var catalogEntry in runtime.AvailableManifests)
                {
                    if (!IsTrueMetadata(catalogEntry, "LoadFirst"))
                        runtime.DisabledUserAddons.Remove(catalogEntry.Name);
                }
                runtime.InvalidateAddonLoadability();
                return 0;
        }

        var addon = RequiredAddOn(
            runtime,
            state,
            1,
            UsageFor(operation));
        var manifest = addon.Manifest;

        switch (operation)
        {
            case "GetAddOnName":
                PushStringOrNil(state, manifest?.Name);
                return 1;
            case "GetAddOnTitle":
                PushStringOrNil(state, Metadata(manifest, "Title"));
                return 1;
            case "GetAddOnNotes":
                PushStringOrNil(state, Metadata(manifest, "Notes"));
                return 1;
            case "GetAddOnInfo":
            {
                var loadability = GetLoadability(
                    runtime,
                    addon.Name,
                    manifest,
                    demandLoaded: false,
                    []);
                PushStringOrNil(state, manifest?.Name);
                PushStringOrNil(state, Metadata(manifest, "Title"));
                PushStringOrNil(state, Metadata(manifest, "Notes"));
                lua_pushboolean(state, loadability.Loadable ? 1 : 0);
                PushStringOrNil(state, loadability.Reason);
                lua_pushstring(state, SecurityName(runtime, manifest));
                return 6;
            }
            case "GetAddOnInterfaceVersion":
                lua_pushinteger(state, InterfaceVersion(runtime, manifest));
                return 1;
            case "GetAddOnLocalTable":
                return manifest is not null &&
                       runtime.TryPushAddonLocalTable(state, manifest.Name)
                    ? 1
                    : 0;
            case "GetAddOnMetadata":
            {
                var key = RequiredString(
                    state,
                    2,
                    "Usage: local value = C_AddOns.GetAddOnMetadata(name, variable)");
                PushStringOrNil(state, Metadata(manifest, key));
                return 1;
            }
            case "GetAddOnDependencies":
                return PushDependencies(state, manifest, optional: false);
            case "GetAddOnOptionalDependencies":
                return PushDependencies(state, manifest, optional: true);
            case "GetAddOnEnableState":
                OptionalString(
                    state,
                    2,
                    "Usage: local state = C_AddOns.GetAddOnEnableState(name [, character])");
                lua_pushinteger(
                    state,
                    manifest is not null &&
                    !runtime.DisabledUserAddons.Contains(manifest.Name)
                        ? 2
                        : 0);
                return 1;
            case "DoesAddOnExist":
                lua_pushboolean(state, manifest is not null ? 1 : 0);
                return 1;
            case "DoesAddOnHaveLoadError":
                lua_pushboolean(
                    state,
                    manifest is not null &&
                    runtime.AddonLoadErrors.ContainsKey(manifest.Name)
                        ? 1
                        : 0);
                return 1;
            case "IsAddOnDefaultEnabled":
                lua_pushboolean(state, IsDefaultEnabled(manifest) ? 1 : 0);
                return 1;
            case "IsAddOnLoadOnDemand":
                lua_pushboolean(
                    state,
                    IsTrueMetadata(manifest, "LoadOnDemand") ? 1 : 0);
                return 1;
            case "IsAddOnLoadable":
            {
                OptionalString(
                    state,
                    2,
                    "Usage: local loadable, reason = C_AddOns.IsAddOnLoadable(name [, character, demandLoaded])");
                var demandLoaded = OptionalBoolean(
                    state,
                    3,
                    defaultValue: false,
                    "Usage: local loadable, reason = C_AddOns.IsAddOnLoadable(name [, character, demandLoaded])");
                var loadability = GetLoadability(
                    runtime,
                    addon.Name,
                    manifest,
                    demandLoaded,
                    []);
                lua_pushboolean(state, loadability.Loadable ? 1 : 0);
                PushStringOrNil(state, loadability.Reason);
                return 2;
            }
            case "IsAddOnLoaded":
            {
                var loaded = manifest is not null && runtime.IsAddonLoaded(manifest.Name);
                var loadedOrLoading = loaded ||
                                      manifest is not null &&
                                      runtime.IsAddonLoading(manifest.Name);
                lua_pushboolean(state, loadedOrLoading ? 1 : 0);
                lua_pushboolean(state, loaded ? 1 : 0);
                return 2;
            }
            case "GetAddOnSecurity":
                lua_pushinteger(state, SecurityStatus(runtime, manifest));
                return 1;
            case "DisableAddOn":
                OptionalString(
                    state,
                    2,
                    "Usage: C_AddOns.DisableAddOn(name [, character])");
                if (manifest is not null)
                {
                    if (runtime.IsSecureAddon(manifest.Name))
                        return luaL_error(state, "Cannot disable a Secure AddOn");
                    runtime.DisabledUserAddons.Add(manifest.Name);
                    runtime.InvalidateAddonLoadability();
                }
                return 0;
            case "EnableAddOn":
                OptionalString(
                    state,
                    2,
                    "Usage: C_AddOns.EnableAddOn(name [, character])");
                if (manifest is not null && !IsTrueMetadata(manifest, "LoadFirst"))
                {
                    runtime.DisabledUserAddons.Remove(manifest.Name);
                    runtime.InvalidateAddonLoadability();
                }
                return 0;
            case "LoadAddOn":
            {
                var loadability = GetLoadability(
                    runtime,
                    addon.Name,
                    manifest,
                    demandLoaded: true,
                    []);
                if (!loadability.Loadable)
                {
                    lua_pushnil(state);
                    lua_pushstring(state, loadability.Reason ?? "UNKNOWN_ERROR");
                    return 2;
                }

                if (runtime.TryLoadAddon(addon.Name, out var reason))
                {
                    lua_pushboolean(state, 1);
                    lua_pushnil(state);
                    return 2;
                }

                lua_pushnil(state);
                lua_pushstring(state, reason ?? "UNKNOWN_ERROR");
                return 2;
            }
            default:
                return 0;
        }
    }

    private static AddOnResolution RequiredAddOn(
        LuaRuntime runtime,
        lua_State state,
        int index,
        string usage)
    {
        var type = lua_type(state, index);
        if (type == LUA_TNUMBER)
        {
            var addonIndex = (int)lua_tonumber(state, index) - 1;
            var manifest = addonIndex >= 0 &&
                           addonIndex < runtime.AvailableManifests.Count
                ? runtime.AvailableManifests[addonIndex]
                : null;
            return new AddOnResolution(manifest?.Name ?? string.Empty, manifest);
        }

        if (type == LUA_TSTRING)
        {
            var name = lua_tostring(state, index) ?? string.Empty;
            return new AddOnResolution(name, runtime.GetAddonManifest(name));
        }

        luaL_error(state, usage);
        return default;
    }

    private static Loadability GetLoadability(
        LuaRuntime runtime,
        string name,
        AddonManifest? manifest,
        bool demandLoaded,
        HashSet<string> visiting)
    {
        if (runtime.TryGetAddonLoadability(
                name,
                demandLoaded,
                out var cachedLoadable,
                out var cachedReason))
        {
            return new Loadability(cachedLoadable, cachedReason);
        }

        var result = CalculateLoadability(runtime, manifest, demandLoaded, visiting);
        runtime.CacheAddonLoadability(
            name,
            demandLoaded,
            result.Loadable,
            result.Reason);
        return result;
    }

    private static Loadability CalculateLoadability(
        LuaRuntime runtime,
        AddonManifest? manifest,
        bool demandLoaded,
        HashSet<string> visiting)
    {
        if (manifest is null)
            return new Loadability(false, "MISSING");
        if (!visiting.Add(manifest.Name))
            return new Loadability(false, "DEP_MISSING");
        try
        {
            if (runtime.DisabledUserAddons.Contains(manifest.Name))
                return new Loadability(false, "DISABLED");

            var interfaceVersion = InterfaceVersion(runtime, manifest);
            if (runtime.AddonVersionCheckEnabled &&
                interfaceVersion > 0 &&
                interfaceVersion != runtime.InterfaceVersion)
            {
                return new Loadability(false, "INTERFACE_VERSION");
            }

            foreach (var dependencyName in Dependencies(manifest, optional: false))
            {
                var dependency = runtime.GetAddonManifest(dependencyName);
                if (dependency is null)
                    return new Loadability(false, "DEP_MISSING");
                if (runtime.DisabledUserAddons.Contains(dependency.Name))
                    return new Loadability(false, "DEP_DISABLED");

                var dependencyLoadability = GetLoadability(
                    runtime,
                    dependency.Name,
                    dependency,
                    demandLoaded: true,
                    visiting);
                if (!dependencyLoadability.Loadable)
                {
                    return new Loadability(
                        false,
                        DependencyReason(dependencyLoadability.Reason));
                }
            }

            if (!demandLoaded && IsTrueMetadata(manifest, "LoadOnDemand"))
                return new Loadability(false, "DEMAND_LOADED");

            return new Loadability(true, null);
        }
        finally
        {
            visiting.Remove(manifest.Name);
        }
    }

    private static string DependencyReason(string? reason) => reason switch
    {
        "DISABLED" or "DEP_DISABLED" => "DEP_DISABLED",
        "DEMAND_LOADED" => "DEP_DEMAND_LOADED",
        "INTERFACE_VERSION" => "DEP_INTERFACE_VERSION",
        "INSECURE" => "DEP_INSECURE",
        "BANNED" => "DEP_BANNED",
        "NOT_AVAILABLE" => "DEP_NOT_AVAILABLE",
        "CORRUPT" => "DEP_CORRUPT",
        _ => "DEP_MISSING"
    };

    private static int PushDependencies(
        lua_State state,
        AddonManifest? manifest,
        bool optional)
    {
        var values = Dependencies(manifest, optional);
        foreach (var value in values)
            lua_pushstring(state, value);
        return values.Count;
    }

    private static IReadOnlyList<string> Dependencies(
        AddonManifest? manifest,
        bool optional)
    {
        if (manifest is null)
            return [];

        var prefixes = optional
            ? new[] { "OptionalDep", "OptionalDeps", "OptionalDependencies" }
            : new[]
            {
                "Dependencies", "Dep", "RequiredDep", "RequiredDeps",
                "RequiredDependencies"
            };
        return prefixes
            .Where(manifest.Metadata.ContainsKey)
            .SelectMany(key => manifest.Metadata[key].Split(
                [',', ' ', '\t'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int InterfaceVersion(
        LuaRuntime runtime,
        AddonManifest? manifest)
    {
        if (manifest is null)
            return 0;
        if (IsTrueMetadata(manifest, "AlwaysUpToDate"))
            return runtime.InterfaceVersion;
        return int.TryParse(
            Metadata(manifest, "Interface")?.Split(',')[0].Trim(),
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var value)
            ? value
            : 0;
    }

    private static bool IsDefaultEnabled(AddonManifest? manifest)
    {
        if (manifest is null)
            return false;
        var value = RawMetadata(manifest, "DefaultState");
        return value is null ||
               !value.Equals("disabled", StringComparison.OrdinalIgnoreCase) &&
               !value.Equals("false", StringComparison.OrdinalIgnoreCase) &&
               value != "0";
    }

    private static int SecurityStatus(
        LuaRuntime runtime,
        AddonManifest? manifest) =>
        manifest is not null && runtime.IsSecureAddon(manifest.Name) ? 0 : 1;

    private static string SecurityName(
        LuaRuntime runtime,
        AddonManifest? manifest) =>
        SecurityStatus(runtime, manifest) == 0 ? Secure : Insecure;

    private static string? Metadata(AddonManifest? manifest, string key)
    {
        var value = RawMetadata(manifest, key);
        if (value is not null)
            return value;
        return manifest is not null &&
               key.Equals("Title", StringComparison.OrdinalIgnoreCase)
            ? manifest.Name
            : null;
    }

    private static string? RawMetadata(AddonManifest? manifest, string key) =>
        manifest?.Metadata.TryGetValue(key, out var value) == true ? value : null;

    private static bool IsTrueMetadata(AddonManifest? manifest, string key) =>
        RawMetadata(manifest, key) is { } value &&
        (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("true", StringComparison.OrdinalIgnoreCase));

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static string? OptionalString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index || lua_isnil(state, index) != 0)
            return null;
        return RequiredString(state, index, usage);
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TBOOLEAN)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static bool OptionalBoolean(
        lua_State state,
        int index,
        bool defaultValue,
        string usage)
    {
        if (lua_gettop(state) < index || lua_isnil(state, index) != 0)
            return defaultValue;
        return RequiredBoolean(state, index, usage);
    }

    private static void PushStringOrNil(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static string UsageFor(string operation) => operation switch
    {
        "DisableAddOn" => "Usage: C_AddOns.DisableAddOn(name [, character])",
        "DoesAddOnExist" => "Usage: local exists = C_AddOns.DoesAddOnExist(name)",
        "DoesAddOnHaveLoadError" =>
            "Usage: local hadError = C_AddOns.DoesAddOnHaveLoadError(name)",
        "EnableAddOn" => "Usage: C_AddOns.EnableAddOn(name [, character])",
        "GetAddOnDependencies" =>
            "Usage: local (unpackedPrimitiveType)* = C_AddOns.GetAddOnDependencies(name)",
        "GetAddOnEnableState" =>
            "Usage: local state = C_AddOns.GetAddOnEnableState(name [, character])",
        "GetAddOnInfo" =>
            "Usage: local name, title, notes, loadable, reason, security = C_AddOns.GetAddOnInfo(name)",
        "GetAddOnInterfaceVersion" =>
            "Usage: local interfaceVersion = C_AddOns.GetAddOnInterfaceVersion(name)",
        "GetAddOnLocalTable" =>
            "Usage: local table = C_AddOns.GetAddOnLocalTable(name)",
        "GetAddOnMetadata" =>
            "Usage: local value = C_AddOns.GetAddOnMetadata(name, variable)",
        "GetAddOnName" => "Usage: local name = C_AddOns.GetAddOnName(index)",
        "GetAddOnNotes" => "Usage: local notes = C_AddOns.GetAddOnNotes(name)",
        "GetAddOnOptionalDependencies" =>
            "Usage: local (unpackedPrimitiveType)* = C_AddOns.GetAddOnOptionalDependencies(name)",
        "GetAddOnSecurity" =>
            "Usage: local security = C_AddOns.GetAddOnSecurity(name)",
        "GetAddOnTitle" => "Usage: local title = C_AddOns.GetAddOnTitle(name)",
        "IsAddOnDefaultEnabled" =>
            "Usage: local defaultEnabled = C_AddOns.IsAddOnDefaultEnabled(name)",
        "IsAddOnLoadOnDemand" =>
            "Usage: local loadOnDemand = C_AddOns.IsAddOnLoadOnDemand(name)",
        "IsAddOnLoadable" =>
            "Usage: local loadable, reason = C_AddOns.IsAddOnLoadable(name [, character, demandLoaded])",
        "IsAddOnLoaded" =>
            "Usage: local loadedOrLoading, loaded = C_AddOns.IsAddOnLoaded(name)",
        "LoadAddOn" => "Usage: local loaded, value = C_AddOns.LoadAddOn(name)",
        _ => $"Usage: C_AddOns.{operation}(name)"
    };

    private static void SetEnum(
        lua_State state,
        string name,
        params (string Name, int Value)[] fields)
    {
        lua_getglobal(state, "Enum");
        if (lua_type(state, -1) != LUA_TTABLE)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }
        lua_newtable(state);
        foreach (var field in fields)
        {
            lua_pushinteger(state, field.Value);
            lua_setfield(state, -2, field.Name);
        }
        lua_setfield(state, -2, name);
        lua_pop(state, 1);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int minimum,
        int maximum,
        int count)
    {
        SetEnum(
            state,
            name,
            ("NumValues", count),
            ("MinValue", minimum),
            ("MaxValue", maximum));
    }

    private readonly record struct AddOnResolution(
        string Name,
        AddonManifest? Manifest);

    private readonly record struct Loadability(bool Loadable, string? Reason);
}
