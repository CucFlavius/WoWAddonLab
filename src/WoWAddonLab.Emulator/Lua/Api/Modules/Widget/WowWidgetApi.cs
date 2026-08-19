namespace WoWAddonLab.Emulator.Lua;

internal static class WowWidgetApi
{
    private static readonly HashSet<string> CreatableFrameXmlObjectTypes = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "ArchaeologyDigSiteFrame", "BlobFrame", "Browser", "Button",
        "CheckButton", "Checkout", "CinematicModel", "ColorSelect", "Cooldown",
        "DressUpModel", "EditBox", "FogOfWarFrame", "FontString", "Frame",
        "GameTooltip", "Line", "MapScene", "MessageFrame", "Minimap", "Model",
        "ModelFFX", "ModelScene", "MovieFrame", "NamePlate", "OffscreenFrame",
        "PlayerModel", "QuestPOIFrame", "ScenarioPOIFrame", "ScrollFrame",
        "ScrollingMessageFrame", "SimpleHTML", "Slider", "StatusBar", "TabardModel",
        "Texture", "UICamera",
        "UnitPositionFrame", "WorldFrame"
    };

    private static readonly string[] ScriptObject =
    [
        "GetName", "GetObjectType", "HasAnySecretAspect", "HasSecretAspect",
        "HasSecretValues", "IsForbidden", "IsObjectType", "IsPreventingSecretValues",
        "SetForbidden", "SetToDefaults"
    ];

    private static readonly string[] ScriptRegion =
    [
        "GetName", "GetObjectType", "HasAnySecretAspect", "HasSecretAspect",
        "HasSecretValues", "IsForbidden", "IsObjectType", "IsPreventingSecretValues",
        "SetForbidden", "SetToDefaults",
        "ClearParentKey", "GetDebugName", "GetParent", "GetParentKey", "SetParentKey"
    ];

    private static readonly string[] Region =
    [
        "AdjustPointsOffset", "CanChangeProtectedState", "ClearAllPoints", "ClearParentKey",
        "ClearPoint", "ClearPointsOffset", "ClearScripts", "CollapsesLayout",
        "CreateAnimationGroup", "GetAnimationGroups", "GetAlpha", "GetBottom", "GetCenter", "GetDebugName",
        "GetEffectiveScale", "GetHeight", "GetLeft", "GetName", "GetObjectType", "GetParentKey",
        "GetDrawLayer", "GetNumPoints", "GetParent", "GetPoint", "GetRight", "GetScale", "GetScript", "HasScript", "GetTop",
        "GetPointByName", "GetRect", "GetScaledRect", "GetSize", "GetSourceLocation",
        "GetWidth", "Hide", "HookScript", "Intersects",
        "GetVertexColor",
        "EnableMouse", "EnableMouseMotion", "EnableMouseWheel",
        "CanPropagateMouseClicks", "CanPropagateMouseMotion",
        "HasAnySecretAspect", "HasSecretAspect", "HasSecretValues",
        "IsAnchoringRestricted", "IsAnchoringSecret", "IsCollapsed", "IsDragging",
        "IsForbidden", "IsIgnoringParentAlpha", "IsIgnoringParentScale",
        "IsMouseClickEnabled", "IsMouseEnabled", "IsMouseMotionEnabled",
        "IsMouseMotionFocus",
        "IsMouseOver",
        "IsMouseWheelEnabled",
        "IsObjectLoaded", "IsObjectType", "IsPreventingSecretValues", "IsProtected",
        "IsRectValid", "IsShown", "IsVisible",
        "SetAllPoints", "SetAlpha", "SetAlphaFromBoolean", "SetCollapsesLayout", "SetHeight",
        "SetIgnoreParentAlpha", "SetIgnoreParentScale", "SetParent", "SetPointsOffset",
        "SetDrawLayer", "SetForbidden", "SetMouseClickEnabled", "SetMouseMotionEnabled", "SetParentKey",
        "SetPassThroughButtons", "SetPoint", "SetPropagateMouseClicks",
        "SetPropagateMouseMotion", "SetScale",
        "SetScript", "SetShown", "SetSize", "SetToDefaults", "SetVertexColor",
        "SetVertexColorFromBoolean", "SetWidth", "ShouldButtonPassThrough", "Show",
        "StopAnimating"
    ];

    private static readonly string[] Frame =
    [
        "AbortDrag", "CanChangeAttribute", "ClearAlphaGradient", "ClearAttribute",
        "ClearAttributes", "CreateFontString", "CreateLine", "CreateMaskTexture",
        "CreateTexture", "DesaturateHierarchy", "DisableDrawLayer", "DoesClipChildren",
        "DoesHyperlinkPropagateToParent", "EnableDrawLayer", "EnableGamePadButton",
        "EnableGamePadStick", "EnableKeyboard", "ExecuteAttribute", "GetAlpha",
        "GetAttribute", "GetBoundsRect", "GetChildren", "GetClampRectInsets",
        "GetDontSavePosition", "GetEffectiveAlpha", "GetEffectiveScale",
        "GetEffectivelyFlattensRenderLayers", "GetFlattensRenderLayers",
        "GetFrameLevel", "GetFrameStrata", "GetHighestFrameLevel",
        "GetHitRectInsets", "GetHyperlinksEnabled", "GetID", "GetNumChildren",
        "GetNumRegions", "GetOnUpdateMode", "GetPropagateKeyboardInput", "GetRaisedFrameLevel",
        "GetRegions", "GetResizeBounds", "GetScale", "GetWindow",
        "HasAlphaGradient", "HasFixedFrameLevel", "HasFixedFrameStrata", "Hide",
        "InterceptStartDrag", "IsClampedToScreen", "IsDrawLayerEnabled",
        "IsEventRegistered", "IsFrameBuffer", "IsGamePadButtonEnabled",
        "IsGamePadStickEnabled", "IsHighlightLocked", "IsIgnoringChildrenForBounds",
        "IsIgnoringParentAlpha", "IsIgnoringParentScale", "IsKeyboardEnabled",
        "IsMovable", "IsObjectLoaded", "IsResizable", "IsShown", "IsToplevel",
        "IsUserPlaced", "IsUsingParentLevel", "IsVisible", "LockHighlight", "Lower",
        "Raise", "RegisterAllEvents", "RegisterEvent", "RegisterEventCallback",
        "RegisterForDrag", "RegisterUnitEvent", "RegisterUnitEventCallback",
        "AddRoleset", "GetRolesetNames", "IsRolesetFiltered", "RemoveRoleset", "SetRolesets",
        "RotateTextures", "SetAlpha", "SetAlphaFromBoolean", "SetAlphaGradient",
        "SetAttribute", "SetAttributeNoHandler", "SetClampRectInsets",
        "SetClampedToScreen", "SetClipsChildren", "SetDontSavePosition",
        "SetDrawLayerEnabled", "SetFixedFrameLevel", "SetFixedFrameStrata",
        "SetFlattensRenderLayers", "SetFrameLevel", "SetFrameStrata",
        "SetHighlightLocked", "SetHitRectInsets", "SetHyperlinkPropagateToParent",
        "SetHyperlinksEnabled", "SetID", "SetIgnoreParentAlpha",
        "SetIgnoreParentScale", "SetIgnoringChildrenForBounds", "SetIsFrameBuffer",
        "SetMovable", "SetOnUpdateMode", "SetPropagateKeyboardInput", "SetResizable", "SetResizeBounds",
        "SetScale", "SetShown", "SetToplevel", "SetUserPlaced",
        "SetUsingParentLevel", "SetWindow", "Show", "StartMoving", "StartSizing",
        "StopMovingOrSizing", "UnlockHighlight", "UnregisterAllEvents",
        "UnregisterEvent"
    ];

    private static readonly string[] Button =
    [
        "ClearDisabledTexture", "ClearHighlightTexture", "ClearNormalTexture", "ClearPushedTexture",
        "Click", "Disable", "Enable", "GetButtonState", "GetDisabledTexture", "GetHighlightTexture", "GetNormalTexture",
        "GetDisabledFontObject", "GetHighlightFontObject", "GetNormalFontObject",
        "GetFontString", "GetMotionScriptsWhileDisabled", "GetPushedTextOffset", "GetPushedTexture", "GetText",
        "GetTextHeight", "GetTextWidth", "IsEnabled",
        "RegisterForClicks", "RegisterForMouse", "SetFormattedText",
        "SetDisabledAtlas", "SetDisabledTexture", "SetHighlightAtlas", "SetHighlightTexture", "SetNormalAtlas",
        "SetDisabledFontObject", "SetHighlightFontObject", "SetNormalFontObject", "SetNormalTexture",
        "SetEnabled", "SetFontString", "SetMotionScriptsWhileDisabled", "SetPushedAtlas", "SetPushedTexture",
        "SetPushedTextOffset", "SetButtonState", "SetText"
    ];

    private static readonly string[] CheckButton =
    [
        "GetChecked", "GetCheckedTexture", "GetDisabledCheckedTexture",
        "SetChecked", "SetCheckedTexture", "SetDisabledCheckedTexture"
    ];

    private static readonly string[] Texture =
    [
        "AddMaskTexture", "GetMaskTexture", "GetNumMaskTextures", "RemoveMaskTexture",
        "ClearTextureSlice", "ClearVertexOffsets",
        "GetAtlas", "GetBlendMode", "GetDesaturation", "GetHorizTile", "GetRotation", "GetTexCoord", "GetTexture",
        "GetTextureFileID", "GetTextureFilePath",
        "GetTextureSliceMargins", "GetTextureSliceMode",
        "GetTexelSnappingBias", "GetVertTile", "GetVertexColor", "GetVertexOffset",
        "IsBlockingLoadRequested", "IsDesaturated", "IsSnappingToPixelGrid", "SetAtlas",
        "SetBlendMode", "SetBlockingLoadRequested", "SetColorTexture", "SetDesaturated", "SetDesaturation", "SetGradient",
        "SetHorizTile", "ResetTexCoord", "SetMask", "SetRotation", "SetSnapToPixelGrid", "SetSpriteSheetCell", "SetTexCoord",
        "SetTexelSnappingBias", "SetTexture", "SetTextureSliceMargins", "SetTextureSliceMode",
        "SetVertTile", "SetVertexColor", "SetVertexOffset"
    ];

    private static readonly string[] Font =
    [
        "CopyFontObject", "GetAlpha", "GetFont", "GetFontHeight", "GetFontObject",
        "GetFontObjectForAlphabet", "GetIndentedWordWrap", "GetJustifyH", "GetJustifyV",
        "GetShadowColor", "GetShadowOffset", "GetSpacing", "GetTextColor", "SetAlpha",
        "SetFont", "SetFontHeight", "SetFontObject", "SetIndentedWordWrap", "SetJustifyH",
        "SetJustifyV", "SetShadowColor", "SetShadowOffset", "SetSpacing", "SetTextColor"
    ];

    private static readonly string[] FontString =
    [
        "CalculateScreenAreaFromCharacterSpan",
        "CanNonSpaceWrap", "CanWordWrap", "ClearAlphaGradient", "ClearText",
        "FindCharacterIndexAtCoordinate", "GetAlphaGradient", "GetFieldSize",
        "GetFont", "GetFontHeight", "GetFontObject", "GetIndentedWordWrap",
        "GetJustifyH", "GetJustifyV", "GetLineHeight", "GetMaxLines",
        "GetNumLines", "GetRotation", "GetScaleAnimationMode", "GetShadowColor",
        "GetShadowOffset", "GetSmoothScaling", "GetSpacing", "GetStringHeight",
        "GetStringWidth", "GetText", "GetTextColor", "GetTextScale",
        "GetUnboundedStringWidth", "GetUnboundedStringWidthForText",
        "GetWrappedWidth", "IsTruncated", "OnColorsUpdated", "SetAlphaGradient",
        "SetFixedColor", "SetFont", "SetFontHeight", "SetFontObject",
        "SetFormattedText", "SetIndentedWordWrap", "SetJustifyH", "SetJustifyV",
        "SetMaxLines", "SetNonSpaceWrap", "SetRotation", "SetScaleAnimationMode",
        "SetShadowColor", "SetShadowOffset", "SetSmoothScaling", "SetSpacing",
        "SetText", "SetTextColor", "SetTextHeight", "SetTextScale",
        "SetTextToFit", "SetWordWrap"
    ];

    private static readonly string[] EditBox =
    [
        "AddHistoryLine", "ClearFocus", "ClearHighlightText", "ClearHistory",
        "Disable", "Enable", "GetAltArrowKeyMode", "GetBlinkSpeed",
        "GetCursorPosition", "GetDisplayText", "GetFont", "GetFontObject",
        "GetHighlightColor", "GetHistoryLines", "GetIndentedWordWrap",
        "GetInputLanguage", "GetJustifyH", "GetJustifyV", "GetMaxBytes",
        "GetMaxLetters", "GetNumLetters", "GetNumLines", "GetNumber",
        "GetShadowColor", "GetShadowOffset", "GetSpacing", "GetText",
        "GetTextColor", "GetTextInsets", "GetUTF8CursorPosition",
        "GetVisibleTextByteLimit", "HasFocus", "HasText", "HighlightText",
        "Insert", "IsAlphabeticOnly", "IsAutoFocus", "IsCountInvisibleLetters",
        "IsEnabled", "IsInIMECompositionMode", "IsMultiLine", "IsNumeric",
        "IsNumericFullRange", "IsPassword", "IsSecureText", "ResetInputMode",
        "SetAlphabeticOnly", "SetAltArrowKeyMode", "SetAutoFocus",
        "SetBlinkSpeed", "SetCountInvisibleLetters", "SetCursorPosition",
        "SetEnabled", "SetFocus", "SetFont", "SetFontObject",
        "SetHighlightColor", "SetHistoryLines", "SetIndentedWordWrap",
        "SetJustifyH", "SetJustifyV", "SetMaxBytes", "SetMaxLetters",
        "SetMultiLine", "SetNumber", "SetNumeric", "SetNumericFullRange",
        "SetPassword", "SetSecureText", "SetSecurityDisablePaste",
        "SetSecurityDisableSetText", "SetShadowColor", "SetShadowOffset",
        "SetSpacing", "SetText", "SetTextColor", "SetTextInsets",
        "SetVisibleTextByteLimit", "ToggleInputLanguage"
    ];

    private static readonly string[] Line =
    [
        "ClearAllPoints", "GetEndPoint", "GetHitRectThickness", "GetStartPoint",
        "GetThickness", "SetEndPoint", "SetHitRectThickness", "SetStartPoint",
        "SetThickness"
    ];

    private static readonly string[] ScrollFrame =
    [
        "GetHorizontalScroll", "GetHorizontalScrollRange", "GetScrollChild",
        "GetVerticalScroll", "GetVerticalScrollRange",
        "SetHorizontalScroll", "SetScrollChild", "SetVerticalScroll", "UpdateScrollChildRect"
    ];

    private static readonly string[] MessageFrame =
    [
        "AddMessage", "Clear", "GetFadeDuration", "GetFadePower", "GetFading",
        "GetFont", "GetFontObject", "GetFontStringByID", "GetIndentedWordWrap",
        "GetInsertMode", "GetJustifyH", "GetJustifyV", "GetShadowColor",
        "GetShadowOffset", "GetSpacing", "GetTextColor", "GetTimeVisible",
        "HasMessageByID", "ResetMessageFadeByID", "SetFadeDuration", "SetFadePower",
        "SetFading", "SetFont", "SetFontObject", "SetIndentedWordWrap",
        "SetInsertMode", "SetJustifyH", "SetJustifyV", "SetShadowColor",
        "SetShadowOffset", "SetSpacing", "SetTextColor", "SetTimeVisible"
    ];

    private static readonly string[] Browser =
    [
        "ClearFocus", "CopyExternalLink", "DeleteCookies", "NavigateBack", "NavigateForward",
        "NavigateHome", "NavigateReload", "NavigateStop", "NavigateTo", "OpenExternalLink",
        "OpenTicket", "SetFocus", "SetZoom"
    ];

    private static readonly string[] Checkout =
    [
        "CancelOpenCheckout", "ClearFocus", "CloseCheckout", "CopyExternalLink",
        "OpenCheckout", "OpenExternalLink", "SetFocus", "SetZoom"
    ];

    private static readonly string[] MovieFrame =
    [
        "EnableSubtitles", "StartMovie", "StartMovieByName", "StopMovie"
    ];

    private static readonly string[] Slider =
    [
        "Disable", "Enable", "GetMinMaxValues", "GetObeyStepOnDrag", "GetOrientation", "GetStepsPerPage",
        "GetThumbTexture", "GetValue", "GetValueStep", "IsDraggingThumb", "IsEnabled",
        "SetEnabled",
        "SetMinMaxValues", "SetObeyStepOnDrag", "SetOrientation", "SetStepsPerPage",
        "SetThumbTexture", "SetValue", "SetValueStep"
    ];

    private static readonly string[] StatusBar =
    [
        "GetFillStyle", "GetInterpolatedValue", "GetMinMaxValues", "GetOrientation", "GetReverseFill",
        "GetRotatesTexture", "GetStatusBarColor", "GetStatusBarDesaturation",
        "GetStatusBarTexture", "GetTimerDuration", "GetValue", "IsInterpolating",
        "IsStatusBarDesaturated", "SetColorFill", "SetFillStyle", "SetMinMaxValues",
        "SetOrientation", "SetReverseFill", "SetRotatesTexture",
        "SetStatusBarColor", "SetStatusBarDesaturation", "SetStatusBarDesaturated",
        "SetStatusBarTexture", "SetTimerDuration", "SetToTargetValue", "SetValue"
    ];

    private static readonly string[] GameTooltip =
    [
        "AddFontStrings", "SetCustomWordWrapMinWidth", "SetCustomLineSpacing",
        "GetCustomLineSpacing", "SetShrinkToFitWrapped", "SetAllowShowWithNoLines",
        "IsOwned", "GetOwner", "SetOwner", "GetAnchorType", "SetAnchorType",
        "ClearLines", "AddLine", "AddDoubleLine", "AddTexture", "AddAtlas",
        "AppendText", "FadeOut", "NumLines", "SetFrameStack", "CopyTooltip",
        "SetObjectTooltipPosition",
        "ClearPadding", "GetLeftLine", "GetMinimumWidth", "GetPadding",
        "GetRightLine", "SetMinimumWidth", "SetPadding", "SetText"
    ];

    private static readonly string[] Cooldown =
    [
        "Clear", "GetCooldownDisplayDuration", "GetCooldownDuration", "GetCooldownTimes",
        "GetCountdownAbbrevThreshold", "GetCountdownFontString", "GetCountdownFormatter",
        "GetCountdownMillisecondsThreshold", "GetDrawBling", "GetDrawEdge", "GetDrawSwipe",
        "GetEdgeScale", "GetHideCountdownNumbers", "GetMinimumCountdownDuration", "GetReverse",
        "GetRotation", "GetUseAuraDisplayTime", "IsPaused", "Pause", "Resume",
        "SetBlingTexture", "SetCooldown", "SetCooldownDuration", "SetCooldownFromDurationObject",
        "SetCooldownFromExpirationTime", "SetCooldownUNIX", "SetCountdownAbbrevThreshold",
        "SetCountdownFont", "SetCountdownFormatter", "SetCountdownMillisecondsThreshold",
        "SetDrawBling", "SetDrawEdge", "SetDrawSwipe", "SetEdgeColor", "SetEdgeScale",
        "SetEdgeTexture", "SetHideCountdownNumbers", "SetMinimumCountdownDuration", "SetPaused",
        "SetReverse", "SetRotation", "SetSwipeColor", "SetSwipeTexture", "SetTexCoordRange",
        "SetUseAuraDisplayTime", "SetUseCircularEdge"
    ];

    private static readonly string[] ColorSelect =
    [
        "ClearColorWheelTexture", "GetColorAlpha", "GetColorAlphaTexture",
        "GetColorAlphaThumbTexture", "GetColorHSV", "GetColorRGB",
        "GetColorValueTexture", "GetColorValueThumbTexture", "GetColorWheelTexture",
        "GetColorWheelThumbTexture", "SetColorAlpha", "SetColorAlphaTexture",
        "SetColorAlphaThumbTexture", "SetColorHSV", "SetColorRGB",
        "SetColorValueTexture", "SetColorValueThumbTexture", "SetColorWheelTexture",
        "SetColorWheelThumbTexture"
    ];

    private static readonly string[] SimpleHtml =
    [
        "GetContentHeight", "GetFont", "GetFontObject", "GetHyperlinkFormat",
        "GetIndentedWordWrap", "GetJustifyH", "GetJustifyV", "GetShadowColor",
        "GetShadowOffset", "GetSpacing", "GetTextColor", "GetTextData",
        "SetFont", "SetFontObject", "SetHyperlinkFormat", "SetIndentedWordWrap",
        "SetJustifyH", "SetJustifyV", "SetShadowColor", "SetShadowOffset",
        "SetSpacing", "SetText", "SetTextColor"
    ];

    private static readonly string[] Blob =
    [
        "DrawAll", "DrawBlob", "DrawNone", "EnableMerging", "EnableSmoothing", "GetMapID",
        "SetBorderAlpha", "SetBorderScalar", "SetBorderTexture", "SetFillAlpha", "SetFillTexture",
        "SetMapID", "SetMergeThreshold", "SetNumSplinePoints"
    ];

    private static readonly string[] Minimap =
    [
        "GetPingPosition", "GetZoom", "GetZoomLevels", "PingLocation",
        "SetArchBlobInsideAlpha", "SetArchBlobInsideTexture", "SetArchBlobOutsideAlpha",
        "SetArchBlobOutsideTexture", "SetArchBlobRingAlpha", "SetArchBlobRingScalar",
        "SetArchBlobRingTexture", "SetMaskTexture", "SetQuestBlobInsideAlpha",
        "SetQuestBlobInsideTexture", "SetQuestBlobOutsideAlpha", "SetQuestBlobOutsideTexture",
        "SetQuestBlobRingAlpha", "SetQuestBlobRingScalar", "SetQuestBlobRingTexture",
        "SetTaskBlobInsideAlpha", "SetTaskBlobInsideTexture", "SetTaskBlobOutsideAlpha",
        "SetTaskBlobOutsideTexture", "SetTaskBlobRingAlpha", "SetTaskBlobRingScalar",
        "SetTaskBlobRingTexture", "SetZoom", "UpdateBlips"
    ];

    private static readonly string[] QuestPoi =
    [
        "GetNumTooltips", "GetTooltipIndex", "UpdateMouseOverTooltip"
    ];

    private static readonly string[] ScenarioPoi =
    [
        "GetScenarioTooltipText", "UpdateMouseOverTooltip"
    ];

    private static readonly string[] FogOfWar =
    [
        "GetFogOfWarBackgroundAtlas", "GetFogOfWarBackgroundTexture", "GetFogOfWarMaskAtlas",
        "GetFogOfWarMaskTexture", "GetMaskScalar", "GetUiMapID", "SetFogOfWarBackgroundAtlas",
        "SetFogOfWarBackgroundTexture", "SetFogOfWarMaskAtlas", "SetFogOfWarMaskTexture",
        "SetMaskScalar", "SetUiMapID"
    ];

    private static readonly string[] UnitPosition =
    [
        "AddUnit", "ClearUnits", "FinalizeUnits", "GetMouseOverUnits", "GetPlayerPingScale",
        "GetUiMapID", "SetPlayerPingScale", "SetPlayerPingTexture", "SetUiMapID", "SetUnitColor",
        "StartPlayerPing", "StopPlayerPing"
    ];

    private static readonly string[] ModelScene =
    [
        "ClearFog", "CreateActor", "GetActorAtIndex", "GetAllowOverlappedModels",
        "GetCameraFarClip", "GetCameraFieldOfView", "GetCameraForward", "GetCameraNearClip",
        "GetCameraPosition", "GetCameraRight", "GetCameraUp", "GetDrawLayer",
        "GetFogColor", "GetFogFar", "GetFogNear", "GetLightAmbientColor",
        "GetLightDiffuseColor", "GetLightDirection", "GetLightPosition", "GetLightType",
        "GetNumActors", "GetViewInsets", "GetViewTranslation", "IsLightVisible",
        "Project3DPointTo2D",
        "SetAllowOverlappedModels", "SetCameraFarClip", "SetCameraFieldOfView",
        "SetCameraNearClip", "SetCameraOrientationByAxisVectors",
        "SetCameraOrientationByYawPitchRoll", "SetCameraPosition", "SetDesaturation",
        "SetDrawLayer", "SetFogColor", "SetFogFar", "SetFogNear",
        "SetLightAmbientColor", "SetLightDiffuseColor",
        "SetLightDirection", "SetLightPosition", "SetLightType", "SetPaused", "SetViewInsets",
        "SetLightVisible", "SetViewTranslation", "TakeActor"
    ];

    private static readonly string[] ModelSceneActor =
    [
        "AttachToMount", "CalculateMountScale", "DetachFromMount", "Dress",
        "DressPlayerSlot", "GetAutoDress", "GetItemTransmogInfo",
        "GetItemTransmogInfoList", "GetObeyHideInTransmogFlag", "GetPaused",
        "GetSheathed", "GetUseTransmogChoices", "GetUseTransmogSkin", "IsGeoReady",
        "IsSlotAllowed", "IsSlotVisible", "ReleaseFrontEndCharacterDisplays",
        "ResetNextHandSlot", "SetAutoDress",
        "SetFrontEndLobbyModelFromDefaultCharacterDisplay", "SetItemTransmogInfo",
        "SetModelByHyperlink", "SetObeyHideInTransmogFlag", "SetPaused", "SetSheathed",
        "SetSheathedCategory", "SetUseTransmogChoices", "SetUseTransmogSkin", "Undress",
        "UndressSlot", "UseUnitSheatheCategory",
        "ClearModel", "GetActiveBoundingBox", "GetAlpha", "GetAnimation",
        "GetAnimationBlendOperation", "GetAnimationVariation", "GetDesaturation",
        "GetMaxBoundingBox", "GetModelFileID", "GetModelPath", "GetModelUnitGUID",
        "GetParticleOverrideScale", "GetPitch", "GetPosition", "GetRoll", "GetScale",
        "GetSpellVisualKit", "GetYaw", "Hide", "IsLoaded",
        "IsPreferringModelCollisionBounds", "IsShown", "IsUsingCenterForOrigin",
        "IsVisible", "PlayAnimationKit", "SetAlpha", "SetAnimation",
        "SetAnimationBlendOperation", "SetDesaturation", "SetGradientMask",
        "SetGradientMaskWithDyes", "SetModelByCreatureDisplayID", "SetModelByFileID",
        "SetModelByPath", "SetModelByUnit", "SetParticleOverrideScale", "SetPitch",
        "SetPlayerModelFromGlues", "SetPosition", "SetPreferModelCollisionBounds",
        "SetRoll", "SetScale", "SetShown", "SetSpellVisualKit",
        "SetUseCenterForOrigin", "SetYaw", "Show", "StopAnimationKit", "TryOn"
    ];

    private static readonly string[] SimpleModel =
    [
        "AdvanceTime", "ClearFog", "ClearModel", "ClearTransform", "GetCameraDistance",
        "GetCameraFacing", "GetCameraPosition", "GetCameraRoll", "GetCameraTarget",
        "GetDesaturation", "GetFacing", "GetFogColor", "GetFogFar", "GetFogNear", "GetLight",
        "GetModelAlpha", "GetModelDrawLayer", "GetModelFileID", "GetModelScale",
        "GetPaused", "GetPitch", "GetPosition", "GetRoll", "GetShadowEffect",
        "GetViewInsets", "GetViewTranslation", "GetWorldScale", "HasAttachmentPoints",
        "HasCustomCamera", "IsUsingModelCenterToTransform", "MakeCurrentCameraCustom",
        "ReplaceIconTexture", "SetCamera", "SetCameraDistance", "SetCameraFacing",
        "SetCameraPosition", "SetCameraRoll", "SetCameraTarget", "SetCustomCamera",
        "SetDesaturation", "SetFacing", "SetFogColor", "SetFogFar", "SetFogNear",
        "SetGlow", "SetGradientMask", "SetLight", "SetModel", "SetModelAlpha",
        "SetModelDrawLayer", "SetModelScale", "SetParticlesEnabled", "SetPaused",
        "SetPitch", "SetPosition", "SetRoll", "SetSequence", "SetSequenceTime",
        "SetShadowEffect", "SetTransform", "SetUseGBuffer", "SetViewInsets",
        "SetViewTranslation", "TransformCameraSpaceToModelSpace", "UseModelCenterToTransform"
    ];

    private static readonly string[] CharacterModel =
    [
        "ApplySpellVisualKit", "CanSetUnit", "FreezeAnimation", "GetDisplayInfo", "GetDoBlend",
        "GetKeepModelOnHide", "HasAnimation", "PlayAnimKit", "RefreshCamera", "RefreshUnit",
        "SetAnimation", "SetBarberShopAlternateForm", "SetCamDistanceScale", "SetCreature",
        "SetDisplayInfo", "SetDoBlend", "SetItem", "SetItemAppearance", "SetKeepModelOnHide",
        "SetPortraitZoom", "SetRotation", "SetUnit", "StopAnimKit", "ZeroCachedCenterXY"
    ];

    private static readonly string[] DressUpModel =
    [
        "Dress", "GetAutoDress", "GetItemTransmogInfo", "GetItemTransmogInfoList",
        "GetObeyHideInTransmogFlag", "GetSheathed", "GetUseTransmogChoices",
        "GetUseTransmogSkin", "IsGeoReady", "IsSlotAllowed", "IsSlotVisible", "SetAutoDress",
        "SetItemTransmogInfo", "SetObeyHideInTransmogFlag", "SetSheathed",
        "SetUseTransmogChoices", "SetUseTransmogSkin", "TryOn", "Undress", "UndressSlot"
    ];

    private static readonly string[] CinematicModel =
    [
        "EquipItem", "InitializeCamera", "InitializePanCamera", "RefreshCamera",
        "SetAnimOffset", "SetCameraPosition", "SetCameraTarget", "SetCreatureData",
        "SetFacingLeft", "SetFadeTimes", "SetHeightFactor", "SetJumpInfo",
        "SetPanDistance", "SetSpellVisualKit", "SetTargetDistance", "StartPan",
        "StopPan", "UnequipItems"
    ];

    private static readonly string[] AnimationGroup =
    [
        "CreateAnimation", "Finish", "GetAnimationSpeedMultiplier", "GetAnimations",
        "GetDuration", "GetElapsed", "GetLoopState", "GetLooping",
        "GetProgress", "GetScript", "HasScript", "HookScript", "IsDone", "IsPaused",
        "IsPendingFinish", "IsPlaying", "IsReverse", "IsSetToFinalAlpha", "Pause",
        "Play", "RemoveAnimations", "Restart", "SetAnimationSpeedMultiplier",
        "SetLooping", "SetPlaying", "SetScript", "SetToFinalAlpha", "Stop"
    ];

    private static readonly string[] Animation =
    [
        "GetDuration", "GetElapsed", "GetEndDelay", "GetOrder",
        "GetProgress", "GetRegionParent", "GetScript", "GetSmoothProgress", "GetSmoothing",
        "GetStartDelay", "GetTarget", "HasScript", "HookScript", "IsDelaying", "IsDone",
        "IsPaused", "IsPlaying", "IsStopped", "Pause", "Play", "Restart", "SetChildKey",
        "SetDuration", "SetEndDelay", "SetOrder", "SetParent", "SetPlaying", "SetScript",
        "SetSmoothProgress", "SetSmoothing", "SetStartDelay", "SetTarget", "SetTargetKey",
        "SetTargetName", "SetTargetParent", "Stop"
    ];

    private static readonly string[] Alpha =
    [
        "GetFromAlpha", "GetToAlpha", "SetFromAlpha", "SetToAlpha"
    ];

    private static readonly string[] Rotation =
    [
        "GetDegrees", "GetOrigin", "GetRadians",
        "SetDegrees", "SetOrigin", "SetRadians"
    ];

    private static readonly string[] Translation =
    [
        "GetOffset", "SetOffset"
    ];

    private static readonly string[] Scale =
    [
        "GetOrigin", "GetScale", "GetScaleFrom", "GetScaleTo",
        "SetOrigin", "SetScale", "SetScaleFrom", "SetScaleTo"
    ];

    private static readonly string[] FlipBook =
    [
        "GetFlipBookColumns", "GetFlipBookFrameHeight", "GetFlipBookFrameWidth",
        "GetFlipBookFrames", "GetFlipBookRows", "SetFlipBookColumns",
        "SetFlipBookFrameHeight", "SetFlipBookFrameWidth", "SetFlipBookFrames",
        "SetFlipBookRows"
    ];

    private static readonly string[] VertexColor =
    [
        "GetEndColor", "GetStartColor", "SetEndColor", "SetStartColor"
    ];

    private static readonly string[] Path =
    [
        "CreateControlPoint", "GetControlPoints", "GetCurveType",
        "GetMaxControlPointOrder", "SetCurveType"
    ];

    private static readonly string[] ControlPoint =
    [
        "GetOffset", "GetOrder", "SetOffset", "SetOrder", "SetParent"
    ];

    public static IReadOnlySet<string> MethodsFor(string objectType)
    {
        var normalized = objectType.ToLowerInvariant();
        return normalized switch
        {
            "font" => Merge(ScriptObject, Font),
            "texture" or "masktexture" => Merge(Region, Texture),
            "fontstring" => Merge(Region, FontString),
            "simplehtml" => Merge(Region, Frame, SimpleHtml),
            "line" => Merge(Region, Texture, Line),
            "modelsceneactor" => Merge(ScriptRegion, ModelSceneActor),
            "animationgroup" => Merge(ScriptRegion, AnimationGroup),
            "animation" => Merge(ScriptRegion, Animation),
            "alpha" => Merge(ScriptRegion, Animation, Alpha),
            "rotation" => Merge(ScriptRegion, Animation, Rotation),
            "scale" or "linescale" => Merge(ScriptRegion, Animation, Scale),
            "flipbook" => Merge(ScriptRegion, Animation, FlipBook),
            "vertexcolor" => Merge(ScriptRegion, Animation, VertexColor),
            "path" => Merge(ScriptRegion, Animation, Path),
            "controlpoint" => Merge(ScriptRegion, ControlPoint),
            "translation" or "linetranslation" or "texturecoord" or
                "texturecoordtranslation" =>
                Merge(ScriptRegion, Animation, Translation),
            "modelscene" => Merge(Region, Frame, ModelScene),
            "model" => Merge(Region, Frame, SimpleModel),
            "playermodel" or "charactermodel" or "tabardmodel" =>
                Merge(Region, Frame, SimpleModel, CharacterModel),
            "dressupmodel" =>
                Merge(Region, Frame, SimpleModel, CharacterModel, DressUpModel),
            "cinematicmodel" =>
                Merge(Region, Frame, SimpleModel, CharacterModel, CinematicModel),
            "checkbutton" => Merge(Region, Frame, Button, CheckButton),
            "button" or "aurabutton" or "dropdownbutton" or "dropdowntogglebutton" or
                "eventbutton" or "itembutton" => Merge(Region, Frame, Button),
            "editbox" or "eventeditbox" => Merge(Region, Frame, EditBox),
            "browser" => Merge(Region, Frame, Browser),
            "checkout" => Merge(Region, Frame, Checkout),
            "movieframe" => Merge(Region, Frame, MovieFrame),
            "scrollframe" or "eventscrollframe" => Merge(Region, Frame, ScrollFrame),
            "messageframe" or "scrollingmessageframe" =>
                Merge(Region, Frame, MessageFrame),
            "slider" => Merge(Region, Frame, Slider),
            "statusbar" => Merge(Region, Frame, StatusBar),
            "cooldown" => Merge(Region, Frame, Cooldown),
            "colorselect" => Merge(Region, Frame, ColorSelect),
            "minimap" or "simpleminimap" => Merge(Region, Frame, Minimap),
            "blobframe" or "archaeologydigsiteframe" =>
                Merge(Region, Frame, Blob),
            "questpoiframe" => Merge(Region, Frame, Blob, QuestPoi),
            "scenariopoiframe" => Merge(Region, Frame, Blob, ScenarioPoi),
            "fogofwarframe" => Merge(Region, Frame, FogOfWar),
            "unitpositionframe" => Merge(Region, Frame, UnitPosition),
            "gametooltip" => Merge(Region, Frame, GameTooltip),
            _ => Merge(Region, Frame)
        };
    }

    public static bool IsCreatableFrameXmlObjectType(string objectType) =>
        CreatableFrameXmlObjectTypes.Contains(objectType);

    public static bool IsFrameWidget(string objectType) =>
        MethodsFor(objectType).Contains("GetFrameLevel");

    public static bool IsWidget(string objectType) =>
        IsFrameWidget(objectType) ||
        objectType.Equals("Texture", StringComparison.OrdinalIgnoreCase) ||
        objectType.Equals("MaskTexture", StringComparison.OrdinalIgnoreCase) ||
        objectType.Equals("FontString", StringComparison.OrdinalIgnoreCase) ||
        objectType.Equals("Line", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlySet<string> Merge(params IEnumerable<string>[] groups)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups)
            result.UnionWith(group);
        return result;
    }
}
