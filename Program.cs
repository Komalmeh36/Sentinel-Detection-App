
using Microsoft.EntityFrameworkCore;
using Sentinel.Web.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Production: Neon PostgreSQL
if (builder.Environment.IsProduction())
{
    var connectionString = builder.Configuration
        .GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Production database connection string is missing.");

    var npgsqlConnectionString =
        DatabaseConnectionString.ConvertToNpgsql(connectionString);

    builder.Services.AddDbContext<PostgresApplicationDbContext>(
        options => options.UseNpgsql(npgsqlConnectionString));

    builder.Services.AddScoped<ApplicationDbContext>(
        serviceProvider =>
            serviceProvider.GetRequiredService<PostgresApplicationDbContext>());
}
// Development: Local SQLite
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(
            builder.Configuration.GetConnectionString("DefaultConnection")));
}

var app = builder.Build();

// Automatically apply PostgreSQL migrations in production
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();

    var db = scope.ServiceProvider
        .GetRequiredService<PostgresApplicationDbContext>();

    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
