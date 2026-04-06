using Microsoft.EntityFrameworkCore;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.Repositories;
using WireGuardUI.Tests.Helpers;
namespace WireGuardUI.Tests.Infrastructure;

public class ClientRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsAllClients()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Clients.Add(new WireGuardClient { Id = "id1", Name = "Alice" });
        ctx.Clients.Add(new WireGuardClient { Id = "id2", Name = "Bob" });
        await ctx.SaveChangesAsync();

        var repo = new ClientRepository(ctx);
        var result = await repo.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AddAsync_PersistsClient()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new ClientRepository(ctx);
        var client = new WireGuardClient { Id = "id1", Name = "Alice" };

        await repo.AddAsync(client);
        var result = await repo.GetByIdAsync("id1");

        Assert.NotNull(result);
        Assert.Equal("Alice", result.Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesClient()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Clients.Add(new WireGuardClient { Id = "id1", Name = "Alice" });
        await ctx.SaveChangesAsync();

        var repo = new ClientRepository(ctx);
        await repo.DeleteAsync("id1");

        Assert.Empty(ctx.Clients);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesClientFields()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Clients.Add(new WireGuardClient { Id = "id1", Name = "Alice", Enabled = true });
        await ctx.SaveChangesAsync();

        var repo = new ClientRepository(ctx);
        var client = await repo.GetByIdAsync("id1");
        client!.Enabled = false;
        client.Name = "Alice Updated";
        await repo.UpdateAsync(client);

        var updated = await repo.GetByIdAsync("id1");
        Assert.False(updated!.Enabled);
        Assert.Equal("Alice Updated", updated.Name);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotThrow_WhenSameKeyIsAlreadyTracked()
    {
        using var ctx = TestDbContextFactory.Create();
        ctx.Clients.Add(new WireGuardClient { Id = "id1", Name = "Alice", Enabled = true });
        await ctx.SaveChangesAsync();

        // Keep one instance tracked in DbContext local cache
        _ = await ctx.Clients.FirstAsync(c => c.Id == "id1");

        var repo = new ClientRepository(ctx);
        var detached = new WireGuardClient
        {
            Id = "id1",
            Name = "Alice Detached",
            Enabled = false,
            PublicKey = "pub",
            PrivateKey = "priv",
            PresharedKey = "psk"
        };

        await repo.UpdateAsync(detached);

        var updated = await repo.GetByIdAsync("id1");
        Assert.NotNull(updated);
        Assert.Equal("Alice Detached", updated!.Name);
        Assert.False(updated.Enabled);
    }
}
