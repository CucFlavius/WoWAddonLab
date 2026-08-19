namespace WoWAddonLab.Assets;

public abstract class TactCatalog
{
    protected static object? Field(dynamic row, string name)
    {
        try
        {
            return row[name];
        }
        catch
        {
            return null;
        }
    }

    protected static int Integer(dynamic row, string name) =>
        Convert.ToInt32(Field(row, name) ?? 0);

    protected static uint Unsigned(dynamic row, string name) =>
        Convert.ToUInt32(Field(row, name) ?? 0);

    protected static bool Boolean(dynamic row, string name) =>
        Convert.ToInt32(Field(row, name) ?? 0) != 0;

    protected static string Text(dynamic row, params string[] names)
    {
        foreach (var name in names)
        {
            var value = Convert.ToString(Field(row, name));
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return string.Empty;
    }
}
