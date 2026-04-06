using Microsoft.EntityFrameworkCore;
using WireGuardUI.Core.Models;
namespace WireGuardUI.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WireGuardClient> Clients { get; set; }
    public DbSet<ServerConfig> ServerConfigs { get; set; }
    public DbSet<GlobalSetting> GlobalSettings { get; set; }
    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<EmailSetting> EmailSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WireGuardClient>(b =>
        {
            b.HasKey(c => c.Id);
            b.PrimitiveCollection(c => c.AllocatedIPs);
            b.PrimitiveCollection(c => c.AllowedIPs);
            b.PrimitiveCollection(c => c.ExtraAllowedIPs);
        });

        modelBuilder.Entity<ServerConfig>(b =>
        {
            b.HasKey(c => c.Id);
            b.PrimitiveCollection(c => c.Addresses);
        });

        modelBuilder.Entity<GlobalSetting>(b =>
        {
            b.HasKey(g => g.Id);
            b.PrimitiveCollection(g => g.DnsServers);
        });

        modelBuilder.Entity<AdminUser>(b => b.HasKey(u => u.Id));
        modelBuilder.Entity<EmailSetting>(b => b.HasKey(e => e.Id));
    }
}
