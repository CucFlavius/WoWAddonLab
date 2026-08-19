using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace WoWAddonLab.Emulator.Lua;

internal static partial class WowXmlDocument
{
    private const string SchemaInstanceNamespace =
        "http://www.w3.org/2001/XMLSchema-instance";

    [GeneratedRegex(@"<(?<name>[A-Za-z_][\w.-]*)(?<separator>\s|/?>)")]
    private static partial Regex RootElementPattern();

    public static XDocument Load(string path)
    {
        try
        {
            return XDocument.Load(path, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException)
        {
            var source = File.ReadAllText(path);
            if (!UsesImplicitSchemaInstancePrefix(source))
                throw;

            return XDocument.Parse(DeclareSchemaInstancePrefix(source), LoadOptions.PreserveWhitespace);
        }
    }

    private static bool UsesImplicitSchemaInstancePrefix(string source) =>
        source.Contains("xsi:", StringComparison.Ordinal) &&
        !source.Contains("xmlns:xsi", StringComparison.Ordinal);

    private static string DeclareSchemaInstancePrefix(string source) =>
        RootElementPattern().Replace(
            source,
            match =>
                $"<{match.Groups["name"].Value} xmlns:xsi=\"{SchemaInstanceNamespace}\"" +
                match.Groups["separator"].Value,
            1);
}
