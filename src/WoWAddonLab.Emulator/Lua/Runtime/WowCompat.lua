
local function wipe_impl(t)
    for k in pairs(t) do t[k] = nil end
    return t
end

local native_xpcall = xpcall
xpcall = function(callback, errorHandler, ...)
    local argumentCount = select("#", ...)
    if argumentCount == 0 then
        return native_xpcall(callback, errorHandler)
    end

    local arguments = { ... }
    return native_xpcall(function()
        return callback(unpack(arguments, 1, argumentCount))
    end, errorHandler)
end

loadstring_untainted = loadstring_untainted or loadstring
scrub = scrub or function(...)
    return ...
end

wipe = wipe_impl
table.wipe = wipe_impl
table.create = table.create or function()
    return {}
end
tinsert = tinsert or table.insert
tremove = tremove or table.remove
tsort = tsort or table.sort

local stringMetatable = debug.getmetatable("")
if stringMetatable then
    stringMetatable.__index = string
end

math.atan2 = math.atan2 or function(y, x)
    if x > 0 then return math.atan(y / x) end
    if x < 0 and y >= 0 then return math.atan(y / x) + math.pi end
    if x < 0 and y < 0 then return math.atan(y / x) - math.pi end
    if x == 0 and y > 0 then return math.pi / 2 end
    if x == 0 and y < 0 then return -math.pi / 2 end
    return 0
end

math.clamp = math.clamp or function(value, minimum, maximum)
    if minimum > maximum then
        return maximum
    end
    if value < minimum then
        return minimum
    end
    if value > maximum then
        return maximum
    end
    return value
end

math.lerp = math.lerp or function(startValue, endValue, amount)
    return (endValue - startValue) * amount + startValue
end

math.normalize = math.normalize or function(value, minimum, maximum)
    if minimum == maximum then
        return 0
    end
    return (value - minimum) / (maximum - minimum)
end

math.remap = math.remap or function(
    value,
    sourceMinimum,
    sourceMaximum,
    destinationMinimum,
    destinationMaximum)
    local amount = 0
    if sourceMinimum ~= sourceMaximum then
        amount = (value - sourceMinimum) / (sourceMaximum - sourceMinimum)
    end
    return (destinationMaximum - destinationMinimum) * amount + destinationMinimum
end

math.round = math.round or function(value, decimalPlaces)
    local scale = 10 ^ (decimalPlaces or 0)
    if scale < 0.000001 then
        return 0
    end
    local scaled = value * scale
    if scaled >= 0 then
        return math.floor(scaled + 0.5) / scale
    end
    return math.ceil(scaled - 0.5) / scale
end

math.saturate = math.saturate or function(value)
    if not (value >= 0) then
        return 0
    end
    if value > 1 then
        return 1
    end
    return value
end

math.sign = math.sign or function(value)
    if value <= 0 then
        if value >= 0 then
            return 0
        end
        return -1
    end
    return 1
end

math.wrap = math.wrap or function(value, minimum, maximum)
    local range = maximum - minimum
    if range > 0 then
        return minimum + math.fmod(math.fmod(value - minimum, range) + range, range)
    end
    return minimum
end

table.contains = table.contains or function(tbl, value)
    for _, entry in pairs(tbl) do
        if entry == value then
            return true
        end
    end
    return false
end

table.count = table.count or function(tbl)
    local total = 0
    for _ in pairs(tbl) do
        total = total + 1
    end
    return total
end

table.indexof = table.indexof or function(tbl, value)
    for index = 1, #tbl do
        if tbl[index] == value then
            return index
        end
    end
    return nil
end

table.isempty = table.isempty or function(tbl)
    return next(tbl) == nil
end

table.isfrozen = table.isfrozen or function()
    return false
end

table.keys = table.keys or function(tbl)
    local keys = {}
    for key in pairs(tbl) do
        keys[#keys + 1] = key
    end
    return keys
end

table.values = table.values or function(tbl)
    local values = {}
    for _, value in pairs(tbl) do
        values[#values + 1] = value
    end
    return values
end

table.removeunordered = table.removeunordered or function(tbl, index)
    local count = #tbl
    index = index or count
    if index < 1 or index > count then
        return nil
    end
    local removed = tbl[index]
    if index ~= count then
        tbl[index] = tbl[count]
    end
    tbl[count] = nil
    return removed
end

table.removevalue = table.removevalue or function(tbl, value)
    local count = #tbl
    local target = 1
    for index = 1, count do
        if tbl[index] ~= value then
            if target ~= index then
                tbl[target] = tbl[index]
            end
            target = target + 1
        end
    end
    for index = target, count do
        tbl[index] = nil
    end
    return count - target + 1
end

LoadAddOn = LoadAddOn or function(name)
    return C_AddOns.LoadAddOn(name)
end

UIParentLoadAddOn = UIParentLoadAddOn or function(name)
    local loaded, reason = C_AddOns.LoadAddOn(name)
    return loaded, reason
end

C_Timer.NewTimedSignalMap = C_Timer.NewTimedSignalMap or function(callback)
    local pendingGeneration = {}
    local signalMap = {}

    function signalMap:SignalAt(key, time)
        local generation = (pendingGeneration[key] or 0) + 1
        pendingGeneration[key] = generation
        local delay = time - GetTime()
        if delay < 0 then
            delay = 0
        end
        C_Timer.After(delay, function()
            if pendingGeneration[key] ~= generation then
                return
            end
            pendingGeneration[key] = nil
            callback(key)
        end)
    end

    function signalMap:CancelSignal(key)
        pendingGeneration[key] = (pendingGeneration[key] or 0) + 1
    end

    return signalMap
end

max = max or math.max
min = min or math.min
abs = abs or math.abs
date = date or os.date

local native_string_format = string.format
local function call_native_string_format(formatString, ...)
    local results = { pcall(native_string_format, formatString, ...) }
    if not results[1] then
        error(results[2], 3)
    end
    return unpack(results, 2)
end
local function normalize_format_conversions(formatString)
    if type(formatString) ~= "string" then
        return formatString
    end
    local pieces = {}
    local position = 1
    while position <= #formatString do
        local percent = formatString:find("%", position, true)
        if not percent then
            pieces[#pieces + 1] = formatString:sub(position)
            break
        end
        pieces[#pieces + 1] = formatString:sub(position, percent - 1)
        if formatString:sub(percent + 1, percent + 1) == "%" then
            pieces[#pieces + 1] = "%%"
            position = percent + 2
        else
            local conversion = formatString:find("[cdeEfgFGiouxXqs]", percent + 1)
            if not conversion then
                pieces[#pieces + 1] = formatString:sub(percent)
                break
            end
            local specifier = formatString:sub(percent, conversion)
            if formatString:sub(conversion, conversion) == "F" then
                specifier = specifier:sub(1, -2) .. "f"
            end
            pieces[#pieces + 1] = specifier
            position = conversion + 1
        end
    end
    return table.concat(pieces)
end

string.format = function(formatString, ...)
    if type(formatString) ~= "string" then
        return call_native_string_format(formatString, ...)
    end
    local normalizedFormat = normalize_format_conversions(formatString)
    local arguments = { n = select("#", ...), ... }
    if not normalizedFormat:find("%%%d+%$") then
        local argumentIndex = 0
        normalizedFormat:gsub(
            "%%([%-%+ #0]*%d*%.?%d*[cdeEfgGiouxXqs])",
            function(specifier)
                argumentIndex = argumentIndex + 1
                local conversion = specifier:sub(-1)
                if (conversion == "d" or conversion == "i") and
                    tonumber(arguments[argumentIndex]) == nil then
                    arguments[argumentIndex] = 0
                end
                return "%" .. specifier
            end)
        return call_native_string_format(
            normalizedFormat,
            unpack(arguments, 1, arguments.n))
    end
    local ordered = {}
    local orderedCount = 0
    local nextSequentialArgument = 1
    local normalized = normalizedFormat:gsub(
        "%%(%d*%$?)([%-%+ #0]*%d*%.?%d*[cdeEfgGiouxXqs])",
        function(position, specifier)
            local index = position:match("^(%d+)%$$")
            if index then
                index = tonumber(index)
            else
                index = nextSequentialArgument
                nextSequentialArgument = nextSequentialArgument + 1
            end
            orderedCount = orderedCount + 1
            local value = arguments[index]
            local conversion = specifier:sub(-1)
            if (conversion == "d" or conversion == "i") and tonumber(value) == nil then
                value = 0
            end
            ordered[orderedCount] = value
            return "%" .. specifier
        end)
    return call_native_string_format(normalized, unpack(ordered, 1, orderedCount))
end

function CreateColor(r, g, b, a)
    local color = { r = r or 0, g = g or 0, b = b or 0, a = a == nil and 1 or a }
    function color:GetRGBA() return self.r, self.g, self.b, self.a end
    function color:GetRGB() return self.r, self.g, self.b end
    local function ToByte(value)
        return math.floor(Clamp(value or 0, 0, 1) * 255 + 0.5)
    end
    function color:GenerateHexColor()
        return string.format("%02x%02x%02x", ToByte(self.r), ToByte(self.g), ToByte(self.b))
    end
    function color:GenerateHexColorMarkup()
        return string.format(
            "|c%02x%02x%02x%02x",
            ToByte(self.a),
            ToByte(self.r),
            ToByte(self.g),
            ToByte(self.b))
    end
    function color:WrapTextInColorCode(text)
        return self:GenerateHexColorMarkup() .. tostring(text or "") .. "|r"
    end
    return color
end

function Clamp(value, minimum, maximum)
    return math.max(minimum, math.min(maximum, value))
end

function Mixin(object, ...)
    for index = 1, select("#", ...) do
        local mixin = select(index, ...)
        for key, value in pairs(mixin) do object[key] = value end
    end
    return object
end

function CreateFromMixins(...)
    return Mixin({}, ...)
end

securecallfunction = securecallfunction or function(callback, ...)
    if type(callback) == "string" then
        callback = _G[callback]
    end
    return callback(...)
end

securecall = securecall or securecallfunction
issecretvalue = issecretvalue or function() return false end
canaccessvalue = canaccessvalue or function() return true end
canaccessallvalues = canaccessallvalues or function() return true end
issecure = issecure or function() return true end
forceinsecure = forceinsecure or function(callback, ...) return callback(...) end
secretwrap = secretwrap or function(value) return value end
settablesecurity = settablesecurity or function() end

C_Secrets = C_Secrets or {}

local function NeverSecret()
    return Enum and Enum.SecrecyLevel and Enum.SecrecyLevel.NeverSecret or 0
end

local function NotSecret()
    return false
end

C_Secrets.CanCompareUnitTokens = C_Secrets.CanCompareUnitTokens or function() return true end
C_Secrets.HasSecretRestrictions = C_Secrets.HasSecretRestrictions or NotSecret
C_Secrets.GetPowerTypeSecrecy = C_Secrets.GetPowerTypeSecrecy or NeverSecret
C_Secrets.GetSpellAuraSecrecy = C_Secrets.GetSpellAuraSecrecy or NeverSecret
C_Secrets.GetSpellCastSecrecy = C_Secrets.GetSpellCastSecrecy or NeverSecret
C_Secrets.GetSpellCooldownSecrecy = C_Secrets.GetSpellCooldownSecrecy or NeverSecret
C_Secrets.ShouldActionCooldownBeSecret = C_Secrets.ShouldActionCooldownBeSecret or NotSecret
C_Secrets.ShouldAurasBeSecret = C_Secrets.ShouldAurasBeSecret or NotSecret
C_Secrets.ShouldCooldownsBeSecret = C_Secrets.ShouldCooldownsBeSecret or NotSecret
C_Secrets.ShouldSpellAuraBeSecret = C_Secrets.ShouldSpellAuraBeSecret or NotSecret
C_Secrets.ShouldSpellBookItemCooldownBeSecret = C_Secrets.ShouldSpellBookItemCooldownBeSecret or NotSecret
C_Secrets.ShouldSpellCooldownBeSecret = C_Secrets.ShouldSpellCooldownBeSecret or NotSecret
C_Secrets.ShouldTotemSlotBeSecret = C_Secrets.ShouldTotemSlotBeSecret or NotSecret
C_Secrets.ShouldUnitSpellCastingBeSecret = C_Secrets.ShouldUnitSpellCastingBeSecret or NotSecret

table.count = table.count or function(values)
    local total = 0
    for _ in pairs(values or {}) do
        total = total + 1
    end
    return total
end

C_PaperDollInfo = C_PaperDollInfo or {}
C_PaperDollInfo.GetInventorySlotInfo = C_PaperDollInfo.GetInventorySlotInfo or function(slotName)
    return GetInventorySlotInfo(slotName)
end

C_CVar = C_CVar or {}
C_CVar.AreCVarsLoaded = C_CVar.AreCVarsLoaded or function() return true end

GetGlobalEnvironment = GetGlobalEnvironment or function() return _G end

C_PaperDollInfo.GetTemporaryEnchantmentInfo =
    C_PaperDollInfo.GetTemporaryEnchantmentInfo or function() end

C_FriendList = C_FriendList or {}
C_FriendList.IsLegacyFriendSystemEnabled =
    C_FriendList.IsLegacyFriendSystemEnabled or function() return true end

local loadOnDemandAddOns = {
    APIDocumentation_LoadUI = "Blizzard_APIDocumentation",
    AchievementFrame_LoadUI = "Blizzard_AchievementUI",
    ArdenwealdGardening_LoadUI = "Blizzard_ArdenwealdGardening",
    BattlefieldMap_LoadUI = "Blizzard_BattlefieldMap",
    BoostTutorial_LoadUI = "Blizzard_BoostTutorial",
    Calendar_LoadUI = "Blizzard_Calendar",
    ChallengeMode_LoadUI = "Blizzard_ChallengesUI",
    ClassTrainerFrame_LoadUI = "Blizzard_TrainerUI",
    CollectionsJournal_LoadUI = "Blizzard_Collections",
    CombatLog_LoadUI = "Blizzard_CombatLog",
    CombatText_LoadUI = "Blizzard_CombatText",
    Commentator_LoadUI = "Blizzard_Commentator",
    CooldownBroadcaster_LoadUI = "Blizzard_CooldownBroadcaster",
    CovenantCallings_LoadUI = "Blizzard_CovenantCallings",
    DebugTools_LoadUI = "Blizzard_DebugTools",
    EncounterJournal_LoadUI = "Blizzard_EncounterJournal",
    EventTrace_LoadUI = "Blizzard_EventTrace",
    ExpansionTrial_LoadUI = "Blizzard_ExpansionTrial",
    Garrison_LoadUI = "Blizzard_GarrisonUI",
    GenericTraitUI_LoadUI = "Blizzard_GenericTraitUI",
    HousingControls_LoadUI = "Blizzard_HousingControls",
    HybridMinimap_LoadUI = "Blizzard_HybridMinimap",
    ItemSocketingFrame_LoadUI = "Blizzard_ItemSocketingUI",
    LandingSoulbinds_LoadUI = "Blizzard_LandingSoulbinds",
    MacroFrame_LoadUI = "Blizzard_MacroUI",
    MovePad_LoadUI = "Blizzard_MovePad",
    NPE_LoadUI = "Blizzard_NewPlayerExperience",
    PVPUI_LoadUI = "Blizzard_PVPUI",
    PlayerSpellsFrame_LoadUI = "Blizzard_PlayerSpells",
    ProfessionsFrame_LoadUI = "Blizzard_Professions",
    RaidFrame_LoadUI = "Blizzard_RaidUI",
    RemixArtifactTutorialUI_LoadUI = "Blizzard_RemixArtifactTutorialUI",
    SoulbindViewer_LoadUI = "Blizzard_Soulbinds",
    SubscriptionInterstitial_LoadUI = "Blizzard_SubscriptionInterstitialUI",
    TimeManager_LoadUI = "Blizzard_TimeManager",
}

local function LoadAddOnUi(addonName)
    return function()
        if C_AddOns and C_AddOns.LoadAddOn then
            C_AddOns.LoadAddOn(addonName)
        end
    end
end

for loaderName, addonName in pairs(loadOnDemandAddOns) do
    _G[loaderName] = rawget(_G, loaderName) or LoadAddOnUi(addonName)
end

local function TogglePanelUi(loaderName, frameName)
    return function()
        _G[loaderName]()
        local frame = _G[frameName]
        if frame then
            ToggleFrame(frame)
        end
    end
end

ToggleAchievementFrame = rawget(_G, "ToggleAchievementFrame") or function(...)
    AchievementFrame_LoadUI()
    if AchievementFrame_ToggleAchievementFrame then
        AchievementFrame_ToggleAchievementFrame(...)
    end
end

ToggleCollectionsJournal = rawget(_G, "ToggleCollectionsJournal") or function(tabIndex)
    CollectionsJournal_LoadUI()
    if not CollectionsJournal then
        return
    end
    if tabIndex and CollectionsJournal_SetTab then
        if CollectionsJournal:IsShown() and CollectionsJournal.selectedTab == tabIndex then
            HideUIPanel(CollectionsJournal)
            return
        end
        CollectionsJournal_SetTab(CollectionsJournal, tabIndex)
        ShowUIPanel(CollectionsJournal)
        return
    end
    ToggleFrame(CollectionsJournal)
end

ToggleEncounterJournal = rawget(_G, "ToggleEncounterJournal") or
    TogglePanelUi("EncounterJournal_LoadUI", "EncounterJournal")
ToggleProfessionsBook = rawget(_G, "ToggleProfessionsBook") or function()
    if C_AddOns and C_AddOns.LoadAddOn then
        C_AddOns.LoadAddOn("Blizzard_ProfessionsBook")
    end
    if ProfessionsBookFrame then
        ToggleFrame(ProfessionsBookFrame)
    end
end
ToggleCalendar = rawget(_G, "ToggleCalendar") or
    TogglePanelUi("Calendar_LoadUI", "CalendarFrame")
ToggleBattlefieldMap = rawget(_G, "ToggleBattlefieldMap") or
    TogglePanelUi("BattlefieldMap_LoadUI", "BattlefieldMapFrame")

IsBoostTutorialScenario = IsBoostTutorialScenario or function() return false end
GetBuildOption = GetBuildOption or function() return false end

C_RecruitAFriend = C_RecruitAFriend or {}
C_RecruitAFriend.IsSystemEnabled = C_RecruitAFriend.IsSystemEnabled or function() return false end

C_UnitAuras = C_UnitAuras or {}
C_UnitAuras.GetHiddenGroupBuffs = C_UnitAuras.GetHiddenGroupBuffs or function() return {} end
C_UnitAuras.GetGroupBuffVisualAlerts = C_UnitAuras.GetGroupBuffVisualAlerts or function() return {} end

C_DelvesUI = C_DelvesUI or {}
C_DelvesUI.GetFlavorNodeForCompanion = C_DelvesUI.GetFlavorNodeForCompanion or function() return 0 end

C_LFGList = C_LFGList or {}
C_LFGList.IsCensoredActiveEntryUnresolved =
    C_LFGList.IsCensoredActiveEntryUnresolved or function() return false end

C_Discord = C_Discord or {}
C_Discord.Authorize = C_Discord.Authorize or function() end
C_Discord.GetDiscordChannelName = C_Discord.GetDiscordChannelName or function() return nil end
C_Discord.GetDiscordUserID = C_Discord.GetDiscordUserID or function() return nil end
C_Discord.GetDiscordUserName = C_Discord.GetDiscordUserName or function() return nil end
C_Discord.GetDisplayNameType = C_Discord.GetDisplayNameType or function() return nil end
C_Discord.GetGuildLinkStatus = C_Discord.GetGuildLinkStatus or function() return false, nil, nil end
C_Discord.GetNumDiscordChannels = C_Discord.GetNumDiscordChannels or function() return 0, false end
C_Discord.GetNumDiscordServers = C_Discord.GetNumDiscordServers or function() return 0 end
C_Discord.GetServerLinkableChannels = C_Discord.GetServerLinkableChannels or function() end
C_Discord.GetServerName = C_Discord.GetServerName or function() return nil end
C_Discord.GuildLink = C_Discord.GuildLink or function() end
C_Discord.GuildUnlink = C_Discord.GuildUnlink or function() end
C_Discord.IsEnabled = C_Discord.IsEnabled or function() return false end
C_Discord.IsGuildChannelLinked = C_Discord.IsGuildChannelLinked or function() return false end
C_Discord.IsGuildSettingSet = C_Discord.IsGuildSettingSet or function() return false end
C_Discord.IsUserOAuthed = C_Discord.IsUserOAuthed or function() return false end
C_Discord.RefreshAuth = C_Discord.RefreshAuth or function() end
C_Discord.SetGuildSetting = C_Discord.SetGuildSetting or function() end
C_Discord.UpdateDiscordServers = C_Discord.UpdateDiscordServers or function() end
C_Discord.UpdateGuildLobby = C_Discord.UpdateGuildLobby or function() end

C_SocialUI = C_SocialUI or {}
C_SocialUI.IsSystemEnabled = C_SocialUI.IsSystemEnabled or function() return false end

C_ClientScene = C_ClientScene or {}
C_ClientScene.IsSceneTypeActive = C_ClientScene.IsSceneTypeActive or function() return false end

securecopy = securecopy or function(value)
    if type(value) ~= "table" then
        return value
    end
    local copy = {}
    for key, entry in pairs(value) do
        copy[key] = securecopy(entry)
    end
    return copy
end

C_HousingLayout = C_HousingLayout or {}
C_HousingLayout.AnyRoomsOnFloor = C_HousingLayout.AnyRoomsOnFloor or function() return false end
C_HousingLayout.CanSetViewedFloor = C_HousingLayout.CanSetViewedFloor or function() return false end
C_HousingLayout.CancelActiveLayoutEditing = C_HousingLayout.CancelActiveLayoutEditing or function() end
C_HousingLayout.ConfirmStairChoice = C_HousingLayout.ConfirmStairChoice or function() end
C_HousingLayout.DeselectFloorplan = C_HousingLayout.DeselectFloorplan or function() end
C_HousingLayout.DeselectRoomOrDoor = C_HousingLayout.DeselectRoomOrDoor or function() end
C_HousingLayout.GetBaseRoomFloor = C_HousingLayout.GetBaseRoomFloor or function() return 0 end
C_HousingLayout.GetHighestOccupiedFloorIndex = C_HousingLayout.GetHighestOccupiedFloorIndex or function() return 0 end
C_HousingLayout.GetLowestOccupiedFloorIndex = C_HousingLayout.GetLowestOccupiedFloorIndex or function() return 0 end
C_HousingLayout.GetNumActiveRooms = C_HousingLayout.GetNumActiveRooms or function() return 0 end
C_HousingLayout.GetRoomPlacementBudget = C_HousingLayout.GetRoomPlacementBudget or function() return nil end
C_HousingLayout.GetRoomPlayerIsIn = C_HousingLayout.GetRoomPlayerIsIn or function() return nil end
C_HousingLayout.GetSelectedBlueprintFloorplan = C_HousingLayout.GetSelectedBlueprintFloorplan or function() return 0, nil end
C_HousingLayout.GetSelectedDoor = C_HousingLayout.GetSelectedDoor or function() return 0, nil end
C_HousingLayout.GetSelectedFloorplan = C_HousingLayout.GetSelectedFloorplan or function() return nil end
C_HousingLayout.GetSelectedRoom = C_HousingLayout.GetSelectedRoom or function() return nil end
C_HousingLayout.GetSelectedStairwellRoomCount = C_HousingLayout.GetSelectedStairwellRoomCount or function() return 0 end
C_HousingLayout.GetSpentPlacementBudget = C_HousingLayout.GetSpentPlacementBudget or function() return nil end
C_HousingLayout.GetViewedFloor = C_HousingLayout.GetViewedFloor or function() return 0 end
C_HousingLayout.HasAnySelections = C_HousingLayout.HasAnySelections or function() return false end
C_HousingLayout.HasRoomPlacementBudget = C_HousingLayout.HasRoomPlacementBudget or function() return false end
C_HousingLayout.HasSelectedBlueprintFloorplan = C_HousingLayout.HasSelectedBlueprintFloorplan or function() return false end
C_HousingLayout.HasSelectedDoor = C_HousingLayout.HasSelectedDoor or function() return false end
C_HousingLayout.HasSelectedFloorplan = C_HousingLayout.HasSelectedFloorplan or function() return false end
C_HousingLayout.HasSelectedRoom = C_HousingLayout.HasSelectedRoom or function() return false end
C_HousingLayout.HasStairs = C_HousingLayout.HasStairs or function() return false end
C_HousingLayout.HasValidConnection = C_HousingLayout.HasValidConnection or function() return false end
C_HousingLayout.IsBaseRoom = C_HousingLayout.IsBaseRoom or function() return false end
C_HousingLayout.IsDraggingRoom = C_HousingLayout.IsDraggingRoom or function() return false, false end
C_HousingLayout.MoveDraggedRoom = C_HousingLayout.MoveDraggedRoom or function() end
C_HousingLayout.MoveLayoutCamera = C_HousingLayout.MoveLayoutCamera or function() end
C_HousingLayout.RemoveRoom = C_HousingLayout.RemoveRoom or function() end
C_HousingLayout.RoomHasStairs = C_HousingLayout.RoomHasStairs or function() return false end
C_HousingLayout.RotateFocusedRoom = C_HousingLayout.RotateFocusedRoom or function() end
C_HousingLayout.RotateRoom = C_HousingLayout.RotateRoom or function() end
C_HousingLayout.SelectFloorplan = C_HousingLayout.SelectFloorplan or function() end
C_HousingLayout.SetViewedFloor = C_HousingLayout.SetViewedFloor or function() end
C_HousingLayout.StartDrag = C_HousingLayout.StartDrag or function() end
C_HousingLayout.StopDrag = C_HousingLayout.StopDrag or function() end
C_HousingLayout.StopDraggingRoom = C_HousingLayout.StopDraggingRoom or function() end
C_HousingLayout.ZoomLayoutCamera = C_HousingLayout.ZoomLayoutCamera or function() return false end

C_HousingBlueprint = C_HousingBlueprint or {}
C_HousingBlueprint.CanExportRoom = C_HousingBlueprint.CanExportRoom or function() return false end
C_HousingBlueprint.CanExportTypeFromCurrentLocation = C_HousingBlueprint.CanExportTypeFromCurrentLocation or function() return false end
C_HousingBlueprint.CanImportTypeFromCurrentLocation = C_HousingBlueprint.CanImportTypeFromCurrentLocation or function() return false end
C_HousingBlueprint.DeleteBlueprint = C_HousingBlueprint.DeleteBlueprint or function() end
C_HousingBlueprint.ExportBlueprint = C_HousingBlueprint.ExportBlueprint or function() end
C_HousingBlueprint.ExportRoomBlueprint = C_HousingBlueprint.ExportRoomBlueprint or function() end
C_HousingBlueprint.GetBlueprintHyperlink = C_HousingBlueprint.GetBlueprintHyperlink or function() return "" end
C_HousingBlueprint.GetBlueprintTypeForCode = C_HousingBlueprint.GetBlueprintTypeForCode or function() return nil end
C_HousingBlueprint.GetExportAvailability = C_HousingBlueprint.GetExportAvailability or function() return nil end
C_HousingBlueprint.GetFeatureAvailability = C_HousingBlueprint.GetFeatureAvailability or function() return nil end
C_HousingBlueprint.GetImportAvailability = C_HousingBlueprint.GetImportAvailability or function() return nil end
C_HousingBlueprint.ImportBlueprint = C_HousingBlueprint.ImportBlueprint or function() end
C_HousingBlueprint.IsShareCodeValid = C_HousingBlueprint.IsShareCodeValid or function() return false end
C_HousingBlueprint.RenameBlueprint = C_HousingBlueprint.RenameBlueprint or function() end
C_HousingBlueprint.RequestBlueprintCollection = C_HousingBlueprint.RequestBlueprintCollection or function() end
C_HousingBlueprint.RequestBlueprintContents = C_HousingBlueprint.RequestBlueprintContents or function() end
C_HousingBlueprint.RequestBlueprintContentsForContext = C_HousingBlueprint.RequestBlueprintContentsForContext or function() end
C_HousingBlueprint.StartImportRoomBlueprint = C_HousingBlueprint.StartImportRoomBlueprint or function() end
C_HousingBlueprint.UpdateBlueprintStringFromInput = C_HousingBlueprint.UpdateBlueprintStringFromInput or function() return "" end

C_HousingExpertMode = C_HousingExpertMode or {}
C_HousingExpertMode.CancelActiveEditing = C_HousingExpertMode.CancelActiveEditing or function() end
C_HousingExpertMode.CommitDecorMovement = C_HousingExpertMode.CommitDecorMovement or function() end
C_HousingExpertMode.CommitHouseExteriorPosition = C_HousingExpertMode.CommitHouseExteriorPosition or function() end
C_HousingExpertMode.GetHoveredDecorInfo = C_HousingExpertMode.GetHoveredDecorInfo or function() return nil end
C_HousingExpertMode.GetPrecisionSubmode = C_HousingExpertMode.GetPrecisionSubmode or function() return nil end
C_HousingExpertMode.GetPrecisionSubmodeRestriction = C_HousingExpertMode.GetPrecisionSubmodeRestriction or function() return nil end
C_HousingExpertMode.GetSelectedDecorInfo = C_HousingExpertMode.GetSelectedDecorInfo or function() return nil end
C_HousingExpertMode.IsDecorSelected = C_HousingExpertMode.IsDecorSelected or function() return false end
C_HousingExpertMode.IsGridVisible = C_HousingExpertMode.IsGridVisible or function() return false end
C_HousingExpertMode.IsHouseExteriorHovered = C_HousingExpertMode.IsHouseExteriorHovered or function() return false end
C_HousingExpertMode.IsHouseExteriorSelected = C_HousingExpertMode.IsHouseExteriorSelected or function() return false end
C_HousingExpertMode.IsHoveringDecor = C_HousingExpertMode.IsHoveringDecor or function() return false end
C_HousingExpertMode.RemoveSelectedDecor = C_HousingExpertMode.RemoveSelectedDecor or function() end
C_HousingExpertMode.ResetPrecisionChanges = C_HousingExpertMode.ResetPrecisionChanges or function() end
C_HousingExpertMode.SelectNextRotationAxis = C_HousingExpertMode.SelectNextRotationAxis or function() end
C_HousingExpertMode.SetGridVisible = C_HousingExpertMode.SetGridVisible or function() end
C_HousingExpertMode.SetPrecisionIncrementingActive = C_HousingExpertMode.SetPrecisionIncrementingActive or function() end
C_HousingExpertMode.SetPrecisionSubmode = C_HousingExpertMode.SetPrecisionSubmode or function() end

C_HousingCleanupMode = C_HousingCleanupMode or {}
C_HousingCleanupMode.GetHoveredDecorInfo = C_HousingCleanupMode.GetHoveredDecorInfo or function() return nil end
C_HousingCleanupMode.IsHoveringDecor = C_HousingCleanupMode.IsHoveringDecor or function() return false end
C_HousingCleanupMode.RemoveSelectedDecor = C_HousingCleanupMode.RemoveSelectedDecor or function() end

C_HousingCustomizeMode = C_HousingCustomizeMode or {}
C_HousingCustomizeMode.ApplyDyeToSelectedDecor = C_HousingCustomizeMode.ApplyDyeToSelectedDecor or function() end
C_HousingCustomizeMode.ApplyPetToSelectedDecor = C_HousingCustomizeMode.ApplyPetToSelectedDecor or function() end
C_HousingCustomizeMode.ApplyThemeToRoom = C_HousingCustomizeMode.ApplyThemeToRoom or function() end
C_HousingCustomizeMode.ApplyThemeToSelectedRoomComponent = C_HousingCustomizeMode.ApplyThemeToSelectedRoomComponent or function() end
C_HousingCustomizeMode.ApplyWallpaperToAllWalls = C_HousingCustomizeMode.ApplyWallpaperToAllWalls or function() end
C_HousingCustomizeMode.ApplyWallpaperToSelectedRoomComponent = C_HousingCustomizeMode.ApplyWallpaperToSelectedRoomComponent or function() end
C_HousingCustomizeMode.CancelActiveEditing = C_HousingCustomizeMode.CancelActiveEditing or function() end
C_HousingCustomizeMode.ClearDyesForSelectedDecor = C_HousingCustomizeMode.ClearDyesForSelectedDecor or function() end
C_HousingCustomizeMode.ClearTargetRoomComponent = C_HousingCustomizeMode.ClearTargetRoomComponent or function() end
C_HousingCustomizeMode.CommitDyesForSelectedDecor = C_HousingCustomizeMode.CommitDyesForSelectedDecor or function() return false end
C_HousingCustomizeMode.GetHoveredDecorInfo = C_HousingCustomizeMode.GetHoveredDecorInfo or function() return nil end
C_HousingCustomizeMode.GetHoveredRoomComponentInfo = C_HousingCustomizeMode.GetHoveredRoomComponentInfo or function() return nil end
C_HousingCustomizeMode.GetNumDyesToRemoveOnSelectedDecor = C_HousingCustomizeMode.GetNumDyesToRemoveOnSelectedDecor or function() return 0 end
C_HousingCustomizeMode.GetNumDyesToSpendOnSelectedDecor = C_HousingCustomizeMode.GetNumDyesToSpendOnSelectedDecor or function() return 0 end
C_HousingCustomizeMode.GetPreviewDyesOnSelectedDecor = C_HousingCustomizeMode.GetPreviewDyesOnSelectedDecor or function() return nil end
C_HousingCustomizeMode.GetRecentlyUsedDyes = C_HousingCustomizeMode.GetRecentlyUsedDyes or function() return nil end
C_HousingCustomizeMode.GetRecentlyUsedThemeSets = C_HousingCustomizeMode.GetRecentlyUsedThemeSets or function() return nil end
C_HousingCustomizeMode.GetRecentlyUsedWallpapers = C_HousingCustomizeMode.GetRecentlyUsedWallpapers or function() return nil end
C_HousingCustomizeMode.GetSelectedDecorInfo = C_HousingCustomizeMode.GetSelectedDecorInfo or function() return nil end
C_HousingCustomizeMode.GetSelectedDecorPetInfo = C_HousingCustomizeMode.GetSelectedDecorPetInfo or function() return nil, nil end
C_HousingCustomizeMode.GetSelectedRoomComponentInfo = C_HousingCustomizeMode.GetSelectedRoomComponentInfo or function() return nil end
C_HousingCustomizeMode.GetThemeSetInfo = C_HousingCustomizeMode.GetThemeSetInfo or function() return nil end
C_HousingCustomizeMode.GetWallpapersForRoomComponentType = C_HousingCustomizeMode.GetWallpapersForRoomComponentType or function() return nil end
C_HousingCustomizeMode.IsDecorSelected = C_HousingCustomizeMode.IsDecorSelected or function() return false end
C_HousingCustomizeMode.IsHouseExteriorDoorHovered = C_HousingCustomizeMode.IsHouseExteriorDoorHovered or function() return false end
C_HousingCustomizeMode.IsHoveringDecor = C_HousingCustomizeMode.IsHoveringDecor or function() return false end
C_HousingCustomizeMode.IsHoveringRoomComponent = C_HousingCustomizeMode.IsHoveringRoomComponent or function() return false end
C_HousingCustomizeMode.IsRoomComponentSelected = C_HousingCustomizeMode.IsRoomComponentSelected or function() return false end
C_HousingCustomizeMode.RoomComponentSupportsVariant = C_HousingCustomizeMode.RoomComponentSupportsVariant or function() return false end
C_HousingCustomizeMode.RoomConnectionSupportsDoorType = C_HousingCustomizeMode.RoomConnectionSupportsDoorType or function() return false end
C_HousingCustomizeMode.SetRoomComponentCeilingType = C_HousingCustomizeMode.SetRoomComponentCeilingType or function() end
C_HousingCustomizeMode.SetRoomComponentDoorType = C_HousingCustomizeMode.SetRoomComponentDoorType or function() end

secureexecuterange = secureexecuterange or function(values, callback, ...)
    for key, value in pairs(values or {}) do
        callback(key, value, ...)
    end
end

CreateSecureDelegate = CreateSecureDelegate or function(callback)
    return function(...) return callback(...) end
end

function GetOrCreateTableEntry(owner, key)
    local value = owner[key]
    if value == nil then
        value = {}
        owner[key] = value
    end
    return value
end

function CreateCounter(initial)
    local value = initial or 0
    return function()
        value = value + 1
        return value
    end
end

function GenerateClosure(callback, ...)
    local arguments = { ... }
    local count = select("#", ...)
    return function(...)
        return callback(unpack(arguments, 1, count), ...)
    end
end

function SafePack(...)
    return { n = select("#", ...), ... }
end

function SafeUnpack(values, first)
    if type(values) ~= "table" then return end
    return unpack(values, first or 1, values.n or #values)
end

function CopyTable(source, shallow)
    if type(source) ~= "table" then return source end
    local seen = {}
    local function copy(value)
        if type(value) ~= "table" then return value end
        if seen[value] then return seen[value] end
        local result = {}
        seen[value] = result
        for key, child in pairs(value) do
            result[shallow and key or copy(key)] = shallow and child or copy(child)
        end
        return result
    end
    return copy(source)
end

function hooksecurefunc(target, method, hook)
    local owner, name
    if type(target) == "table" then
        owner, name = target, method
    else
        owner, name, hook = _G, target, method
    end
    local original = owner[name]
    owner[name] = function(...)
        local results = original and { original(...) } or {}
        hook(...)
        return unpack(results)
    end
end

function tostringall(...)
    local result = {}
    for index = 1, select("#", ...) do result[index] = tostring(select(index, ...)) end
    return unpack(result)
end

function debugstack(threadOrStart, startOrCount1, count1OrCount2, count2)
    local thread
    local start
    local count
    if type(threadOrStart) == "thread" then
        thread = threadOrStart
        start = startOrCount1 or 0
        count = count1OrCount2 or 12
    else
        start = threadOrStart or 1
        count = startOrCount1 or 12
    end

    local lines = {}
    local firstLevel = start + 1
    for level = firstLevel, firstLevel + count - 1 do
        local info
        if thread then
            info = debug.getinfo(thread, level, "Sln")
        else
            info = debug.getinfo(level, "Sln")
        end
        if not info then break end
        local source = info.source or "=[C]"
        local location
        if source:sub(1, 1) == "=" then
            location = source:sub(2)
        else
            location = "[string \"" .. source .. "\"]"
        end
        if info.currentline and info.currentline >= 0 then
            location = location .. ":" .. info.currentline
        end
        if info.name then
            lines[#lines + 1] = location .. ": in function `" .. info.name .. "'"
        elseif info.what == "main" then
            lines[#lines + 1] = location .. ": in main chunk"
        else
            lines[#lines + 1] = location .. ": in function <" .. source ..
                ":" .. tostring(info.linedefined or 0) .. ">"
        end
    end
    return table.concat(lines, "\n")
end

debuglocals = debuglocals or function() return "" end
GetCallstackHeight = GetCallstackHeight or function() return 1 end
GetErrorCallstackHeight = GetErrorCallstackHeight or function() return 1 end
SetErrorCallstackHeight = SetErrorCallstackHeight or function() end
addframetext = addframetext or function() end

local wowAddonLabErrorHandler
seterrorhandler = seterrorhandler or function(callback)
    wowAddonLabErrorHandler = callback
end
geterrorhandler = geterrorhandler or function()
    return wowAddonLabErrorHandler or function(message) print(message) end
end
CallErrorHandler = CallErrorHandler or function(message)
    return geterrorhandler()(message)
end

function __WoWAddonLabTraceback(message)
    return debug.traceback(tostring(message), 2)
end

UISpecialFrames = UISpecialFrames or {}
SlashCmdList = SlashCmdList or {}
SOUNDKIT = SOUNDKIT or {}
Enum = Enum or {}
Enum.UITextureSliceMode = Enum.UITextureSliceMode or {
    Stretched = 0,
    Tiled = 1,
}
Enum.OnUpdateMode = Enum.OnUpdateMode or {
    Disabled = 0,
    RunWhenVisible = 1,
    RunWhenVisibleOnce = 2,
    RunOnce = 3,
    RunAlways = 4,
}
Enum.TableSecurityOption = Enum.TableSecurityOption or {
    DisallowTaintedAccess = 0,
    DisallowSecretKeys = 1,
    SecretWrapContents = 2,
}

local function CreateLazyEnum(enumName)
    local nextValue = 1
    return setmetatable({ None = 0 }, {
        __index = function(values, key)
            if key == "NumValues" and string.find(enumName, "Meta$", 1) then
                rawset(values, key, nextValue)
                return nextValue
            end
            local value = nextValue
            nextValue = nextValue + 1
            rawset(values, key, value)
            return value
        end,
    })
end

setmetatable(Enum, {
    __index = function(enums, enumName)
        local values = CreateLazyEnum(enumName)
        rawset(enums, enumName, values)
        return values
    end,
})

setmetatable(SOUNDKIT, {
    __index = function(values, key)
        local value = 0
        rawset(values, key, value)
        return value
    end,
})

function __WoWAddonLabInvokeSlashCommand(command, arguments)
    local normalized = string.lower(command or "")
    for key, callback in pairs(SlashCmdList) do
        if type(callback) == "function" then
            local index = 1
            while true do
                local registered = _G["SLASH_" .. key .. index]
                if not registered then break end
                if string.lower(registered) == normalized then
                    callback(arguments or "")
                    return true
                end
                index = index + 1
            end
        end
    end
    return false
end

PLAYER_DIFFICULTY_MYTHIC_PLUS = PLAYER_DIFFICULTY_MYTHIC_PLUS or "Mythic+"
LFG_TYPE_DUNGEON = LFG_TYPE_DUNGEON or "Dungeon"
BATTLEFIELDS = BATTLEFIELDS or "Battlegrounds"
ARENA = ARENA or "Arena"
RAID = RAID or "Raid"
SCENARIOS_PVEFRAME = SCENARIOS_PVEFRAME or "Scenarios"
DELVES_LABEL = DELVES_LABEL or "Delves"
BINDING_HEADER_HOUSING_SYSTEM = BINDING_HEADER_HOUSING_SYSTEM or "Housing"

IsFalling = IsFalling or function() return false end
IsOutdoors = IsOutdoors or function() return true end
IsInInstance = IsInInstance or function() return false, "none" end
UnitIsDeadOrGhost = UnitIsDeadOrGhost or function() return false end
IsMounted = IsMounted or function() return false end
UnitInVehicle = UnitInVehicle or function() return false end
UnitHasVehicleUI = UnitHasVehicleUI or function() return false end
UnitFullName = UnitFullName or function() return "Player", "Emulator" end
GetServerTime = GetServerTime or function() return os.time() end
GetCurrentRegion = GetCurrentRegion or function() return 3 end
GetCurrentEnvironment = GetCurrentEnvironment or function() return getfenv(2) end
GetFonts = GetFonts or function() return {} end
GetFontInfo = GetFontInfo or function() return nil end
IsMacClient = IsMacClient or function() return false end
IsGMClient = IsGMClient or function() return false end
AddSourceLocationExclude = AddSourceLocationExclude or function() end
RegisterUIPanel = RegisterUIPanel or function() end
SwapToGlobalEnvironment = SwapToGlobalEnvironment or function() setfenv(2, _G) end
UnitClass = UnitClass or function() return "Warrior", "WARRIOR", 1 end
UnitNameUnmodified = UnitNameUnmodified or function() return "Player" end
UnitPVPName = UnitPVPName or function() return "Player" end
LocalizedClassList = LocalizedClassList or function()
    return {
        WARRIOR = "Warrior", MAGE = "Mage", ROGUE = "Rogue", DRUID = "Druid",
        HUNTER = "Hunter", SHAMAN = "Shaman", PRIEST = "Priest",
        WARLOCK = "Warlock", PALADIN = "Paladin", DEATHKNIGHT = "Death Knight",
        MONK = "Monk", DEMONHUNTER = "Demon Hunter", EVOKER = "Evoker",
    }
end

local cvars = {}
C_CVar = C_CVar or {}
C_CVar.RegisterCVar = C_CVar.RegisterCVar or function(name, default)
    if cvars[name] == nil then cvars[name] = tostring(default or "") end
end
C_CVar.SetCVar = C_CVar.SetCVar or function(name, value) cvars[name] = tostring(value or "") end
C_CVar.GetCVar = C_CVar.GetCVar or function(name) return cvars[name] end
C_CVar.GetCVarBool = C_CVar.GetCVarBool or function(name)
    local value = C_CVar.GetCVar(name)
    return value == "1" or value == "true"
end
GetCVar = GetCVar or C_CVar.GetCVar
GetCVarBool = GetCVarBool or C_CVar.GetCVarBool
GetCVarDefault = GetCVarDefault or C_CVar.GetCVarDefault
GetCVarInfo = GetCVarInfo or C_CVar.GetCVarInfo
SetCVar = SetCVar or function(name, value)
    if type(value) == "boolean" then value = value and "1" or "0" end
    return C_CVar.SetCVar(name, value)
end

C_UnitAuras = C_UnitAuras or {}
C_UnitAuras.GetPlayerAuraBySpellID = C_UnitAuras.GetPlayerAuraBySpellID or function() return nil end

C_EventUtils = C_EventUtils or {}
C_EventUtils.IsEventValid = C_EventUtils.IsEventValid or function() return true end

Constants = Constants or {}
setmetatable(Constants, {
    __index = function(constants, groupName)
        local nextValue = 0
        local group = setmetatable({}, {
            __index = function(values, key)
                local value = nextValue
                nextValue = nextValue + 1
                rawset(values, key, value)
                return value
            end,
        })
        rawset(constants, groupName, group)
        return group
    end,
})

C_Glue = C_Glue or {}
C_Glue.IsOnGlueScreen = C_Glue.IsOnGlueScreen or function() return false end

C_SettingsUtil = C_SettingsUtil or {}
C_SettingsUtil.NotifySettingsLoaded = C_SettingsUtil.NotifySettingsLoaded or function() end

CVarCallbackRegistry = CVarCallbackRegistry or {}
CVarCallbackRegistry.RegisterCallback = CVarCallbackRegistry.RegisterCallback or function() end
CVarCallbackRegistry.RegisterCallbackForAllCVarUpdates =
    CVarCallbackRegistry.RegisterCallbackForAllCVarUpdates or function() end
CVarCallbackRegistry.SetCVarCachable = CVarCallbackRegistry.SetCVarCachable or function() end
CVarCallbackRegistry.GetCVarValue = CVarCallbackRegistry.GetCVarValue or function(_, name)
    return C_CVar.GetCVar(name)
end
CVarCallbackRegistry.GetCVarValueBool =
    CVarCallbackRegistry.GetCVarValueBool or function(self, name)
        local value = self:GetCVarValue(name)
        return value == "1" or value == "true"
    end
CVarCallbackRegistry.GetCVarNumberOrDefault =
    CVarCallbackRegistry.GetCVarNumberOrDefault or function(self, name)
        return tonumber(self:GetCVarValue(name)) or 0
    end

C_Log = C_Log or {}
C_Log.LogErrorMessage = C_Log.LogErrorMessage or function() end

C_RestrictedActions = C_RestrictedActions or {}
C_RestrictedActions.CheckAllowProtectedFunctions =
    C_RestrictedActions.CheckAllowProtectedFunctions or function() return true end

C_QuestLog = C_QuestLog or {}
C_QuestLog.RequestLoadQuestByID = C_QuestLog.RequestLoadQuestByID or function() end
C_QuestLog.ReadyForTurnIn = C_QuestLog.ReadyForTurnIn or function() return false end
C_QuestLog.GetQuestAdditionalHighlights =
    C_QuestLog.GetQuestAdditionalHighlights or function() return nil end

C_Item = C_Item or {}
C_Item.RequestLoadItemDataByID = C_Item.RequestLoadItemDataByID or function() end

C_Spell = C_Spell or {}
C_Spell.RequestLoadSpellData = C_Spell.RequestLoadSpellData or function() end


C_AddOns.GetNumAddOns = C_AddOns.GetNumAddOns or function() return 0 end
C_AddOns.EnableAddOn = C_AddOns.EnableAddOn or function() return true end

C_SuperTrack = C_SuperTrack or {}
C_SuperTrack.GetSuperTrackedQuestID = C_SuperTrack.GetSuperTrackedQuestID or function() return 0 end
C_SuperTrack.SetSuperTrackedQuestID = C_SuperTrack.SetSuperTrackedQuestID or function() end
C_SuperTrack.GetSuperTrackedMapPin = C_SuperTrack.GetSuperTrackedMapPin or function() return nil end
C_SuperTrack.GetSuperTrackedVignette = C_SuperTrack.GetSuperTrackedVignette or function() return nil end
C_SuperTrack.GetSuperTrackedContent = C_SuperTrack.GetSuperTrackedContent or function() return nil end
C_SuperTrack.ClearSuperTrackedMapPin = C_SuperTrack.ClearSuperTrackedMapPin or function() end
C_SuperTrack.IsSuperTrackingAnything = C_SuperTrack.IsSuperTrackingAnything or function() return false end

C_Sound = C_Sound or {}
C_Sound.PlaySound = C_Sound.PlaySound or PlaySound
C_Sound.GetSoundScaledVolume = C_Sound.GetSoundScaledVolume or function() return 1 end
C_Sound.GetSoundScaledPitch = C_Sound.GetSoundScaledPitch or function() return 1 end

C_GameRules = C_GameRules or {}
C_GameRules.IsGameRuleActive = C_GameRules.IsGameRuleActive or function() return false end
C_GameRules.GetActiveGameMode = C_GameRules.GetActiveGameMode or function()
    return Enum.GameMode.Standard
end

C_ScriptedAnimations = C_ScriptedAnimations or {}
C_ScriptedAnimations.GetAllScriptedAnimationEffects =
    C_ScriptedAnimations.GetAllScriptedAnimationEffects or function() return {} end

C_AutoComplete = C_AutoComplete or {}
C_AutoComplete.GetAutoCompleteResults =
    C_AutoComplete.GetAutoCompleteResults or function() return nil end
AUTOCOMPLETE_LIST = AUTOCOMPLETE_LIST or {}
AUTOCOMPLETE_LIST.ADDFRIEND = AUTOCOMPLETE_LIST.ADDFRIEND or { include = 0, exclude = 0 }
AUTOCOMPLETE_LIST.CHANINVITE = AUTOCOMPLETE_LIST.CHANINVITE or { include = 0, exclude = 0 }

C_Club = C_Club or {}
C_Club.GetInvitationCandidates = C_Club.GetInvitationCandidates or function() return nil end
C_Club.GetClubInfo = C_Club.GetClubInfo or function()
    return { clubType = Enum.ClubType.Character }
end

C_CinematicList = C_CinematicList or {}
C_CinematicList.GetUICinematicList = C_CinematicList.GetUICinematicList or function() return {} end

C_TransmogOutfitInfo = C_TransmogOutfitInfo or {}
C_TransmogOutfitInfo.GetAllSlotLocationInfo =
    C_TransmogOutfitInfo.GetAllSlotLocationInfo or function() return nil, nil end

C_VoiceChat = C_VoiceChat or {}
C_VoiceChat.GetPushToTalkBinding = C_VoiceChat.GetPushToTalkBinding or function() return {} end
C_VoiceChat.SetPushToTalkBinding = C_VoiceChat.SetPushToTalkBinding or function() end
C_VoiceChat.GetTtsVoices = C_VoiceChat.GetTtsVoices or function() return {} end
C_VoiceChat.GetActiveChannelID = C_VoiceChat.GetActiveChannelID or function() return nil end
C_VoiceChat.GetActiveChannelType = C_VoiceChat.GetActiveChannelType or function() return nil end
C_VoiceChat.GetChannel = C_VoiceChat.GetChannel or function() return nil end
C_VoiceChat.IsTranscriptionAllowed =
    C_VoiceChat.IsTranscriptionAllowed or function() return false end
C_VoiceChat.IsMuted = C_VoiceChat.IsMuted or function() return false end
C_VoiceChat.IsSpeakForMeActive = C_VoiceChat.IsSpeakForMeActive or function() return false end

C_Macro = C_Macro or {}
C_Macro.SetMacroExecuteLineCallback = C_Macro.SetMacroExecuteLineCallback or function() end
C_Macro.RunMacroText = C_Macro.RunMacroText or function() end

C_PaperDollInfo = C_PaperDollInfo or {}
C_PaperDollInfo.IsRangedSlotShown = C_PaperDollInfo.IsRangedSlotShown or function() return false end

C_Bank = C_Bank or {}
C_Bank.FetchNumPurchasedBankTabs = C_Bank.FetchNumPurchasedBankTabs or function() return 0 end

C_Container = C_Container or {}
C_Container.GetContainerNumFreeSlots =
    C_Container.GetContainerNumFreeSlots or function() return 0, 0 end
C_Container.GetContainerFreeSlots = C_Container.GetContainerFreeSlots or function() return {} end
C_Container.GetContainerItemInfo = C_Container.GetContainerItemInfo or function() return nil end

local moduleValues = { [false] = {}, [true] = {} }
local moduleNamesBySource = {}
local moduleDependencies = {}
local nilModule = {}

function __WoWAddonLabRegisterModuleSource(moduleName, source)
    moduleNamesBySource[source] = moduleName
end

function __WoWAddonLabRegisterModule(moduleName, isSecure, value)
    moduleValues[isSecure][string.lower(moduleName)] = value == nil and nilModule or value
end

function __WoWAddonLabSetModuleDependencies(addonName, dependencies)
    local names = {}
    for name in pairs(dependencies) do
        names[string.lower(name)] = true
    end
    moduleDependencies[string.lower(addonName)] = names
end

import = import or function(name)
    if type(name) ~= "string" then
        error("Usage: import(moduleName)", 2)
    end
    if name == "" then
        error("Invalid import: Module name is empty", 2)
    end
    local caller = moduleNamesBySource[debug.getinfo(2, "S").source]
    if string.sub(name, 1, 1) == "." then
        if not caller then
            error("Invalid import: Can't perform a relative import from a non-module", 2)
        end
        local base, rest = caller, name
        while string.sub(rest, 1, 1) == "." do
            base = string.match(base, "^(.*)%.[^.]*$")
            if not base then
                error("Invalid import: Relative imports may only be used within the same addon", 2)
            end
            rest = string.sub(rest, 2)
        end
        if rest ~= "" then
            name = base .. "." .. rest
        elseif string.find(base, ".", 1, true) then
            error("Invalid import: Relative imports without a trailing identifier may only refer to the addon root", 2)
        else
            name = base
        end
    end
    if not string.find(name, ".", 1, true) then
        name = name .. ".init"
    end
    if caller then
        local callerAddon = string.lower(string.match(caller, "^[^.]*"))
        local targetAddon = string.lower(string.match(name, "^[^.]*"))
        if callerAddon ~= targetAddon and
            not (moduleDependencies[callerAddon] and moduleDependencies[callerAddon][targetAddon]) then
            error("Invalid import: Modules from other addons may only be imported if the calling addon has a direct dependency on the addon being imported", 2)
        end
    end
    local value = moduleValues[getfenv(2) ~= _G][string.lower(name)]
    if value == nil then
        error("Invalid import: No module with that name exists", 2)
    end
    if value == nilModule then
        return nil
    end
    return value
end

C_InputInterfaceStyle = C_InputInterfaceStyle or {}
C_InputInterfaceStyle.GetCurrentStyle = C_InputInterfaceStyle.GetCurrentStyle or function()
    return Enum.InputDeviceInterfaceType.Mkb
end

C_GamePad = C_GamePad or {}
C_GamePad.GetActiveDeviceID = C_GamePad.GetActiveDeviceID or function() return -1 end
C_GamePad.GetDeviceMappedState = C_GamePad.GetDeviceMappedState or function() return nil end
C_GamePad.StickIndexToConfigName = C_GamePad.StickIndexToConfigName or function() return nil end
C_GamePad.SetAllowHoverEventsWithFreeLook = C_GamePad.SetAllowHoverEventsWithFreeLook or function() end

C_GamepadUI = C_GamepadUI or {}
C_GamepadUI.GetFirstGamepadActionStorageSlotIndex =
    C_GamepadUI.GetFirstGamepadActionStorageSlotIndex or function() return 0 end
C_GamepadUI.GetFirstGamepadActionBarStorageSlotIndexForActiveStance =
    C_GamepadUI.GetFirstGamepadActionBarStorageSlotIndexForActiveStance or function() return nil end

C_ActionBar = C_ActionBar or {}
C_ActionBar.ShouldShowKeyring = C_ActionBar.ShouldShowKeyring or function() return false end
C_ActionBar.EnableActionCombatActionRangeCheck =
    C_ActionBar.EnableActionCombatActionRangeCheck or function() end
C_ActionBar.EnterActionButton = C_ActionBar.EnterActionButton or function() end
C_ActionBar.LeaveActionButton = C_ActionBar.LeaveActionButton or function() end

C_AuctionHouse = C_AuctionHouse or {}
C_AuctionHouse.GetItemKeySampleLink = C_AuctionHouse.GetItemKeySampleLink or function() return nil end

C_AutoLoot = C_AutoLoot or {}
C_AutoLoot.SetUseAutoLootToggle = C_AutoLoot.SetUseAutoLootToggle or function() end

C_Bank.ShouldUsePlayerBagsInBank = C_Bank.ShouldUsePlayerBagsInBank or function() return false end

C_BattleNet = C_BattleNet or {}
C_BattleNet.AreHighResTexturesInstalled =
    C_BattleNet.AreHighResTexturesInstalled or function() return true end
C_BattleNet.IsFriendTagSupportedForCurrentGameType =
    C_BattleNet.IsFriendTagSupportedForCurrentGameType or function() return true end
C_BattleNet.SetBlocked = C_BattleNet.SetBlocked or function() end

C_BlizzCon2026 = C_BlizzCon2026 or {}
C_BlizzCon2026.SetExperience = C_BlizzCon2026.SetExperience or function() end

C_CatalogShop = C_CatalogShop or {}
C_CatalogShop.BypassAchievementRequirement =
    C_CatalogShop.BypassAchievementRequirement or function() return false end
C_CatalogShop.ConfirmVCProductPurchase = C_CatalogShop.ConfirmVCProductPurchase or function() end
C_CatalogShop.GetSelectionGroupInfo = C_CatalogShop.GetSelectionGroupInfo or function() return {} end
C_CatalogShop.IsFinishedLoadingVirtualCurrencies =
    C_CatalogShop.IsFinishedLoadingVirtualCurrencies or function() return true end
C_CatalogShop.OpenCatalogShopInteractionFromRewards =
    C_CatalogShop.OpenCatalogShopInteractionFromRewards or function() return "" end
C_CatalogShop.PurchaseProductWithSelectedProducts =
    C_CatalogShop.PurchaseProductWithSelectedProducts or function() return false end
C_CatalogShop.ShouldDisplayUsesTemporaryCharactersWarningText =
    C_CatalogShop.ShouldDisplayUsesTemporaryCharactersWarningText or function() return false end
C_CatalogShop.ValidateOverrideShopInteraction =
    C_CatalogShop.ValidateOverrideShopInteraction or function() return false end

PlatformIsHandheld = PlatformIsHandheld or function() return false end
UIReload = UIReload or function() if ReloadUI then ReloadUI() end end

C_Discord.IsVoiceEnabled = C_Discord.IsVoiceEnabled or function() return false end

C_EditMode = C_EditMode or {}
C_EditMode.GetEditModeDefaultLayout = C_EditMode.GetEditModeDefaultLayout or function()
    return Enum.EditModePresetLayouts.Modern
end

C_ExternalEventURL = C_ExternalEventURL or {}
C_ExternalEventURL.CanShow = C_ExternalEventURL.CanShow or function() return false end

C_GameRules.AccountHasSDEnabled = C_GameRules.AccountHasSDEnabled or function() return false end
C_GameRules.GetForeverExperiencePreset =
    C_GameRules.GetForeverExperiencePreset or function() return nil end
C_GameRules.IsAccountKeyBindingsEnabled =
    C_GameRules.IsAccountKeyBindingsEnabled or function() return false end
C_GameRules.IsHardcoreActive = C_GameRules.IsHardcoreActive or function()
    return C_GameRules.IsGameRuleActive(Enum.GameRule.HardcoreRuleset)
end
C_GameRules.IsRewardsShopEnabled = C_GameRules.IsRewardsShopEnabled or function() return false end
C_GameRules.IsSDHDToggleEnabled = C_GameRules.IsSDHDToggleEnabled or function() return false end
C_GameRules.SetSDHDToggleValue = C_GameRules.SetSDHDToggleValue or function() end

C_GuildInfo = C_GuildInfo or {}
C_GuildInfo.GetPreferredPlaySettings = C_GuildInfo.GetPreferredPlaySettings or function()
    return false, 0, 0, 0, 0, 0
end
C_GuildInfo.GetPreferredPlaySettingsFeatures =
    C_GuildInfo.GetPreferredPlaySettingsFeatures or function() return false, false end
C_GuildInfo.RequestPreferredPlaySettings =
    C_GuildInfo.RequestPreferredPlaySettings or function() return false end
C_GuildInfo.SetPreferredPlaySettings = C_GuildInfo.SetPreferredPlaySettings or function() return false end

C_AccountServices = C_AccountServices or {}
C_AccountServices.GetPreferredPlaySettings = C_AccountServices.GetPreferredPlaySettings or function()
    return false, 0, 0, 0, 0, 0
end
C_AccountServices.GetPreferredPlaySettingsFeatures =
    C_AccountServices.GetPreferredPlaySettingsFeatures or function() return false, false end
C_AccountServices.RequestPreferredPlaySettings =
    C_AccountServices.RequestPreferredPlaySettings or function() return false end
C_AccountServices.SetPreferredPlaySettings =
    C_AccountServices.SetPreferredPlaySettings or function() return false end
C_AccountServices.GetPreferredLocales = C_AccountServices.GetPreferredLocales or function() return {}, {} end
C_AccountServices.GetPreferredDatacenterLocalities =
    C_AccountServices.GetPreferredDatacenterLocalities or function() return {}, {} end

C_Secrets.ShouldUnitIdentityBeSecret = C_Secrets.ShouldUnitIdentityBeSecret or function() return false end
C_BattleNet.CanToggleHighResTexturesWithoutClientReload =
    C_BattleNet.CanToggleHighResTexturesWithoutClientReload or function() return false end
C_Item.GetWeaponEnchantInfo = C_Item.GetWeaponEnchantInfo or function() return {} end

C_HeirloomInfo = C_HeirloomInfo or {}
C_HeirloomInfo.HeirloomsAvailable = C_HeirloomInfo.HeirloomsAvailable or function() return true end
C_MountJournal = C_MountJournal or {}
C_MountJournal.HasDisplayableMount = C_MountJournal.HasDisplayableMount or function() return true end
C_MountJournal.MountEquipmentAvailable =
    C_MountJournal.MountEquipmentAvailable or function() return true end
C_ToyBoxInfo = C_ToyBoxInfo or {}
C_ToyBoxInfo.HasAnyToy = C_ToyBoxInfo.HasAnyToy or function() return true end
C_TransmogCollection = C_TransmogCollection or {}
C_TransmogCollection.HasAnyAppearance = C_TransmogCollection.HasAnyAppearance or function() return true end
C_WarbandScene = C_WarbandScene or {}
C_WarbandScene.WarbandScenesAvailable =
    C_WarbandScene.WarbandScenesAvailable or function() return true end

C_Item.ConfirmDeleteItem = C_Item.ConfirmDeleteItem or function() end
C_Item.DeleteItem = C_Item.DeleteItem or function() end

C_LFGInfo = C_LFGInfo or {}
C_LFGInfo.LeaveInstanceBySpell = C_LFGInfo.LeaveInstanceBySpell or function() end
C_LootFrame = C_LootFrame or {}
C_LootFrame.TryAutoLoot = C_LootFrame.TryAutoLoot or function() end

C_NameUtil = C_NameUtil or {}
C_NameUtil.ReplaceSurnameSeparatorWithLinkSeparator =
    C_NameUtil.ReplaceSurnameSeparatorWithLinkSeparator or function(fullName) return fullName end

C_PaperDollInfo.AmmoNeeded = C_PaperDollInfo.AmmoNeeded or function() return false end
C_PaperDollInfo.CursorCanGoInSlot = C_PaperDollInfo.CursorCanGoInSlot or function() return false end

C_PartyInfo = C_PartyInfo or {}
C_PartyInfo.CanPromoteToLeader = C_PartyInfo.CanPromoteToLeader or function() return false end
C_PartyInfo.IsRaidListEnabled = C_PartyInfo.IsRaidListEnabled or function() return false end
C_PartyInfo.IsRaidListSupported = C_PartyInfo.IsRaidListSupported or function() return false end

C_PetInfo = C_PetInfo or {}
C_PetInfo.CanPetEatItem = C_PetInfo.CanPetEatItem or function() return false end
C_PetInfo.GetPetFoodTypes = C_PetInfo.GetPetFoodTypes or function() return {} end
C_PetInfo.GetPetHappiness = C_PetInfo.GetPetHappiness or function() return 0, 0, 0 end
C_PetInfo.GetPetLoyalty = C_PetInfo.GetPetLoyalty or function() return "" end
C_PetInfo.GetPetTrainingPoints = C_PetInfo.GetPetTrainingPoints or function() return 0, 0 end
C_PetInfo.GetPetUIModelSceneID = C_PetInfo.GetPetUIModelSceneID or function() return 0 end

C_PlayerInfo = C_PlayerInfo or {}
C_PlayerInfo.ShouldDisplaySurname = C_PlayerInfo.ShouldDisplaySurname or function() return false end
C_PlayerInteractionManager = C_PlayerInteractionManager or {}
C_PlayerInteractionManager.ShardTransferConfirm =
    C_PlayerInteractionManager.ShardTransferConfirm or function() end

CanPortGraveyard = CanPortGraveyard or function() return false end
IsOnGroundFloorInJailersTower = IsOnGroundFloorInJailersTower or function() return false end
GetPetExperience = GetPetExperience or function() return 0, 0 end

C_ScenarioInfo = C_ScenarioInfo or {}
C_ScenarioInfo.IsTieredEntranceScenario = C_ScenarioInfo.IsTieredEntranceScenario or function() return false end
GetEvictionTimeRemaining = GetEvictionTimeRemaining or function() return 0 end
GetMeleeHitChanceTakenModifier = GetMeleeHitChanceTakenModifier or function() return 0 end
ReagentsFromBankAllowed = ReagentsFromBankAllowed or function() return true end
ShouldDisplayRaidRolesInSocialFrame = ShouldDisplayRaidRolesInSocialFrame or function() return false end
ShouldDisplayStoryModeTooltipInSocialFrame =
    ShouldDisplayStoryModeTooltipInSocialFrame or function() return false end

C_PvP = C_PvP or {}
C_PvP.GetRandomTrainingGroundArenaRewards =
    C_PvP.GetRandomTrainingGroundArenaRewards or function() return 0, 0, nil, nil, nil end
C_PvP.GetRandomTrainingGroundBGRewards =
    C_PvP.GetRandomTrainingGroundBGRewards or function() return 0, 0, nil, nil, nil end
C_PvP.JoinRandomTrainingGroundBG = C_PvP.JoinRandomTrainingGroundBG or function() end

C_RaidLocks = C_RaidLocks or {}
C_RaidLocks.IsRaidLockExtendFeatureSupported =
    C_RaidLocks.IsRaidLockExtendFeatureSupported or function() return true end
C_RecentAllies = C_RecentAllies or {}
C_RecentAllies.IsInteractionCategoryFilterSupportedForCurrentGameType =
    C_RecentAllies.IsInteractionCategoryFilterSupportedForCurrentGameType or function() return true end

C_SkillInfo = C_SkillInfo or {}
C_SkillInfo.AbandonSkill = C_SkillInfo.AbandonSkill or function() end
C_SkillInfo.GetSkillLineInfoByID = C_SkillInfo.GetSkillLineInfoByID or function() return nil end

C_SocialRestrictions = C_SocialRestrictions or {}
C_SocialRestrictions.AcknowledgeAgeVerificationRestriction =
    C_SocialRestrictions.AcknowledgeAgeVerificationRestriction or function() end
C_SocialRestrictions.IsAgeVerificationRestricted =
    C_SocialRestrictions.IsAgeVerificationRestricted or function() return false end
C_SocialRestrictions.IsAgeVerificationRestrictedMinor =
    C_SocialRestrictions.IsAgeVerificationRestrictedMinor or function() return false end

C_SpecializationInfo = C_SpecializationInfo or {}
C_SpecializationInfo.GetAllClassIDs = C_SpecializationInfo.GetAllClassIDs or function()
    local classIDs = {}
    for index = 1, GetNumClasses() do
        local _, _, classID = GetClassInfo(index)
        if classID then
            table.insert(classIDs, classID)
        end
    end
    return classIDs
end
C_SpecializationInfo.HasPlayerEarnedATalentPoint =
    C_SpecializationInfo.HasPlayerEarnedATalentPoint or function() return UnitLevel("player") >= 10 end
C_SpecializationInfo.IsSpecSelectionEnabled =
    C_SpecializationInfo.IsSpecSelectionEnabled or function() return true end

C_SpellBook = C_SpellBook or {}
C_SpellBook.GetClassSkillLineInfo = C_SpellBook.GetClassSkillLineInfo or function()
    return C_SpellBook.GetSpellBookSkillLineInfo(2)
end
C_SpellBook.IsSpellBookItemLooseFlyoutMember =
    C_SpellBook.IsSpellBookItemLooseFlyoutMember or function() return false end
C_SpellBook.IsSpellBookItemLowRank = C_SpellBook.IsSpellBookItemLowRank or function() return false end
C_SpellBook.SetBarSlotFromIntro = C_SpellBook.SetBarSlotFromIntro or function() end

C_Spell.CancelItemTempEnchantment = C_Spell.CancelItemTempEnchantment or function() end
C_Spell.GetConfirmationPromptHideOnEscape =
    C_Spell.GetConfirmationPromptHideOnEscape or function() return false end
C_Spell.GetTargetSpellID = C_Spell.GetTargetSpellID or function() return 0 end
C_Spell.CancelAutoRepeatSpell = C_Spell.CancelAutoRepeatSpell or function() return false end
C_Spell.GetItemCooldown = C_Spell.GetItemCooldown or function()
    return { startTime = 0, duration = 0, isEnabled = true, isActive = false, modRate = 1 }
end
C_Spell.IsActiveSpell = C_Spell.IsActiveSpell or function() return false end
C_Spell.PlaceTargetingSpellAtCursor = C_Spell.PlaceTargetingSpellAtCursor or function() end

C_StableInfo = C_StableInfo or {}
C_StableInfo.BuyStableSlot = C_StableInfo.BuyStableSlot or function() end
C_StableInfo.GetNextStableSlotCost = C_StableInfo.GetNextStableSlotCost or function() return 0 end

C_StorePublic = C_StorePublic or {}
C_StorePublic.IsRewardsShopOverrideEnabled =
    C_StorePublic.IsRewardsShopOverrideEnabled or function() return false end

C_TradeSkillUI = C_TradeSkillUI or {}
C_TradeSkillUI.CanTradeSkillShowCraftingUI =
    C_TradeSkillUI.CanTradeSkillShowCraftingUI or function() return true end

C_Trainer = C_Trainer or {}
local categorizeTrainerUI = false
C_Trainer.GetCategorizeTrainerUI = C_Trainer.GetCategorizeTrainerUI or function()
    return categorizeTrainerUI
end
C_Trainer.SetCategorizeTrainerUI = C_Trainer.SetCategorizeTrainerUI or function(value)
    categorizeTrainerUI = value and true or false
end
C_Trainer.GetTrainerType = C_Trainer.GetTrainerType or function() return Enum.TrainerType.General end

C_TransmogOutfitInfo.GetOptionsForSlot =
    C_TransmogOutfitInfo.GetOptionsForSlot or function() return {}, nil end
C_TransmogOutfitInfo.SetViewedOptionForSlot =
    C_TransmogOutfitInfo.SetViewedOptionForSlot or function() end

C_UnitAuras.GetRefreshCarryOverDuration =
    C_UnitAuras.GetRefreshCarryOverDuration or function() return nil end

RegionalUniqueNamesEnabled = RegionalUniqueNamesEnabled or function() return false end
UnitDefenseSkill = UnitDefenseSkill or function() return 0, 0 end
UnitHasEffectivelyTankAura = UnitHasEffectivelyTankAura or function() return false end
UnitIsInInteractRange = UnitIsInInteractRange or function(unit) return UnitExists(unit) end
UnitResistance = UnitResistance or function() return 0, 0, 0, 0 end

C_VideoOptions = C_VideoOptions or {}
C_VideoOptions.AreHighResTexturesAvailable =
    C_VideoOptions.AreHighResTexturesAvailable or function() return true end
C_VideoOptions.IsLinearEnabledOnStart = C_VideoOptions.IsLinearEnabledOnStart or function() return false end
C_VideoOptions.IsOutlineModeSupported = C_VideoOptions.IsOutlineModeSupported or function() return true end
C_VideoOptions.IsPBRWaterSupported = C_VideoOptions.IsPBRWaterSupported or function() return true end
C_VideoOptions.IsSecondaryLightingSupported =
    C_VideoOptions.IsSecondaryLightingSupported or function() return true end

C_VoiceChat.GetActiveVoiceProviderID = C_VoiceChat.GetActiveVoiceProviderID or function()
    return Enum.VoiceProviderID.Legacy
end
C_VoiceChat.RestoreActiveVoiceProvider =
    C_VoiceChat.RestoreActiveVoiceProvider or function() return false end
C_VoiceChat.SetActiveVoiceProvider = C_VoiceChat.SetActiveVoiceProvider or function() return false end

C_WowTokenUI = C_WowTokenUI or {}
C_WowTokenUI.ShouldDisplayInAuctionHouse =
    C_WowTokenUI.ShouldDisplayInAuctionHouse or function() return true end

C_WoWHackAppearance = C_WoWHackAppearance or {}
C_WoWHackAppearance.GetPlayerAuraSpellVisualKitIDs =
    C_WoWHackAppearance.GetPlayerAuraSpellVisualKitIDs or function() return {} end

C_SecureTransfer = C_SecureTransfer or {}
C_SecureTransfer.GetMailReceiveInfo = C_SecureTransfer.GetMailReceiveInfo or function()
    return { sender = "", money = 0 }
end
C_SecureTransfer.GetMailSendInfo = C_SecureTransfer.GetMailSendInfo or function()
    return { target = "", sendMoney = 0 }
end
C_SecureTransfer.GetVCPurchaseProductID = C_SecureTransfer.GetVCPurchaseProductID or function() return 0 end
C_SecureTransfer.ShouldShowTradeRMTWarning =
    C_SecureTransfer.ShouldShowTradeRMTWarning or function() return false end
C_SecureTransfer.TakeMailMoney = C_SecureTransfer.TakeMailMoney or function() end

TurnOrActionToggle = TurnOrActionToggle or function() end

C_ClassColor = C_ClassColor or {}
local classColors = {
    WARRIOR = { 0.78, 0.61, 0.43 },
    PALADIN = { 0.96, 0.55, 0.73 },
    HUNTER = { 0.67, 0.83, 0.45 },
    ROGUE = { 1.00, 0.96, 0.41 },
    PRIEST = { 1.00, 1.00, 1.00 },
    DEATHKNIGHT = { 0.77, 0.12, 0.23 },
    SHAMAN = { 0.00, 0.44, 0.87 },
    MAGE = { 0.25, 0.78, 0.92 },
    WARLOCK = { 0.53, 0.53, 0.93 },
    MONK = { 0.00, 1.00, 0.60 },
    DRUID = { 1.00, 0.49, 0.04 },
    DEMONHUNTER = { 0.64, 0.19, 0.79 },
    EVOKER = { 0.20, 0.58, 0.50 }
}
C_ClassColor.GetClassColor = C_ClassColor.GetClassColor or function(classFile)
    local color = classColors[classFile] or { 0.8, 0.8, 0.8 }
    return CreateColor(color[1], color[2], color[3], 1)
end

C_ColorUtil = C_ColorUtil or {}
C_ColorUtil.GenerateTextColorCode = C_ColorUtil.GenerateTextColorCode or function(color)
    local r, g, b, a = color:GetRGBA()
    return string.format(
        "%.2X%.2X%.2X%.2X",
        math.floor((a or 1) * 255 + 0.5),
        math.floor((r or 0) * 255 + 0.5),
        math.floor((g or 0) * 255 + 0.5),
        math.floor((b or 0) * 255 + 0.5))
end
C_ColorUtil.WrapTextInColorCode = C_ColorUtil.WrapTextInColorCode or function(text, code)
    return "|c" .. tostring(code) .. tostring(text or "") .. "|r"
end
C_ColorUtil.WrapTextInColor = C_ColorUtil.WrapTextInColor or function(text, color)
    return C_ColorUtil.WrapTextInColorCode(text, C_ColorUtil.GenerateTextColorCode(color))
end
C_ColorUtil.ConvertRGBToHSV = C_ColorUtil.ConvertRGBToHSV or function(r, g, b)
    return 0, 0, math.max(r or 0, g or 0, b or 0)
end
C_ColorUtil.ConvertHSVToHSL = C_ColorUtil.ConvertHSVToHSL or function(h, s, v)
    return h or 0, s or 0, v or 0
end

C_UIColor = C_UIColor or {}
C_UIColor.GetColors = C_UIColor.GetColors or function()
    return {
        { baseTag = "NORMAL_FONT_COLOR", color = CreateColor(1, 0.82, 0) },
        { baseTag = "HIGHLIGHT_FONT_COLOR", color = CreateColor(1, 1, 1) },
        { baseTag = "RED_FONT_COLOR", color = CreateColor(1, 0.1, 0.1) },
        { baseTag = "GREEN_FONT_COLOR", color = CreateColor(0.1, 1, 0.1) },
        { baseTag = "GRAY_FONT_COLOR", color = CreateColor(0.5, 0.5, 0.5) },
        { baseTag = "ORANGE_FONT_COLOR", color = CreateColor(1, 0.5, 0.1) },
        { baseTag = "COMMON_GRAY_COLOR", color = CreateColor(0.62, 0.62, 0.62) },
        { baseTag = "UNCOMMON_GREEN_COLOR", color = CreateColor(0.12, 1, 0) },
        { baseTag = "RARE_BLUE_COLOR", color = CreateColor(0, 0.44, 0.87) },
        { baseTag = "EPIC_PURPLE_COLOR", color = CreateColor(0.64, 0.21, 0.93) },
        { baseTag = "LEGENDARY_ORANGE_COLOR", color = CreateColor(1, 0.5, 0) },
        { baseTag = "ARTIFACT_GOLD_COLOR", color = CreateColor(0.9, 0.8, 0.5) },
        { baseTag = "HEIRLOOM_BLUE_COLOR", color = CreateColor(0, 0.8, 1) },
    }
end
PLAYER_FACTION_COLOR_HORDE = CreateColor(0.9, 0.05, 0.07)
PLAYER_FACTION_COLOR_ALLIANCE = CreateColor(0, 0.44, 0.87)

C_ColorOverrides = C_ColorOverrides or {}
C_ColorOverrides.GetColorForQuality = C_ColorOverrides.GetColorForQuality or function(quality)
    local colors = {
        CreateColor(0.62, 0.62, 0.62), CreateColor(1, 1, 1),
        CreateColor(0.12, 1, 0), CreateColor(0, 0.44, 0.87),
        CreateColor(0.64, 0.21, 0.93), CreateColor(1, 0.5, 0),
        CreateColor(0.9, 0.8, 0.5), CreateColor(0, 0.8, 1),
    }
    return colors[(quality or 0) + 1] or CreateColor(1, 1, 1)
end
C_ColorOverrides.GetDefaultColorForQuality = C_ColorOverrides.GetDefaultColorForQuality
    or C_ColorOverrides.GetColorForQuality
C_ColorOverrides.GetColorOverrideInfo = C_ColorOverrides.GetColorOverrideInfo or function()
    return nil
end
C_ColorOverrides.SetColorOverride = C_ColorOverrides.SetColorOverride or function() end
C_ColorOverrides.RemoveColorOverride = C_ColorOverrides.RemoveColorOverride or function() end
C_ColorOverrides.ClearColorOverrides = C_ColorOverrides.ClearColorOverrides or function() end

UnitRace = UnitRace or function() return "Human", "Human", 1 end
UnitSex = UnitSex or function() return 2 end
GetClientDisplayExpansionLevel = GetClientDisplayExpansionLevel or function() return 11 end
GetChatTypeIndex = GetChatTypeIndex or function() return 1 end
GetChatWindowInfo = GetChatWindowInfo or function(id)
    return "Chat " .. tostring(id or 1), nil, 12, 1, 1, 1, false, false, false
end
GetInventorySlotInfo = GetInventorySlotInfo or function(name)
    local slots = { MainHandSlot = 16, SecondaryHandSlot = 17, RangedSlot = 18 }
    return slots[name] or 0
end
RegisterStaticConstants = RegisterStaticConstants or function() end
BACKPACK_CONTAINER = 0
NUM_TOTAL_EQUIPPED_BAG_SLOTS = 4
CreateAtlasMarkup = CreateAtlasMarkup or function(atlas, width, height)
    return string.format("|A:%s:%s:%s|a", tostring(atlas or ""), tostring(width or 0), tostring(height or 0))
end

for _, raidTarget in ipairs({
    "STAR", "CIRCLE", "DIAMOND", "TRIANGLE", "MOON", "SQUARE", "CROSS", "SKULL",
}) do
    for index = 1, 3 do
        _G["ICON_TAG_RAID_TARGET_" .. raidTarget .. index] =
            "{" .. string.lower(raidTarget) .. "}"
    end
end
for index, raidTarget in ipairs({
    "STAR", "CIRCLE", "DIAMOND", "TRIANGLE", "MOON", "SQUARE", "CROSS", "SKULL",
}) do
    _G["RAID_TARGET_" .. index] = "{" .. string.lower(raidTarget) .. "}"
    _G["GROUP" .. index .. "_CHAT_TAG1"] = "g" .. index
    _G["GROUP" .. index .. "_CHAT_TAG2"] = "group" .. index
end

EventUtil = EventUtil or {}
EventUtil.ContinueOnAddOnLoaded = EventUtil.ContinueOnAddOnLoaded or function(name, callback)
    C_Timer.After(0, callback)
end
EventUtil.ContinueAfterAllEvents = EventUtil.ContinueAfterAllEvents or function(callback)
    C_Timer.After(0, callback)
end

EventRegistry = EventRegistry or {}
EventRegistry.RegisterFrameEventAndCallback =
    EventRegistry.RegisterFrameEventAndCallback or function(self, event, callback) end

function CreateDataProvider(collection)
    local provider = { collection = collection or {} }
    function provider:Insert(value) table.insert(self.collection, value) end
    function provider:InsertTable(values)
        for _, value in ipairs(values or {}) do table.insert(self.collection, value) end
    end
    function provider:Flush() wipe(self.collection) end
    function provider:GetSize() return #self.collection end
    function provider:EnumerateEntireRange() return ipairs(self.collection) end
    return provider
end

function CreateScrollBoxListLinearView()
    local view = {}
    function view:SetElementInitializer(frameType, initializer)
        self.frameType = frameType
        self.elementInitializer = initializer
    end
    function view:SetElementExtent(extent) self.elementExtent = extent end
    function view:SetPadding(...) self.padding = { ... } end
    function view:SetDataProvider(provider) self.dataProvider = provider end
    return view
end

ScrollUtil = ScrollUtil or {}
ScrollUtil.InitScrollBoxListWithScrollBar =
    ScrollUtil.InitScrollBoxListWithScrollBar or function(scrollBox, scrollBar, view)
        scrollBox.scrollView = view
        scrollBox.SetDataProvider = scrollBox.SetDataProvider or function(self, provider)
            self.dataProvider = provider
            view:SetDataProvider(provider)
        end
        scrollBox.GetDataProvider = scrollBox.GetDataProvider or function(self)
            return self.dataProvider
        end
        scrollBox.ForEachFrame = scrollBox.ForEachFrame or function(self, callback)
            for _, frame in ipairs(self.frames or {}) do callback(frame) end
        end
        scrollBox.GetScrollPercentage =
            scrollBox.GetScrollPercentage or function(self) return self.scrollPercentage or 0 end
        scrollBox.SetScrollPercentage = scrollBox.SetScrollPercentage or function(self, value)
            self.scrollPercentage = value
        end
    end

PanelTemplates_TabResize = PanelTemplates_TabResize or function() end
PanelTemplates_SetTab = PanelTemplates_SetTab or function(frame, tabID) frame.selectedTab = tabID end
PanelTemplates_SetNumTabs = PanelTemplates_SetNumTabs or function(frame, count) frame.numTabs = count end

FrameUtil = FrameUtil or {}
FrameUtil.SpecializeFrameWithMixins =
    FrameUtil.SpecializeFrameWithMixins or function(frame, ...) return Mixin(frame, ...) end

local function CreateMenuDescription()
    local description = {}
    function description:CreateTitle(text) self.title = text return self end
    function description:CreateCheckbox(text, isSelected, setSelected) return self end
    function description:CreateRadio(text, isSelected, setSelected) return self end
    function description:CreateButton(text, callback)
        local child = CreateMenuDescription()
        child.text = text
        child.callback = callback
        return child
    end
    function description:CreateDivider() return self end
    return description
end

MenuUtil = MenuUtil or {}
MenuUtil.CreateContextMenu = MenuUtil.CreateContextMenu or function(owner, generator)
    local root = CreateMenuDescription()
    generator(owner, root)
    return root
end

string.join = string.join or function(separator, ...)
    local values = {}
    for index = 1, select("#", ...) do values[index] = tostring(select(index, ...)) end
    return table.concat(values, separator)
end

string.split = string.split or function(delimiters, value, pieces)
    return unpack(strsplittable(delimiters, value, pieces))
end

strsplit = strsplit or string.split
strjoin = strjoin or string.join

local globalMetatable = getmetatable(_G) or {}
local previousGlobalIndex = globalMetatable.__index
__WoWAddonLabBootstrapFallbackEnabled = false
__WoWAddonLabBootstrapStringFallbackEnabled = false
__WoWAddonLabLegacyConstantFallbacks = {}
local nextLegacyConstantFallback = -1000000
local bootstrapStringDenyList = {
    EDIT_MODE_MODERN_SYSTEM_MAP = true,
    EDIT_MODE_CLASSIC_SYSTEM_MAP = true,
    EDIT_MODE_OVERRIDE_LAYOUTS = true,
    EDIT_MODE_OVERRIDE_LAYOUT_MAP = true,
    LAST_ACTIVE_CHAT_EDIT_BOX = true,
}
globalMetatable.__index = function(values, key)
    local previous
    if type(previousGlobalIndex) == "function" then
        previous = previousGlobalIndex(values, key)
    elseif type(previousGlobalIndex) == "table" then
        previous = previousGlobalIndex[key]
    end
    if previous ~= nil then return previous end
    if __WoWAddonLabBootstrapFallbackEnabled and
        type(key) == "string" and
        string.find(key, "^LE_[A-Z0-9_]+$") then
        local fallback = nextLegacyConstantFallback
        nextLegacyConstantFallback = nextLegacyConstantFallback - 1
        rawset(values, key, fallback)
        __WoWAddonLabLegacyConstantFallbacks[key] = fallback
        return fallback
    end
    if __WoWAddonLabBootstrapFallbackEnabled and
        __WoWAddonLabBootstrapStringFallbackEnabled and
        type(key) == "string" and
        not bootstrapStringDenyList[key] and
        (not string.find(key, "%d") or string.find(key, "^SLASH_CAA_")) and
        string.find(key, "^[A-Z][A-Z0-9_]*$") then
        local fallback = string.find(key, "^SLASH_") and string.lower(key) or key
        rawset(values, key, fallback)
        return fallback
    end
    return nil
end
setmetatable(_G, globalMetatable)
