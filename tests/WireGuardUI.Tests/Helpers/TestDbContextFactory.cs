using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WireGuardUI.Infrastructure.Data;
namespace WireGuardUI.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
