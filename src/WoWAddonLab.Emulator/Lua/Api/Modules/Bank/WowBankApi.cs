using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowBankApi : LuaApiModule
{
    private const ulong MaximumTransferAmount = 100_000_000_000;
    private const ulong MaximumDepositedMoney = 1_000_000_000_000;
    private const uint SupportedDepositFlags = 0x39F;

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AreAnyBankTypesViewable",
        "AutoDepositItemsIntoBank",
        "CanDepositMoney",
        "CanPurchaseBankTab",
        "CanUseBank",
        "CanViewBank",
        "CanWithdrawMoney",
        "CloseBankFrame",
        "DepositMoney",
        "DoesBankTypeSupportAutoDeposit",
        "DoesBankTypeSupportMoneyTransfer",
        "FetchBankLockedReason",
        "FetchDepositedMoney",
        "FetchNextPurchasableBankTabData",
        "FetchNumPurchasedBankTabs",
        "FetchPurchasedBankTabData",
        "FetchPurchasedBankTabIDs",
        "FetchViewableBankTypes",
        "HasMaxBankTabs",
        "IsItemAllowedInBankType",
        "PurchaseBankTab",
        "UpdateBankTabSettings",
        "WithdrawMoney"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Bank");
    }

    internal static void RegisterEnums(lua_State state)
    {
        PushEnum(
            state,
            "BankType",
            ("Character", 0),
            ("Guild", 1),
            ("Account", 2));
        PushEnumMeta(state, "BankTypeMeta", 0, 2, 3);
        PushEnum(
            state,
            "BankLockedReason",
            ("None", 0),
            ("NoAccountInventoryLock", 1),
            ("BankDisabled", 2),
            ("BankConversionFailed", 3));
        PushEnumMeta(state, "BankLockedReasonMeta", 0, 3, 4);
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var runtime = LuaBindings.GetRuntime(state);
        var bank = runtime.Bank;

        if (operation == "AreAnyBankTypesViewable")
        {
            lua_pushboolean(
                state,
                bank.ViewableBankTypes.Contains(0) ||
                bank.ViewableBankTypes.Contains(2)
                    ? 1
                    : 0);
            return 1;
        }
        if (operation == "CloseBankFrame")
        {
            bank.IsBankFrameOpen = false;
            return 0;
        }
        if (operation == "FetchViewableBankTypes")
        {
            PushViewableBankTypes(state, bank);
            return 1;
        }

        var usage = Usage(operation);
        var bankType = RequiredBankType(state, 1, usage);
        switch (operation)
        {
            case "AutoDepositItemsIntoBank":
                if (SupportsAutoDeposit(bankType) && CanUse(bank, bankType))
                {
                    bank.AutoDepositRequestCount++;
                    bank.LastAutoDepositBankType = bankType;
                }
                return 0;

            case "CanDepositMoney":
                lua_pushboolean(state, CanDeposit(bank, bankType) ? 1 : 0);
                return 1;

            case "CanPurchaseBankTab":
                lua_pushboolean(state, CanPurchase(bank, bankType) ? 1 : 0);
                return 1;

            case "CanUseBank":
                lua_pushboolean(state, CanUse(bank, bankType) ? 1 : 0);
                return 1;

            case "CanViewBank":
                lua_pushboolean(
                    state,
                    bank.ViewableBankTypes.Contains(bankType) ? 1 : 0);
                return 1;

            case "CanWithdrawMoney":
                lua_pushboolean(state, CanWithdraw(bank, bankType) ? 1 : 0);
                return 1;

            case "DepositMoney":
            {
                var amount = RequiredAmount(state, 2, usage);
                if (amount > 0 &&
                    amount < MaximumTransferAmount &&
                    amount <= (ulong)Math.Max(0, runtime.Client.Money) &&
                    CanDeposit(bank, bankType) &&
                    amount < MaximumDepositedMoney - bank.DepositedMoney)
                {
                    bank.DepositRequestCount++;
                    bank.LastDepositBankType = bankType;
                    bank.LastDepositAmount = amount;
                }
                return 0;
            }

            case "DoesBankTypeSupportAutoDeposit":
                lua_pushboolean(state, SupportsAutoDeposit(bankType) ? 1 : 0);
                return 1;

            case "DoesBankTypeSupportMoneyTransfer":
                lua_pushboolean(state, bankType == 2 ? 1 : 0);
                return 1;

            case "FetchBankLockedReason":
                if (bank.LockedReasonByBankType.TryGetValue(
                        bankType,
                        out var lockedReason))
                {
                    lua_pushinteger(state, lockedReason);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;

            case "FetchDepositedMoney":
                PushUnsigned(state, bankType == 2 ? bank.DepositedMoney : 0);
                return 1;

            case "FetchNextPurchasableBankTabData":
                if (bank.NextPurchasableTabByBankType.TryGetValue(
                        bankType,
                        out var nextTab))
                {
                    PushNextPurchasableTab(state, nextTab);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;

            case "FetchNumPurchasedBankTabs":
                lua_pushinteger(state, PurchasedTabs(bank, bankType).Count);
                return 1;

            case "FetchPurchasedBankTabData":
                PushPurchasedTabData(state, PurchasedTabs(bank, bankType));
                return 1;

            case "FetchPurchasedBankTabIDs":
                PushPurchasedTabIds(state, PurchasedTabs(bank, bankType));
                return 1;

            case "HasMaxBankTabs":
                lua_pushboolean(state, HasMaxTabs(bank, bankType) ? 1 : 0);
                return 1;

            case "IsItemAllowedInBankType":
                if (lua_type(state, 2) is not (LUA_TTABLE or LUA_TUSERDATA))
                    return luaL_error(state, usage);
                lua_pushboolean(
                    state,
                    bankType switch
                    {
                        0 => 1,
                        2 when bank.AccountBankItemAllowed => 1,
                        _ => 0
                    });
                return 1;

            case "PurchaseBankTab":
                if (CanPurchase(bank, bankType))
                {
                    bank.PurchaseRequestCount++;
                    bank.LastPurchaseBankType = bankType;
                }
                return 0;

            case "UpdateBankTabSettings":
            {
                var tabId = RequiredContainerId(state, 2, usage);
                var tabName = RequiredString(state, 3, usage);
                var tabIcon = RequiredString(state, 4, usage);
                var depositFlags = RequiredBagSlotFlags(state, 5, usage);
                var validTab = bankType switch
                {
                    0 => tabId is >= 6 and <= 11,
                    2 => tabId is >= 12 and <= 16,
                    _ => false
                };
                if (validTab && CanUse(bank, bankType))
                {
                    bank.TabSettingsRequestCount++;
                    bank.LastTabSettingsRequest = new WowBankTabSettingsRequest(
                        bankType,
                        tabId,
                        tabName,
                        tabIcon,
                        (depositFlags & ~SupportedDepositFlags) == 0
                            ? depositFlags
                            : 0);
                }
                return 0;
            }

            case "WithdrawMoney":
            {
                var amount = RequiredAmount(state, 2, usage);
                var resultingPlayerMoney =
                    (ulong)Math.Max(0, runtime.Client.Money) + amount;
                if (amount > 0 &&
                    amount < MaximumTransferAmount &&
                    amount <= bank.DepositedMoney &&
                    resultingPlayerMoney < MaximumTransferAmount &&
                    CanWithdraw(bank, bankType))
                {
                    bank.WithdrawRequestCount++;
                    bank.LastWithdrawBankType = bankType;
                    bank.LastWithdrawAmount = amount;
                }
                return 0;
            }
        }

        return 0;
    }

    private static bool CanUse(WowBankState bank, byte bankType) =>
        (bankType == 0 || bankType == 2) &&
        bank.UsableBankTypes.Contains(bankType) &&
        bank.ViewableBankTypes.Contains(bankType);

    private static bool CanDeposit(WowBankState bank, byte bankType) =>
        bankType == 2 &&
        CanUse(bank, bankType) &&
        bank.DepositedMoney < MaximumDepositedMoney;

    private static bool CanWithdraw(WowBankState bank, byte bankType) =>
        bankType == 2 &&
        CanUse(bank, bankType) &&
        bank.DepositedMoney > 0;

    private static bool CanPurchase(WowBankState bank, byte bankType) =>
        CanUse(bank, bankType) &&
        !HasMaxTabs(bank, bankType) &&
        bank.NextPurchasableTabByBankType.ContainsKey(bankType);

    private static bool SupportsAutoDeposit(byte bankType) =>
        bankType is 0 or 2;

    private static bool HasMaxTabs(WowBankState bank, byte bankType)
    {
        var maximum = bankType switch
        {
            0 => 6,
            2 => 5,
            _ => 0
        };
        return PurchasedTabs(bank, bankType).Count >= maximum;
    }

    private static IList<WowBankTabData> PurchasedTabs(
        WowBankState bank,
        byte bankType) =>
        bank.PurchasedTabsByBankType.TryGetValue(bankType, out var tabs)
            ? tabs
            : Array.Empty<WowBankTabData>();

    private static byte RequiredBankType(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return unchecked((byte)luaL_error(state, usage));
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            return unchecked((byte)luaL_error(state, usage));
        }
        var value = unchecked((byte)(int)number);
        return value <= 2
            ? value
            : unchecked((byte)luaL_error(state, usage));
    }

    private static int RequiredContainerId(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        var value = unchecked((int)number);
        return value is >= 1 and <= 16
            ? value
            : luaL_error(state, usage);
    }

    private static uint RequiredBagSlotFlags(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return unchecked((uint)luaL_error(state, usage));
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            return unchecked((uint)luaL_error(state, usage));
        }
        var value = unchecked((int)number);
        return value is >= 0 and <= 1023
            ? (uint)value
            : unchecked((uint)luaL_error(state, usage));
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static ulong RequiredAmount(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return unchecked((ulong)luaL_error(state, usage));
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number < 0 || number > ulong.MaxValue)
            return unchecked((ulong)luaL_error(state, usage));
        return (ulong)number;
    }

    private static void PushNextPurchasableTab(
        lua_State state,
        WowNextPurchasableBankTabData tab)
    {
        lua_newtable(state);
        PushUnsignedField(state, "tabCost", tab.TabCost);
        PushBooleanField(state, "canAfford", tab.CanAfford);
        PushOptionalStringField(
            state,
            "purchasePromptTitle",
            tab.PurchasePromptTitle);
        PushOptionalStringField(
            state,
            "purchasePromptBody",
            tab.PurchasePromptBody);
        PushOptionalStringField(
            state,
            "purchasePromptConfirmation",
            tab.PurchasePromptConfirmation);
    }

    private static void PushPurchasedTabData(
        lua_State state,
        IList<WowBankTabData> tabs)
    {
        lua_createtable(state, tabs.Count, 0);
        for (var index = 0; index < tabs.Count; index++)
        {
            var tab = tabs[index];
            lua_newtable(state);
            PushNumberField(state, "ID", tab.Id);
            PushNumberField(state, "bankType", tab.BankType);
            PushOptionalStringField(state, "name", tab.Name);
            if (tab.IconFileId is { } iconFileId && iconFileId != 0)
                PushNumberField(state, "icon", iconFileId);
            PushNumberField(state, "depositFlags", tab.DepositFlags);
            PushOptionalStringField(
                state,
                "tabCleanupConfirmation",
                tab.TabCleanupConfirmation);
            PushOptionalStringField(
                state,
                "tabNameEditBoxHeader",
                tab.TabNameEditBoxHeader);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushPurchasedTabIds(
        lua_State state,
        IList<WowBankTabData> tabs)
    {
        lua_createtable(state, tabs.Count, 0);
        for (var index = 0; index < tabs.Count; index++)
        {
            lua_pushinteger(state, tabs[index].Id);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushViewableBankTypes(
        lua_State state,
        WowBankState bank)
    {
        var values = Enumerable.Range(0, 3)
            .Select(value => (byte)value)
            .Where(bank.ViewableBankTypes.Contains)
            .ToArray();
        lua_createtable(state, values.Length, 0);
        for (var index = 0; index < values.Length; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushEnum(
        lua_State state,
        string name,
        params (string Name, int Value)[] values)
    {
        lua_newtable(state);
        foreach (var (field, value) in values)
            PushNumberField(state, field, value);
        lua_setfield(state, -2, name);
    }

    private static void PushEnumMeta(
        lua_State state,
        string name,
        int minimum,
        int maximum,
        int count)
    {
        lua_newtable(state);
        PushNumberField(state, "MinValue", minimum);
        PushNumberField(state, "MaxValue", maximum);
        PushNumberField(state, "NumValues", count);
        lua_setfield(state, -2, name);
    }

    private static void PushNumberField(
        lua_State state,
        string name,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void PushUnsignedField(
        lua_State state,
        string name,
        ulong value)
    {
        PushUnsigned(state, value);
        lua_setfield(state, -2, name);
    }

    private static void PushBooleanField(
        lua_State state,
        string name,
        bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static void PushOptionalStringField(
        lua_State state,
        string name,
        string? value)
    {
        if (value is null)
            return;
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void PushUnsigned(lua_State state, ulong value) =>
        lua_pushnumber(state, value);

    private static string Usage(string operation) =>
        operation switch
        {
            "CanDepositMoney" =>
                "Usage: local canDepositMoney = C_Bank.CanDepositMoney(bankType)",
            "CanPurchaseBankTab" =>
                "Usage: local canPurchaseBankTab = C_Bank.CanPurchaseBankTab(bankType)",
            "CanUseBank" =>
                "Usage: local canUseBank = C_Bank.CanUseBank(bankType)",
            "CanViewBank" =>
                "Usage: local canViewBank = C_Bank.CanViewBank(bankType)",
            "CanWithdrawMoney" =>
                "Usage: local canWithdrawMoney = C_Bank.CanWithdrawMoney(bankType)",
            "FetchBankLockedReason" =>
                "Usage: local reason = C_Bank.FetchBankLockedReason(bankType)",
            "FetchDepositedMoney" =>
                "Usage: local amount = C_Bank.FetchDepositedMoney(bankType)",
            "FetchNextPurchasableBankTabData" =>
                "Usage: local nextPurchasableTabData = C_Bank.FetchNextPurchasableBankTabData(bankType)",
            "FetchNumPurchasedBankTabs" =>
                "Usage: local numPurchasedBankTabs = C_Bank.FetchNumPurchasedBankTabs(bankType)",
            "HasMaxBankTabs" =>
                "Usage: local hasMaxBankTabs = C_Bank.HasMaxBankTabs(bankType)",
            "IsItemAllowedInBankType" =>
                "Usage: local isItemAllowedInBankType = C_Bank.IsItemAllowedInBankType(bankType, itemLocation)",
            "UpdateBankTabSettings" =>
                "Usage: C_Bank.UpdateBankTabSettings(bankType, tabID, tabName, tabIcon, depositFlags)",
            "DepositMoney" =>
                "Usage: C_Bank.DepositMoney(bankType, amount)",
            "WithdrawMoney" =>
                "Usage: C_Bank.WithdrawMoney(bankType, amount)",
            _ => $"Usage: C_Bank.{operation}(bankType)"
        };
}
