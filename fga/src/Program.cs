using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ODataFga.Database;
using ODataFga.Fga;
using ODataFga.Hosted;
using ODataFga.OData;
using ODataFga.Services;
using ODataFga.Services.Implementations;
using OpenFga.Sdk.Client;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Read the Settings for OpenFGA
string fgaUrl = builder.Configuration["OpenFga:ApiUrl"] ?? "http://localhost:8080";

// Read the Settings for PostgreSQL
string connectionString = builder.Configuration.GetConnectionString("Postgres") ?? "Host=localhost;Database=postgres;Username=postgres;Password=password";

// Check if we got a Store to work with, if not create one and the model
string storeId = await OpenFgaSetup.EnsureStoreAndModel(fgaUrl, "DemoStore");

// Register OpenFGA Client for the configured store and API URL
builder.Services.AddSingleton<IOpenFgaClient>(sp =>
{
    return new OpenFgaClient(new ClientConfiguration { ApiUrl = fgaUrl, StoreId = storeId });
});

// Register the Identity Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpHeaderUserService>();

// Register a DB Context Factory with EF Core Interceptors for RLS
builder.Services.AddDbContextFactory<AppDbContext>((sp, options) => 
{
    options.UseNpgsql(connectionString);

    // Inject our RLS Interceptor to push the Session State to Postgres
    ICurrentUserService currentUser = sp.GetRequiredService<ICurrentUserService>();

    options.AddInterceptors(new RlsInterceptor(currentUser));
});

// Resolve the DbContext from the Factory for regular usage in the app (e.g. in Controllers or Services)
builder.Services.AddScoped(sp => 
{
    return sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
});

// Register the Bulk Permission Sync Service
builder.Services.AddSingleton<IPermissionSyncService, PostgresBulkPermissionSyncService>();

// Register the Document Service handling the main logic for creating documents and resolving permissions
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IGroupService, GroupService>();

// Register Controllers with OData support
builder.Services.AddControllers()
    .AddOData(opt => opt
        .AddRouteComponents("odata", AppEdmModel.GetEdmModel())
        .Select().Filter().OrderBy().Expand().Count().SetMaxTop(100));

// Background Worker
builder.Services.AddHostedService<BitmaskWatcherService>();

WebApplication app = builder.Build();

// Initialize the Database and Create required Functions
using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.EnsureCreatedAsync();

    // Dynamically discover secured entities and apply RLS policies
    IEnumerable<IEntityType> securedEntityTypes = db.Model.GetEntityTypes()
        .Where(t => typeof(ISecuredResource).IsAssignableFrom(t.ClrType) && !t.ClrType.IsAbstract);

    foreach (IEntityType entityType in securedEntityTypes)
    {
        string? tableName = entityType.GetTableName();

        string? fgaType = entityType.ClrType.GetCustomAttribute<FgaTypeAttribute>()?.Name;

        if (tableName != null && fgaType != null)
        {
            await db.Database.ExecuteSqlRawAsync(RlsPolicyGenerator.Generate(tableName, fgaType));
        }
    }
}

app.MapControllers();
app.Run();

public partial class Program { }