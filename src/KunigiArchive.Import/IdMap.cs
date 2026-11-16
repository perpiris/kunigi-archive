using KunigiArchive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KunigiArchive.Import;

// Old long id → new Guid, kept in the new database (import.IdMap), so every run gives a row the same Guid:
// links to /api/... ids and anything that remembers an id stay valid across re-runs.
internal sealed class IdMap
{
    private readonly Dictionary<(string Table, long OldId), Guid> _ids = [];
    private readonly List<(string Table, long OldId, Guid NewId)> _added = [];

    public static async Task<IdMap> LoadAsync(KunigiArchiveDbContext dbContext, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            IF SCHEMA_ID('import') IS NULL EXEC('CREATE SCHEMA import');
            IF OBJECT_ID('import.IdMap') IS NULL
                CREATE TABLE import.IdMap (
                    TableName nvarchar(50) NOT NULL,
                    OldId bigint NOT NULL,
                    NewId uniqueidentifier NOT NULL,
                    CONSTRAINT PK_IdMap PRIMARY KEY (TableName, OldId));
            """,
            cancellationToken);

        List<Row> rows = await dbContext.Database
            .SqlQueryRaw<Row>("SELECT TableName, OldId, NewId FROM import.IdMap")
            .ToListAsync(cancellationToken);

        IdMap map = new IdMap();
        foreach (Row row in rows)
        {
            map._ids[(row.TableName, row.OldId)] = row.NewId;
        }

        return map;
    }

    public Guid For(string table, long oldId)
    {
        if (_ids.TryGetValue((table, oldId), out Guid id))
        {
            return id;
        }

        id = Guid.CreateVersion7();
        _ids[(table, oldId)] = id;
        _added.Add((table, oldId, id));
        return id;
    }

    public bool TryGet(string table, long oldId, out Guid id)
    {
        return _ids.TryGetValue((table, oldId), out id);
    }

    // A user already in the new database (the admin seeded on beta, say) keeps its id: the old user is mapped to it.
    public void Set(string table, long oldId, Guid id)
    {
        if (_ids.TryGetValue((table, oldId), out Guid existing) && existing == id)
        {
            return;
        }

        bool known = _ids.ContainsKey((table, oldId));
        _ids[(table, oldId)] = id;
        if (known)
        {
            _added.RemoveAll(a => a.Table == table && a.OldId == oldId);
            _replaced.Add((table, oldId, id));
        }
        else
        {
            _added.Add((table, oldId, id));
        }
    }

    private readonly List<(string Table, long OldId, Guid NewId)> _replaced = [];

    // In the run's transaction, with the rows that use the ids.
    public async Task SaveAsync(KunigiArchiveDbContext dbContext, CancellationToken cancellationToken)
    {
        foreach ((string table, long oldId, Guid newId) in _replaced)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE import.IdMap SET NewId = {newId} WHERE TableName = {table} AND OldId = {oldId}", cancellationToken);
        }

        foreach ((string table, long oldId, Guid newId) in _added)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO import.IdMap (TableName, OldId, NewId) VALUES ({table}, {oldId}, {newId})", cancellationToken);
        }

        _added.Clear();
        _replaced.Clear();
    }

    private sealed class Row
    {
        public string TableName { get; set; } = null!;
        public long OldId { get; set; }
        public Guid NewId { get; set; }
    }
}
