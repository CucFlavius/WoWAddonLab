using System.Xml.Linq;

namespace WoWAddonLab.Emulator.Lua;

internal static class WowBindingsXmlLoader
{
    private static readonly IReadOnlyDictionary<string, int> BindingContexts =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["None"] = 0,
            ["HousingEditor"] = 1,
            ["HousingEditorBasicDecorMode"] = 2,
            ["HousingEditorExpertDecorMode"] = 3,
            ["HousingEditorCustomizeMode"] = 4,
            ["HousingEditorCleanupMode"] = 5,
            ["HousingEditorLayoutMode"] = 6,
            ["HousingEditorBasicAndExpertDecorMode"] = 7,
            ["HousingEditorExteriorCustomizationMode"] = 8,
            ["ReservedFutureFeatureBinding01"] = 9
        };

    public static void Load(LuaRuntime runtime, string path)
    {
        if (!File.Exists(path))
            return;

        var document = WowXmlDocument.Load(path);
        foreach (var element in document.Descendants().Where(value =>
                     value.Name.LocalName.Equals("Binding", StringComparison.OrdinalIgnoreCase)))
        {
            var command = Attribute(element, "name");
            if (string.IsNullOrWhiteSpace(command))
                continue;

            var category = Attribute(element, "category") ?? string.Empty;
            if (Attribute(element, "header") is { Length: > 0 } header)
                RegisterSynthetic(runtime, $"HEADER_{header}", category);
            if (Attribute(element, "preface") is { Length: > 0 } preface)
                RegisterSynthetic(runtime, $"PREFACE_{preface}", category);

            var contextName = Attribute(element, "bindingContext") ?? "None";
            var context = BindingContexts.GetValueOrDefault(contextName);
            var customBindingType = Attribute(element, "customBindingID") is { } custom &&
                                    custom.Equals("VoicePushToTalk", StringComparison.OrdinalIgnoreCase)
                ? 0
                : (int?)null;
            var tags = Split(Attribute(element, "searchTags"))
                .Select(value => Resolve(runtime, value));
            runtime.Bindings.Register(
                command,
                category,
                element.Value,
                IsTrue(Attribute(element, "runOnUp")),
                context,
                customBindingType,
                tags);
        }
    }

    public static void LoadDefaults(LuaRuntime runtime, string path)
    {
        if (!File.Exists(path))
            return;

        foreach (var sourceLine in File.ReadLines(path))
        {
            var line = sourceLine.Trim();
            if (!line.StartsWith("bind ", StringComparison.OrdinalIgnoreCase))
                continue;
            var fields = line.Split(
                [' ', '\t'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (fields.Length == 3)
                runtime.Bindings.AddKey(fields[2], fields[1]);
        }
    }

    private static void RegisterSynthetic(LuaRuntime runtime, string command, string category) =>
        runtime.Bindings.Register(command, category, string.Empty, false, 0, null, []);

    private static string Resolve(LuaRuntime runtime, string value) =>
        runtime.GlobalStringProvider?.Strings.TryGetValue(value, out var resolved) == true
            ? resolved
            : value;

    private static IEnumerable<string> Split(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool IsTrue(string? value) =>
        value is not null &&
        (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");

    private static string? Attribute(XElement element, string name) =>
        element.Attributes()
            .FirstOrDefault(value => value.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?.Value;
}
