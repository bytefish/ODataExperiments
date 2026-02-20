using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using ODataFga.Database;
using ODataFga.Fga;
using ODataFga.Hosted;
using ODataFga.OData;
using ODataFga.Services;
using ODataFga.Services.Implementations;
using OpenFga.Sdk.Client;

var builder = WebApplication.CreateBuilder(args);

// Read the Settings for OpenFGA
string fgaUrl = builder.Configuration["OpenFga:ApiUrl"] ?? "http://localhost:8080";

// Read the Settings for PostgreSQL
string connectionString = builder.Configuration.GetConnectionString("Postgres") ?? "Host=localhost;Database=postgres;Username=postgres;Password=password";

// Check if we got a Store to work with, if not create one and the model
var storeId = await OpenFgaSetup.EnsureStoreAndModel(fgaUrl);

// Register OpenFGA Client
builder.Services.AddSingleton<IOpenFgaClient>(sp =>
{
    return new OpenFgaClient(new ClientConfiguration { ApiUrl = fgaUrl, StoreId = storeId });
});

// Register the Identity Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpHeaderUserService>();

// Register EF Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Register the Bulk Permission Sync Service
builder.Services.AddSingleton<IPermissionSyncService, PostgresBulkPermissionSyncService>();

// Register the Document Service handling the main logic for creating documents and resolving permissions
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Register Controllers with OData support
builder.Services.AddControllers()
    .AddOData(opt => opt
        .AddRouteComponents("odata", AppEdmModel.GetEdmModel())
        .Select().Filter().OrderBy().Expand().Count().SetMaxTop(100));

// Background Worker
builder.Services.AddHostedService<BitmaskWatcherService>();

var app = builder.Build();

// Initialize the Database and Create required Functions
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    await db.Database.EnsureCreatedAsync();

    // Create the Recursive CTE Function
    await db.Database.ExecuteSqlRawAsync(@"
        CREATE OR REPLACE FUNCTION sp_resolve_ancestors(p_folder_id text)
        RETURNS TABLE (""Id"" text) AS $$
        BEGIN
            RETURN QUERY
            WITH RECURSIVE path_tree AS (
                SELECT ""Id"", ""ParentId"", 1 as depth FROM ""Folders"" WHERE ""Id"" = p_folder_id
                UNION ALL
                SELECT f.""Id"", f.""ParentId"", p.depth + 1 FROM ""Folders"" f
                INNER JOIN path_tree p ON f.""Id"" = p.""ParentId"" WHERE p.depth < 50 
            )
            SELECT pt.""Id"" FROM path_tree pt ORDER BY depth DESC;
        END;
        $$ LANGUAGE plpgsql;
    ");
}

app.MapControllers();
app.Run();

public partial class Program { }