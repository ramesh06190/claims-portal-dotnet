using ClaimsPortal.Infrastructure.Data;
using ClaimsPortal.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ClaimsPortal.Tests.Web;

/// <summary>Runs the real app in memory, with SQL Server swapped for SQLite and the outbox worker turned off.</summary>
public sealed class ClaimsPortalFactory : WebApplicationFactory<Program>
{
    private readonly SqliteDatabase _database = new();

    public ClaimsDbContext CreateDbContext() => _database.CreateContext();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Claims", "Server=not-used-in-tests");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ClaimsDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ClaimsDbContext>>();
            services.AddDbContext<ClaimsDbContext>(options => options.UseSqlite(_database.Connection));

            // Tests check the outbox rows themselves, so the background publisher must not consume them
            services.RemoveAll<IHostedService>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _database.Dispose();
        }
    }
}
