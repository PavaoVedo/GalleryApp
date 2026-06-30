using GalleryApp.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GalleryApp.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Email"] = "admin@test.local",
                ["Admin:Password"] = "Admin12345!",
                ["Storage:Provider"] = "Local",
                ["Storage:Local:RootPath"] = Path.Combine(Path.GetTempPath(), "gallery-tests-storage")
            });
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Admin:Email"] = "admin@test.local",
                ["Admin:Password"] = "Admin12345!",
                ["Storage:Provider"] = "Local",
                ["Storage:Local:RootPath"] = Path.Combine(Path.GetTempPath(), "gallery-tests-storage"),
                ["Storage:Minio:Endpoint"] = "localhost:9000",   // dummy, never connected to
                ["Storage:Minio:AccessKey"] = "test",
                ["Storage:Minio:SecretKey"] = "test",
                ["Storage:Minio:Bucket"] = "test"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                    d.ServiceType == typeof(ApplicationDbContext))
                .ToList();
            foreach (var d in descriptors) services.Remove(d);

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection?.Dispose();
    }


}