using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure for deployment
builder.WebHost.UseUrls("http://0.0.0.0:8080");

// Add services to the container.
builder.Services.AddControllers();

// Add Entity Framework with PostgreSQL support for production
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

    if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgres://"))
    {
        // Convert Render PostgreSQL URL to connection string
        var uri = new Uri(connectionString);
        var npgsqlConnectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath[1..]};Username={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]};SSL Mode=Require;Trust Server Certificate=true";
        options.UseNpgsql(npgsqlConnectionString);
    }
    else
    {
        // Fallback to SQL Server for local development
        options.UseSqlServer(connectionString);
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