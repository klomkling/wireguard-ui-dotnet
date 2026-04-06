# WireGuard UI (ASP.NET Core) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a cross-platform (Windows + Linux) WireGuard VPN management UI using ASP.NET Core Blazor Server, replacing the original Go-based wireguard-ui with a .NET 10 implementation.

**Architecture:** 3-project layered solution — `WireGuardUI.Core` (models + interfaces), `WireGuardUI.Infrastructure` (EF Core + SQLite, WireGuard shell, email, crypto), `WireGuardUI.Web` (Blazor Server, MudBlazor). Platform differences (Linux vs Windows WireGuard) are isolated behind `IWireGuardService` with two concrete implementations selected at startup via `RuntimeInformation.IsOSPlatform()`.

**Tech Stack:** .NET 10, Blazor Server, EF Core 10 + SQLite, MudBlazor, BCrypt.Net-Next, MailKit, QRCoder, BouncyCastle.Cryptography, xUnit + bUnit + NSubstitute (tests)

---

## File Map

```
wireguard-ui/
├── WireGuardUI.sln
├── src/
│   ├── WireGuardUI.Core/
│   │   ├── Models/
│   │   │   ├── WireGuardClient.cs
│   │   │   ├── ServerConfig.cs
│   │   │   ├── GlobalSetting.cs
│   │   │   ├── AdminUser.cs
│   │   │   ├── EmailSetting.cs
│   │   │   ├── ApplyResult.cs
│   │   │   ├── WireGuardStatus.cs
│   │   │   └── Result.cs
│   │   ├── Interfaces/
│   │   │   ├── IClientRepository.cs
│   │   │   ├── IServerConfigRepository.cs
│   │   │   ├── IGlobalSettingRepository.cs
│   │   │   ├── IEmailSettingRepository.cs
│   │   │   ├── IAdminUserRepository.cs
│   │   │   ├── IWireGuardService.cs
│   │   │   ├── IEmailService.cs
│   │   │   ├── IKeyPairService.cs
│   │   │   └── IIpAllocationService.cs
│   │   └── Services/
│   │       └── IpAllocationService.cs
│   ├── WireGuardUI.Infrastructure/
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── AppDbContextFactory.cs
│   │   │   └── Migrations/  (generated)
│   │   ├── Repositories/
│   │   │   ├── ClientRepository.cs
│   │   │   ├── ServerConfigRepository.cs
│   │   │   ├── GlobalSettingRepository.cs
│   │   │   ├── EmailSettingRepository.cs
│   │   │   └── AdminUserRepository.cs
│   │   ├── WireGuard/
│   │   │   ├── WireGuardConfigGenerator.cs
│   │   │   ├── LinuxWireGuardService.cs
│   │   │   └── WindowsWireGuardService.cs
│   │   ├── Email/
│   │   │   └── SmtpEmailService.cs
│   │   └── Crypto/
│   │       └── BouncyCastleKeyPairService.cs
│   └── WireGuardUI.Web/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── WireGuardUiSettings.cs
│       ├── Components/
│       │   ├── App.razor
│       │   ├── Routes.razor
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor
│       │   │   └── NavMenu.razor
│       │   ├── Shared/
│       │   │   ├── ConfirmDialog.razor
│       │   │   ├── QrCodeDisplay.razor
│       │   │   └── RedirectToLogin.razor
│       │   └── Pages/
│       │       ├── Login.razor
│       │       ├── Clients.razor
│       │       ├── ClientDialog.razor
│       │       ├── QrCodeDialog.razor
│       │       ├── EmailDialog.razor
│       │       ├── Server.razor
│       │       ├── GlobalSettings.razor
│       │       ├── EmailSettings.razor
│       │       ├── Status.razor
│       │       └── Profile.razor
│       └── wwwroot/
│           └── app.css
└── tests/
    └── WireGuardUI.Tests/
        ├── Core/
        │   ├── IpAllocationServiceTests.cs
        │   └── WireGuardConfigGeneratorTests.cs
        ├── Infrastructure/
        │   ├── ClientRepositoryTests.cs
        │   └── KeyPairServiceTests.cs
        └── Helpers/
            └── TestDbContextFactory.cs
```

---

## Task 1: Create Solution and Projects

**Files:**
- Create: `WireGuardUI.sln`
- Create: `src/WireGuardUI.Core/WireGuardUI.Core.csproj`
- Create: `src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj`
- Create: `src/WireGuardUI.Web/WireGuardUI.Web.csproj`
- Create: `tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj`

- [ ] **Step 1: Create solution and project scaffolding**

Run from `/Users/sarawut/GitHub/Idevs/single-repo/wireguard-ui/`:

```bash
dotnet new sln -n WireGuardUI

dotnet new classlib -n WireGuardUI.Core -f net10.0 -o src/WireGuardUI.Core
dotnet new classlib -n WireGuardUI.Infrastructure -f net10.0 -o src/WireGuardUI.Infrastructure
dotnet new blazor -n WireGuardUI.Web -f net10.0 -o src/WireGuardUI.Web --interactivity Server --no-https
dotnet new xunit -n WireGuardUI.Tests -f net10.0 -o tests/WireGuardUI.Tests

dotnet sln add src/WireGuardUI.Core/WireGuardUI.Core.csproj
dotnet sln add src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj
dotnet sln add src/WireGuardUI.Web/WireGuardUI.Web.csproj
dotnet sln add tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj
```

> If `dotnet new blazor --interactivity Server` fails, try: `dotnet new blazorserver -n WireGuardUI.Web -f net10.0 -o src/WireGuardUI.Web --no-https`

- [ ] **Step 2: Add project references**

```bash
dotnet add src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj reference src/WireGuardUI.Core/WireGuardUI.Core.csproj
dotnet add src/WireGuardUI.Web/WireGuardUI.Web.csproj reference src/WireGuardUI.Core/WireGuardUI.Core.csproj
dotnet add src/WireGuardUI.Web/WireGuardUI.Web.csproj reference src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj
dotnet add tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj reference src/WireGuardUI.Core/WireGuardUI.Core.csproj
dotnet add tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj reference src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj
```

- [ ] **Step 3: Add NuGet packages**

```bash
# Infrastructure
dotnet add src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj package BCrypt.Net-Next
dotnet add src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj package MailKit
dotnet add src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj package BouncyCastle.Cryptography

# Web
dotnet add src/WireGuardUI.Web/WireGuardUI.Web.csproj package MudBlazor
dotnet add src/WireGuardUI.Web/WireGuardUI.Web.csproj package QRCoder
dotnet add src/WireGuardUI.Web/WireGuardUI.Web.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add src/WireGuardUI.Web/WireGuardUI.Web.csproj package Microsoft.EntityFrameworkCore.Design

# Tests
dotnet add tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj package NSubstitute
dotnet add tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj package Microsoft.Data.Sqlite
dotnet add tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj package Microsoft.EntityFrameworkCore.Sqlite
dotnet add tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj package bunit
```

- [ ] **Step 4: Delete generated boilerplate files**

```bash
rm src/WireGuardUI.Core/Class1.cs
rm src/WireGuardUI.Infrastructure/Class1.cs
rm tests/WireGuardUI.Tests/UnitTest1.cs
# Delete generated Blazor sample pages (keep App.razor, Routes.razor, MainLayout, _Imports.razor)
rm -f src/WireGuardUI.Web/Components/Pages/Counter.razor
rm -f src/WireGuardUI.Web/Components/Pages/Weather.razor
rm -f src/WireGuardUI.Web/Components/Pages/Home.razor
```

- [ ] **Step 5: Verify solution builds**

```bash
dotnet build WireGuardUI.sln
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6: Commit**

```bash
git add .
git commit -m "chore: scaffold solution with 3-project layered structure"
```

---

## Task 2: Core Models

**Files:**
- Create: `src/WireGuardUI.Core/Models/WireGuardClient.cs`
- Create: `src/WireGuardUI.Core/Models/ServerConfig.cs`
- Create: `src/WireGuardUI.Core/Models/GlobalSetting.cs`
- Create: `src/WireGuardUI.Core/Models/AdminUser.cs`
- Create: `src/WireGuardUI.Core/Models/EmailSetting.cs`
- Create: `src/WireGuardUI.Core/Models/ApplyResult.cs`
- Create: `src/WireGuardUI.Core/Models/WireGuardStatus.cs`
- Create: `src/WireGuardUI.Core/Models/Result.cs`

- [ ] **Step 1: Create WireGuardClient.cs**

```csharp
// src/WireGuardUI.Core/Models/WireGuardClient.cs
namespace WireGuardUI.Core.Models;

public class WireGuardClient
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string PresharedKey { get; set; } = string.Empty;
    public List<string> AllocatedIPs { get; set; } = [];
    public List<string> AllowedIPs { get; set; } = ["0.0.0.0/0"];
    public List<string> ExtraAllowedIPs { get; set; } = [];
    public string? Endpoint { get; set; }
    public bool UseServerDns { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Create ServerConfig.cs**

```csharp
// src/WireGuardUI.Core/Models/ServerConfig.cs
namespace WireGuardUI.Core.Models;

public class ServerConfig
{
    public int Id { get; set; } = 1;
    public string PrivateKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public List<string> Addresses { get; set; } = ["10.252.1.0/24"];
    public int ListenPort { get; set; } = 51820;
    public string? PostUp { get; set; }
    public string? PreDown { get; set; }
    public string? PostDown { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 3: Create GlobalSetting.cs**

```csharp
// src/WireGuardUI.Core/Models/GlobalSetting.cs
namespace WireGuardUI.Core.Models;

public class GlobalSetting
{
    public int Id { get; set; } = 1;
    public string EndpointAddress { get; set; } = string.Empty;
    public List<string> DnsServers { get; set; } = ["1.1.1.1"];
    public int Mtu { get; set; } = 1450;
    public int PersistentKeepalive { get; set; } = 15;
    public string FirewallMark { get; set; } = "0xca6c";
    public string Table { get; set; } = string.Empty;
    public string ConfigFilePath { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 4: Create AdminUser.cs**

```csharp
// src/WireGuardUI.Core/Models/AdminUser.cs
namespace WireGuardUI.Core.Models;

public class AdminUser
{
    public int Id { get; set; } = 1;
    public string Username { get; set; } = "admin";
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 5: Create EmailSetting.cs**

```csharp
// src/WireGuardUI.Core/Models/EmailSetting.cs
namespace WireGuardUI.Core.Models;

public class EmailSetting
{
    public int Id { get; set; } = 1;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string Encryption { get; set; } = "STARTTLS"; // None | SSL | TLS | STARTTLS
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 6: Create ApplyResult.cs**

```csharp
// src/WireGuardUI.Core/Models/ApplyResult.cs
namespace WireGuardUI.Core.Models;

public record ApplyResult(bool Success, string? ErrorMessage = null, string? RawOutput = null)
{
    public static ApplyResult Ok(string? output = null) => new(true, null, output);
    public static ApplyResult Fail(string error, string? output = null) => new(false, error, output);
}
```

- [ ] **Step 7: Create WireGuardStatus.cs**

```csharp
// src/WireGuardUI.Core/Models/WireGuardStatus.cs
namespace WireGuardUI.Core.Models;

public record WireGuardStatus(
    string InterfaceName,
    string PublicKey,
    int ListenPort,
    List<WireGuardPeer> Peers);

public record WireGuardPeer(
    string PublicKey,
    string? Endpoint,
    List<string> AllowedIPs,
    string? LastHandshake,
    long RxBytes,
    long TxBytes);
```

- [ ] **Step 8: Create Result.cs**

```csharp
// src/WireGuardUI.Core/Models/Result.cs
namespace WireGuardUI.Core.Models;

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
```

- [ ] **Step 9: Build and commit**

```bash
dotnet build src/WireGuardUI.Core/WireGuardUI.Core.csproj
```

Expected: `Build succeeded. 0 Error(s)`

```bash
git add src/WireGuardUI.Core/Models/
git commit -m "feat(core): add domain models"
```

---

## Task 3: Core Interfaces

**Files:**
- Create: `src/WireGuardUI.Core/Interfaces/IClientRepository.cs`
- Create: `src/WireGuardUI.Core/Interfaces/IServerConfigRepository.cs`
- Create: `src/WireGuardUI.Core/Interfaces/IGlobalSettingRepository.cs`
- Create: `src/WireGuardUI.Core/Interfaces/IEmailSettingRepository.cs`
- Create: `src/WireGuardUI.Core/Interfaces/IAdminUserRepository.cs`
- Create: `src/WireGuardUI.Core/Interfaces/IWireGuardService.cs`
- Create: `src/WireGuardUI.Core/Interfaces/IEmailService.cs`
- Create: `src/WireGuardUI.Core/Interfaces/IKeyPairService.cs`
- Create: `src/WireGuardUI.Core/Interfaces/IIpAllocationService.cs`

- [ ] **Step 1: Create repository interfaces**

```csharp
// src/WireGuardUI.Core/Interfaces/IClientRepository.cs
using WireGuardUI.Core.Models;
namespace WireGuardUI.Core.Interfaces;

public interface IClientRepository
{
    Task<List<WireGuardClient>> GetAllAsync();
    Task<WireGuardClient?> GetByIdAsync(string id);
    Task AddAsync(WireGuardClient client);
    Task UpdateAsync(WireGuardClient client);
    Task DeleteAsync(string id);
}
```

```csharp
// src/WireGuardUI.Core/Interfaces/IServerConfigRepository.cs
using WireGuardUI.Core.Models;
namespace WireGuardUI.Core.Interfaces;

public interface IServerConfigRepository
{
    Task<ServerConfig> GetAsync();
    Task SaveAsync(ServerConfig config);
}
```

```csharp
// src/WireGuardUI.Core/Interfaces/IGlobalSettingRepository.cs
using WireGuardUI.Core.Models;
namespace WireGuardUI.Core.Interfaces;

public interface IGlobalSettingRepository
{
    Task<GlobalSetting> GetAsync();
    Task SaveAsync(GlobalSetting setting);
}
```

```csharp
// src/WireGuardUI.Core/Interfaces/IEmailSettingRepository.cs
using WireGuardUI.Core.Models;
namespace WireGuardUI.Core.Interfaces;

public interface IEmailSettingRepository
{
    Task<EmailSetting> GetAsync();
    Task SaveAsync(EmailSetting setting);
}
```

```csharp
// src/WireGuardUI.Core/Interfaces/IAdminUserRepository.cs
using WireGuardUI.Core.Models;
namespace WireGuardUI.Core.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser> GetAsync();
    Task SaveAsync(AdminUser user);
}
```

- [ ] **Step 2: Create service interfaces**

```csharp
// src/WireGuardUI.Core/Interfaces/IWireGuardService.cs
using WireGuardUI.Core.Models;
namespace WireGuardUI.Core.Interfaces;

public interface IWireGuardService
{
    Task<string> GenerateConfigAsync();
    Task WriteConfigAsync(string configContent);
    Task<ApplyResult> SyncConfAsync();
    Task<Result<WireGuardStatus>> GetStatusAsync();
}
```

```csharp
// src/WireGuardUI.Core/Interfaces/IEmailService.cs
using WireGuardUI.Core.Models;
namespace WireGuardUI.Core.Interfaces;

public interface IEmailService
{
    Task<Result> SendClientConfigAsync(WireGuardClient client, ServerConfig server, GlobalSetting settings, string toEmail);
    Task<Result> TestConnectionAsync(EmailSetting emailSetting);
}
```

```csharp
// src/WireGuardUI.Core/Interfaces/IKeyPairService.cs
namespace WireGuardUI.Core.Interfaces;

public interface IKeyPairService
{
    (string PrivateKey, string PublicKey) GenerateKeyPair();
    string GeneratePresharedKey();
}
```

```csharp
// src/WireGuardUI.Core/Interfaces/IIpAllocationService.cs
namespace WireGuardUI.Core.Interfaces;

public interface IIpAllocationService
{
    string AllocateNextIp(string serverSubnet, IEnumerable<string> existingAllocatedIps);
}
```

- [ ] **Step 3: Build and commit**

```bash
dotnet build src/WireGuardUI.Core/WireGuardUI.Core.csproj
```

Expected: `Build succeeded. 0 Error(s)`

```bash
git add src/WireGuardUI.Core/Interfaces/
git commit -m "feat(core): add repository and service interfaces"
```

---

## Task 4: IpAllocationService (TDD)

**Files:**
- Create: `tests/WireGuardUI.Tests/Core/IpAllocationServiceTests.cs`
- Create: `src/WireGuardUI.Core/Services/IpAllocationService.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/WireGuardUI.Tests/Core/IpAllocationServiceTests.cs
using WireGuardUI.Core.Services;
namespace WireGuardUI.Tests.Core;

public class IpAllocationServiceTests
{
    private readonly IpAllocationService _sut = new();

    [Fact]
    public void AllocateNextIp_EmptySubnet_ReturnsFirstHostIp()
    {
        var result = _sut.AllocateNextIp("10.252.1.0/24", []);
        Assert.Equal("10.252.1.1/32", result);
    }

    [Fact]
    public void AllocateNextIp_FirstTwoTaken_ReturnsThirdHostIp()
    {
        var result = _sut.AllocateNextIp("10.252.1.0/24", ["10.252.1.1/32", "10.252.1.2/32"]);
        Assert.Equal("10.252.1.3/32", result);
    }

    [Fact]
    public void AllocateNextIp_SkipsExistingIpsWithoutCidrSuffix()
    {
        var result = _sut.AllocateNextIp("10.252.1.0/24", ["10.252.1.1"]);
        Assert.Equal("10.252.1.2/32", result);
    }

    [Fact]
    public void AllocateNextIp_SubnetFull_ThrowsInvalidOperationException()
    {
        // /30 has 2 usable hosts: .1 and .2
        var existing = new[] { "10.0.0.1/32", "10.0.0.2/32" };
        Assert.Throws<InvalidOperationException>(() =>
            _sut.AllocateNextIp("10.0.0.0/30", existing));
    }
}
```

- [ ] **Step 2: Run to verify tests fail**

```bash
dotnet test tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj --filter "IpAllocationServiceTests"
```

Expected: compilation error — `IpAllocationService` does not exist yet.

- [ ] **Step 3: Implement IpAllocationService**

```csharp
// src/WireGuardUI.Core/Services/IpAllocationService.cs
using System.Net;
using WireGuardUI.Core.Interfaces;
namespace WireGuardUI.Core.Services;

public class IpAllocationService : IIpAllocationService
{
    public string AllocateNextIp(string serverSubnet, IEnumerable<string> existingAllocatedIps)
    {
        var parts = serverSubnet.Split('/');
        var networkAddress = IPAddress.Parse(parts[0]);
        var prefixLength = int.Parse(parts[1]);

        var bytes = networkAddress.GetAddressBytes();
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        var networkUint = BitConverter.ToUInt32(bytes, 0);
        var hostCount = (uint)(1 << (32 - prefixLength));

        var existingSet = existingAllocatedIps
            .Select(ip => ip.Split('/')[0])
            .ToHashSet();

        // Skip network (.0) and broadcast (last) addresses
        for (uint i = 1; i < hostCount - 1; i++)
        {
            var candidateUint = networkUint + i;
            var candidateBytes = BitConverter.GetBytes(candidateUint);
            if (BitConverter.IsLittleEndian) Array.Reverse(candidateBytes);
            var candidate = new IPAddress(candidateBytes).ToString();

            if (!existingSet.Contains(candidate))
                return $"{candidate}/32";
        }

        throw new InvalidOperationException($"No available IPs in subnet {serverSubnet}.");
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj --filter "IpAllocationServiceTests"
```

Expected: `Passed! - Failed: 0, Passed: 4`

- [ ] **Step 5: Commit**

```bash
git add src/WireGuardUI.Core/Services/ tests/WireGuardUI.Tests/Core/IpAllocationServiceTests.cs
git commit -m "feat(core): add IpAllocationService with tests"
```

---

## Task 5: EF Core AppDbContext

**Files:**
- Create: `src/WireGuardUI.Infrastructure/Data/AppDbContext.cs`
- Create: `src/WireGuardUI.Infrastructure/Data/AppDbContextFactory.cs`
- Create: `tests/WireGuardUI.Tests/Helpers/TestDbContextFactory.cs`

- [ ] **Step 1: Create AppDbContext**

```csharp
// src/WireGuardUI.Infrastructure/Data/AppDbContext.cs
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
```

- [ ] **Step 2: Create design-time factory for EF migrations**

```csharp
// src/WireGuardUI.Infrastructure/Data/AppDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace WireGuardUI.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=design-time.db")
            .Options;
        return new AppDbContext(options);
    }
}
```

- [ ] **Step 3: Create test helper**

```csharp
// tests/WireGuardUI.Tests/Helpers/TestDbContextFactory.cs
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
```

- [ ] **Step 4: Run initial EF migration**

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate \
  --project src/WireGuardUI.Infrastructure \
  --startup-project src/WireGuardUI.Web \
  --output-dir Data/Migrations
```

Expected: migration files created in `src/WireGuardUI.Infrastructure/Data/Migrations/`

- [ ] **Step 5: Build and commit**

```bash
dotnet build WireGuardUI.sln
git add src/WireGuardUI.Infrastructure/Data/ tests/WireGuardUI.Tests/Helpers/
git commit -m "feat(infra): add EF Core AppDbContext and SQLite migration"
```

---

## Task 6: Repositories

**Files:**
- Create: `src/WireGuardUI.Infrastructure/Repositories/ClientRepository.cs`
- Create: `src/WireGuardUI.Infrastructure/Repositories/ServerConfigRepository.cs`
- Create: `src/WireGuardUI.Infrastructure/Repositories/GlobalSettingRepository.cs`
- Create: `src/WireGuardUI.Infrastructure/Repositories/EmailSettingRepository.cs`
- Create: `src/WireGuardUI.Infrastructure/Repositories/AdminUserRepository.cs`
- Create: `tests/WireGuardUI.Tests/Infrastructure/ClientRepositoryTests.cs`

- [ ] **Step 1: Write ClientRepository tests**

```csharp
// tests/WireGuardUI.Tests/Infrastructure/ClientRepositoryTests.cs
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
}
```

- [ ] **Step 2: Run to verify tests fail**

```bash
dotnet test tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj --filter "ClientRepositoryTests"
```

Expected: compilation error — `ClientRepository` does not exist.

- [ ] **Step 3: Implement ClientRepository**

```csharp
// src/WireGuardUI.Infrastructure/Repositories/ClientRepository.cs
using Microsoft.EntityFrameworkCore;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.Data;
namespace WireGuardUI.Infrastructure.Repositories;

public class ClientRepository(AppDbContext db) : IClientRepository
{
    public Task<List<WireGuardClient>> GetAllAsync() =>
        db.Clients.OrderBy(c => c.CreatedAt).ToListAsync();

    public Task<WireGuardClient?> GetByIdAsync(string id) =>
        db.Clients.FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(WireGuardClient client)
    {
        db.Clients.Add(client);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(WireGuardClient client)
    {
        client.UpdatedAt = DateTime.UtcNow;
        db.Clients.Update(client);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var client = await db.Clients.FindAsync(id);
        if (client is not null)
        {
            db.Clients.Remove(client);
            await db.SaveChangesAsync();
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj --filter "ClientRepositoryTests"
```

Expected: `Passed! - Failed: 0, Passed: 4`

- [ ] **Step 5: Implement remaining repositories**

```csharp
// src/WireGuardUI.Infrastructure/Repositories/ServerConfigRepository.cs
using Microsoft.EntityFrameworkCore;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.Data;
namespace WireGuardUI.Infrastructure.Repositories;

public class ServerConfigRepository(AppDbContext db) : IServerConfigRepository
{
    public async Task<ServerConfig> GetAsync() =>
        await db.ServerConfigs.FirstOrDefaultAsync() ?? new ServerConfig();

    public async Task SaveAsync(ServerConfig config)
    {
        config.UpdatedAt = DateTime.UtcNow;
        var existing = await db.ServerConfigs.FirstOrDefaultAsync();
        if (existing is null)
            db.ServerConfigs.Add(config);
        else
        {
            db.Entry(existing).CurrentValues.SetValues(config);
            existing.Addresses = config.Addresses;
        }
        await db.SaveChangesAsync();
    }
}
```

```csharp
// src/WireGuardUI.Infrastructure/Repositories/GlobalSettingRepository.cs
using Microsoft.EntityFrameworkCore;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.Data;
namespace WireGuardUI.Infrastructure.Repositories;

public class GlobalSettingRepository(AppDbContext db) : IGlobalSettingRepository
{
    public async Task<GlobalSetting> GetAsync() =>
        await db.GlobalSettings.FirstOrDefaultAsync() ?? new GlobalSetting();

    public async Task SaveAsync(GlobalSetting setting)
    {
        setting.UpdatedAt = DateTime.UtcNow;
        var existing = await db.GlobalSettings.FirstOrDefaultAsync();
        if (existing is null)
            db.GlobalSettings.Add(setting);
        else
        {
            db.Entry(existing).CurrentValues.SetValues(setting);
            existing.DnsServers = setting.DnsServers;
        }
        await db.SaveChangesAsync();
    }
}
```

```csharp
// src/WireGuardUI.Infrastructure/Repositories/EmailSettingRepository.cs
using Microsoft.EntityFrameworkCore;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.Data;
namespace WireGuardUI.Infrastructure.Repositories;

public class EmailSettingRepository(AppDbContext db) : IEmailSettingRepository
{
    public async Task<EmailSetting> GetAsync() =>
        await db.EmailSettings.FirstOrDefaultAsync() ?? new EmailSetting();

    public async Task SaveAsync(EmailSetting setting)
    {
        setting.UpdatedAt = DateTime.UtcNow;
        var existing = await db.EmailSettings.FirstOrDefaultAsync();
        if (existing is null)
            db.EmailSettings.Add(setting);
        else
            db.Entry(existing).CurrentValues.SetValues(setting);
        await db.SaveChangesAsync();
    }
}
```

```csharp
// src/WireGuardUI.Infrastructure/Repositories/AdminUserRepository.cs
using Microsoft.EntityFrameworkCore;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.Data;
namespace WireGuardUI.Infrastructure.Repositories;

public class AdminUserRepository(AppDbContext db) : IAdminUserRepository
{
    public async Task<AdminUser> GetAsync() =>
        await db.AdminUsers.FirstOrDefaultAsync() ?? new AdminUser();

    public async Task SaveAsync(AdminUser user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        var existing = await db.AdminUsers.FirstOrDefaultAsync();
        if (existing is null)
            db.AdminUsers.Add(user);
        else
            db.Entry(existing).CurrentValues.SetValues(user);
        await db.SaveChangesAsync();
    }
}
```

- [ ] **Step 6: Build and commit**

```bash
dotnet build WireGuardUI.sln
git add src/WireGuardUI.Infrastructure/Repositories/ tests/WireGuardUI.Tests/Infrastructure/
git commit -m "feat(infra): add EF Core repository implementations with tests"
```

---

## Task 7: Key Pair Service (TDD)

**Files:**
- Create: `tests/WireGuardUI.Tests/Infrastructure/KeyPairServiceTests.cs`
- Create: `src/WireGuardUI.Infrastructure/Crypto/BouncyCastleKeyPairService.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// tests/WireGuardUI.Tests/Infrastructure/KeyPairServiceTests.cs
using WireGuardUI.Infrastructure.Crypto;
namespace WireGuardUI.Tests.Infrastructure;

public class KeyPairServiceTests
{
    private readonly BouncyCastleKeyPairService _sut = new();

    [Fact]
    public void GenerateKeyPair_ReturnsBase64EncodedKeys()
    {
        var (privateKey, publicKey) = _sut.GenerateKeyPair();

        Assert.NotEmpty(privateKey);
        Assert.NotEmpty(publicKey);
        // WireGuard keys are 32 bytes = 44 chars base64 (with padding)
        Assert.Equal(44, privateKey.Length);
        Assert.Equal(44, publicKey.Length);
        // Must be valid base64
        Assert.True(IsValidBase64(privateKey));
        Assert.True(IsValidBase64(publicKey));
    }

    [Fact]
    public void GenerateKeyPair_EachCallProducesDifferentKeys()
    {
        var (priv1, _) = _sut.GenerateKeyPair();
        var (priv2, _) = _sut.GenerateKeyPair();
        Assert.NotEqual(priv1, priv2);
    }

    [Fact]
    public void GeneratePresharedKey_Returns44CharBase64()
    {
        var key = _sut.GeneratePresharedKey();
        Assert.Equal(44, key.Length);
        Assert.True(IsValidBase64(key));
    }

    private static bool IsValidBase64(string s)
    {
        try { Convert.FromBase64String(s); return true; }
        catch { return false; }
    }
}
```

- [ ] **Step 2: Run to verify tests fail**

```bash
dotnet test tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj --filter "KeyPairServiceTests"
```

Expected: compilation error — `BouncyCastleKeyPairService` does not exist.

- [ ] **Step 3: Implement BouncyCastleKeyPairService**

```csharp
// src/WireGuardUI.Infrastructure/Crypto/BouncyCastleKeyPairService.cs
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using WireGuardUI.Core.Interfaces;
namespace WireGuardUI.Infrastructure.Crypto;

public class BouncyCastleKeyPairService : IKeyPairService
{
    public (string PrivateKey, string PublicKey) GenerateKeyPair()
    {
        var generator = new X25519KeyPairGenerator();
        generator.Init(new X25519KeyGenerationParameters(new SecureRandom()));
        var keyPair = generator.GenerateKeyPair();

        var privateKey = (X25519PrivateKeyParameters)keyPair.Private;
        var publicKey = (X25519PublicKeyParameters)keyPair.Public;

        return (
            Convert.ToBase64String(privateKey.GetEncoded()),
            Convert.ToBase64String(publicKey.GetEncoded())
        );
    }

    public string GeneratePresharedKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return Convert.ToBase64String(key);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj --filter "KeyPairServiceTests"
```

Expected: `Passed! - Failed: 0, Passed: 3`

- [ ] **Step 5: Commit**

```bash
git add src/WireGuardUI.Infrastructure/Crypto/ tests/WireGuardUI.Tests/Infrastructure/KeyPairServiceTests.cs
git commit -m "feat(infra): add BouncyCastle key pair service with tests"
```

---

## Task 8: WireGuard Config Generator (TDD)

**Files:**
- Create: `src/WireGuardUI.Infrastructure/WireGuard/WireGuardConfigGenerator.cs`
- Create: `tests/WireGuardUI.Tests/Core/WireGuardConfigGeneratorTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
// tests/WireGuardUI.Tests/Core/WireGuardConfigGeneratorTests.cs
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.WireGuard;
namespace WireGuardUI.Tests.Core;

public class WireGuardConfigGeneratorTests
{
    private static ServerConfig MakeServer() => new()
    {
        PrivateKey = "serverPrivKey==",
        PublicKey = "serverPubKey==",
        Addresses = ["10.252.1.0/24"],
        ListenPort = 51820
    };

    private static GlobalSetting MakeSettings() => new()
    {
        EndpointAddress = "vpn.example.com",
        DnsServers = ["1.1.1.1"],
        Mtu = 1450,
        PersistentKeepalive = 15
    };

    [Fact]
    public void GenerateServerConfig_ContainsInterfaceSection()
    {
        var server = MakeServer();
        var clients = new List<WireGuardClient>();

        var result = WireGuardConfigGenerator.GenerateServerConfig(server, MakeSettings(), clients);

        Assert.Contains("[Interface]", result);
        Assert.Contains("ListenPort = 51820", result);
        Assert.Contains("PrivateKey = serverPrivKey==", result);
        Assert.Contains("Address = 10.252.1.0/24", result);
        Assert.Contains("MTU = 1450", result);
    }

    [Fact]
    public void GenerateServerConfig_OnlyIncludesEnabledClients()
    {
        var server = MakeServer();
        var clients = new List<WireGuardClient>
        {
            new() { Name = "Alice", PublicKey = "alicePub==", AllocatedIPs = ["10.252.1.2/32"], Enabled = true },
            new() { Name = "Bob",   PublicKey = "bobPub==",   AllocatedIPs = ["10.252.1.3/32"], Enabled = false }
        };

        var result = WireGuardConfigGenerator.GenerateServerConfig(server, MakeSettings(), clients);

        Assert.Contains("alicePub==", result);
        Assert.DoesNotContain("bobPub==", result);
    }

    [Fact]
    public void GenerateClientConfig_ContainsBothSections()
    {
        var client = new WireGuardClient
        {
            PrivateKey = "clientPrivKey==",
            PublicKey = "clientPubKey==",
            PresharedKey = "psk==",
            AllocatedIPs = ["10.252.1.2/32"],
            AllowedIPs = ["0.0.0.0/0"],
            UseServerDns = true
        };

        var result = WireGuardConfigGenerator.GenerateClientConfig(client, MakeServer(), MakeSettings());

        Assert.Contains("[Interface]", result);
        Assert.Contains("PrivateKey = clientPrivKey==", result);
        Assert.Contains("Address = 10.252.1.2/32", result);
        Assert.Contains("[Peer]", result);
        Assert.Contains("PublicKey = serverPubKey==", result);
        Assert.Contains("Endpoint = vpn.example.com:51820", result);
        Assert.Contains("PresharedKey = psk==", result);
        Assert.Contains("PersistentKeepalive = 15", result);
        Assert.Contains("DNS = 1.1.1.1", result);
    }
}
```

- [ ] **Step 2: Run to verify tests fail**

```bash
dotnet test tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj --filter "WireGuardConfigGeneratorTests"
```

Expected: compilation error — `WireGuardConfigGenerator` does not exist.

- [ ] **Step 3: Implement WireGuardConfigGenerator**

```csharp
// src/WireGuardUI.Infrastructure/WireGuard/WireGuardConfigGenerator.cs
using System.Text;
using WireGuardUI.Core.Models;
namespace WireGuardUI.Infrastructure.WireGuard;

public static class WireGuardConfigGenerator
{
    public static string GenerateServerConfig(
        ServerConfig server,
        GlobalSetting settings,
        IEnumerable<WireGuardClient> clients)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Interface]");
        sb.AppendLine($"Address = {string.Join(", ", server.Addresses)}");
        sb.AppendLine($"ListenPort = {server.ListenPort}");
        sb.AppendLine($"PrivateKey = {server.PrivateKey}");
        sb.AppendLine($"MTU = {settings.Mtu}");

        if (settings.DnsServers.Count > 0)
            sb.AppendLine($"DNS = {string.Join(", ", settings.DnsServers)}");
        if (!string.IsNullOrWhiteSpace(settings.FirewallMark) && settings.FirewallMark != "0")
            sb.AppendLine($"FwMark = {settings.FirewallMark}");
        if (!string.IsNullOrWhiteSpace(settings.Table))
            sb.AppendLine($"Table = {settings.Table}");
        if (!string.IsNullOrWhiteSpace(server.PostUp))
            sb.AppendLine($"PostUp = {server.PostUp}");
        if (!string.IsNullOrWhiteSpace(server.PreDown))
            sb.AppendLine($"PreDown = {server.PreDown}");
        if (!string.IsNullOrWhiteSpace(server.PostDown))
            sb.AppendLine($"PostDown = {server.PostDown}");

        foreach (var client in clients.Where(c => c.Enabled))
        {
            sb.AppendLine();
            sb.AppendLine("[Peer]");
            sb.AppendLine($"# Name = {client.Name}");
            sb.AppendLine($"PublicKey = {client.PublicKey}");
            if (!string.IsNullOrWhiteSpace(client.PresharedKey))
                sb.AppendLine($"PresharedKey = {client.PresharedKey}");
            var allowedIps = client.AllocatedIPs.Concat(client.ExtraAllowedIPs);
            sb.AppendLine($"AllowedIPs = {string.Join(", ", allowedIps)}");
            if (!string.IsNullOrWhiteSpace(client.Endpoint))
                sb.AppendLine($"Endpoint = {client.Endpoint}");
        }

        return sb.ToString();
    }

    public static string GenerateClientConfig(
        WireGuardClient client,
        ServerConfig server,
        GlobalSetting settings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Interface]");
        sb.AppendLine($"PrivateKey = {client.PrivateKey}");
        sb.AppendLine($"Address = {string.Join(", ", client.AllocatedIPs)}");
        if (client.UseServerDns && settings.DnsServers.Count > 0)
            sb.AppendLine($"DNS = {string.Join(", ", settings.DnsServers)}");
        sb.AppendLine($"MTU = {settings.Mtu}");
        sb.AppendLine();
        sb.AppendLine("[Peer]");
        sb.AppendLine($"PublicKey = {server.PublicKey}");
        if (!string.IsNullOrWhiteSpace(client.PresharedKey))
            sb.AppendLine($"PresharedKey = {client.PresharedKey}");
        sb.AppendLine($"Endpoint = {settings.EndpointAddress}:{server.ListenPort}");
        var allowedIps = client.AllowedIPs.Concat(client.ExtraAllowedIPs);
        sb.AppendLine($"AllowedIPs = {string.Join(", ", allowedIps)}");
        sb.AppendLine($"PersistentKeepalive = {settings.PersistentKeepalive}");

        return sb.ToString();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj --filter "WireGuardConfigGeneratorTests"
```

Expected: `Passed! - Failed: 0, Passed: 3`

- [ ] **Step 5: Commit**

```bash
git add src/WireGuardUI.Infrastructure/WireGuard/WireGuardConfigGenerator.cs tests/WireGuardUI.Tests/Core/WireGuardConfigGeneratorTests.cs
git commit -m "feat(infra): add WireGuard config generator with tests"
```

---

## Task 9: Linux WireGuard Service

**Files:**
- Create: `src/WireGuardUI.Infrastructure/WireGuard/LinuxWireGuardService.cs`

- [ ] **Step 1: Implement LinuxWireGuardService**

```csharp
// src/WireGuardUI.Infrastructure/WireGuard/LinuxWireGuardService.cs
using System.Diagnostics;
using System.Text.RegularExpressions;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
namespace WireGuardUI.Infrastructure.WireGuard;

public class LinuxWireGuardService(
    IClientRepository clientRepo,
    IServerConfigRepository serverRepo,
    IGlobalSettingRepository settingRepo) : IWireGuardService
{
    public async Task<string> GenerateConfigAsync()
    {
        var server = await serverRepo.GetAsync();
        var settings = await settingRepo.GetAsync();
        var clients = await clientRepo.GetAllAsync();
        return WireGuardConfigGenerator.GenerateServerConfig(server, settings, clients);
    }

    public async Task WriteConfigAsync(string configContent)
    {
        var settings = await settingRepo.GetAsync();
        var path = string.IsNullOrWhiteSpace(settings.ConfigFilePath)
            ? "/etc/wireguard/wg0.conf"
            : settings.ConfigFilePath;

        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, configContent);
        File.Move(tmpPath, path, overwrite: true);
    }

    public async Task<ApplyResult> SyncConfAsync()
    {
        var settings = await settingRepo.GetAsync();
        var configPath = string.IsNullOrWhiteSpace(settings.ConfigFilePath)
            ? "/etc/wireguard/wg0.conf"
            : settings.ConfigFilePath;
        var tunnelName = Path.GetFileNameWithoutExtension(configPath); // e.g. "wg0"

        // Try wg syncconf first
        var syncResult = await RunAsync("wg", $"syncconf {tunnelName} {configPath}");
        if (syncResult.ExitCode == 0)
            return ApplyResult.Ok(syncResult.Output);

        // Fallback: wg-quick down + up
        await RunAsync("wg-quick", $"down {tunnelName}");
        var upResult = await RunAsync("wg-quick", $"up {tunnelName}");
        return upResult.ExitCode == 0
            ? ApplyResult.Ok(upResult.Output)
            : ApplyResult.Fail($"wg syncconf and wg-quick both failed: {upResult.Output}", upResult.Output);
    }

    public async Task<Result<WireGuardStatus>> GetStatusAsync()
    {
        var result = await RunAsync("wg", "show all dump");
        if (result.ExitCode != 0)
            return Result<WireGuardStatus>.Failure(result.Output);

        return Result<WireGuardStatus>.Success(ParseDump(result.Output));
    }

    private static WireGuardStatus ParseDump(string dump)
    {
        var lines = dump.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var peers = new List<WireGuardPeer>();
        string interfaceName = "wg0", publicKey = "", listenPort = "0";

        foreach (var line in lines)
        {
            var parts = line.Split('\t');
            if (parts.Length >= 5 && parts[0] == "wg0")
            {
                if (parts[1] != "(none)" && parts[3] == "(none)")
                {
                    // Interface line: interface, private-key, public-key, listen-port, fwmark
                    interfaceName = parts[0];
                    publicKey = parts[2];
                    listenPort = parts[3];
                }
                else if (parts.Length >= 8)
                {
                    // Peer line: interface, public-key, preshared-key, endpoint, allowed-ips, latest-handshake, rx, tx
                    peers.Add(new WireGuardPeer(
                        PublicKey: parts[1],
                        Endpoint: parts[3] == "(none)" ? null : parts[3],
                        AllowedIPs: parts[4].Split(',').Select(s => s.Trim()).ToList(),
                        LastHandshake: parts[5] == "0" ? null : DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts[5])).ToString("u"),
                        RxBytes: long.TryParse(parts[6], out var rx) ? rx : 0,
                        TxBytes: long.TryParse(parts[7], out var tx) ? tx : 0));
                }
            }
        }

        return new WireGuardStatus(interfaceName, publicKey, int.TryParse(listenPort, out var port) ? port : 0, peers);
    }

    private static async Task<(int ExitCode, string Output)> RunAsync(string fileName, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, string.IsNullOrEmpty(error) ? output : error);
    }
}
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj
git add src/WireGuardUI.Infrastructure/WireGuard/LinuxWireGuardService.cs
git commit -m "feat(infra): add Linux WireGuard service"
```

---

## Task 10: Windows WireGuard Service

**Files:**
- Create: `src/WireGuardUI.Infrastructure/WireGuard/WindowsWireGuardService.cs`

- [ ] **Step 1: Implement WindowsWireGuardService**

```csharp
// src/WireGuardUI.Infrastructure/WireGuard/WindowsWireGuardService.cs
using System.Diagnostics;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
namespace WireGuardUI.Infrastructure.WireGuard;

public class WindowsWireGuardService(
    IClientRepository clientRepo,
    IServerConfigRepository serverRepo,
    IGlobalSettingRepository settingRepo,
    string wgExePath = @"C:\Program Files\WireGuard\wg.exe") : IWireGuardService
{
    public async Task<string> GenerateConfigAsync()
    {
        var server = await serverRepo.GetAsync();
        var settings = await settingRepo.GetAsync();
        var clients = await clientRepo.GetAllAsync();
        return WireGuardConfigGenerator.GenerateServerConfig(server, settings, clients);
    }

    public async Task WriteConfigAsync(string configContent)
    {
        var settings = await settingRepo.GetAsync();
        var path = string.IsNullOrWhiteSpace(settings.ConfigFilePath)
            ? @"C:\Program Files\WireGuard\wg0.conf"
            : settings.ConfigFilePath;

        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, configContent);
        File.Move(tmpPath, path, overwrite: true);
    }

    public async Task<ApplyResult> SyncConfAsync()
    {
        var settings = await settingRepo.GetAsync();
        var configPath = string.IsNullOrWhiteSpace(settings.ConfigFilePath)
            ? @"C:\Program Files\WireGuard\wg0.conf"
            : settings.ConfigFilePath;
        var tunnelName = Path.GetFileNameWithoutExtension(configPath); // e.g. "wg0"

        // Try wg syncconf
        var syncResult = await RunAsync(wgExePath, $"syncconf {tunnelName} \"{configPath}\"");
        if (syncResult.ExitCode == 0)
            return ApplyResult.Ok(syncResult.Output);

        // Fallback: restart Windows Service
        await RunAsync("net", $"stop \"WireGuardTunnel${tunnelName}\"");
        var startResult = await RunAsync("net", $"start \"WireGuardTunnel${tunnelName}\"");
        return startResult.ExitCode == 0
            ? ApplyResult.Ok(startResult.Output)
            : ApplyResult.Fail($"wg syncconf and service restart both failed: {startResult.Output}", startResult.Output);
    }

    public async Task<Result<WireGuardStatus>> GetStatusAsync()
    {
        var result = await RunAsync(wgExePath, "show all dump");
        if (result.ExitCode != 0)
            return Result<WireGuardStatus>.Failure(result.Output);

        return Result<WireGuardStatus>.Success(ParseDump(result.Output));
    }

    private static WireGuardStatus ParseDump(string dump)
    {
        var lines = dump.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var peers = new List<WireGuardPeer>();
        string interfaceName = "wg0", publicKey = "", listenPort = "0";

        foreach (var line in lines)
        {
            var parts = line.Split('\t');
            if (parts.Length >= 5)
            {
                if (parts.Length == 5)
                {
                    interfaceName = parts[0];
                    publicKey = parts[2];
                    listenPort = parts[3];
                }
                else if (parts.Length >= 8)
                {
                    peers.Add(new WireGuardPeer(
                        PublicKey: parts[1],
                        Endpoint: parts[3] == "(none)" ? null : parts[3],
                        AllowedIPs: parts[4].Split(',').Select(s => s.Trim()).ToList(),
                        LastHandshake: parts[5] == "0" ? null : DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts[5])).ToString("u"),
                        RxBytes: long.TryParse(parts[6], out var rx) ? rx : 0,
                        TxBytes: long.TryParse(parts[7], out var tx) ? tx : 0));
                }
            }
        }

        return new WireGuardStatus(interfaceName, publicKey, int.TryParse(listenPort, out var port) ? port : 0, peers);
    }

    private static async Task<(int ExitCode, string Output)> RunAsync(string fileName, string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, string.IsNullOrEmpty(error) ? output : error);
    }
}
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj
git add src/WireGuardUI.Infrastructure/WireGuard/WindowsWireGuardService.cs
git commit -m "feat(infra): add Windows WireGuard service"
```

---

## Task 11: SMTP Email Service

**Files:**
- Create: `src/WireGuardUI.Infrastructure/Email/SmtpEmailService.cs`

- [ ] **Step 1: Implement SmtpEmailService**

```csharp
// src/WireGuardUI.Infrastructure/Email/SmtpEmailService.cs
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Infrastructure.WireGuard;
namespace WireGuardUI.Infrastructure.Email;

public class SmtpEmailService(IEmailSettingRepository emailSettingRepo) : IEmailService
{
    public async Task<Result> SendClientConfigAsync(
        WireGuardClient client,
        ServerConfig server,
        GlobalSetting settings,
        string toEmail)
    {
        try
        {
            var emailSetting = await emailSettingRepo.GetAsync();
            var configContent = WireGuardConfigGenerator.GenerateClientConfig(client, server, settings);
            var fileName = $"{client.Name.Replace(" ", "_")}.conf";

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(emailSetting.FromAddress));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = $"WireGuard Config — {client.Name}";

            var builder = new BodyBuilder
            {
                TextBody = $"Please find your WireGuard configuration for '{client.Name}' attached."
            };
            builder.Attachments.Add(fileName, System.Text.Encoding.UTF8.GetBytes(configContent), ContentType.Parse("text/plain"));
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            var secureSocketOptions = emailSetting.Encryption switch
            {
                "SSL" => SecureSocketOptions.SslOnConnect,
                "TLS" => SecureSocketOptions.SslOnConnect,
                "STARTTLS" => SecureSocketOptions.StartTls,
                _ => SecureSocketOptions.None
            };
            await smtp.ConnectAsync(emailSetting.SmtpHost, emailSetting.SmtpPort, secureSocketOptions);
            if (!string.IsNullOrWhiteSpace(emailSetting.SmtpUsername))
                await smtp.AuthenticateAsync(emailSetting.SmtpUsername, emailSetting.SmtpPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> TestConnectionAsync(EmailSetting emailSetting)
    {
        try
        {
            using var smtp = new SmtpClient();
            var secureSocketOptions = emailSetting.Encryption switch
            {
                "SSL" => SecureSocketOptions.SslOnConnect,
                "TLS" => SecureSocketOptions.SslOnConnect,
                "STARTTLS" => SecureSocketOptions.StartTls,
                _ => SecureSocketOptions.None
            };
            await smtp.ConnectAsync(emailSetting.SmtpHost, emailSetting.SmtpPort, secureSocketOptions);
            if (!string.IsNullOrWhiteSpace(emailSetting.SmtpUsername))
                await smtp.AuthenticateAsync(emailSetting.SmtpUsername, emailSetting.SmtpPassword);
            await smtp.DisconnectAsync(true);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build src/WireGuardUI.Infrastructure/WireGuardUI.Infrastructure.csproj
git add src/WireGuardUI.Infrastructure/Email/
git commit -m "feat(infra): add MailKit SMTP email service"
```

---

## Task 12: Settings + Program.cs + DI Wiring + DB Seeding

**Files:**
- Create: `src/WireGuardUI.Web/WireGuardUiSettings.cs`
- Modify: `src/WireGuardUI.Web/appsettings.json`
- Modify: `src/WireGuardUI.Web/Program.cs`

- [ ] **Step 1: Create settings class**

```csharp
// src/WireGuardUI.Web/WireGuardUiSettings.cs
using System.Runtime.InteropServices;
namespace WireGuardUI.Web;

public class WireGuardUiSettings
{
    public string DbPath { get; set; } = string.Empty;
    public int SessionExpiryHours { get; set; } = 8;
    public string DefaultUsername { get; set; } = "admin";
    public string DefaultPassword { get; set; } = "admin";
    public string WireGuardTunnelName { get; set; } = "wg0";
    public string WireGuardConfigPath { get; set; } = string.Empty;
    public string WireGuardExePath { get; set; } = string.Empty;

    public string ResolvedDbPath => string.IsNullOrWhiteSpace(DbPath)
        ? RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WireGuardUI", "wireguard-ui.db")
            : "/var/lib/wireguard-ui/wireguard-ui.db"
        : DbPath;

    public string ResolvedConfigPath => string.IsNullOrWhiteSpace(WireGuardConfigPath)
        ? RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\Program Files\WireGuard\wg0.conf"
            : "/etc/wireguard/wg0.conf"
        : WireGuardConfigPath;

    public string ResolvedWgExePath => string.IsNullOrWhiteSpace(WireGuardExePath)
        ? RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\Program Files\WireGuard\wg.exe"
            : "wg"
        : WireGuardExePath;
}
```

- [ ] **Step 2: Update appsettings.json**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "WireGuardUI": {
    "DbPath": "",
    "SessionExpiryHours": 8,
    "DefaultUsername": "admin",
    "DefaultPassword": "admin",
    "WireGuardTunnelName": "wg0",
    "WireGuardConfigPath": "",
    "WireGuardExePath": ""
  }
}
```

- [ ] **Step 3: Write Program.cs**

```csharp
// src/WireGuardUI.Web/Program.cs
using System.Runtime.InteropServices;
using System.Security.Claims;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using WireGuardUI.Core.Interfaces;
using WireGuardUI.Core.Models;
using WireGuardUI.Core.Services;
using WireGuardUI.Infrastructure.Crypto;
using WireGuardUI.Infrastructure.Data;
using WireGuardUI.Infrastructure.Email;
using WireGuardUI.Infrastructure.Repositories;
using WireGuardUI.Infrastructure.WireGuard;
using WireGuardUI.Web;
using WireGuardUI.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Settings
var settings = builder.Configuration.GetSection("WireGuardUI").Get<WireGuardUiSettings>()
    ?? new WireGuardUiSettings();
builder.Services.AddSingleton(settings);

// Database
var dbDir = Path.GetDirectoryName(settings.ResolvedDbPath);
if (!string.IsNullOrEmpty(dbDir)) Directory.CreateDirectory(dbDir);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"DataSource={settings.ResolvedDbPath}"));

// Repositories
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IServerConfigRepository, ServerConfigRepository>();
builder.Services.AddScoped<IGlobalSettingRepository, GlobalSettingRepository>();
builder.Services.AddScoped<IEmailSettingRepository, EmailSettingRepository>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();

// Services
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddSingleton<IKeyPairService, BouncyCastleKeyPairService>();
builder.Services.AddSingleton<IIpAllocationService, IpAllocationService>();

// Platform-specific WireGuard service
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    builder.Services.AddScoped<IWireGuardService>(sp => new WindowsWireGuardService(
        sp.GetRequiredService<IClientRepository>(),
        sp.GetRequiredService<IServerConfigRepository>(),
        sp.GetRequiredService<IGlobalSettingRepository>(),
        settings.ResolvedWgExePath));
else
    builder.Services.AddScoped<IWireGuardService, LinuxWireGuardService>();

// Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/account/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(settings.SessionExpiryHours);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Blazor + MudBlazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

var app = builder.Build();

// Migrate and seed DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Seed admin user if none exists
    if (!db.AdminUsers.Any())
    {
        db.AdminUsers.Add(new AdminUser
        {
            Username = settings.DefaultUsername,
            PasswordHash = BCrypt.HashPassword(settings.DefaultPassword)
        });
        db.SaveChanges();
    }

    // Seed global settings if none exists
    if (!db.GlobalSettings.Any())
    {
        db.GlobalSettings.Add(new GlobalSetting
        {
            ConfigFilePath = settings.ResolvedConfigPath
        });
        db.SaveChanges();
    }
}

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Auth endpoints (cookies must be written by non-Blazor endpoints)
app.MapPost("/account/login", async (HttpContext ctx, IAdminUserRepository userRepo,
    string? returnUrl) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    var user = await userRepo.GetAsync();
    if (user.Username != username || !BCrypt.Verify(password, user.PasswordHash))
        return Results.Redirect("/login?error=1");

    var claims = new[] { new Claim(ClaimTypes.Name, user.Username) };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
});

app.MapPost("/account/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

- [ ] **Step 4: Build and commit**

```bash
dotnet build src/WireGuardUI.Web/WireGuardUI.Web.csproj
git add src/WireGuardUI.Web/Program.cs src/WireGuardUI.Web/appsettings.json src/WireGuardUI.Web/WireGuardUiSettings.cs
git commit -m "feat(web): add DI wiring, auth, DB seeding in Program.cs"
```

---

## Task 13: App Shell + Layout

**Files:**
- Modify: `src/WireGuardUI.Web/Components/App.razor`
- Create: `src/WireGuardUI.Web/Components/Routes.razor`
- Create: `src/WireGuardUI.Web/Components/Shared/RedirectToLogin.razor`
- Modify: `src/WireGuardUI.Web/Components/Layout/MainLayout.razor`
- Modify: `src/WireGuardUI.Web/Components/Layout/NavMenu.razor`
- Modify: `src/WireGuardUI.Web/Components/Layout/MainLayout.razor.css`

- [ ] **Step 1: Update App.razor**

```razor
@* src/WireGuardUI.Web/Components/App.razor *@
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link rel="stylesheet" href="app.css" />
    <HeadOutlet />
</head>
<body>
    <Routes />
    <script src="_framework/blazor.web.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>
</html>
```

- [ ] **Step 2: Create Routes.razor**

```razor
@* src/WireGuardUI.Web/Components/Routes.razor *@
<Router AppAssembly="typeof(App).Assembly">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)">
            <NotAuthorized>
                <RedirectToLogin />
            </NotAuthorized>
            <Authorizing>
                <MudProgressCircular Indeterminate="true" />
            </Authorizing>
        </AuthorizeRouteView>
        <FocusOnNavigate RouteData="routeData" Selector="h1" />
    </Found>
</Router>

@using WireGuardUI.Web.Components.Layout
@using WireGuardUI.Web.Components.Shared
@using MudBlazor
```

- [ ] **Step 3: Create RedirectToLogin.razor**

```razor
@* src/WireGuardUI.Web/Components/Shared/RedirectToLogin.razor *@
@inject NavigationManager Nav
@code {
    protected override void OnInitialized()
    {
        var returnUrl = Uri.EscapeDataString(Nav.Uri);
        Nav.NavigateTo($"/login?returnUrl={returnUrl}", forceLoad: true);
    }
}
```

- [ ] **Step 4: Update MainLayout.razor**

```razor
@* src/WireGuardUI.Web/Components/Layout/MainLayout.razor *@
@inherits LayoutComponentBase
@inject NavigationManager Nav
@inject AuthenticationStateProvider AuthStateProvider

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="1" Color="Color.Dark">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit"
                       Edge="Edge.Start" OnClick="@ToggleDrawer" />
        <MudText Typo="Typo.h6" Class="ml-2">WireGuard UI</MudText>
        <MudSpacer />
        <AuthorizeView>
            <Authorized>
                <MudText Typo="Typo.body2" Class="mr-2">@context.User.Identity?.Name</MudText>
                <form method="post" action="/account/logout">
                    <AntiforgeryToken />
                    <MudIconButton Icon="@Icons.Material.Filled.Logout" Color="Color.Inherit"
                                   ButtonType="ButtonType.Submit" Title="Logout" />
                </form>
            </Authorized>
        </AuthorizeView>
    </MudAppBar>

    <MudDrawer @bind-Open="_drawerOpen" Elevation="2" Variant="DrawerVariant.Mini" ClipMode="DrawerClipMode.Always">
        <NavMenu />
    </MudDrawer>

    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="pa-4">
            <ErrorBoundary>
                @Body
            </ErrorBoundary>
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private bool _drawerOpen = true;
    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;
}
```

- [ ] **Step 5: Update NavMenu.razor**

```razor
@* src/WireGuardUI.Web/Components/Layout/NavMenu.razor *@
@inject NavigationManager Nav

<MudNavMenu>
    <MudNavLink Href="/" Match="NavLinkMatch.All"
                Icon="@Icons.Material.Filled.People">Clients</MudNavLink>
    <MudNavLink Href="/server"
                Icon="@Icons.Material.Filled.Dns">Server</MudNavLink>
    <MudNavLink Href="/global-settings"
                Icon="@Icons.Material.Filled.Settings">Global Settings</MudNavLink>
    <MudNavLink Href="/email-settings"
                Icon="@Icons.Material.Filled.Email">Email Settings</MudNavLink>
    <MudNavLink Href="/status"
                Icon="@Icons.Material.Filled.MonitorHeart">Status</MudNavLink>
    <MudDivider Class="my-2" />
    <MudNavLink Href="/profile"
                Icon="@Icons.Material.Filled.AccountCircle">Profile</MudNavLink>
</MudNavMenu>
```

- [ ] **Step 6: Build and commit**

```bash
dotnet build src/WireGuardUI.Web/WireGuardUI.Web.csproj
git add src/WireGuardUI.Web/Components/
git commit -m "feat(web): add app shell, layout, and navigation"
```

---

## Task 14: Login Page

**Files:**
- Create: `src/WireGuardUI.Web/Components/Pages/Login.razor`

- [ ] **Step 1: Create Login.razor**

```razor
@* src/WireGuardUI.Web/Components/Pages/Login.razor *@
@page "/login"
@layout MudBlazor.MudLayout
@using WireGuardUI.Web.Components.Layout

<PageTitle>Login — WireGuard UI</PageTitle>

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudContainer MaxWidth="MaxWidth.Small" Class="d-flex align-center justify-center" Style="min-height:100vh">
    <MudPaper Elevation="4" Class="pa-8" Style="width:100%;max-width:400px">
        <MudText Typo="Typo.h5" Align="Align.Center" Class="mb-6">
            <MudIcon Icon="@Icons.Material.Filled.Lock" Class="mr-2" />
            WireGuard UI
        </MudText>

        @if (Request.Query.ContainsKey("error"))
        {
            <MudAlert Severity="Severity.Error" Class="mb-4">
                Invalid username or password.
            </MudAlert>
        }

        <form method="post" action="/account/login">
            <AntiforgeryToken />
            <input type="hidden" name="returnUrl" value="@(Request.Query["returnUrl"])" />

            <MudTextField name="username" Label="Username" Variant="Variant.Outlined"
                          FullWidth="true" Class="mb-4" autocomplete="username" />
            <MudTextField name="password" Label="Password" Variant="Variant.Outlined"
                          InputType="InputType.Password" FullWidth="true" Class="mb-6"
                          autocomplete="current-password" />
            <MudButton ButtonType="ButtonType.Submit" Variant="Variant.Filled"
                       Color="Color.Primary" FullWidth="true" Size="Size.Large">
                Sign In
            </MudButton>
        </form>
    </MudPaper>
</MudContainer>

@inject IHttpContextAccessor HttpContextAccessor
@code {
    private HttpRequest Request => HttpContextAccessor.HttpContext!.Request;
}
```

Add `IHttpContextAccessor` to DI in `Program.cs` (add after `builder.Services.AddMudServices()`):

```csharp
builder.Services.AddHttpContextAccessor();
```

- [ ] **Step 2: Build, run and verify login page renders**

```bash
dotnet run --project src/WireGuardUI.Web
```

Open http://localhost:5000/login — should show login form. Try default credentials `admin/admin` — should redirect to `/`.

- [ ] **Step 3: Commit**

```bash
git add src/WireGuardUI.Web/Components/Pages/Login.razor
git commit -m "feat(web): add login page"
```

---

## Task 15: Shared Components

**Files:**
- Create: `src/WireGuardUI.Web/Components/Shared/ConfirmDialog.razor`
- Create: `src/WireGuardUI.Web/Components/Shared/QrCodeDisplay.razor`

- [ ] **Step 1: Create ConfirmDialog.razor**

```razor
@* src/WireGuardUI.Web/Components/Shared/ConfirmDialog.razor *@
@inject MudBlazor.IDialogService DialogService

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">@Title</MudText>
    </TitleContent>
    <DialogContent>
        <MudText>@Message</MudText>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Error" Variant="Variant.Filled" OnClick="Confirm">@ConfirmText</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public string Title { get; set; } = "Confirm";
    [Parameter] public string Message { get; set; } = "Are you sure?";
    [Parameter] public string ConfirmText { get; set; } = "Confirm";

    private void Confirm() => MudDialog.Close(DialogResult.Ok(true));
    private void Cancel() => MudDialog.Cancel();
}
```

- [ ] **Step 2: Create QrCodeDisplay.razor**

```razor
@* src/WireGuardUI.Web/Components/Shared/QrCodeDisplay.razor *@
@using QRCoder

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">QR Code — @ClientName</MudText>
    </TitleContent>
    <DialogContent>
        <div class="d-flex justify-center pa-4">
            @if (_qrDataUri is not null)
            {
                <img src="@_qrDataUri" alt="QR Code" style="width:256px;height:256px" />
            }
        </div>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Close">Close</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public string ClientName { get; set; } = string.Empty;
    [Parameter] public string ConfigContent { get; set; } = string.Empty;

    private string? _qrDataUri;

    protected override void OnInitialized()
    {
        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(ConfigContent, QRCodeGenerator.ECCLevel.Q);
        using var code = new PngByteQRCode(data);
        var bytes = code.GetGraphic(10);
        _qrDataUri = $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }

    private void Close() => MudDialog.Close();
}
```

- [ ] **Step 3: Build and commit**

```bash
dotnet build src/WireGuardUI.Web/WireGuardUI.Web.csproj
git add src/WireGuardUI.Web/Components/Shared/
git commit -m "feat(web): add ConfirmDialog and QrCodeDisplay shared components"
```

---

## Task 16: Clients Page + ClientDialog

**Files:**
- Create: `src/WireGuardUI.Web/Components/Pages/Clients.razor`
- Create: `src/WireGuardUI.Web/Components/Pages/ClientDialog.razor`

- [ ] **Step 1: Create ClientDialog.razor**

```razor
@* src/WireGuardUI.Web/Components/Pages/ClientDialog.razor *@
@inject IKeyPairService KeyPairService
@inject IIpAllocationService IpAllocationService

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">@(IsEdit ? "Edit Client" : "New Client")</MudText>
    </TitleContent>
    <DialogContent>
        <MudTextField @bind-Value="_client.Name" Label="Name" Variant="Variant.Outlined"
                      FullWidth="true" Class="mb-3" Required="true" />
        <MudTextField @bind-Value="_client.Email" Label="Email (optional)" Variant="Variant.Outlined"
                      FullWidth="true" Class="mb-3" InputType="InputType.Email" />
        <MudTextField @bind-Value="_allocatedIpsText" Label="Allocated IPs (one per line)"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3"
                      Lines="2" HelperText="e.g. 10.252.1.2/32" />
        <MudTextField @bind-Value="_allowedIpsText" Label="Allowed IPs (one per line)"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3"
                      Lines="2" HelperText="e.g. 0.0.0.0/0" />
        <MudTextField @bind-Value="_extraAllowedIpsText" Label="Extra Allowed IPs (optional)"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3" Lines="2" />
        <MudTextField @bind-Value="_client.Endpoint" Label="Endpoint override (optional)"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3" />
        <MudSwitch @bind-Value="_client.UseServerDns" Label="Use server DNS" Color="Color.Primary" />
        <MudSwitch @bind-Value="_client.Enabled" Label="Enable after creation" Color="Color.Primary" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary" Variant="Variant.Filled" OnClick="Submit"
                   Disabled="string.IsNullOrWhiteSpace(_client.Name)">
            @(IsEdit ? "Save" : "Create")
        </MudButton>
    </DialogActions>
</MudDialog>

@using WireGuardUI.Core.Interfaces
@using WireGuardUI.Core.Models
@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public WireGuardClient? ExistingClient { get; set; }
    [Parameter] public string ServerSubnet { get; set; } = "10.252.1.0/24";
    [Parameter] public List<string> ExistingAllocatedIps { get; set; } = [];

    private WireGuardClient _client = new();
    private string _allocatedIpsText = string.Empty;
    private string _allowedIpsText = "0.0.0.0/0";
    private string _extraAllowedIpsText = string.Empty;
    private bool IsEdit => ExistingClient is not null;

    protected override void OnInitialized()
    {
        if (ExistingClient is not null)
        {
            _client = new WireGuardClient
            {
                Id = ExistingClient.Id,
                Name = ExistingClient.Name,
                Email = ExistingClient.Email,
                PrivateKey = ExistingClient.PrivateKey,
                PublicKey = ExistingClient.PublicKey,
                PresharedKey = ExistingClient.PresharedKey,
                AllocatedIPs = [..ExistingClient.AllocatedIPs],
                AllowedIPs = [..ExistingClient.AllowedIPs],
                ExtraAllowedIPs = [..ExistingClient.ExtraAllowedIPs],
                Endpoint = ExistingClient.Endpoint,
                UseServerDns = ExistingClient.UseServerDns,
                Enabled = ExistingClient.Enabled,
                CreatedAt = ExistingClient.CreatedAt
            };
            _allocatedIpsText = string.Join("\n", _client.AllocatedIPs);
            _allowedIpsText = string.Join("\n", _client.AllowedIPs);
            _extraAllowedIpsText = string.Join("\n", _client.ExtraAllowedIPs);
        }
        else
        {
            var (priv, pub) = KeyPairService.GenerateKeyPair();
            _client.PrivateKey = priv;
            _client.PublicKey = pub;
            _client.PresharedKey = KeyPairService.GeneratePresharedKey();
            try
            {
                var nextIp = IpAllocationService.AllocateNextIp(ServerSubnet, ExistingAllocatedIps);
                _allocatedIpsText = nextIp;
            }
            catch { _allocatedIpsText = string.Empty; }
        }
    }

    private void Submit()
    {
        _client.AllocatedIPs = _allocatedIpsText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        _client.AllowedIPs = _allowedIpsText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        _client.ExtraAllowedIPs = _extraAllowedIpsText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        MudDialog.Close(DialogResult.Ok(_client));
    }

    private void Cancel() => MudDialog.Cancel();
}
```

- [ ] **Step 2: Create Clients.razor**

```razor
@* src/WireGuardUI.Web/Components/Pages/Clients.razor *@
@page "/"
@rendermode InteractiveServer
@attribute [Authorize]

<PageTitle>Clients — WireGuard UI</PageTitle>

<MudText Typo="Typo.h5" Class="mb-4">Clients</MudText>

<MudStack Row="true" Class="mb-4" AlignItems="AlignItems.Center">
    <MudTextField @bind-Value="_search" Label="Search" Adornment="Adornment.Start"
                  AdornmentIcon="@Icons.Material.Filled.Search" Variant="Variant.Outlined"
                  Clearable="true" DebounceInterval="300" Style="max-width:300px" />
    <MudSpacer />
    <MudButton Variant="Variant.Filled" Color="Color.Primary"
               StartIcon="@Icons.Material.Filled.Add" OnClick="OpenAddDialog">
        New Client
    </MudButton>
    <MudButton Variant="Variant.Outlined" Color="Color.Secondary"
               StartIcon="@Icons.Material.Filled.Sync" OnClick="ApplyConfig"
               Disabled="_applying">
        @(_applying ? "Applying…" : "Apply Config")
    </MudButton>
</MudStack>

@if (_applyResult is not null)
{
    <MudAlert Severity="@(_applyResult.Success ? Severity.Success : Severity.Error)" Class="mb-4" ShowCloseIcon="true"
              CloseIconClicked="() => _applyResult = null">
        @(_applyResult.Success ? "Configuration applied successfully." : _applyResult.ErrorMessage)
    </MudAlert>
}

<MudTable Items="_filteredClients" Dense="true" Hover="true" Striped="true" Loading="_loading">
    <HeaderContent>
        <MudTh>Name</MudTh>
        <MudTh>Email</MudTh>
        <MudTh>Allocated IP</MudTh>
        <MudTh>Enabled</MudTh>
        <MudTh>Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd>@context.Name</MudTd>
        <MudTd>@context.Email</MudTd>
        <MudTd>@string.Join(", ", context.AllocatedIPs)</MudTd>
        <MudTd>
            <MudSwitch Value="context.Enabled" ValueChanged="(bool v) => ToggleEnabled(context, v)"
                       Color="Color.Success" />
        </MudTd>
        <MudTd>
            <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small"
                           OnClick="() => OpenEditDialog(context)" Title="Edit" />
            <MudIconButton Icon="@Icons.Material.Filled.QrCode" Size="Size.Small"
                           OnClick="() => OpenQrDialog(context)" Title="QR Code" />
            <MudIconButton Icon="@Icons.Material.Filled.Download" Size="Size.Small"
                           OnClick="() => DownloadConfig(context)" Title="Download" />
            <MudIconButton Icon="@Icons.Material.Filled.Email" Size="Size.Small"
                           OnClick="() => OpenEmailDialog(context)" Title="Send via email"
                           Disabled="string.IsNullOrEmpty(context.Email)" />
            <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" Color="Color.Error"
                           OnClick="() => DeleteClient(context)" Title="Delete" />
        </MudTd>
    </RowTemplate>
    <NoRecordsContent>
        <MudText>No clients found. Click "New Client" to add one.</MudText>
    </NoRecordsContent>
</MudTable>

@using WireGuardUI.Core.Interfaces
@using WireGuardUI.Core.Models
@using WireGuardUI.Infrastructure.WireGuard
@using WireGuardUI.Web.Components.Shared
@inject IClientRepository ClientRepo
@inject IServerConfigRepository ServerRepo
@inject IGlobalSettingRepository SettingRepo
@inject IWireGuardService WireGuardService
@inject IEmailService EmailService
@inject IDialogService DialogService
@inject ISnackbar Snackbar
@inject NavigationManager Nav

@code {
    private List<WireGuardClient> _clients = [];
    private string _search = string.Empty;
    private bool _loading = true;
    private bool _applying = false;
    private ApplyResult? _applyResult;

    private IEnumerable<WireGuardClient> _filteredClients => string.IsNullOrWhiteSpace(_search)
        ? _clients
        : _clients.Where(c =>
            c.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
            (c.Email?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false));

    protected override async Task OnInitializedAsync()
    {
        _clients = await ClientRepo.GetAllAsync();
        _loading = false;
    }

    private async Task OpenAddDialog()
    {
        var server = await ServerRepo.GetAsync();
        var existingIps = _clients.SelectMany(c => c.AllocatedIPs).ToList();
        var subnet = server.Addresses.FirstOrDefault() ?? "10.252.1.0/24";

        var parameters = new DialogParameters<ClientDialog>
        {
            { x => x.ServerSubnet, subnet },
            { x => x.ExistingAllocatedIps, existingIps }
        };
        var dialog = await DialogService.ShowAsync<ClientDialog>("New Client", parameters);
        var result = await dialog.Result;
        if (!result!.Canceled && result.Data is WireGuardClient newClient)
        {
            await ClientRepo.AddAsync(newClient);
            _clients = await ClientRepo.GetAllAsync();
            Snackbar.Add($"Client '{newClient.Name}' created.", Severity.Success);
        }
    }

    private async Task OpenEditDialog(WireGuardClient client)
    {
        var server = await ServerRepo.GetAsync();
        var existingIps = _clients.Where(c => c.Id != client.Id)
            .SelectMany(c => c.AllocatedIPs).ToList();

        var parameters = new DialogParameters<ClientDialog>
        {
            { x => x.ExistingClient, client },
            { x => x.ServerSubnet, server.Addresses.FirstOrDefault() ?? "10.252.1.0/24" },
            { x => x.ExistingAllocatedIps, existingIps }
        };
        var dialog = await DialogService.ShowAsync<ClientDialog>("Edit Client", parameters);
        var result = await dialog.Result;
        if (!result!.Canceled && result.Data is WireGuardClient updated)
        {
            await ClientRepo.UpdateAsync(updated);
            _clients = await ClientRepo.GetAllAsync();
            Snackbar.Add($"Client '{updated.Name}' updated.", Severity.Success);
        }
    }

    private async Task ToggleEnabled(WireGuardClient client, bool enabled)
    {
        client.Enabled = enabled;
        await ClientRepo.UpdateAsync(client);
    }

    private async Task DeleteClient(WireGuardClient client)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.Title, "Delete Client" },
            { x => x.Message, $"Delete '{client.Name}'? This cannot be undone." },
            { x => x.ConfirmText, "Delete" }
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Delete", parameters);
        var result = await dialog.Result;
        if (!result!.Canceled)
        {
            await ClientRepo.DeleteAsync(client.Id);
            _clients = await ClientRepo.GetAllAsync();
            Snackbar.Add($"Client '{client.Name}' deleted.", Severity.Info);
        }
    }

    private async Task OpenQrDialog(WireGuardClient client)
    {
        var server = await ServerRepo.GetAsync();
        var settings = await SettingRepo.GetAsync();
        var config = WireGuardConfigGenerator.GenerateClientConfig(client, server, settings);

        var parameters = new DialogParameters<QrCodeDisplay>
        {
            { x => x.ClientName, client.Name },
            { x => x.ConfigContent, config }
        };
        await DialogService.ShowAsync<QrCodeDisplay>("QR Code", parameters);
    }

    private async Task DownloadConfig(WireGuardClient client)
    {
        var server = await ServerRepo.GetAsync();
        var settings = await SettingRepo.GetAsync();
        var config = WireGuardConfigGenerator.GenerateClientConfig(client, server, settings);
        var fileName = $"{client.Name.Replace(" ", "_")}.conf";
        var bytes = System.Text.Encoding.UTF8.GetBytes(config);
        var base64 = Convert.ToBase64String(bytes);
        await Nav.NavigateTo($"javascript:downloadFile('{fileName}','{base64}')", false);
        // Note: add downloadFile JS helper to wwwroot/app.js (see Task 16 Step 3)
    }

    private async Task OpenEmailDialog(WireGuardClient client)
    {
        var parameters = new DialogParameters<EmailDialog>
        {
            { x => x.Client, client }
        };
        await DialogService.ShowAsync<EmailDialog>("Send Config", parameters);
    }

    private async Task ApplyConfig()
    {
        _applying = true;
        _applyResult = null;
        var config = await WireGuardService.GenerateConfigAsync();
        await WireGuardService.WriteConfigAsync(config);
        _applyResult = await WireGuardService.SyncConfAsync();
        _applying = false;
    }
}
```

- [ ] **Step 3: Add JS download helper to wwwroot/app.js**

Create `src/WireGuardUI.Web/wwwroot/app.js`:

```js
// src/WireGuardUI.Web/wwwroot/app.js
function downloadFile(filename, base64) {
    const link = document.createElement('a');
    link.href = 'data:text/plain;base64,' + base64;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}
```

Add to `App.razor` before `</body>`:
```html
<script src="app.js"></script>
```

And update `DownloadConfig` in Clients.razor to use JS interop:

Add to the `@inject` section:
```razor
@inject IJSRuntime JS
```

Replace the `DownloadConfig` method body with:
```csharp
private async Task DownloadConfig(WireGuardClient client)
{
    var server = await ServerRepo.GetAsync();
    var settings = await SettingRepo.GetAsync();
    var config = WireGuardConfigGenerator.GenerateClientConfig(client, server, settings);
    var fileName = $"{client.Name.Replace(" ", "_")}.conf";
    var bytes = System.Text.Encoding.UTF8.GetBytes(config);
    var base64 = Convert.ToBase64String(bytes);
    await JS.InvokeVoidAsync("downloadFile", fileName, base64);
}
```

- [ ] **Step 4: Build and commit**

```bash
dotnet build src/WireGuardUI.Web/WireGuardUI.Web.csproj
git add src/WireGuardUI.Web/Components/Pages/Clients.razor \
        src/WireGuardUI.Web/Components/Pages/ClientDialog.razor \
        src/WireGuardUI.Web/wwwroot/app.js
git commit -m "feat(web): add Clients page with add/edit/delete/QR/download"
```

---

## Task 17: Email Dialog

**Files:**
- Create: `src/WireGuardUI.Web/Components/Pages/EmailDialog.razor`

- [ ] **Step 1: Create EmailDialog.razor**

```razor
@* src/WireGuardUI.Web/Components/Pages/EmailDialog.razor *@
@inject IServerConfigRepository ServerRepo
@inject IGlobalSettingRepository SettingRepo
@inject IEmailService EmailService
@inject ISnackbar Snackbar

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">Send Config — @Client.Name</MudText>
    </TitleContent>
    <DialogContent>
        <MudTextField @bind-Value="_toEmail" Label="Send to email" Variant="Variant.Outlined"
                      FullWidth="true" InputType="InputType.Email"
                      HelperText="Defaults to client's email if set." />
        @if (_sending)
        {
            <MudProgressLinear Indeterminate="true" Class="mt-3" />
        }
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel" Disabled="_sending">Cancel</MudButton>
        <MudButton Color="Color.Primary" Variant="Variant.Filled" OnClick="Send"
                   Disabled="_sending || string.IsNullOrWhiteSpace(_toEmail)">
            Send
        </MudButton>
    </DialogActions>
</MudDialog>

@using WireGuardUI.Core.Interfaces
@using WireGuardUI.Core.Models
@code {
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public WireGuardClient Client { get; set; } = null!;

    private string _toEmail = string.Empty;
    private bool _sending;

    protected override void OnInitialized() => _toEmail = Client.Email ?? string.Empty;

    private async Task Send()
    {
        _sending = true;
        var server = await ServerRepo.GetAsync();
        var settings = await SettingRepo.GetAsync();
        var result = await EmailService.SendClientConfigAsync(Client, server, settings, _toEmail);
        _sending = false;

        if (result.IsSuccess)
        {
            Snackbar.Add($"Config sent to {_toEmail}.", Severity.Success);
            MudDialog.Close();
        }
        else
        {
            Snackbar.Add($"Failed to send: {result.ErrorMessage}", Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build src/WireGuardUI.Web/WireGuardUI.Web.csproj
git add src/WireGuardUI.Web/Components/Pages/EmailDialog.razor
git commit -m "feat(web): add email dialog for sending client config"
```

---

## Task 18: Server Page

**Files:**
- Create: `src/WireGuardUI.Web/Components/Pages/Server.razor`

- [ ] **Step 1: Create Server.razor**

```razor
@* src/WireGuardUI.Web/Components/Pages/Server.razor *@
@page "/server"
@rendermode InteractiveServer
@attribute [Authorize]

<PageTitle>Server — WireGuard UI</PageTitle>
<MudText Typo="Typo.h5" Class="mb-4">Server Configuration</MudText>

@if (_config is null)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    <MudPaper Class="pa-6" Elevation="1">
        <MudText Typo="Typo.h6" Class="mb-4">Key Pair</MudText>
        <MudTextField Value="_config.PublicKey" Label="Public Key" ReadOnly="true"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3"
                      Adornment="Adornment.End" AdornmentIcon="@Icons.Material.Filled.ContentCopy"
                      OnAdornmentClick="() => CopyToClipboard(_config.PublicKey)" />
        <MudButton Variant="Variant.Outlined" Color="Color.Warning"
                   StartIcon="@Icons.Material.Filled.VpnKey" OnClick="RegenerateKeyPair">
            Regenerate Key Pair
        </MudButton>

        <MudDivider Class="my-6" />

        <MudText Typo="Typo.h6" Class="mb-4">Interface</MudText>
        <MudTextField @bind-Value="_addressesText" Label="Addresses (one per line)"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3" Lines="2" />
        <MudNumericField @bind-Value="_config.ListenPort" Label="Listen Port"
                         Variant="Variant.Outlined" Class="mb-3" Min="1" Max="65535" />
        <MudTextField @bind-Value="_config.PostUp" Label="PostUp" Variant="Variant.Outlined"
                      FullWidth="true" Class="mb-3" />
        <MudTextField @bind-Value="_config.PreDown" Label="PreDown" Variant="Variant.Outlined"
                      FullWidth="true" Class="mb-3" />
        <MudTextField @bind-Value="_config.PostDown" Label="PostDown" Variant="Variant.Outlined"
                      FullWidth="true" Class="mb-3" />

        <MudStack Row="true" Class="mt-4" Spacing="2">
            <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="Save">Save</MudButton>
            <MudButton Variant="Variant.Outlined" Color="Color.Secondary"
                       StartIcon="@Icons.Material.Filled.Sync" OnClick="SaveAndApply"
                       Disabled="_applying">
                @(_applying ? "Applying…" : "Save & Apply Config")
            </MudButton>
        </MudStack>

        @if (_applyResult is not null)
        {
            <MudAlert Class="mt-4" Severity="@(_applyResult.Success ? Severity.Success : Severity.Error)"
                      ShowCloseIcon="true" CloseIconClicked="() => _applyResult = null">
                @(_applyResult.Success ? "Applied successfully." : _applyResult.ErrorMessage)
            </MudAlert>
        }
    </MudPaper>
}

@using WireGuardUI.Core.Interfaces
@using WireGuardUI.Core.Models
@using WireGuardUI.Web.Components.Shared
@inject IServerConfigRepository ServerRepo
@inject IWireGuardService WireGuardService
@inject IKeyPairService KeyPairService
@inject IDialogService DialogService
@inject ISnackbar Snackbar
@inject IJSRuntime JS

@code {
    private ServerConfig? _config;
    private string _addressesText = string.Empty;
    private bool _applying;
    private ApplyResult? _applyResult;

    protected override async Task OnInitializedAsync()
    {
        _config = await ServerRepo.GetAsync();
        _addressesText = string.Join("\n", _config.Addresses);
    }

    private async Task Save()
    {
        _config!.Addresses = _addressesText.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        await ServerRepo.SaveAsync(_config);
        Snackbar.Add("Server configuration saved.", Severity.Success);
    }

    private async Task SaveAndApply()
    {
        await Save();
        _applying = true;
        var config = await WireGuardService.GenerateConfigAsync();
        await WireGuardService.WriteConfigAsync(config);
        _applyResult = await WireGuardService.SyncConfAsync();
        _applying = false;
    }

    private async Task RegenerateKeyPair()
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.Title, "Regenerate Key Pair" },
            { x => x.Message, "All clients will need to update their [Peer] PublicKey. Continue?" },
            { x => x.ConfirmText, "Regenerate" }
        };
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Warning", parameters);
        var result = await dialog.Result;
        if (!result!.Canceled)
        {
            var (priv, pub) = KeyPairService.GenerateKeyPair();
            _config!.PrivateKey = priv;
            _config.PublicKey = pub;
            await ServerRepo.SaveAsync(_config);
            Snackbar.Add("Key pair regenerated. Remember to re-distribute client configs.", Severity.Warning);
        }
    }

    private async Task CopyToClipboard(string text) =>
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
}
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build src/WireGuardUI.Web/WireGuardUI.Web.csproj
git add src/WireGuardUI.Web/Components/Pages/Server.razor
git commit -m "feat(web): add Server configuration page"
```

---

## Task 19: Global Settings Page

**Files:**
- Create: `src/WireGuardUI.Web/Components/Pages/GlobalSettings.razor`

- [ ] **Step 1: Create GlobalSettings.razor**

```razor
@* src/WireGuardUI.Web/Components/Pages/GlobalSettings.razor *@
@page "/global-settings"
@rendermode InteractiveServer
@attribute [Authorize]

<PageTitle>Global Settings — WireGuard UI</PageTitle>
<MudText Typo="Typo.h5" Class="mb-4">Global Settings</MudText>

@if (_setting is null)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    <MudPaper Class="pa-6" Elevation="1">
        <MudTextField @bind-Value="_setting.EndpointAddress" Label="Endpoint Address"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3"
                      HelperText="Public IP or hostname clients use to connect" />
        <MudTextField @bind-Value="_dnsText" Label="DNS Servers (comma-separated)"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3"
                      HelperText="e.g. 1.1.1.1, 8.8.8.8" />
        <MudNumericField @bind-Value="_setting.Mtu" Label="MTU"
                         Variant="Variant.Outlined" Class="mb-3" Min="576" Max="9000" />
        <MudNumericField @bind-Value="_setting.PersistentKeepalive" Label="Persistent Keepalive (seconds)"
                         Variant="Variant.Outlined" Class="mb-3" Min="0" />
        <MudTextField @bind-Value="_setting.FirewallMark" Label="Firewall Mark"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3" />
        <MudTextField @bind-Value="_setting.Table" Label="Routing Table"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3" />
        <MudTextField @bind-Value="_setting.ConfigFilePath" Label="Config File Path"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3" />

        <MudStack Row="true" Class="mt-4" Spacing="2">
            <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="Save">Save</MudButton>
            <MudButton Variant="Variant.Outlined" Color="Color.Secondary"
                       StartIcon="@Icons.Material.Filled.Sync" OnClick="SaveAndApply"
                       Disabled="_applying">
                @(_applying ? "Applying…" : "Save & Apply Config")
            </MudButton>
        </MudStack>

        @if (_applyResult is not null)
        {
            <MudAlert Class="mt-4" Severity="@(_applyResult.Success ? Severity.Success : Severity.Error)"
                      ShowCloseIcon="true" CloseIconClicked="() => _applyResult = null">
                @(_applyResult.Success ? "Applied successfully." : _applyResult.ErrorMessage)
            </MudAlert>
        }
    </MudPaper>
}

@using WireGuardUI.Core.Interfaces
@using WireGuardUI.Core.Models
@inject IGlobalSettingRepository SettingRepo
@inject IWireGuardService WireGuardService
@inject ISnackbar Snackbar

@code {
    private GlobalSetting? _setting;
    private string _dnsText = string.Empty;
    private bool _applying;
    private ApplyResult? _applyResult;

    protected override async Task OnInitializedAsync()
    {
        _setting = await SettingRepo.GetAsync();
        _dnsText = string.Join(", ", _setting.DnsServers);
    }

    private async Task Save()
    {
        _setting!.DnsServers = _dnsText.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        await SettingRepo.SaveAsync(_setting);
        Snackbar.Add("Global settings saved.", Severity.Success);
    }

    private async Task SaveAndApply()
    {
        await Save();
        _applying = true;
        var config = await WireGuardService.GenerateConfigAsync();
        await WireGuardService.WriteConfigAsync(config);
        _applyResult = await WireGuardService.SyncConfAsync();
        _applying = false;
    }
}
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build src/WireGuardUI.Web/WireGuardUI.Web.csproj
git add src/WireGuardUI.Web/Components/Pages/GlobalSettings.razor
git commit -m "feat(web): add Global Settings page"
```

---

## Task 20: Email Settings Page

**Files:**
- Create: `src/WireGuardUI.Web/Components/Pages/EmailSettings.razor`

- [ ] **Step 1: Create EmailSettings.razor**

```razor
@* src/WireGuardUI.Web/Components/Pages/EmailSettings.razor *@
@page "/email-settings"
@rendermode InteractiveServer
@attribute [Authorize]

<PageTitle>Email Settings — WireGuard UI</PageTitle>
<MudText Typo="Typo.h5" Class="mb-4">Email Settings</MudText>

@if (_setting is null)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    <MudPaper Class="pa-6" Elevation="1">
        <MudTextField @bind-Value="_setting.SmtpHost" Label="SMTP Host"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3" />
        <MudNumericField @bind-Value="_setting.SmtpPort" Label="SMTP Port"
                         Variant="Variant.Outlined" Class="mb-3" Min="1" Max="65535" />
        <MudTextField @bind-Value="_setting.SmtpUsername" Label="SMTP Username"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3" />
        <MudTextField @bind-Value="_setting.SmtpPassword" Label="SMTP Password"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3"
                      InputType="InputType.Password" />
        <MudTextField @bind-Value="_setting.FromAddress" Label="From Address"
                      Variant="Variant.Outlined" FullWidth="true" Class="mb-3"
                      InputType="InputType.Email" />
        <MudSelect @bind-Value="_setting.Encryption" Label="Encryption"
                   Variant="Variant.Outlined" Class="mb-3">
            <MudSelectItem Value="@("None")">None</MudSelectItem>
            <MudSelectItem Value="@("STARTTLS")">STARTTLS</MudSelectItem>
            <MudSelectItem Value="@("SSL")">SSL</MudSelectItem>
            <MudSelectItem Value="@("TLS")">TLS</MudSelectItem>
        </MudSelect>

        <MudStack Row="true" Class="mt-4" Spacing="2">
            <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="Save">Save</MudButton>
            <MudButton Variant="Variant.Outlined" Color="Color.Info"
                       StartIcon="@Icons.Material.Filled.Send" OnClick="TestConnection"
                       Disabled="_testing">
                @(_testing ? "Testing…" : "Test Connection")
            </MudButton>
        </MudStack>
    </MudPaper>
}

@using WireGuardUI.Core.Interfaces
@using WireGuardUI.Core.Models
@inject IEmailSettingRepository SettingRepo
@inject IEmailService EmailService
@inject ISnackbar Snackbar

@code {
    private EmailSetting? _setting;
    private bool _testing;

    protected override async Task OnInitializedAsync() =>
        _setting = await SettingRepo.GetAsync();

    private async Task Save()
    {
        await SettingRepo.SaveAsync(_setting!);
        Snackbar.Add("Email settings saved.", Severity.Success);
    }

    private async Task TestConnection()
    {
        _testing = true;
        await SettingRepo.SaveAsync(_setting!);
        var result = await EmailService.TestConnectionAsync(_setting!);
        _testing = false;
        Snackbar.Add(result.IsSuccess ? "Connection successful!" : $"Failed: {result.ErrorMessage}",
            result.IsSuccess ? Severity.Success : Severity.Error);
    }
}
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build src/WireGuardUI.Web/WireGuardUI.Web.csproj
git add src/WireGuardUI.Web/Components/Pages/EmailSettings.razor
git commit -m "feat(web): add Email Settings page"
```

---

## Task 21: Status Page

**Files:**
- Create: `src/WireGuardUI.Web/Components/Pages/Status.razor`

- [ ] **Step 1: Create Status.razor**

```razor
@* src/WireGuardUI.Web/Components/Pages/Status.razor *@
@page "/status"
@rendermode InteractiveServer
@attribute [Authorize]
@implements IAsyncDisposable

<PageTitle>Status — WireGuard UI</PageTitle>
<MudText Typo="Typo.h5" Class="mb-4">
    WireGuard Status
    <MudChip Color="@(_status is null ? Color.Default : Color.Success)" Size="Size.Small" Class="ml-2">
        @(_loading ? "Loading…" : _status is null ? "Unavailable" : "Live")
    </MudChip>
</MudText>

@if (_error is not null)
{
    <MudAlert Severity="Severity.Warning">@_error</MudAlert>
}
else if (_status is not null)
{
    <MudPaper Class="pa-4 mb-4" Elevation="1">
        <MudText Typo="Typo.subtitle1"><strong>Interface:</strong> @_status.InterfaceName</MudText>
        <MudText Typo="Typo.subtitle1"><strong>Public Key:</strong> @_status.PublicKey</MudText>
        <MudText Typo="Typo.subtitle1"><strong>Listen Port:</strong> @_status.ListenPort</MudText>
    </MudPaper>

    <MudText Typo="Typo.h6" Class="mb-2">Peers (@_status.Peers.Count)</MudText>
    <MudTable Items="_status.Peers" Dense="true" Hover="true" Striped="true">
        <HeaderContent>
            <MudTh>Public Key</MudTh>
            <MudTh>Endpoint</MudTh>
            <MudTh>Allowed IPs</MudTh>
            <MudTh>Last Handshake</MudTh>
            <MudTh>RX / TX</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd Style="font-family:monospace;font-size:12px">
                @context.PublicKey[..12]…
            </MudTd>
            <MudTd>@(context.Endpoint ?? "—")</MudTd>
            <MudTd>@string.Join(", ", context.AllowedIPs)</MudTd>
            <MudTd>@(context.LastHandshake ?? "Never")</MudTd>
            <MudTd>@FormatBytes(context.RxBytes) / @FormatBytes(context.TxBytes)</MudTd>
        </RowTemplate>
    </MudTable>
}

<MudText Typo="Typo.caption" Class="mt-4" Color="Color.Secondary">
    Auto-refreshes every 10 seconds.
</MudText>

@using WireGuardUI.Core.Interfaces
@using WireGuardUI.Core.Models
@inject IWireGuardService WireGuardService

@code {
    private WireGuardStatus? _status;
    private string? _error;
    private bool _loading = true;
    private Timer? _timer;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await Refresh();
        _timer = new Timer(async _ =>
        {
            await Refresh();
            await InvokeAsync(StateHasChanged);
        }, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }

    private async Task Refresh()
    {
        _loading = true;
        var result = await WireGuardService.GetStatusAsync();
        if (result.IsSuccess)
        {
            _status = result.Value;
            _error = null;
        }
        else
        {
            _error = $"Could not read WireGuard status: {result.ErrorMessage}";
        }
        _loading = false;
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };

    public async ValueTask DisposeAsync()
    {
        if (_timer is not null)
            await _timer.DisposeAsync();
    }
}
```

- [ ] **Step 2: Build and commit**

```bash
dotnet build src/WireGuardUI.Web/WireGuardUI.Web.csproj
git add src/WireGuardUI.Web/Components/Pages/Status.razor
git commit -m "feat(web): add Status page with auto-refresh"
```

---

## Task 22: Profile Page

**Files:**
- Create: `src/WireGuardUI.Web/Components/Pages/Profile.razor`

- [ ] **Step 1: Create Profile.razor**

```razor
@* src/WireGuardUI.Web/Components/Pages/Profile.razor *@
@page "/profile"
@rendermode InteractiveServer
@attribute [Authorize]

<PageTitle>Profile — WireGuard UI</PageTitle>
<MudText Typo="Typo.h5" Class="mb-4">Profile</MudText>

<MudPaper Class="pa-6" Elevation="1" Style="max-width:480px">
    <MudText Typo="Typo.h6" Class="mb-4">Change Credentials</MudText>

    <MudTextField @bind-Value="_username" Label="Username" Variant="Variant.Outlined"
                  FullWidth="true" Class="mb-3" />
    <MudTextField @bind-Value="_currentPassword" Label="Current Password"
                  Variant="Variant.Outlined" FullWidth="true" Class="mb-3"
                  InputType="InputType.Password" />
    <MudTextField @bind-Value="_newPassword" Label="New Password"
                  Variant="Variant.Outlined" FullWidth="true" Class="mb-3"
                  InputType="InputType.Password" />
    <MudTextField @bind-Value="_confirmPassword" Label="Confirm New Password"
                  Variant="Variant.Outlined" FullWidth="true" Class="mb-4"
                  InputType="InputType.Password" />

    @if (_error is not null)
    {
        <MudAlert Severity="Severity.Error" Class="mb-3">@_error</MudAlert>
    }

    <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="Save"
               Disabled="_saving">
        @(_saving ? "Saving…" : "Save Changes")
    </MudButton>
</MudPaper>

@using WireGuardUI.Core.Interfaces
@using WireGuardUI.Core.Models
@inject IAdminUserRepository UserRepo
@inject ISnackbar Snackbar

@code {
    private string _username = string.Empty;
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string? _error;
    private bool _saving;

    protected override async Task OnInitializedAsync()
    {
        var user = await UserRepo.GetAsync();
        _username = user.Username;
    }

    private async Task Save()
    {
        _error = null;

        if (string.IsNullOrWhiteSpace(_username))
        { _error = "Username cannot be empty."; return; }

        var user = await UserRepo.GetAsync();

        // If changing password, validate current password first
        if (!string.IsNullOrEmpty(_newPassword))
        {
            if (!BCrypt.Net.BCrypt.Verify(_currentPassword, user.PasswordHash))
            { _error = "Current password is incorrect."; return; }
            if (_newPassword != _confirmPassword)
            { _error = "New passwords do not match."; return; }
            if (_newPassword.Length < 8)
            { _error = "New password must be at least 8 characters."; return; }
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(_newPassword);
        }

        user.Username = _username;
        _saving = true;
        await UserRepo.SaveAsync(user);
        _saving = false;

        _currentPassword = _newPassword = _confirmPassword = string.Empty;
        Snackbar.Add("Profile updated successfully.", Severity.Success);
    }
}
```

Add `BCrypt.Net-Next` to the Web project:

```bash
dotnet add src/WireGuardUI.Web/WireGuardUI.Web.csproj package BCrypt.Net-Next
```

- [ ] **Step 2: Build full solution and run tests**

```bash
dotnet build WireGuardUI.sln
dotnet test tests/WireGuardUI.Tests/WireGuardUI.Tests.csproj
```

Expected:
```
Build succeeded. 0 Error(s)
Passed! - Failed: 0, Passed: 10
```

- [ ] **Step 3: Commit**

```bash
git add src/WireGuardUI.Web/Components/Pages/Profile.razor
git commit -m "feat(web): add Profile page for credential management"
```

---

## Task 23: Final Wiring + Smoke Test

- [ ] **Step 1: Add `_Imports.razor` global usings for Web project**

Update or create `src/WireGuardUI.Web/Components/_Imports.razor`:

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using MudBlazor
@using WireGuardUI.Web.Components
@using WireGuardUI.Web.Components.Layout
@using WireGuardUI.Web.Components.Shared
@using WireGuardUI.Web.Components.Pages
```

- [ ] **Step 2: Run the app and smoke-test all pages**

```bash
dotnet run --project src/WireGuardUI.Web
```

Visit each route and verify it renders without errors:
- http://localhost:5000/login → login form renders
- Login with `admin/admin` → redirects to `/`
- http://localhost:5000/ → Clients page renders (empty table)
- http://localhost:5000/server → Server page renders
- http://localhost:5000/global-settings → Global Settings renders
- http://localhost:5000/email-settings → Email Settings renders
- http://localhost:5000/status → Status page renders (may show error if `wg` not installed — that's OK)
- http://localhost:5000/profile → Profile page renders
- Logout → redirects to `/login`

- [ ] **Step 3: Add client — end-to-end test**

In the running app:
1. Navigate to Clients → click "New Client"
2. Enter name "TestClient" → click Create
3. Verify the client appears in the table
4. Click QR icon → verify QR code dialog opens
5. Click Download → verify `.conf` file downloads
6. Click Edit → change name → Save → verify updated
7. Toggle enabled → verify toggle works
8. Click Delete → confirm → verify removed

- [ ] **Step 4: Final commit**

```bash
git add .
git commit -m "feat: complete WireGuard UI implementation — all pages wired and smoke-tested"
```

---

## Self-Review Checklist

| Spec Requirement | Covered In |
|---|---|
| 3-project layered solution | Task 1 |
| WireGuardClient, ServerConfig, GlobalSetting, AdminUser, EmailSetting models | Task 2 |
| All Core interfaces | Task 3 |
| IpAllocationService (pure, tested) | Task 4 |
| EF Core + SQLite + migrations | Task 5 |
| All 5 repositories | Task 6 |
| BouncyCastle key generation (tested) | Task 7 |
| wg0.conf + client config generation (tested) | Task 8 |
| Linux WireGuard service (syncconf + wg-quick fallback) | Task 9 |
| Windows WireGuard service (syncconf + service restart fallback) | Task 10 |
| MailKit email service | Task 11 |
| Program.cs DI, auth, DB seeding | Task 12 |
| MudBlazor layout + navigation | Task 13 |
| Login page with cookie auth | Task 14 |
| ConfirmDialog + QrCodeDisplay | Task 15 |
| Clients page (add, edit, delete, toggle, QR, download, email) | Tasks 16–17 |
| Server page (edit, keypair regen, apply) | Task 18 |
| Global Settings page | Task 19 |
| Email Settings page + test connection | Task 20 |
| Status page (live wg show, auto-refresh) | Task 21 |
| Profile page (change username + password) | Task 22 |
| Platform-specific defaults (Windows/Linux paths) | Task 12 |
| `applyResult` surfaced via Snackbar | Tasks 16, 18, 19 |
| `ErrorBoundary` on each page | Task 13 (MainLayout) |
| Download client config via JS | Task 16 |
| BCrypt password hashing, never store plaintext | Tasks 12, 22 |
