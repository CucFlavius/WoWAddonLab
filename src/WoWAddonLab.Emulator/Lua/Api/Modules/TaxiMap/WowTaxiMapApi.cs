using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTaxiMapApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] LegacyFunctions =
    [
        "SetTaxiMap",
        "GetTaxiMapID",
        "NumTaxiNodes",
        "TaxiNodeName",
        "TaxiNodePosition",
        "TaxiNodeCost",
        "TakeTaxiNode",
        "CloseTaxiMap",
        "TaxiNodeGetType",
        "TaxiGetNodeSlot",
        "TaxiGetSrcX",
        "TaxiGetSrcY",
        "TaxiGetDestX",
        "TaxiGetDestY",
        "GetNumRoutes",
        "TaxiIsDirectFlight"
    ];

    private static readonly string[] NamespaceFunctions =
    [
        "GetAllTaxiNodes",
        "GetTaxiNodesForMap",
        "ShouldMapShowTaxiNodes"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in LegacyFunctions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);

        lua_newtable(state);
        foreach (var function in NamespaceFunctions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_TaxiMap");
        RegisterEnums(state);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        return operation switch
        {
            "SetTaxiMap" => SetTaxiMap(runtime),
            "GetTaxiMapID" => GetTaxiMapId(state, runtime.TaxiMap),
            "NumTaxiNodes" => PushNumber(state, runtime.TaxiMap.Nodes.Count),
            "TaxiNodeName" => TaxiNodeName(state, runtime.TaxiMap),
            "TaxiNodePosition" => TaxiNodePosition(state, runtime.TaxiMap),
            "TaxiNodeCost" => TaxiNodeCost(state, runtime.TaxiMap),
            "TakeTaxiNode" => TakeTaxiNode(state, runtime.TaxiMap),
            "CloseTaxiMap" => CloseTaxiMap(runtime),
            "TaxiNodeGetType" => TaxiNodeGetType(state, runtime.TaxiMap),
            "TaxiGetNodeSlot" => TaxiGetNodeSlot(state, runtime.TaxiMap),
            "TaxiGetSrcX" => TaxiGetRouteCoordinate(
                state,
                runtime.TaxiMap,
                sourceIndex: true,
                xCoordinate: true),
            "TaxiGetSrcY" => TaxiGetRouteCoordinate(
                state,
                runtime.TaxiMap,
                sourceIndex: true,
                xCoordinate: false),
            "TaxiGetDestX" => TaxiGetRouteCoordinate(
                state,
                runtime.TaxiMap,
                sourceIndex: false,
                xCoordinate: true),
            "TaxiGetDestY" => TaxiGetRouteCoordinate(
                state,
                runtime.TaxiMap,
                sourceIndex: false,
                xCoordinate: false),
            "GetNumRoutes" => GetNumRoutes(state, runtime.TaxiMap),
            "TaxiIsDirectFlight" => TaxiIsDirectFlight(
                state,
                runtime.TaxiMap),
            "GetAllTaxiNodes" => GetAllTaxiNodes(
                state,
                runtime.TaxiMap),
            "GetTaxiNodesForMap" => GetTaxiNodesForMap(
                state,
                runtime.TaxiMap),
            "ShouldMapShowTaxiNodes" => ShouldMapShowTaxiNodes(
                state,
                runtime.TaxiMap),
            _ => 0
        };
    }

    private static int SetTaxiMap(LuaRuntime runtime)
    {
        if (runtime.TaxiMap.TaxiSystemAvailable)
            runtime.TaxiMap.SetTaxiMapRequests++;
        return 0;
    }

    private static int GetTaxiMapId(
        lua_State state,
        WowTaxiMapState taxiMap)
    {
        if (taxiMap.ActiveMapId is not { } mapId || mapId == 0)
            return 0;
        lua_pushinteger(state, mapId);
        return 1;
    }

    private static int TaxiNodeName(
        lua_State state,
        WowTaxiMapState taxiMap)
    {
        const string usage = "Usage: TaxiNodeName(slot)";
        var slot = RequiredLegacyInteger(state, 1, usage);
        var node = NodeAt(taxiMap, slot);
        lua_pushstring(state, node?.Name ?? "INVALID");
        return 1;
    }

    private static int TaxiNodePosition(
        lua_State state,
        WowTaxiMapState taxiMap)
    {
        const string usage = "Usage: TaxiNodePosition(slot)";
        var slot = RequiredLegacyInteger(state, 1, usage);
        var node = NodeAt(taxiMap, slot);
        if (node is null)
            return luaL_error(state, "Invalid taxi node slot");
        lua_pushnumber(state, (float)node.X);
        lua_pushnumber(state, (float)node.Y);
        return 2;
    }

    private static int TaxiNodeCost(
        lua_State state,
        WowTaxiMapState taxiMap)
    {
        const string usage = "Usage: TaxiNodeCost(slot)";
        var slot = RequiredLegacyInteger(state, 1, usage);
        var node = NodeAt(taxiMap, slot);
        if (node is null)
            return luaL_error(state, "Invalid taxi node slot");
        lua_pushnumber(state, node.Cost);
        return 1;
    }

    private static int TakeTaxiNode(
        lua_State state,
        WowTaxiMapState taxiMap)
    {
        const string usage = "Usage: TakeTaxiNode(slot)";
        var slot = RequiredLegacyInteger(state, 1, usage);
        taxiMap.TakeTaxiNodeRequests++;
        taxiMap.LastTakenTaxiSlot = slot;
        return 0;
    }

    private static int CloseTaxiMap(LuaRuntime runtime)
    {
        runtime.TaxiMap.CloseTaxiMapRequests++;

        const int taxiInteractionType = 6;
        var interactions = runtime.PlayerInteractions;
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = taxiInteractionType;
        if (interactions.HasActiveInteraction &&
            interactions.CurrentInteractionType == taxiInteractionType)
        {
            interactions.HasActiveInteraction = false;
            interactions.HasPendingInteraction = false;
            interactions.CurrentInteractionType = 0;
            interactions.PendingInteractionType = 0;
            interactions.ValidNpcInteractionTypes.Clear();
        }
        return 0;
    }

    private static int TaxiNodeGetType(
        lua_State state,
        WowTaxiMapState taxiMap)
    {
        const string usage = "Usage: TaxiNodeGetType(slot)";
        var slot = RequiredLegacyInteger(state, 1, usage);
        lua_pushstring(state, NodeAt(taxiMap, slot)?.Type ?? "NONE");
        return 1;
    }

    private static int TaxiGetNodeSlot(
        lua_State state,
        WowTaxiMapState taxiMap)
    {
        var nodeSlot = RequiredLegacyInteger(state, 1, string.Empty);
        var routeIndex = RequiredLegacyInteger(state, 2, string.Empty);
        if (lua_toboolean(state, 3) != 0)
            routeIndex--;

        var result = 1;
        var route = NodeAt(taxiMap, nodeSlot)?.RouteSlots;
        if (route is not null &&
            routeIndex >= 0 &&
            routeIndex < route.Count)
        {
            result = route[routeIndex];
        }

        lua_pushnumber(state, result);
        return 1;
    }

    private static int GetNumRoutes(
        lua_State state,
        WowTaxiMapState taxiMap)
    {
        var nodeSlot = RequiredLegacyInteger(state, 1, string.Empty);
        var route = NodeAt(taxiMap, nodeSlot)?.RouteSlots;
        lua_pushnumber(state, Math.Max((route?.Count ?? 0) - 1, 0));
        return 1;
    }

    private static int TaxiIsDirectFlight(
        lua_State state,
        WowTaxiMapState taxiMap)
    {
        var nodeSlot = RequiredLegacyInteger(state, 1, string.Empty);
        var node = NodeAt(taxiMap, nodeSlot);
        lua_pushboolean(
            state,
            node is { RouteCalculationAttempted: true } &&
            node.RouteSlots.Count == 2
                ? 1
                : 0);
        return 1;
    }

    private static int TaxiGetRouteCoordinate(
        lua_State state,
        WowTaxiMapState taxiMap,
        bool sourceIndex,
        bool xCoordinate)
    {
        var nodeSlot = RequiredLegacyInteger(state, 1, string.Empty);
        var routeIndex = RequiredLegacyInteger(state, 2, string.Empty);
        if (sourceIndex)
            routeIndex--;

        var route = NodeAt(taxiMap, nodeSlot)?.RouteSlots;
        WowLegacyTaxiNode? routeNode = null;
        if (route is not null &&
            routeIndex >= 0 &&
            routeIndex < route.Count)
        {
            routeNode = NodeAt(taxiMap, route[routeIndex]);
        }

        lua_pushnumber(
            state,
            routeNode is null
                ? 0
                : (float)(xCoordinate ? routeNode.X : routeNode.Y));
        return 1;
    }

    private static int GetAllTaxiNodes(
        lua_State state,
        WowTaxiMapState taxiMap)
    {
        const string usage =
            "Usage: local taxiNodes = C_TaxiMap.GetAllTaxiNodes(uiMapID)";
        var mapId = RequiredInt32(state, 1, usage);
        taxiMap.AllNodesByMap.TryGetValue(mapId, out var nodes);
        nodes ??= [];

        lua_createtable(state, nodes.Count, 0);
        for (var index = 0; index < nodes.Count; index++)
        {
            PushAllNode(state, nodes[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetTaxiNodesForMap(
        lua_State state,
        WowTaxiMapState taxiMap)
    {
        const string usage =
            "Usage: local mapTaxiNodes = C_TaxiMap.GetTaxiNodesForMap(uiMapID)";
        var mapId = RequiredInt32(state, 1, usage);
        taxiMap.MapNodesByMap.TryGetValue(mapId, out var nodes);
        nodes ??= [];

        lua_createtable(state, nodes.Count, 0);
        for (var index = 0; index < nodes.Count; index++)
        {
            PushMapNode(state, nodes[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int ShouldMapShowTaxiNodes(
        lua_State state,
        WowTaxiMapState taxiMap)
    {
        const string usage =
            "Usage: local shouldShowNodes = C_TaxiMap.ShouldMapShowTaxiNodes(uiMapID)";
        var mapId = RequiredInt32(state, 1, usage);
        lua_pushboolean(
            state,
            taxiMap.MapsShowingTaxiNodes.Contains(mapId) ? 1 : 0);
        return 1;
    }

    private static void PushAllNode(
        lua_State state,
        WowTaxiMapAllNode node)
    {
        lua_createtable(state, 0, 9);
        SetInteger(state, "nodeID", node.NodeId);
        PushVector2(state, node.X, node.Y);
        lua_setfield(state, -2, "position");
        SetOptionalString(state, "name", node.Name);
        SetUnsignedInteger(state, "state", node.State);
        SetInteger(state, "slotIndex", node.SlotIndex);
        SetOptionalString(state, "textureKit", node.TextureKit);
        SetBoolean(state, "useSpecialIcon", node.UseSpecialIcon);
        SetOptionalString(
            state,
            "specialIconCostString",
            node.SpecialIconCostString);
        SetBoolean(
            state,
            "isMapLayerTransition",
            node.IsMapLayerTransition);
    }

    private static void PushMapNode(
        lua_State state,
        WowTaxiMapNode node)
    {
        lua_createtable(state, 0, 7);
        SetInteger(state, "nodeID", node.NodeId);
        PushVector2(state, node.X, node.Y);
        lua_setfield(state, -2, "position");
        SetOptionalString(state, "name", node.Name);
        SetOptionalString(state, "atlasName", node.AtlasName);
        SetUnsignedInteger(state, "faction", node.Faction);
        SetOptionalString(state, "textureKit", node.TextureKit);
        SetBoolean(state, "isUndiscovered", node.IsUndiscovered);
    }

    private static WowLegacyTaxiNode? NodeAt(
        WowTaxiMapState taxiMap,
        int oneBasedSlot) =>
        oneBasedSlot >= 1 && oneBasedSlot <= taxiMap.Nodes.Count
            ? taxiMap.Nodes[oneBasedSlot - 1]
            : null;

    private static int RequiredLegacyInteger(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        return unchecked((int)lua_tonumber(state, index));
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return unchecked((int)value);
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
            "FlightPathFaction",
            [("Neutral", 0), ("Horde", 1), ("Alliance", 2)]);
        SetEnumMeta(state, "FlightPathFactionMeta");
        SetEnum(
            state,
            "FlightPathState",
            [("Current", 0), ("Reachable", 1), ("Unreachable", 2)]);
        SetEnumMeta(state, "FlightPathStateMeta");
        lua_pop(state, 1);
    }

    private static void SetEnum(
        lua_State state,
        string name,
        IEnumerable<(string Name, int Value)> values)
    {
        var entries = values.ToArray();
        lua_createtable(state, 0, entries.Length);
        foreach (var entry in entries)
            SetInteger(state, entry.Name, entry.Value);
        lua_setfield(state, -2, name);
    }

    private static void SetEnumMeta(lua_State state, string name)
    {
        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", 3);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 2);
        lua_setfield(state, -2, name);
    }

    private static void PushVector2(
        lua_State state,
        double x,
        double y)
    {
        lua_createtable(state, 0, 2);
        SetNumber(state, "x", x);
        SetNumber(state, "y", y);

        var target = lua_gettop(state);
        lua_getglobal(state, "Vector2DMixin");
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

    private static int PushNumber(lua_State state, double value)
    {
        lua_pushnumber(state, value);
        return 1;
    }

    private static void SetInteger(
        lua_State state,
        string field,
        int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetUnsignedInteger(
        lua_State state,
        string field,
        uint value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetNumber(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(
        lua_State state,
        string field,
        bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is not null)
        {
            lua_pushstring(state, value);
            lua_setfield(state, -2, field);
        }
    }
}
