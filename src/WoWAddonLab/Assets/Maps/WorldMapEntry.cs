namespace WoWAddonLab.Assets;

public sealed record WorldMapEntry(
    int Id,
    string Directory,
    string Name,
    int MapType,
    int InstanceType,
    int ExpansionId,
    uint WdtFileDataId)
{
    public string Label => $"{Name}  [{Directory}]";
}
