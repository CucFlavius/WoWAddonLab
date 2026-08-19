using System.Globalization;
using System.Text.RegularExpressions;
using WoWAddonLab.Emulator.Addons;

namespace WoWAddonLab.Emulator.Lua;

internal static partial class WowEnumDocumentation
{
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Read(
        IEnumerable<AddonManifest> manifests)
    {
        var values = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
        foreach (var manifest in manifests.Where(manifest =>
                     manifest.Name.Equals(
                         "Blizzard_APIDocumentationGenerated",
                         StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var file in manifest.Files.Where(file =>
                         file.EndsWith("documentation.lua", StringComparison.OrdinalIgnoreCase) &&
                         File.Exists(file)))
            {
                foreach (Match match in EnumFieldRegex().Matches(File.ReadAllText(file)))
                {
                    var enumName = match.Groups["enum"].Value;
                    var fieldName = match.Groups["field"].Value;
                    var value = double.Parse(
                        match.Groups["value"].Value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture);
                    if (!values.TryGetValue(enumName, out var fields))
                    {
                        fields = new Dictionary<string, double>(StringComparer.Ordinal);
                        values[enumName] = fields;
                    }

                    fields[fieldName] = value;
                }
            }
        }

        return values.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyDictionary<string, double>)entry.Value,
            StringComparer.Ordinal);
    }

    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> ReadConstants(
        IEnumerable<AddonManifest> manifests)
    {
        var manifestList = manifests.ToArray();
        var documentedEnums = Read(manifestList);
        var values = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal);
        foreach (var manifest in manifestList.Where(manifest =>
                     manifest.Name.Equals(
                         "Blizzard_APIDocumentationGenerated",
                         StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var file in manifest.Files.Where(file =>
                         file.EndsWith("documentation.lua", StringComparison.OrdinalIgnoreCase) &&
                         File.Exists(file)))
            {
                var source = File.ReadAllText(file);
                foreach (Match tableMatch in ConstantsTableRegex().Matches(source))
                {
                    var tableName = tableMatch.Groups["table"].Value;
                    var openBrace = tableMatch.Index + tableMatch.Length - 1;
                    var closeBrace = FindMatchingBrace(source, openBrace);
                    if (closeBrace < 0)
                        continue;

                    if (!values.TryGetValue(tableName, out var fields))
                    {
                        fields = new Dictionary<string, object>(StringComparer.Ordinal);
                        values[tableName] = fields;
                    }

                    var block = source[(openBrace + 1)..closeBrace];
                    foreach (Match fieldMatch in ConstantFieldRegex().Matches(block))
                    {
                        var fieldName = fieldMatch.Groups["field"].Value;
                        var fieldType = fieldMatch.Groups["type"].Value;
                        var literal = fieldMatch.Groups["value"].Value;
                        if (TryParseLiteral(literal, out var value))
                        {
                            if (value is string enumMember &&
                                documentedEnums.TryGetValue(fieldType, out var enumFields) &&
                                enumFields.TryGetValue(enumMember, out var enumValue))
                            {
                                value = enumValue;
                            }
                            fields[fieldName] = value;
                        }
                    }
                }
            }
        }

        return values.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyDictionary<string, object>)entry.Value,
            StringComparer.Ordinal);
    }

    private static int FindMatchingBrace(string source, int openBrace)
    {
        var depth = 0;
        var quote = '\0';
        var escaped = false;
        for (var index = openBrace; index < source.Length; index++)
        {
            var character = source[index];
            if (quote != '\0')
            {
                if (escaped)
                    escaped = false;
                else if (character == '\\')
                    escaped = true;
                else if (character == quote)
                    quote = '\0';
                continue;
            }

            if (character is '\'' or '"')
            {
                quote = character;
                continue;
            }
            if (character == '{')
                depth++;
            else if (character == '}' && --depth == 0)
                return index;
        }
        return -1;
    }

    private static bool TryParseLiteral(string literal, out object value)
    {
        if (literal.Equals("true", StringComparison.Ordinal))
        {
            value = true;
            return true;
        }
        if (literal.Equals("false", StringComparison.Ordinal))
        {
            value = false;
            return true;
        }
        if (literal.Length >= 2 && literal[0] == '"' && literal[^1] == '"')
        {
            value = Regex.Unescape(literal[1..^1]);
            return true;
        }
        if (double.TryParse(
                literal,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var number))
        {
            value = number;
            return true;
        }

        value = 0d;
        return false;
    }

    [GeneratedRegex(
        """\{\s*Name\s*=\s*"(?<field>[A-Za-z0-9_]+)"\s*,\s*Type\s*=\s*"(?<enum>[A-Za-z0-9_]+)"\s*,\s*EnumValue\s*=\s*(?<value>-?\d+)\s*\}""",
        RegexOptions.CultureInvariant)]
    private static partial Regex EnumFieldRegex();

    [GeneratedRegex(
        """Name\s*=\s*"(?<table>[A-Za-z0-9_]+)"\s*,\s*Type\s*=\s*"Constants"\s*,\s*Values\s*=\s*\{""",
        RegexOptions.CultureInvariant)]
    private static partial Regex ConstantsTableRegex();

    [GeneratedRegex(
        """\{\s*Name\s*=\s*"(?<field>[A-Za-z0-9_]+)"\s*,\s*Type\s*=\s*"(?<type>[^"]+)"\s*,\s*Value\s*=\s*(?<value>-?(?:(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)|true|false|"(?:\\.|[^"])*")\s*\}""",
        RegexOptions.CultureInvariant)]
    private static partial Regex ConstantFieldRegex();
}
