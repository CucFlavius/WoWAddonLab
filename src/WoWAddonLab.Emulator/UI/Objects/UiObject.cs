using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiObject
{
    public required int Id { get; init; }
    public required string ObjectType { get; init; }
    public string? Name { get; init; }
    public string? AddonName { get; set; }
    public string SourceLocation { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? ParentKey { get; set; }
    public List<int> Children { get; } = [];
    public List<UiAnchor> Anchors { get; } = [];
    public Vector2 AnimationOffset { get; set; }
    public Vector2 AnimationScale { get; set; } = Vector2.One;
    public string AnimationScaleOriginPoint { get; set; } = "CENTER";
    public Vector2 AnimationScaleOriginOffset { get; set; }
    public Vector2 LineAnimationOffset { get; set; }
    public Vector2 LineAnimationScale { get; set; } = Vector2.One;
    public string LineAnimationScaleOriginPoint { get; set; } = "CENTER";
    public Vector2 LineAnimationScaleOriginOffset { get; set; }
    public float AnimationRotation { get; set; }
    public string AnimationRotationOriginPoint { get; set; } = "CENTER";
    public Vector2 AnimationRotationOriginOffset { get; set; }
    public int? AllPointsTargetId { get; set; }
    public float? Width { get; set; }
    public float? Height { get; set; }
    public float Scale { get; set; } = 1;
    public float Alpha
    {
        get => _alphaByte / 255f;
        set
        {
            var normalized = Math.Clamp(value, 0, 1);
            _alphaByte = (byte)MathF.Floor(normalized * 255 + .5f);
        }
    }
    public float? AnimationBaseAlpha { get; set; }
    public bool IgnoreParentAlpha { get; set; }
    public Vector4 VertexColor { get; set; } = Vector4.One;
    public bool Shown { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public bool MouseClickEnabled { get; set; }
    public bool MouseMotionEnabled { get; set; }
    public bool MouseEnabled
    {
        get => MouseClickEnabled && MouseMotionEnabled;
        set
        {
            MouseClickEnabled = value;
            MouseMotionEnabled = value;
        }
    }
    public bool MouseWheelEnabled { get; set; }
    public bool KeyboardEnabled { get; set; }
    public bool HyperlinksEnabled { get; set; }
    public bool Forbidden { get; set; }
    public bool Protected { get; set; }
    public bool ProtectedExplicitly { get; set; }
    public bool AnchoringRestricted { get; set; }
    public bool AnchoringSecret { get; set; }
    public bool CollapsesLayout { get; set; }
    public uint SecretAspectMask { get; set; }
    public uint SecondarySecretAspectMask { get; set; }
    public bool ContainsSecretValues { get; set; }
    public bool PreventsSecretValues { get; set; }
    public bool ObjectLoaded { get; set; } = true;
    public bool PropagateKeyboardInput { get; set; }
    public bool PropagateMouseClicks { get; set; }
    public bool PropagateMouseMotion { get; set; }
    public bool DontSavePosition { get; set; }
    public bool FlattensRenderLayers { get; set; }
    public bool IsFrameBuffer { get; set; }
    public bool HyperlinkPropagateToParent { get; set; }
    public bool IgnoreChildrenForBounds { get; set; }
    public bool GamePadButtonEnabled { get; set; }
    public bool GamePadStickEnabled { get; set; }
    public bool Movable { get; set; }
    public bool Resizable { get; set; }
    public Vector2 ResizeMinimum { get; set; }
    public Vector2 ResizeMaximum { get; set; }
    public bool UserPlaced { get; set; }
    public bool Toplevel { get; set; }
    public int FrameId { get; set; }
    public int RaisedFrameLevel { get; set; }
    public bool ClampedToScreen { get; set; }
    public Vector4 ClampRectInsets { get; set; }
    public UiInsets HitRectInsets { get; set; }
    public bool ClipsChildren { get; set; }
    public bool IgnoreParentScale { get; set; }
    public bool Checked { get; set; }
    public UiButtonState ButtonState { get; set; }
    public Vector2 PushedTextOffset { get; set; }
    public bool ButtonStateLocked { get; set; }
    public bool ButtonClickDispatching { get; set; }
    public double? LastButtonClickTime { get; set; }
    public bool HighlightLocked { get; set; }
    public bool MotionScriptsWhileDisabled { get; set; }
    public bool ObeyStepOnDrag { get; set; }
    public double ValueStep { get; set; }
    public double StepsPerPage { get; set; }
    public bool SliderDraggingThumb { get; set; }
    public bool MultiLine { get; set; }
    public bool AutoFocus { get; set; }
    public bool EditBoxAltArrowKeyMode { get; set; }
    public bool EditBoxAlphabeticOnly { get; set; }
    public bool EditBoxCountInvisibleLetters { get; set; }
    public bool EditBoxNumericFullRange { get; set; }
    public bool EditBoxPassword { get; set; }
    public bool EditBoxSecureText { get; set; }
    public bool EditBoxSecurityDisablePaste { get; set; }
    public bool EditBoxImeCompositionMode { get; set; }
    public UiEditBoxInputLanguage EditBoxInputLanguage { get; set; }
    public float EditBoxBlinkSpeed { get; set; }
    public int EditBoxMaximumBytes { get; set; }
    public int EditBoxVisibleTextByteLimit { get; set; }
    public int EditBoxHistoryLines { get; set; }
    public int EditBoxHistoryWriteIndex { get; set; }
    public List<string?> EditBoxHistory { get; } = [];
    public Vector4 EditBoxHighlightColor { get; set; }
    public int EditBoxHighlightStart { get; set; }
    public int EditBoxHighlightEnd { get; set; }
    public int EditBoxDisplayStart { get; set; }
    public List<UiEditBoxCaretStop> EditBoxCaretStops { get; } = [];
    public string FrameStrata { get; set; } = "MEDIUM";
    public int FrameLevel { get; set; }
    public bool FixedFrameLevel { get; set; }
    public bool FixedFrameStrata { get; set; }
    public bool UseParentLevel { get; set; }
    public string DrawLayer { get; set; } = "ARTWORK";
    public int SubLevel { get; set; }
    public HashSet<string> EnabledDrawLayers { get; } =
    [
        "BACKGROUND",
        "BORDER",
        "ARTWORK",
        "OVERLAY"
    ];
    public int LuaReference { get; set; }
    public int? WindowReference { get; set; }
    public Dictionary<string, int> ScriptReferences { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> AttributeReferences { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Events { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool AllEventsRegistered { get; set; }
    public Dictionary<string, List<string>> RegisteredUnitEvents { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<UiFrameEventCallback>> EventCallbackReferences { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ClickRegistrations { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> MouseRegistrations { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> DragRegistrations { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> PassThroughButtons { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, object?> Attributes { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public UiTextureState? Texture { get; set; }
    public List<int> MaskTextureIds { get; } = [];
    public UiFontState? Font { get; set; }
    public ushort FontAlphaGradientStart { get; set; } = ushort.MaxValue;
    public ushort FontAlphaGradientLength { get; set; }
    public float FontRotation { get; set; }
    public byte FontScaleAnimationMode { get; set; }
    public float FontAnimationFontSizeScale { get; set; } = 1;
    public float FontAnimationVertexScale { get; set; } = 1;
    public bool FontSmoothScaling { get; set; }
    public bool FontFixedColor { get; set; }
    public bool HasFrameAlphaGradient { get; set; }
    public Vector2[] FrameAlphaGradientEdges { get; } = new Vector2[2];
    public UiTooltipState? Tooltip { get; set; }
    public Dictionary<string, UiFontState> HtmlFonts { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> HtmlFontObjectIds { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public int?[] FontFamilyMemberIds { get; } = new int?[5];
    public List<UiHtmlContentNode> HtmlContentNodes { get; } = [];
    public string HtmlHyperlinkFormat { get; set; } = "|H%s|h%s|h";
    public bool HtmlIgnoreMarkup { get; set; }
    public float HtmlContentHeight { get; set; }
    public int? FontObjectId
    {
        get => _fontObjectId;
        set
        {
            if (_fontObjectId == value)
                return;
            Owner?.UpdateFontDependent(Id, _fontObjectId, value);
            _fontObjectId = value;
        }
    }

    private int? _fontObjectId;
    private byte _alphaByte = byte.MaxValue;

    internal UiSystem? Owner { get; set; }
    public UiLineState? Line { get; set; }

    public static string? NormalizeMouseButtonName(string? button)
    {
        if (button is null)
            return null;
        if (button.Equals("LeftButton", StringComparison.OrdinalIgnoreCase))
            return "LeftButton";
        if (button.Equals("RightButton", StringComparison.OrdinalIgnoreCase))
            return "RightButton";
        if (button.Equals("MiddleButton", StringComparison.OrdinalIgnoreCase))
            return "MiddleButton";
        if (!button.StartsWith("Button", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(
                button.AsSpan("Button".Length),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out var number) ||
            number is < 1 or > 31)
        {
            return null;
        }
        return number switch
        {
            1 => "LeftButton",
            2 => "RightButton",
            3 => "MiddleButton",
            _ => $"Button{number}"
        };
    }

    public static string? NormalizeMouseButtonRegistration(string? value)
    {
        if (value is null)
            return null;
        if (value.Equals("AnyDown", StringComparison.OrdinalIgnoreCase))
            return "AnyDown";
        if (value.Equals("AnyUp", StringComparison.OrdinalIgnoreCase))
            return "AnyUp";

        var down = value.EndsWith("Down", StringComparison.OrdinalIgnoreCase);
        var up = value.EndsWith("Up", StringComparison.OrdinalIgnoreCase);
        if (!down && !up)
            return null;
        var suffixLength = down ? 4 : 2;
        var button = NormalizeMouseButtonName(value[..^suffixLength]);
        return button is null ? null : button + (down ? "Down" : "Up");
    }

    public bool ShouldButtonPassThrough(string? button) =>
        NormalizeMouseButtonName(button) is { } normalized &&
        PassThroughButtons.Contains(normalized);
    public UiStatusBarState? StatusBar { get; set; }
    public UiCooldownState? Cooldown { get; set; }
    public UiColorSelectState? ColorSelect { get; set; }
    public UiBlobState? Blob { get; set; }
    public UiMinimapState? Minimap { get; set; }
    public UiFogOfWarState? FogOfWar { get; set; }
    public UiUnitPositionState? UnitPosition { get; set; }
    public UiMovieState? Movie { get; set; }
    public UiAnimationGroupState? AnimationGroup { get; set; }
    public UiAnimationState? Animation { get; set; }
    public UiControlPointState? ControlPoint { get; set; }
    public ModelSceneState? ModelScene { get; set; }
    public uint? ModelFileDataId { get; set; }
    public uint ModelBoneFileDataId { get; set; }
    public uint? ModelCreatureDisplayId { get; set; }
    public int ModelDisplayId { get; set; }
    public int ModelMountDisplayId { get; set; }
    public int ModelCreatureId { get; set; }
    public int ModelItemId { get; set; }
    public int ModelItemAppearanceModifierId { get; set; }
    public int ModelItemVisualId { get; set; }
    public int ModelItemAppearanceId { get; set; }
    public int ModelItemSubclass { get; set; } = -1;
    public HashSet<int> ModelCinematicEquippedItemIds { get; } = [];
    public bool ModelBarberShopAlternateForm { get; set; }
    public bool ModelUseNativeForm { get; set; } = true;
    public bool ModelNoMip { get; set; }
    public bool ModelResourceLoaded { get; set; }
    public bool ModelHasAttachmentPoints { get; set; }
    public uint ModelSequenceId { get; set; }
    public int ModelSequenceTimeOffset { get; set; }
    public ushort? ModelResolvedSequenceId { get; set; }
    public int ModelSelectedSequenceIndex { get; set; } = -1;
    public int ModelResolvedSequenceIndex { get; set; } = -1;
    public ushort ModelResolvedSequenceVariation { get; set; }
    public uint ModelResolvedSequenceDurationMilliseconds { get; set; }
    public double ModelSequenceElapsedMilliseconds { get; set; }
    public double ModelSequenceInitialElapsedMilliseconds { get; set; }
    public double ModelSequencePlaybackClockMilliseconds { get; set; }
    public float ModelSequencePlaybackSpeed { get; set; } = 1;
    public uint ModelSequenceRepeatCount { get; set; } = 1;
    public bool ModelSequencePlaying { get; set; }
    public bool ModelSequenceLoops { get; set; }
    public int ModelSequencePlaybackRevision { get; set; }
    public uint? ModelIconTextureFileDataId { get; set; }
    public string? ModelIconTexturePath { get; set; }
    public float ModelShadowEffectStrength { get; set; }
    public UiModelShadowEffectState? ModelShadowEffectState { get; set; }
    public UiModelDissolveEffectState? ModelDissolveEffectState { get; set; }
    public UiModelEdgeGlowEffectState? ModelEdgeGlowEffectState { get; set; }
    public UiModelRenderEffectKind ModelRenderEffectKind { get; set; }
    public int? ModelAnimationKitId { get; set; }
    public bool ModelAnimationKitLooping { get; set; }
    public int? ModelAnimationKitSegmentId { get; set; }
    public byte ModelAnimationKitSegmentOrderIndex { get; set; }
    public uint ModelAnimationKitOneShotDurationMilliseconds { get; set; }
    public double ModelAnimationKitElapsedMilliseconds { get; set; }
    public WowAnimationKitRuntimeState? ModelAnimationKitRuntimeState { get; set; }
    public ushort ModelAnimationKitStopId { get; set; }
    public bool ModelAnimationKitSegmentUsesBoneSet { get; set; }
    public ushort ModelAnimationId { get; set; }
    public int ModelAnimationVariation { get; set; } = -1;
    public int ModelAnimationFrozenFrame { get; set; } = -1;
    public HashSet<ushort> ModelAvailableAnimationIds { get; } = [];
    public List<ushort> ModelAnimationIdsInResourceOrder { get; } = [];
    public List<WowModelSequenceMetadata> ModelSequencesInResourceOrder { get; } = [];
    public List<WowModelAnimationFileMetadata> ModelAnimationFiles { get; } = [];
    public List<WowModelPendingAnimationRequest> ModelPendingAnimationRequests { get; } = [];
    public List<uint> ModelGlobalSequenceDurationsMilliseconds { get; } = [];
    public double ModelGlobalSequenceElapsedMilliseconds { get; set; }
    public float ModelAnimationSpeed { get; set; } = 1;
    public int ModelAnimationTimeOffsetMilliseconds { get; set; }
    public int ModelAnimationBlendOperation { get; set; } = 1;
    public WowModelSequenceBlendState? ModelSequenceBlendState { get; set; }
    public int? ModelMountedToActorId { get; set; }
    public int? ModelMountedRiderActorId { get; set; }
    public uint? ModelSpellVisualKitId { get; set; }
    public bool ModelSpellVisualOneShot { get; set; }
    public List<WowSpellVisualKitApplication> ModelAppliedSpellVisualKits { get; } = [];
    public bool ModelUsesUnitSheatheCategory { get; set; }
    public bool ModelUseTransmogSkin { get; set; }
    public bool ModelUseTransmogChoices { get; set; }
    public bool ModelObeyHideInTransmogFlag { get; set; }
    public bool ModelSheathed { get; set; }
    public bool ModelHideWeapons { get; set; }
    public byte ModelMainHandSheathedCategory { get; set; }
    public byte ModelOffHandSheathedCategory { get; set; }
    public HashSet<int> ModelAllowedInventorySlots { get; } = [];
    public HashSet<int> ModelVisibleInventorySlots { get; } = [];
    public Dictionary<int, UiItemTransmogInfo> ModelItemTransmogInfoBySlot { get; } = [];
    public bool ModelMainHandUsesPairedWeapon { get; set; }
    public bool ModelUseCenterForOriginX { get; set; }
    public bool ModelUseCenterForOriginY { get; set; }
    public bool ModelUseCenterForOriginZ { get; set; }
    public bool ModelAutoDress { get; set; }
    public bool ModelKeepModelOnHide { get; set; }
    public bool ModelDoBlend { get; set; } = true;
    public UiModelLightState ModelLight { get; set; } = new();
    public bool ModelLightEnabled { get; set; }
    public bool ModelFacingLeft { get; set; }
    public float ModelAnimationOffset { get; set; }
    public Vector3? ModelActiveBoundingBoxMinimum { get; set; }
    public Vector3? ModelActiveBoundingBoxMaximum { get; set; }
    public Vector3? ModelAnimationBoundingBoxMinimum { get; set; }
    public Vector3? ModelAnimationBoundingBoxMaximum { get; set; }
    public Vector3? ModelCollisionBoundingBoxMinimum { get; set; }
    public Vector3? ModelCollisionBoundingBoxMaximum { get; set; }
    public Vector3? ModelMaxBoundingBoxMinimum { get; set; }
    public Vector3? ModelMaxBoundingBoxMaximum { get; set; }
    public float? ModelParticleOverrideScale { get; set; }
    public bool ModelPreferCollisionBounds { get; set; }
    public sbyte[] ModelGradientMaskIndices { get; } = new sbyte[4];
    public bool ModelGradientMaskEnabled { get; set; }
    public int?[] ModelGradientDyeColorIds { get; } = new int?[3];
    public float[] ModelGradientDyeTextureIndices { get; } = new float[3];
    public bool ModelGradientDyesEnabled { get; set; }
    public float ModelHeightFactor { get; set; } = 0.5f;
    public float ModelTargetDistance { get; set; } = 0.3f;
    public float ModelPanDistance { get; set; } = 0.2f;
    public float ModelCameraScaleFactor { get; set; } = 1;
    public float ModelCinematicJumpLength { get; set; } = 0.5f;
    public float ModelCinematicJumpHeight { get; set; } = 2.5f;
    public float ModelCinematicFadeInSeconds { get; set; } = 0.1f;
    public float ModelCinematicFadeOutSeconds { get; set; } = 0.2f;
    public int ModelCinematicPanType { get; set; }
    public float ModelCinematicPanDurationSeconds { get; set; }
    public float ModelCinematicPanElapsedSeconds { get; set; }
    public bool ModelCinematicPanDoFade { get; set; }
    public int ModelCinematicPanVisualKitId { get; set; }
    public float ModelCinematicPanStartPositionScale { get; set; }
    public float ModelCinematicPanSpeedMultiplier { get; set; } = 1;
    public bool ModelCinematicPanActive { get; set; }
    public bool ModelHasCustomCamera { get; set; }
    public bool ModelHasCurrentCamera { get; set; }
    public uint? ModelCameraIndex { get; set; }
    public uint? ModelSelectedCameraIndex { get; set; }
    public List<WowModelCameraMetadata> ModelCameras { get; } = [];
    public List<ushort> ModelCameraLookupIndices { get; } = [];
    public bool ModelCharacterCameraActive { get; set; }
    public Vector3 ModelCameraPosition { get; set; }
    public Vector3 ModelCameraTarget { get; set; }
    public float ModelCameraDistance { get; set; }
    public float ModelCameraRoll { get; set; }
    public float ModelCameraFieldOfView { get; set; } = MathF.PI / 2;
    public float ModelCameraNearClip { get; set; }
    public float ModelCameraFarClip { get; set; }
    public UiModelRenderCameraState? ModelRenderCameraState { get; set; }
    public bool ModelFogEnabled { get; set; }
    public Vector4 ModelFogColor { get; set; } = Vector4.One;
    public float ModelFogNear { get; set; }
    public float ModelFogFar { get; set; } = 1;
    public float ModelAlpha { get; set; } = 1;
    public float ModelScale { get; set; } = 1;
    public float ModelDisplayScaleMultiplier { get; set; } = 1;
    public string ModelDrawLayer { get; set; } = "ARTWORK";
    public bool ModelParticlesEnabled { get; set; } = true;
    public float ModelGlow { get; set; }
    public bool ModelUseGBuffer { get; set; }
    public Vector2 ModelViewTranslation { get; set; }
    public UiInsets ModelViewInsets { get; set; }
    public bool ModelUseCenterToTransform { get; set; }
    public Vector3 ModelCenter { get; set; }
    public bool ModelTransformEnabled { get; set; }
    public Vector3 ModelTransformTranslation { get; set; }
    public Vector3 ModelTransformRotation { get; set; }
    public float ModelTransformScale { get; set; } = 1;
    public Matrix4x4 ModelTransformMatrix { get; set; } = Matrix4x4.Identity;
    public float ModelWorldScale { get; set; } = 1;
    public string? ModelPath { get; set; }
    public string? ModelUnitToken { get; set; }
    public WowClubFinderTabardInfoState? ModelGuildTabardInfo { get; set; }
    public Vector3 ModelPosition { get; set; }
    public float ModelYaw { get; set; }
    public float ModelPitch { get; set; }
    public float ModelRoll { get; set; }
    public float ModelPortraitZoom { get; set; }
    public float ModelCamDistanceScale { get; set; } = 1;
    public bool ModelRotationAnimating { get; set; }
    public bool ModelRotationResumeSkipFrame { get; set; }
    public uint ModelRotationResumeTickMilliseconds { get; set; }
    public ushort? ModelRotationTurnAnimationId { get; set; }
    public Vector2 ModelCachedCenterXY { get; set; }
    public int ModelCameraRefreshRevision { get; set; }
    public int ModelUnitRefreshRevision { get; set; }
    public float ModelDesaturation { get; set; }
    public bool ModelPaused { get; set; }
    public bool ModelGlobalPaused { get; set; }
    public bool SecurityDisableSetText { get; set; }
    public List<UiMessageFrameMessage> Messages { get; } = [];
    public List<UiMessageFrameLine> MessageLines { get; } = [];
    public int MessageLineCapacity { get; set; }
    public UiInsets MessageInsets { get; set; }
    public bool MessageFading { get; set; } = true;
    public float MessageTimeVisible { get; set; } = 10;
    public float MessageFadeDuration { get; set; } = 3;
    public float MessageFadePower { get; set; } = 1;
    public string MessageInsertMode { get; set; } = "BOTTOM";
    public int? ScrollChildId { get; set; }
    public float HorizontalScroll { get; set; }
    public float HorizontalScrollRange { get; set; }
    public float VerticalScroll { get; set; }
    public float VerticalScrollRange { get; set; }
    public Vector4 TextInsets { get; set; }
    public int MaximumLetters { get; set; }
    public int CursorPosition { get; set; }
    public string? HighlightTextureAsset { get; set; }
    public int? HighlightTextureId { get; set; }
    public int? NormalTextureId { get; set; }
    public int? PushedTextureId { get; set; }
    public int? DisabledTextureId { get; set; }
    public int? CheckedTextureId { get; set; }
    public int? DisabledCheckedTextureId { get; set; }
    public int? ButtonFontStringId { get; set; }
    public int? NormalFontObjectId { get; set; }
    public string? NormalFontObjectName { get; set; }
    public int? HighlightFontObjectId { get; set; }
    public string? HighlightFontObjectName { get; set; }
    public int? DisabledFontObjectId { get; set; }
    public int? ThumbTextureId { get; set; }
    public string? DisabledFontObjectName { get; set; }
    public string TextValue { get; set; } = string.Empty;
    public bool EditBoxTextChangedPending { get; set; }
    public bool EditBoxTextChangedByUser { get; set; }
    public string? BrowserPage { get; set; }
    public uint? BrowserTicketIndex { get; set; }
    public string? BrowserExternalLink { get; set; }
    public double? BrowserZoom { get; set; }
    public int? CheckoutLastRequestedId { get; set; }
    public bool CheckoutOpen { get; set; }

    public bool IsRegion =>
        ObjectType.Equals("Texture", StringComparison.OrdinalIgnoreCase) ||
        ObjectType.Equals("MaskTexture", StringComparison.OrdinalIgnoreCase) ||
        ObjectType.Equals("FontString", StringComparison.OrdinalIgnoreCase) ||
        ObjectType.Equals("Line", StringComparison.OrdinalIgnoreCase);

    public bool IsFrameWidget =>
        !IsRegion &&
        AnimationGroup is null &&
        Animation is null &&
        ControlPoint is null &&
        !ObjectType.Equals("Font", StringComparison.OrdinalIgnoreCase) &&
        !ObjectType.Equals("ModelSceneActor", StringComparison.OrdinalIgnoreCase);
}
