using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSpellConfirmationApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = GetSpellConfirmationPromptsInfo;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetSpellConfirmationPromptsInfo",
            Callback);
    }

    private static int GetSpellConfirmationPromptsInfo(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        if (!runtime.SpellConfirmation.IsPlayerAvailable)
            return 0;

        lua_createtable(state, runtime.SpellConfirmation.Prompts.Count, 0);

        for (var index = 0; index < runtime.SpellConfirmation.Prompts.Count; index++)
        {
            var prompt = runtime.SpellConfirmation.Prompts[index];
            lua_createtable(state, 0, prompt.CurrencyId == 0 ? 8 : 10);
            SetNumber(state, "spellID", prompt.SpellId);
            SetNumber(state, "confirmType", prompt.ConfirmType);
            SetString(state, "text", prompt.Text);

            var duration = prompt.ExpirationTickMilliseconds is { } expiration
                ? unchecked((int)(expiration - runtime.FrameTime.TickMilliseconds)) / 1000
                : -1;
            SetNumber(state, "duration", duration);

            if (prompt.CurrencyId != 0)
            {
                SetNumber(state, "currencyID", prompt.CurrencyId);
                SetNumber(state, "currencyCost", prompt.CurrencyCost);
            }

            SetNumber(
                state,
                "difficultyID",
                prompt.DifficultyId == 0 ? 14 : prompt.DifficultyId);
            SetNumber(state, "displayItemID", prompt.DisplayItemId);
            SetNumber(state, "itemContext", prompt.ItemContext);
            SetNumber(state, "treasureContextLevel", prompt.TreasureContextLevel);
            lua_rawseti(state, -2, index + 1);
        }

        return 1;
    }

    private static void SetNumber(lua_State state, string field, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetString(lua_State state, string field, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }
}
