
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

C_ClassColor = C_ClassColor or {}
C_ClassColor.GetClassColor = C_ClassColor.GetClassColor or function()
    return CreateColor(0.8, 0.8, 0.8, 1)
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
