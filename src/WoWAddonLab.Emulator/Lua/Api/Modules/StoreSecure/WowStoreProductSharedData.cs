using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowStoreProductSharedData
{
    public double NormalDollars { get; init; }
    public double NormalCents { get; init; }
    public double CurrentDollars { get; init; }
    public double CurrentCents { get; init; }
    public bool BuyableHere { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Tooltip { get; init; }
    public string? Instructions { get; init; }
    public string? Disclaimer { get; init; }
    public uint Flags { get; init; }
    public uint Eligibility { get; init; }
    public bool CanChangeAccount { get; init; }
    public bool CanChangeBNetAccount { get; init; }
    public int? Texture { get; init; }
    public int? ProductDecorator { get; init; }
    public int? BoostType { get; init; }
    public int? ItemId { get; init; }
    public int? VasServiceType { get; init; }
    public string? OverrideBackground { get; init; }
    public WowStoreColor? OverrideTextColor { get; init; }
    public string? OverrideTexture { get; init; }
    public int? ModelSceneId { get; init; }
    public IList<WowStoreProductCard> Cards { get; } = [];
    public IList<WowStoreProductDeliverable> Deliverables { get; } = [];
    public byte CardType { get; init; }
    public byte BannerType { get; init; }
    public int ItemQuantity { get; init; }
}
