using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiBlobState
{
    private readonly string?[] _scenarioTooltipTexts = new string?[8];

    public string? FillTexture { get; set; }
    public uint? FillTextureFileDataId { get; set; }
    public string? BorderTexture { get; set; }
    public uint? BorderTextureFileDataId { get; set; }
    public byte FillAlpha { get; set; } = byte.MaxValue;
    public byte BorderAlpha { get; set; } = byte.MaxValue;
    public float BorderScalar { get; set; } = 1;
    public int MapId { get; set; }
    public float MergeThreshold { get; set; } = 0.25f;
    public int NumSplinePoints { get; set; } = 20;
    public bool MergingEnabled { get; set; } = true;
    public bool SmoothingEnabled { get; set; } = true;
    public List<int> DrawnBlobIds { get; } = [];
    public List<UiBlobArea> Areas { get; } = [];
    public bool DrawAll { get; set; }
    public int MouseOverQuestId { get; set; }
    public List<int> MouseOverObjectiveIndices { get; } = [];
    public int MouseOverScenarioIndex { get; set; } = -1;
    public IReadOnlyList<string?> ScenarioTooltipTexts => _scenarioTooltipTexts;

    public void SetScenarioTooltipText(int index, string? text)
    {
        if ((uint)index >= _scenarioTooltipTexts.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        _scenarioTooltipTexts[index] = text;
    }

    public void ClearMouseOverTooltip()
    {
        MouseOverQuestId = 0;
        MouseOverObjectiveIndices.Clear();
        MouseOverScenarioIndex = -1;
    }
}
