namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMacroState
{
    public const int MaximumAccountMacros = 120;
    public const int MaximumCharacterMacros = 30;
    public const int MaximumNameLength = 64;
    public const int MaximumIconLength = 256;
    public const int MaximumBodyLength = 1024;

    private readonly List<WowMacroInfo> _account = [];
    private readonly List<WowMacroInfo> _character = [];

    public IReadOnlyList<WowMacroInfo> Account => _account;
    public IReadOnlyList<WowMacroInfo> Character => _character;
    public int? PickedUpMacroIndex { get; set; }
    internal int ExecuteLineCallbackReference { get; set; }

    public WowMacroInfo? Find(int index)
    {
        var (macros, offset) = Collection(index);
        var localIndex = index - offset - 1;
        return localIndex >= 0 && localIndex < macros.Count ? macros[localIndex] : null;
    }

    public int FindIndexByName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return 0;

        for (var index = 0; index < _account.Count; index++)
        {
            if (_account[index].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return index + 1;
        }

        for (var index = 0; index < _character.Count; index++)
        {
            if (_character[index].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return MaximumAccountMacros + index + 1;
        }

        return 0;
    }

    public int Create(string name, object? icon, string body, bool perCharacter)
    {
        var macros = perCharacter ? _character : _account;
        var maximum = perCharacter ? MaximumCharacterMacros : MaximumAccountMacros;
        if (macros.Count >= maximum)
            return 0;
        macros.Add(
            new WowMacroInfo(
                Truncate(name, MaximumNameLength),
                NormalizeIcon(icon),
                Truncate(body, MaximumBodyLength)));
        return (perCharacter ? MaximumAccountMacros : 0) + macros.Count;
    }

    public int Edit(int index, string? name, object? icon, bool replaceIcon, string? body)
    {
        var macro = Find(index);
        if (macro is null)
            return 0;
        if (name is not null)
            macro.Name = Truncate(name, MaximumNameLength);
        if (replaceIcon)
            macro.Icon = NormalizeIcon(icon);
        if (body is not null)
            macro.Body = Truncate(body, MaximumBodyLength);
        return index;
    }

    public bool Delete(int index)
    {
        var (macros, offset) = Collection(index);
        var localIndex = index - offset - 1;
        if (localIndex < 0 || localIndex >= macros.Count)
            return false;
        macros.RemoveAt(localIndex);
        if (PickedUpMacroIndex == index)
            PickedUpMacroIndex = null;
        else if (PickedUpMacroIndex > index &&
                 (PickedUpMacroIndex <= MaximumAccountMacros) ==
                 (index <= MaximumAccountMacros))
            PickedUpMacroIndex--;
        return true;
    }

    private static object? NormalizeIcon(object? icon) =>
        icon is string text ? Truncate(text, MaximumIconLength) : icon;

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private (List<WowMacroInfo> Macros, int Offset) Collection(int index) =>
        index > MaximumAccountMacros
            ? (_character, MaximumAccountMacros)
            : (_account, 0);
}
