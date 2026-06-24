using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BeautyStore.Api.Data;

// Used ONLY by EF Core CLI tools (dotnet ef migrations / database update).
// Never instantiated at runtime — the real DbContext comes from DI.
//
// Config loading order (mirrors WebApplication.CreateBuilder):
//   1. appsettings.json              (optional — may not exist)
//   2. appsettings.Development.json  (optional — environment-specific overrides)
//   3. Environment variables         ← set ConnectionStrings__BeautyStoreDb here
//
// To run migrations against Azure SQL, set in your terminal before calling
// dotnet ef database update:
//
//   $env:ConnectionStrings__BeautyStoreDb = "Server=tcp:<server>.database.windows.net,1433;..."
//
sealed class BeautyStoreDbContextFactory : IDesignTimeDbContextFactory<BeautyStoreDbContext>
{
    public BeautyStoreDbContext CreateDbContext(string[] args)
    {
        // AppContext.BaseDirectory is the build output dir where appsettings files
        // are copied by the SDK. Directory.GetCurrentDirectory() is a fallback for
        // cases where the build output path differs from the working directory.
        var projectDir = AppContext.BaseDirectory;

        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(projectDir, "appsettings.json"),             optional: true)
            .AddJsonFile(Path.Combine(projectDir, "appsettings.Development.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("BeautyStoreDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:BeautyStoreDb is not configured.\n" +
                "Set it before running dotnet ef commands:\n\n" +
                "  $env:ConnectionStrings__BeautyStoreDb = " +
                "\"Server=tcp:<server>.database.windows.net,1433;" +
                "Database=BeautyStoreDb;User ID=<login>;Password=<pwd>;" +
                "Encrypt=True;TrustServerCertificate=False;\"");

        return new BeautyStoreDbContext(
            new DbContextOptionsBuilder<BeautyStoreDbContext>()
                .UseSqlServer(connectionString)
                .Options);
    }
}
