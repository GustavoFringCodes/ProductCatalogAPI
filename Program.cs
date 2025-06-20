using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure for deployment
builder.WebHost.UseUrls("http://0.0.0.0:8080");

// Add services to the container.
builder.Services.AddControllers();

// Add Entity Framework with improved PostgreSQL support
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrEmpty(connectionString))
    {
        // Convert Render PostgreSQL URL to proper connection string
        connectionString = connectionString.Replace("postgresql://", "");
        var parts = connectionString.Split('@');
        var userInfo = parts[0].Split(':');
        var hostInfo = parts[1].Split('/');
        var hostAndPort = hostInfo[0].Split(':');

        var username = userInfo[0];
        var password = userInfo[1];
        var host = hostAndPort[0];
        var port = hostAndPort.Length > 1 ? hostAndPort[1] : "5432";
        var database = hostInfo[1];

        var npgsqlConnectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        options.UseNpgsql(npgsqlConnectionString);
    }
    else
    {
        // Fallback to SQL Server for local development
        var fallbackConnection = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseSqlServer(fallbackConnection);
    }
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// AUTO-CREATE DATABASE TABLES ON STARTUP
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        context.Database.EnsureCreated();
        Console.WriteLine("✅ Database and tables created successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error creating database: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductCatalog API V1");
        c.RoutePrefix = "swagger"; // Swagger available at /swagger
    });
}

// Only use HTTPS redirect in development (Render handles HTTPS automatically)
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Add a root route for testing
app.MapGet("/", () => "ProductCatalog API is running! Go to /swagger for API documentation.");

// Enable CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();