using Ambev.DeveloperEvaluation.ORM;
using Ambev.DeveloperEvaluation.WebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Xunit;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

public class SalesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"sales_tests_{Guid.NewGuid():N}";
    private readonly string _connectionString;

    public HttpClient Client { get; private set; } = null!;

    public SalesApiFactory()
    {
        var connection = new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("SALES_TEST_CONNECTION")
            ?? "Host=localhost;Port=5432;Username=developer;Password=local_development_password");

        // Always use a new database, even when the connection is supplied externally.
        connection.Database = _databaseName;
        _connectionString = connection.ConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DefaultContext>>();
            services.AddDbContext<DefaultContext>(options => options.UseNpgsql(_connectionString));
        });
    }

    public async Task InitializeAsync()
    {
        Client = CreateClient();
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();

        if (context.Database.GetDbConnection().Database != _databaseName)
            throw new InvalidOperationException("Integration tests must use their own database.");

        await context.Database.MigrateAsync();
    }

    public async Task<int> CountItemsAsync(Guid saleId)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DefaultContext>();
        return await context.SaleItems.CountAsync(item => item.SaleId == saleId);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Client?.Dispose();
        try
        {
            var options = new DbContextOptionsBuilder<DefaultContext>()
                .UseNpgsql(_connectionString).Options;
            await using var context = new DefaultContext(options);
            await context.Database.EnsureDeletedAsync();
        }
        finally
        {
            await base.DisposeAsync();
        }
    }
}
