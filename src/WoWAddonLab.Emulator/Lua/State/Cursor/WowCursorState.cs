namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCursorState
{
    public WowCursorPayload? Payload { get; private set; }
    public ulong Money { get; private set; }
    public int? HardwareMode { get; set; }
    public object? HoveredItem { get; set; }
    public bool HoveredItemIsTradeItem { get; set; }

    public void SetMoney(ulong amount)
    {
        Money = amount;
        Payload = amount == 0
            ? null
            : new WowCursorPayload(
                WowCursorPayloadKind.Money,
                ["money", amount]);
    }

    public void SetPayload(WowCursorPayloadKind kind, params object?[] infoValues)
    {
        if (kind == WowCursorPayloadKind.None)
        {
            ClearPayload();
            return;
        }

        Money = kind == WowCursorPayloadKind.Money &&
                infoValues.Length > 1 &&
                TryConvertUnsigned(infoValues[1], out var amount)
            ? amount
            : 0;
        Payload = new WowCursorPayload(kind, infoValues);
    }

    public void SetItem(WowItemLocation location, params object?[] infoValues)
    {
        Money = 0;
        Payload = new WowCursorPayload(
            WowCursorPayloadKind.Item,
            infoValues,
            location);
    }

    public WowItemLocation? GetItemLocation()
    {
        if (Payload is not { Kind: WowCursorPayloadKind.Item } payload)
            return null;
        if (payload.ItemLocation is { } location)
            return location;

        foreach (var value in payload.InfoValues)
        {
            if (value is WowItemLocation legacyLocation)
                return legacyLocation;
        }
        return null;
    }

    public void ClearPayload()
    {
        Payload = null;
        Money = 0;
    }

    private static bool TryConvertUnsigned(object? value, out ulong result)
    {
        switch (value)
        {
            case byte number:
                result = number;
                return true;
            case ushort number:
                result = number;
                return true;
            case uint number:
                result = number;
                return true;
            case ulong number:
                result = number;
                return true;
            case sbyte number when number >= 0:
                result = (ulong)number;
                return true;
            case short number when number >= 0:
                result = (ulong)number;
                return true;
            case int number when number >= 0:
                result = (ulong)number;
                return true;
            case long number when number >= 0:
                result = (ulong)number;
                return true;
            default:
                result = 0;
                return false;
        }
    }
}
