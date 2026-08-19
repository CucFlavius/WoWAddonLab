using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCommentatorApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "AreTeamsSwapped",
                     "CanUseCommentatorCheats",
                     "ExitInstance",
                     "FindTeamNameInCurrentInstance",
                     "FollowPlayer",
                     "GetCombatEventInfo",
                     "GetCommentatorHistory",
                     "GetDampeningPercent",
                     "GetIndirectSpellID",
                     "GetMatchDuration",
                     "GetNumPlayers",
                     "GetOrCreateSeries",
                     "GetPlayerAuraInfoByUnit",
                     "GetPlayerCooldownInfoByUnit",
                     "GetPlayerCrowdControlInfoByUnit",
                     "GetPlayerData",
                     "GetPlayerFlagInfoByUnit",
                     "GetPlayerOverrideName",
                     "GetPlayerSpellChargesByUnit",
                     "GetStartLocation",
                     "GetTeamColor",
                     "GetTeamColorByUnit",
                     "GetTimeLeftInMatch",
                     "GetTrackedSpellID",
                     "GetTrackedSpellsByUnit",
                     "GetUnitData",
                     "HasTrackedAuras",
                     "IsSpectating",
                     "IsTrackedSpellByUnit",
                     "IsUsingSmartCamera",
                     "LookAtPlayer",
                     "ResetFoVTarget",
                     "ResetSeriesScores",
                     "SetAdditionalCameraWeightByToken",
                     "SetCameraPosition",
                     "SetCommentatorHistory",
                     "SetFollowCameraSpeeds",
                     "SetMouseDisabled",
                     "SetMoveSpeed",
                     "SetSeriesScore",
                     "SetSmartCameraLocked",
                     "SetSpeedFactor",
                     "SetUseSmartCamera",
                     "SnapCameraLookAtPoint",
                     "SpellUsesItemCharges",
                     "SwapTeamSides"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Commentator");
    }

    private static int Dispatch(lua_State state)
    {
        var commentator = LuaBindings.GetRuntime(state).Commentator;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "IsSpectating":
                lua_pushboolean(state, commentator.IsSpectating ? 1 : 0);
                return 1;
            case "AreTeamsSwapped":
                lua_pushboolean(state, commentator.TeamsSwapped ? 1 : 0);
                return 1;
            case "CanUseCommentatorCheats":
                lua_pushboolean(state, commentator.CanUseCommentatorCheats ? 1 : 0);
                return 1;
            case "FindTeamNameInCurrentInstance":
            {
                const string usage =
                    "Usage: local teamName = " +
                    "C_Commentator.FindTeamNameInCurrentInstance(teamIndex)";
                var teamIndex = RequiredOneBasedUInt32(state, 1, usage);
                if (FindCurrentTeamName(commentator, teamIndex) is { } teamName)
                {
                    lua_pushstring(state, teamName);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "GetCombatEventInfo":
                return 0;
            case "GetDampeningPercent":
                lua_pushinteger(state, commentator.DampeningPercent);
                return 1;
            case "GetMatchDuration":
                lua_pushnumber(state, commentator.MatchDuration);
                return 1;
            case "GetPlayerFlagInfoByUnit":
            {
                const string usage =
                    "Usage: local hasFlag = " +
                    "C_Commentator.GetPlayerFlagInfoByUnit(unitToken)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                lua_pushboolean(state, commentator.FlaggedUnits.Contains(unitToken) ? 1 : 0);
                return 1;
            }
            case "GetIndirectSpellID":
            {
                const string usage =
                    "Usage: local indirectSpellID = C_Commentator.GetIndirectSpellID(trackedSpellID)";
                var trackedSpellId = RequiredInt32(state, 1, usage);
                lua_pushinteger(
                    state,
                    commentator.IndirectSpellIdsByTrackedSpellId.TryGetValue(
                        trackedSpellId,
                        out var indirectSpellId)
                            ? indirectSpellId
                            : trackedSpellId);
                return 1;
            }
            case "GetTrackedSpellID":
            {
                const string usage =
                    "Usage: local trackedSpellID = C_Commentator.GetTrackedSpellID(indirectSpellID)";
                var indirectSpellId = RequiredInt32(state, 1, usage);
                var trackedSpellId = indirectSpellId;
                foreach (var mapping in commentator.IndirectSpellIdsByTrackedSpellId)
                {
                    if (mapping.Value != indirectSpellId)
                        continue;
                    trackedSpellId = mapping.Key;
                    break;
                }
                lua_pushinteger(state, trackedSpellId);
                return 1;
            }
            case "GetNumPlayers":
            {
                const string usage =
                    "Usage: local numPlayers = C_Commentator.GetNumPlayers(factionIndex)";
                var factionIndex = RequiredOneBasedIndex(state, 1, usage);
                if (factionIndex > 2)
                    return 0;
                lua_pushinteger(state, commentator.TeamPlayerCounts[factionIndex - 1]);
                return 1;
            }
            case "GetTimeLeftInMatch":
                if (commentator.TimeLeftInMatch is { } timeLeft)
                    lua_pushnumber(state, timeLeft);
                else
                    lua_pushnil(state);
                return 1;
            case "GetCommentatorHistory":
                PushCommentatorHistory(state, commentator);
                return 1;
            case "GetOrCreateSeries":
            {
                const string usage =
                    "Usage: local data = C_Commentator.GetOrCreateSeries(teamName1, teamName2)";
                var firstTeamName = RequiredString(state, 1, usage);
                var secondTeamName = RequiredString(state, 2, usage);
                var series = commentator.GetOrCreateSeries(firstTeamName, secondTeamName);
                lua_newtable(state);
                lua_newtable(state);
                PushSeriesTeam(state, 1, series.Teams[0]);
                PushSeriesTeam(state, 2, series.Teams[1]);
                lua_setfield(state, -2, "teams");
                return 1;
            }
            case "GetTrackedSpellsByUnit":
            {
                const string usage =
                    "Usage: local spells, result = " +
                    "C_Commentator.GetTrackedSpellsByUnit(unitToken, category)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                var category = RequiredTrackedSpellCategory(state, 2, usage);
                if (!commentator.TrackedSpellsByUnit.TryGetValue(
                        WowCommentatorTrackedSpellKey.Create(unitToken, category),
                        out var tracked))
                {
                    lua_pushnil(state);
                    lua_pushinteger(state, (int)WowTrackedSpellsResult.PlayerNotFound);
                    return 2;
                }

                if (tracked.SpellIds is { } spellIds)
                {
                    lua_createtable(state, spellIds.Count, 0);
                    for (var index = 0; index < spellIds.Count; index++)
                    {
                        lua_pushinteger(state, spellIds[index]);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                else
                {
                    lua_pushnil(state);
                }
                lua_pushinteger(state, (int)tracked.Result);
                return 2;
            }
            case "HasTrackedAuras":
            {
                const string usage =
                    "Usage: local hasOffensiveAura, hasDefensiveAura = " +
                    "C_Commentator.HasTrackedAuras(token)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                commentator.TrackedAurasByUnit.TryGetValue(unitToken, out var tracked);
                lua_pushboolean(state, tracked?.HasOffensiveAura == true ? 1 : 0);
                lua_pushboolean(state, tracked?.HasDefensiveAura == true ? 1 : 0);
                return 2;
            }
            case "IsTrackedSpellByUnit":
            {
                const string usage =
                    "Usage: local isTracked = " +
                    "C_Commentator.IsTrackedSpellByUnit(unitToken, spellID, category)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                var spellId = RequiredInt32(state, 2, usage);
                var category = RequiredTrackedSpellCategory(state, 3, usage);
                var isTracked =
                    commentator.TrackedSpellsByUnit.TryGetValue(
                        WowCommentatorTrackedSpellKey.Create(unitToken, category),
                        out var tracked) &&
                    tracked.SpellIds?.Contains(spellId) == true;
                lua_pushboolean(state, isTracked ? 1 : 0);
                return 1;
            }
            case "IsUsingSmartCamera":
                lua_pushboolean(state, commentator.IsUsingSmartCamera ? 1 : 0);
                return 1;
            case "SpellUsesItemCharges":
            {
                const string usage =
                    "Usage: local spellUsesItemCharges = " +
                    "C_Commentator.SpellUsesItemCharges(spellID)";
                var spellId = RequiredInt32(state, 1, usage);
                lua_pushboolean(state, spellId is 6262 or 452930 ? 1 : 0);
                return 1;
            }
            case "GetPlayerOverrideName":
            {
                const string usage =
                    "Usage: local overrideName = " +
                    "C_Commentator.GetPlayerOverrideName(originalName)";
                var name = RequiredString(state, 1, usage);
                if (commentator.PlayerOverrideNames.TryGetValue(name, out var value))
                {
                    lua_pushstring(state, value);
                    return 1;
                }
                return 0;
            }
            case "GetPlayerAuraInfoByUnit":
            {
                const string usage =
                    "Usage: local startTime, duration, enable = " +
                    "C_Commentator.GetPlayerAuraInfoByUnit(token, spellID)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                var spellId = RequiredInt32(state, 2, usage);
                if (ResolveTeamIndexByUnit(commentator, unitToken) is null ||
                    !commentator.AurasByUnitAndSpell.TryGetValue(
                        WowCommentatorUnitSpellKey.Create(unitToken, spellId),
                        out var aura))
                {
                    return 0;
                }
                return PushTimedEffect(state, aura);
            }
            case "GetPlayerCooldownInfoByUnit":
            {
                const string usage =
                    "Usage: local startTime, duration, enable = " +
                    "C_Commentator.GetPlayerCooldownInfoByUnit(unitToken, spellID)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                var spellId = RequiredInt32(state, 2, usage);
                if (ResolveTeamIndexByUnit(commentator, unitToken) is null)
                    return 0;
                if (!commentator.CooldownsByUnitAndSpell.TryGetValue(
                        WowCommentatorUnitSpellKey.Create(unitToken, spellId),
                        out var cooldown))
                {
                    return PushTimedEffect(
                        state,
                        new WowCommentatorTimedEffectState(0, 0, false));
                }
                return PushTimedEffect(state, cooldown);
            }
            case "GetPlayerCrowdControlInfoByUnit":
            {
                const string usage =
                    "Usage: local spellID, expiration, duration = " +
                    "C_Commentator.GetPlayerCrowdControlInfoByUnit(token)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                if (!commentator.CrowdControlByUnit.TryGetValue(
                        unitToken,
                        out var crowdControl))
                {
                    return 0;
                }
                lua_pushinteger(state, crowdControl.SpellId);
                lua_pushnumber(state, crowdControl.Expiration);
                lua_pushnumber(state, crowdControl.Duration);
                return 3;
            }
            case "GetPlayerData":
            {
                const string usage =
                    "Usage: local info = " +
                    "C_Commentator.GetPlayerData(teamIndex, playerIndex)";
                var teamIndex = RequiredOneBasedUInt32(state, 1, usage);
                var playerIndex = RequiredOneBasedUInt32(state, 2, usage);
                if (commentator.PlayersByPosition.TryGetValue(
                        new WowCommentatorPlayerKey(teamIndex, playerIndex),
                        out var player))
                {
                    PushPlayerData(state, player);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "GetPlayerSpellChargesByUnit":
            {
                const string usage =
                    "Usage: local charges, maxCharges, startTime, duration = " +
                    "C_Commentator.GetPlayerSpellChargesByUnit(unitToken, spellID)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                var spellId = RequiredInt32(state, 2, usage);
                if (ResolveTeamIndexByUnit(commentator, unitToken) is null ||
                    !commentator.SpellChargesByUnitAndSpell.TryGetValue(
                        WowCommentatorUnitSpellKey.Create(unitToken, spellId),
                        out var charges))
                {
                    return 0;
                }
                lua_pushinteger(state, charges.Charges);
                lua_pushinteger(state, charges.MaxCharges);
                lua_pushnumber(state, charges.StartTime);
                lua_pushnumber(state, charges.Duration);
                return 4;
            }
            case "GetStartLocation":
            {
                const string usage =
                    "Usage: local pos = C_Commentator.GetStartLocation(mapID)";
                var mapId = RequiredInt32(state, 1, usage);
                if (!commentator.StartLocationsByMapId.TryGetValue(mapId, out var position))
                    return 0;
                PushVector3Mixin(state, position);
                return 1;
            }
            case "GetTeamColor":
            {
                const string usage =
                    "Usage: local color = C_Commentator.GetTeamColor(teamIndex)";
                var teamIndex = RequiredOneBasedUInt32(state, 1, usage);
                var colorIndex = teamIndex == 1 ? 0 : 1;
                PushColorMixin(state, commentator.TeamColors[colorIndex]);
                return 1;
            }
            case "GetTeamColorByUnit":
            {
                const string usage =
                    "Usage: local color = C_Commentator.GetTeamColorByUnit(unitToken)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                var color = ResolveTeamIndexByUnit(commentator, unitToken) is { } teamIndex
                            ? commentator.TeamColors[teamIndex == 1 ? 0 : 1]
                            : default;
                PushColorMixin(state, color);
                return 1;
            }
            case "GetUnitData":
            {
                const string usage =
                    "Usage: local data = C_Commentator.GetUnitData(unitToken)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                commentator.UnitDataByUnit.TryGetValue(unitToken, out var unitData);
                PushUnitData(state, unitData ?? new WowCommentatorUnitDataState());
                return 1;
            }
            case "ExitInstance":
                if (commentator.CanUseCommentatorCheats)
                    commentator.ExitInstanceRequested = true;
                return 0;
            case "FollowPlayer":
            {
                const string usage =
                    "Usage: C_Commentator.FollowPlayer(" +
                    "factionIndex, playerIndex [, forceInstantTransition])";
                var factionIndex = RequiredOneBasedUInt32(state, 1, usage);
                var playerIndex = RequiredOneBasedUInt32(state, 2, usage);
                var forceInstantTransition = OptionalTruthyBoolean(state, 3) == true;
                if (IsRepresentedPlayer(commentator, factionIndex, playerIndex))
                {
                    commentator.FollowRequest = new WowCommentatorFollowRequest(
                        factionIndex,
                        playerIndex,
                        forceInstantTransition);
                }
                return 0;
            }
            case "LookAtPlayer":
            {
                const string usage =
                    "Usage: C_Commentator.LookAtPlayer(" +
                    "factionIndex, playerIndex [, lookAtIndex])";
                var factionIndex = RequiredOneBasedUInt32(state, 1, usage);
                var playerIndex = RequiredOneBasedUInt32(state, 2, usage);
                var lookAtIndex = OptionalOneBasedUInt32(state, 3, usage);
                if (lookAtIndex is > 2)
                    return 0;
                if (IsRepresentedPlayer(commentator, factionIndex, playerIndex))
                {
                    commentator.LookAtRequest = new WowCommentatorLookAtRequest(
                        factionIndex,
                        playerIndex,
                        lookAtIndex);
                }
                return 0;
            }
            case "ResetFoVTarget":
                commentator.FieldOfViewTarget = MathF.PI / 2;
                return 0;
            case "ResetSeriesScores":
            {
                const string usage =
                    "Usage: C_Commentator.ResetSeriesScores(teamName1, teamName2)";
                var firstTeamName = RequiredString(state, 1, usage);
                var secondTeamName = RequiredString(state, 2, usage);
                var series = commentator.GetOrCreateSeries(firstTeamName, secondTeamName);
                series.Teams[0].Score = 0;
                series.Teams[1].Score = 0;
                return 0;
            }
            case "SetAdditionalCameraWeightByToken":
            {
                const string usage =
                    "Usage: C_Commentator.SetAdditionalCameraWeightByToken(unitToken, weight)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                var weight = RequiredFloat(state, 2, usage);
                if (ResolveTeamIndexByUnit(commentator, unitToken) is not null)
                    commentator.AdditionalCameraWeightsByUnit[unitToken] = weight;
                return 0;
            }
            case "SetCameraPosition":
            {
                const string usage =
                    "Usage: C_Commentator.SetCameraPosition(" +
                    "xPos, yPos, zPos, snapToLocation)";
                var position = new WowCommentatorVector3(
                    RequiredFloat(state, 1, usage),
                    RequiredFloat(state, 2, usage),
                    RequiredFloat(state, 3, usage));
                var snapToLocation = RequiredTruthyBoolean(state, 4, usage);
                commentator.CameraTargetPosition = position;
                if (!commentator.IsUsingSmartCamera || snapToLocation)
                    commentator.CameraPosition = position;
                return 0;
            }
            case "SetCommentatorHistory":
            {
                const string usage =
                    "Usage: C_Commentator.SetCommentatorHistory(history)";
                var history = ReadCommentatorHistory(state, 1, usage);
                commentator.Series.Clear();
                foreach (var parsedSeries in history.Series)
                {
                    var firstName = parsedSeries.Teams.Count > 0
                        ? parsedSeries.Teams[0].Name
                        : string.Empty;
                    var secondName = parsedSeries.Teams.Count > 1
                        ? parsedSeries.Teams[1].Name
                        : string.Empty;
                    var series = commentator.GetOrCreateSeries(firstName, secondName);
                    foreach (var parsedTeam in parsedSeries.Teams)
                    {
                        var team = series.Teams.FirstOrDefault(
                            value => value.Name.Equals(
                                parsedTeam.Name,
                                StringComparison.OrdinalIgnoreCase));
                        if (team is not null)
                            team.Score = parsedTeam.Score;
                    }
                }
                commentator.TeamDirectory.Clear();
                foreach (var entry in history.TeamDirectory)
                    commentator.TeamDirectory.Add(entry);
                commentator.PlayerOverrideNames.Clear();
                foreach (var entry in history.OverrideNames)
                {
                    commentator.PlayerOverrideNames[entry.OriginalName] =
                        entry.NewName;
                }
                return 0;
            }
            case "SetFollowCameraSpeeds":
            {
                const string usage =
                    "Usage: C_Commentator.SetFollowCameraSpeeds(elasticSpeed, minSpeed)";
                commentator.FollowCameraElasticSpeed = RequiredFloat(state, 1, usage);
                commentator.FollowCameraMinimumSpeed = RequiredFloat(state, 2, usage);
                return 0;
            }
            case "SetMouseDisabled":
                commentator.IsMouseDisabled = RequiredTruthyBoolean(
                    state,
                    1,
                    "Usage: C_Commentator.SetMouseDisabled(disabled)");
                return 0;
            case "SetMoveSpeed":
                commentator.MoveSpeed = Math.Clamp(
                    RequiredFloat(
                        state,
                        1,
                        "Usage: C_Commentator.SetMoveSpeed(newSpeed)"),
                    0,
                    40);
                return 0;
            case "SetSeriesScore":
            {
                const string usage =
                    "Usage: C_Commentator.SetSeriesScore(" +
                    "teamName1, teamName2, scoringTeamName, score)";
                var firstTeamName = RequiredString(state, 1, usage);
                var secondTeamName = RequiredString(state, 2, usage);
                var scoringTeamName = RequiredString(state, 3, usage);
                var score = RequiredUInt32(state, 4, usage);
                var series = commentator.GetOrCreateSeries(firstTeamName, secondTeamName);
                var team = series.Teams.FirstOrDefault(
                    value => value.Name.Equals(
                        scoringTeamName,
                        StringComparison.OrdinalIgnoreCase));
                if (team is not null)
                    team.Score = score;
                return 0;
            }
            case "SetSmartCameraLocked":
                commentator.IsSmartCameraLocked = RequiredTruthyBoolean(
                    state,
                    1,
                    "Usage: C_Commentator.SetSmartCameraLocked(locked)");
                return 0;
            case "SetSpeedFactor":
                commentator.SpeedFactor = Math.Max(
                    RequiredFloat(
                        state,
                        1,
                        "Usage: C_Commentator.SetSpeedFactor(factor)"),
                    0);
                return 0;
            case "SetUseSmartCamera":
            {
                var useSmartCamera = RequiredTruthyBoolean(
                    state,
                    1,
                    "Usage: C_Commentator.SetUseSmartCamera(useSmartCamera)");
                if (commentator.IsUsingSmartCamera != useSmartCamera)
                {
                    commentator.IsUsingSmartCamera = useSmartCamera;
                    if (useSmartCamera)
                        commentator.IsSmartCameraLocked = false;
                }
                return 0;
            }
            case "SnapCameraLookAtPoint":
                commentator.CameraLookAtPointSnapped = true;
                return 0;
            case "SwapTeamSides":
                commentator.TeamsSwapped = !commentator.TeamsSwapped;
                LuaBindings.GetRuntime(state).TriggerEvent(
                    "COMMENTATOR_TEAMS_SWAPPED",
                    commentator.TeamsSwapped);
                return 0;
            default:
                return 0;
        }
    }

    private static void PushSeriesTeam(
        lua_State state,
        int index,
        WowCommentatorSeriesTeamState team)
    {
        lua_newtable(state);
        lua_pushstring(state, team.Name);
        lua_setfield(state, -2, "name");
        lua_pushinteger(state, team.Score);
        lua_setfield(state, -2, "score");
        lua_rawseti(state, -2, index);
    }

    private static void PushCommentatorHistory(
        lua_State state,
        WowCommentatorState commentator)
    {
        lua_newtable(state);
        var historyIndex = AbsoluteIndex(state, -1);

        lua_createtable(state, commentator.Series.Count, 0);
        for (var seriesIndex = 0; seriesIndex < commentator.Series.Count; seriesIndex++)
        {
            var series = commentator.Series[seriesIndex];
            lua_newtable(state);
            lua_createtable(state, series.Teams.Count, 0);
            for (var teamIndex = 0; teamIndex < series.Teams.Count; teamIndex++)
                PushSeriesTeam(state, teamIndex + 1, series.Teams[teamIndex]);
            lua_setfield(state, -2, "teams");
            lua_rawseti(state, -2, seriesIndex + 1);
        }
        lua_setfield(state, historyIndex, "series");

        lua_createtable(state, commentator.TeamDirectory.Count, 0);
        for (var index = 0; index < commentator.TeamDirectory.Count; index++)
        {
            var entry = commentator.TeamDirectory[index];
            lua_newtable(state);
            SetStringField(state, "playerName", entry.PlayerName);
            SetStringField(state, "teamName", entry.TeamName);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, historyIndex, "teamDirectory");

        lua_createtable(state, commentator.PlayerOverrideNames.Count, 0);
        var overrideIndex = 1;
        foreach (var entry in commentator.PlayerOverrideNames)
        {
            lua_newtable(state);
            SetStringField(state, "originalName", entry.Key);
            SetStringField(state, "newName", entry.Value);
            lua_rawseti(state, -2, overrideIndex++);
        }
        lua_setfield(state, historyIndex, "overrideNameDirectory");
    }

    private static int PushTimedEffect(
        lua_State state,
        WowCommentatorTimedEffectState effect)
    {
        lua_pushnumber(state, effect.StartTime);
        lua_pushnumber(state, effect.Duration);
        lua_pushboolean(state, effect.Enabled ? 1 : 0);
        return 3;
    }

    private static void PushPlayerData(
        lua_State state,
        WowCommentatorPlayerDataState player)
    {
        lua_newtable(state);
        SetStringField(state, "unitToken", player.UnitToken);
        SetStringField(state, "name", player.Name);
        SetIntegerField(state, "faction", player.Faction);
        SetIntegerField(state, "specialization", player.Specialization);
        SetIntegerField(state, "damageDone", player.DamageDone);
        SetIntegerField(state, "damageTaken", player.DamageTaken);
        SetIntegerField(state, "healingDone", player.HealingDone);
        SetIntegerField(state, "healingTaken", player.HealingTaken);
        SetIntegerField(state, "kills", player.Kills);
        SetIntegerField(state, "deaths", player.Deaths);
        SetIntegerField(state, "soloShuffleRoundWins", player.SoloShuffleRoundWins);
        SetIntegerField(state, "soloShuffleRoundLosses", player.SoloShuffleRoundLosses);
    }

    private static void PushUnitData(
        lua_State state,
        WowCommentatorUnitDataState unit)
    {
        lua_newtable(state);
        SetIntegerField(state, "healthMax", unit.HealthMax);
        SetIntegerField(state, "health", unit.Health);
        SetIntegerField(state, "absorbTotal", unit.AbsorbTotal);
        SetBooleanField(state, "isDeadOrGhost", unit.IsDeadOrGhost);
        SetBooleanField(state, "isFeignDeath", unit.IsFeignDeath);
        SetStringField(state, "powerTypeToken", unit.PowerTypeToken);
        SetIntegerField(state, "power", unit.Power);
        SetIntegerField(state, "powerMax", unit.PowerMax);
    }

    private static void PushVector3Mixin(
        lua_State state,
        WowCommentatorVector3 value)
    {
        lua_newtable(state);
        SetNumberField(state, "x", value.X);
        SetNumberField(state, "y", value.Y);
        SetNumberField(state, "z", value.Z);
        ApplyMixinToTopTable(state, "Vector3DMixin");
    }

    private static void PushColorMixin(
        lua_State state,
        WowCommentatorColor value)
    {
        lua_newtable(state);
        SetNumberField(state, "r", value.Red);
        SetNumberField(state, "g", value.Green);
        SetNumberField(state, "b", value.Blue);
        ApplyMixinToTopTable(state, "ColorMixin");
    }

    private static void ApplyMixinToTopTable(lua_State state, string mixinName)
    {
        var tableIndex = AbsoluteIndex(state, -1);
        lua_getglobal(state, "Mixin");
        if (lua_isfunction(state, -1) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        lua_pushvalue(state, tableIndex);
        lua_getglobal(state, mixinName);
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 3);
            return;
        }

        if (lua_pcall(state, 2, 1, 0) == 0)
        {
            lua_remove(state, tableIndex);
            return;
        }
        lua_pop(state, 1);
    }

    private static ParsedCommentatorHistory ReadCommentatorHistory(
        lua_State state,
        int index,
        string usage)
    {
        index = AbsoluteIndex(state, index);
        if (lua_type(state, index) != LUA_TTABLE)
            return LuaErrorHistory(state, usage);

        var series = ReadHistorySeries(state, index, usage);
        var teamDirectory = ReadTeamDirectory(state, index, usage);
        var overrideNames = ReadOverrideNameDirectory(state, index, usage);
        return new ParsedCommentatorHistory(series, teamDirectory, overrideNames);
    }

    private static IReadOnlyList<ParsedCommentatorSeries> ReadHistorySeries(
        lua_State state,
        int historyIndex,
        string usage)
    {
        var fieldIndex = RequiredTableField(state, historyIndex, "series", usage);
        var result = new List<ParsedCommentatorSeries>();
        var count = CheckedArrayCount(state, fieldIndex, usage);
        for (var index = 1; index <= count; index++)
        {
            lua_rawgeti(state, fieldIndex, index);
            var seriesIndex = AbsoluteIndex(state, -1);
            if (lua_type(state, seriesIndex) != LUA_TTABLE)
                return LuaErrorList<ParsedCommentatorSeries>(state, usage);

            var teamsIndex = RequiredTableField(state, seriesIndex, "teams", usage);
            var teams = new List<ParsedCommentatorSeriesTeam>();
            var teamCount = CheckedArrayCount(state, teamsIndex, usage);
            for (var teamIndex = 1; teamIndex <= teamCount; teamIndex++)
            {
                lua_rawgeti(state, teamsIndex, teamIndex);
                var entryIndex = AbsoluteIndex(state, -1);
                if (lua_type(state, entryIndex) != LUA_TTABLE)
                    return LuaErrorList<ParsedCommentatorSeries>(state, usage);
                var name = RequiredStringValueField(state, entryIndex, "name", usage);
                var score = RequiredUInt32Field(state, entryIndex, "score", usage);
                teams.Add(new ParsedCommentatorSeriesTeam(name, score));
                lua_pop(state, 1);
            }
            lua_pop(state, 1);
            result.Add(new ParsedCommentatorSeries(teams));
            lua_pop(state, 1);
        }
        lua_pop(state, 1);
        return result;
    }

    private static IReadOnlyList<WowCommentatorTeamDirectoryEntryState> ReadTeamDirectory(
        lua_State state,
        int historyIndex,
        string usage)
    {
        var fieldIndex = RequiredTableField(
            state,
            historyIndex,
            "teamDirectory",
            usage);
        var result = new List<WowCommentatorTeamDirectoryEntryState>();
        var count = CheckedArrayCount(state, fieldIndex, usage);
        for (var index = 1; index <= count; index++)
        {
            lua_rawgeti(state, fieldIndex, index);
            var entryIndex = AbsoluteIndex(state, -1);
            if (lua_type(state, entryIndex) != LUA_TTABLE)
                return LuaErrorList<WowCommentatorTeamDirectoryEntryState>(state, usage);
            result.Add(
                new WowCommentatorTeamDirectoryEntryState(
                    RequiredStringValueField(state, entryIndex, "playerName", usage),
                    RequiredStringValueField(state, entryIndex, "teamName", usage)));
            lua_pop(state, 1);
        }
        lua_pop(state, 1);
        return result;
    }

    private static IReadOnlyList<ParsedCommentatorOverrideName> ReadOverrideNameDirectory(
        lua_State state,
        int historyIndex,
        string usage)
    {
        var fieldIndex = RequiredTableField(
            state,
            historyIndex,
            "overrideNameDirectory",
            usage);
        var result = new List<ParsedCommentatorOverrideName>();
        var count = CheckedArrayCount(state, fieldIndex, usage);
        for (var index = 1; index <= count; index++)
        {
            lua_rawgeti(state, fieldIndex, index);
            var entryIndex = AbsoluteIndex(state, -1);
            if (lua_type(state, entryIndex) != LUA_TTABLE)
                return LuaErrorList<ParsedCommentatorOverrideName>(state, usage);
            result.Add(
                new ParsedCommentatorOverrideName(
                    RequiredStringValueField(state, entryIndex, "originalName", usage),
                    RequiredStringValueField(state, entryIndex, "newName", usage)));
            lua_pop(state, 1);
        }
        lua_pop(state, 1);
        return result;
    }

    private static int RequiredTableField(
        lua_State state,
        int tableIndex,
        string name,
        string usage)
    {
        tableIndex = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, tableIndex, name);
        if (lua_type(state, -1) != LUA_TTABLE)
        {
            lua_pop(state, 1);
            luaL_error(state, usage);
            return 0;
        }
        return AbsoluteIndex(state, -1);
    }

    private static string RequiredStringValueField(
        lua_State state,
        int tableIndex,
        string name,
        string usage)
    {
        tableIndex = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, tableIndex, name);
        if (lua_isstring(state, -1) == 0)
        {
            lua_pop(state, 1);
            return LuaErrorString(state, usage);
        }
        var value = lua_tostring(state, -1) ?? string.Empty;
        lua_pop(state, 1);
        return value;
    }

    private static uint RequiredUInt32Field(
        lua_State state,
        int tableIndex,
        string name,
        string usage)
    {
        tableIndex = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, tableIndex, name);
        var value = RequiredUInt32(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static int CheckedArrayCount(
        lua_State state,
        int tableIndex,
        string usage)
    {
        var count = lua_objlen(state, tableIndex);
        if (count > int.MaxValue)
            return luaL_error(state, usage);
        return (int)count;
    }

    private static void SetStringField(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetIntegerField(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetNumberField(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBooleanField(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static string RequiredString(lua_State state, int index, string usage)
    {
        if (lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static string RequiredUnitToken(lua_State state, int index, string usage)
    {
        var unitToken = RequiredString(state, index, usage);
        if (!LuaBindings.IsRecognizedUnitToken(unitToken))
            return LuaErrorString(state, usage);
        return unitToken;
    }

    private static int RequiredInt32(lua_State state, int index, string usage)
    {
        var value = RequiredInteger(state, index, usage);
        if (value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return (int)value;
    }

    private static int RequiredOneBasedIndex(lua_State state, int index, string usage)
    {
        var value = RequiredInteger(state, index, usage);
        if (value < 1 || value > uint.MaxValue)
            return luaL_error(state, usage);
        return value > int.MaxValue ? int.MaxValue : (int)value;
    }

    private static uint RequiredOneBasedUInt32(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInteger(state, index, usage);
        if (value < 1 || value > uint.MaxValue)
            return (uint)luaL_error(state, usage);
        return (uint)value;
    }

    private static uint RequiredUInt32(lua_State state, int index, string usage)
    {
        var value = RequiredInteger(state, index, usage);
        if (value < 0 || value > uint.MaxValue)
            return (uint)luaL_error(state, usage);
        return (uint)value;
    }

    private static WowTrackedSpellCategory RequiredTrackedSpellCategory(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInteger(state, index, usage);
        if (value is < 0 or > 4)
        {
            luaL_error(state, usage);
            return WowTrackedSpellCategory.None;
        }
        return (WowTrackedSpellCategory)value;
    }

    private static float RequiredFloat(lua_State state, int index, string usage)
    {
        if (lua_type(state, index) != LUA_TNUMBER)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < -float.MaxValue ||
            value > float.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return (float)value;
    }

    private static bool RequiredTruthyBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) == LUA_TNONE)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static bool? OptionalTruthyBoolean(lua_State state, int index)
    {
        if (lua_type(state, index) == LUA_TNONE)
            return null;
        return lua_toboolean(state, index) != 0;
    }

    private static uint? OptionalOneBasedUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return null;
        return RequiredOneBasedUInt32(state, index, usage);
    }

    private static bool IsRepresentedPlayer(
        WowCommentatorState commentator,
        uint factionIndex,
        uint playerIndex) =>
        factionIndex is >= 1 and <= 2 &&
        playerIndex <= commentator.TeamPlayerCounts[factionIndex - 1];

    private static uint? ResolveTeamIndexByUnit(
        WowCommentatorState commentator,
        string unitToken)
    {
        if (commentator.TeamIndexByUnit.TryGetValue(unitToken, out var explicitTeamIndex) &&
            explicitTeamIndex is >= 1 and <= 2)
        {
            return explicitTeamIndex;
        }

        foreach (var entry in commentator.PlayersByPosition)
        {
            if (entry.Key.TeamIndex is >= 1 and <= 2 &&
                entry.Value.UnitToken.Equals(
                    unitToken,
                    StringComparison.OrdinalIgnoreCase))
            {
                return entry.Key.TeamIndex;
            }
        }
        return null;
    }

    private static string? FindCurrentTeamName(
        WowCommentatorState commentator,
        uint teamIndex)
    {
        if (teamIndex is < 1 or > 2)
            return null;

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var firstSeen = new List<string>();
        foreach (var player in commentator.PlayersByPosition)
        {
            if (player.Key.TeamIndex != teamIndex)
                continue;
            foreach (var entry in commentator.TeamDirectory)
            {
                if (!entry.PlayerName.Equals(
                        player.Value.Name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!counts.TryAdd(entry.TeamName, 1))
                    counts[entry.TeamName]++;
                else
                    firstSeen.Add(entry.TeamName);
            }
        }

        string? result = null;
        var bestCount = 0;
        foreach (var teamName in firstSeen)
        {
            if (counts[teamName] <= bestCount)
                continue;
            result = teamName;
            bestCount = counts[teamName];
        }
        return result;
    }

    private static long RequiredInteger(lua_State state, int index, string usage)
    {
        if (lua_type(state, index) != LUA_TNUMBER)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value != Math.Truncate(value) ||
            value < long.MinValue ||
            value > long.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return (long)value;
    }

    private static string LuaErrorString(lua_State state, string usage)
    {
        luaL_error(state, usage);
        return string.Empty;
    }

    private static ParsedCommentatorHistory LuaErrorHistory(
        lua_State state,
        string usage)
    {
        luaL_error(state, usage);
        return new ParsedCommentatorHistory([], [], []);
    }

    private static IReadOnlyList<T> LuaErrorList<T>(
        lua_State state,
        string usage)
    {
        luaL_error(state, usage);
        return [];
    }

    private static int AbsoluteIndex(lua_State state, int index) =>
        index < 0 ? lua_gettop(state) + index + 1 : index;

    private sealed record ParsedCommentatorHistory(
        IReadOnlyList<ParsedCommentatorSeries> Series,
        IReadOnlyList<WowCommentatorTeamDirectoryEntryState> TeamDirectory,
        IReadOnlyList<ParsedCommentatorOverrideName> OverrideNames);

    private sealed record ParsedCommentatorSeries(
        IReadOnlyList<ParsedCommentatorSeriesTeam> Teams);

    private sealed record ParsedCommentatorSeriesTeam(string Name, uint Score);

    private sealed record ParsedCommentatorOverrideName(
        string OriginalName,
        string NewName);
}
