namespace WoWAddonLab.Emulator.Lua;

internal static class WowSecureCommandOptionParser
{
    public static WowSecureCommandOptionResult? Parse(
        LuaRuntime runtime,
        string options)
    {
        foreach (var rawClause in options.Split(';'))
        {
            var clause = rawClause.Trim();
            if (clause.Length == 0)
                continue;

            string? target = null;
            var command = clause;
            if (clause[0] == '[')
            {
                var end = clause.IndexOf(']');
                if (end < 0)
                    continue;

                var conditions = clause[1..end];
                command = clause[(end + 1)..].Trim();
                if (!Matches(runtime, conditions, ref target))
                    continue;
            }

            return new WowSecureCommandOptionResult(command, target);
        }

        return null;
    }

    private static bool Matches(
        LuaRuntime runtime,
        string conditions,
        ref string? target)
    {
        foreach (var rawCondition in conditions.Split(','))
        {
            var condition = rawCondition.Trim().ToLowerInvariant();
            if (condition.Length == 0)
                continue;

            if (condition[0] == '@')
            {
                target = condition[1..];
                continue;
            }
            if (condition.StartsWith("target=", StringComparison.Ordinal))
            {
                target = condition[7..];
                continue;
            }

            var separator = condition.IndexOf(':');
            var name = separator < 0 ? condition : condition[..separator];
            var argument = separator < 0 ? string.Empty : condition[(separator + 1)..];
            var invert = name.StartsWith("no", StringComparison.Ordinal) &&
                         IsKnownCondition(name[2..]);
            if (invert)
                name = name[2..];

            var value = Evaluate(runtime, name, argument, target ?? "target");
            if (invert)
                value = !value;
            if (!value)
                return false;
        }

        return true;
    }

    private static bool Evaluate(
        LuaRuntime runtime,
        string name,
        string argument,
        string unitToken)
    {
        var unit = runtime.Units.Find(unitToken);
        var player = runtime.Units.Player;
        var arguments = argument.Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return name switch
        {
            "combat" => runtime.Client.InCombatLockdown || player.IsAffectingCombat,
            "mod" or "modifier" => MatchesModifier(runtime.Input, arguments),
            "petbattle" => runtime.PetBattles.IsInBattle,
            "vehicleui" => player.HasVehicleUi,
            "vehicle" => player.IsInVehicle,
            "overridebar" => runtime.Actions.HasOverrideActionBar,
            "possessbar" => runtime.Actions.IsPossessBarVisible,
            "extrabar" => runtime.Actions.HasExtraActionBar,
            "bonusbar" => MatchesNumber(runtime.Actions.BonusBarIndex + 1, arguments),
            "bar" or "actionbar" => MatchesNumber(
                runtime.Actions.ActionBarPage + 1,
                arguments),
            "stance" or "form" => MatchesNumber(
                runtime.Shapeshift.CurrentFormIndex,
                arguments),
            "spec" => MatchesNumber(runtime.Client.SpecializationIndex ?? 0, arguments),
            "exists" => unit is not null,
            "dead" => unit?.IsDead == true || unit?.IsGhost == true,
            "help" => IsFriendly(player, unit),
            "harm" => unit is not null && !IsFriendly(player, unit),
            "party" => unit?.IsInParty == true,
            "raid" => unit?.RaidIndex is not null,
            "group" => MatchesGroup(player, arguments),
            "mounted" => player.IsMounted,
            "outdoors" => player.IsOutdoors,
            "indoors" => !player.IsOutdoors,
            "channeling" => MatchesChannel(unit, arguments),
            "stealth" => false,
            "swimming" => false,
            "flyable" => false,
            "advflyable" => false,
            "known" => false,
            "equipped" => false,
            "button" => false,
            _ => false
        };
    }

    private static bool MatchesModifier(
        WowInputState input,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
            return input.AltDown || input.ControlDown || input.ShiftDown;

        return arguments.Any(value => value switch
        {
            "alt" => input.AltDown,
            "ctrl" or "control" => input.ControlDown,
            "shift" => input.ShiftDown,
            _ => false
        });
    }

    private static bool MatchesNumber(int value, IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
            return value > 0;
        return arguments.Any(argument =>
            int.TryParse(argument, out var requested) && requested == value);
    }

    private static bool MatchesGroup(
        WowUnitState player,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
            return player.IsInParty || player.RaidIndex is not null;
        return arguments.Any(value => value switch
        {
            "party" => player.IsInParty,
            "raid" => player.RaidIndex is not null,
            _ => false
        });
    }

    private static bool MatchesChannel(
        WowUnitState? unit,
        IReadOnlyList<string> arguments)
    {
        if (unit?.Channel is not { } channel)
            return false;
        if (arguments.Count == 0)
            return true;
        return arguments.Any(value =>
            value.Equals(channel.Name, StringComparison.OrdinalIgnoreCase) ||
            int.TryParse(value, out var spellId) && spellId == channel.SpellId);
    }

    private static bool IsFriendly(WowUnitState player, WowUnitState? unit) =>
        unit is not null &&
        (unit.Guid.Equals(player.Guid, StringComparison.OrdinalIgnoreCase) ||
         !string.IsNullOrEmpty(player.FactionGroupTag) &&
         player.FactionGroupTag.Equals(
             unit.FactionGroupTag,
             StringComparison.OrdinalIgnoreCase));

    private static bool IsKnownCondition(string name) => name is
        "combat" or "mod" or "modifier" or "petbattle" or "vehicleui" or
        "vehicle" or "overridebar" or "possessbar" or "extrabar" or
        "bonusbar" or "bar" or "actionbar" or "stance" or "form" or "spec" or
        "exists" or "dead" or "help" or "harm" or "party" or "raid" or
        "group" or "mounted" or "outdoors" or "indoors" or "channeling" or
        "stealth" or "swimming" or "flyable" or "advflyable" or "known" or
        "equipped" or "button";
}

internal sealed record WowSecureCommandOptionResult(
    string Command,
    string? Target);
