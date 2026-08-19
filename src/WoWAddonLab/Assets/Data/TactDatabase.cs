using DBCD;
using DBCD.Providers;

namespace WoWAddonLab.Assets;

internal sealed class TactDatabase
{
    private readonly DBCD.DBCD _database;
    private readonly Dictionary<string, IDBCDStorage> _loadedTables =
        new(StringComparer.OrdinalIgnoreCase);
    private int _cacheScopeDepth;

    public TactDatabase(IDBCProvider dbcProvider, IDBDProvider dbdProvider)
    {
        _database = new DBCD.DBCD(dbcProvider, dbdProvider);
    }

    public IDBCDStorage Load(string tableName, string build)
    {
        if (_cacheScopeDepth == 0)
            return _database.Load(tableName, build);

        var key = $"{build}:{tableName}";
        if (_loadedTables.TryGetValue(key, out var table))
            return table;

        table = _database.Load(tableName, build);
        _loadedTables.Add(key, table);
        return table;
    }

    public IDisposable BeginCacheScope()
    {
        _cacheScopeDepth++;
        return new CacheScope(this);
    }

    private void EndCacheScope()
    {
        _cacheScopeDepth--;
        if (_cacheScopeDepth == 0)
            _loadedTables.Clear();
    }

    private sealed class CacheScope(TactDatabase owner) : IDisposable
    {
        private TactDatabase? _owner = owner;

        public void Dispose()
        {
            _owner?.EndCacheScope();
            _owner = null;
        }
    }
}
