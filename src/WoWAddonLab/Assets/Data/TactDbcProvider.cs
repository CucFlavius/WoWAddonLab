using DBCD.Providers;

namespace WoWAddonLab.Assets;

internal sealed class TactDbcProvider(TactAssetSource tact) : IDBCProvider
{
    public Stream StreamForTableName(string tableName, string build)
    {
        var bytes = tact.Read($"DBFilesClient\\{tableName}.db2", null);
        return bytes is null
            ? throw new FileNotFoundException($"Client DB2 table was not found: {tableName}.")
            : new MemoryStream(bytes, writable: false);
    }
}
