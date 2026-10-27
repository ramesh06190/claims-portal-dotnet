using ClaimsPortal.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ClaimsPortal.Tests.Infrastructure;

/// <summary>
/// An in-memory SQLite database that lives as long as this object. Closer to a real database
/// than the EF in-memory provider: it enforces constraints and translates real SQL.
/// </summary>
public sealed class SqliteDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteDatabase()
    {
        _connection.Open();
        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    public SqliteConnection Connection => _connection;

    public ClaimsDbContext CreateContext(TimeProvider? time = null) =>
        new(new DbContextOptionsBuilder<ClaimsDbContext>().UseSqlite(_connection).Options, time ?? TimeProvider.System);

    public void Dispose() => _connection.Dispose();
}
