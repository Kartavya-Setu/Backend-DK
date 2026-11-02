using System.Text;
using System.Text.Json.Serialization;
using DirectKarigar.Application.Common.Interfaces;
using DirectKarigar.Infrastructure.Identity;
using DirectKarigar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load secret credentials if present (kept out of version control)
builder.Configuration.AddJsonFile("appsettings.secret.json", optional: true, reloadOnChange: true);

// 1. Database Configuration
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=directkarigar.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // By default for local development, use SQLite for zero setup; seamlessly switch to Postgres if configured
    var usePostgres = builder.Configuration.GetValue<bool>("UsePostgres");
    if (usePostgres)
    {
        var pgConn = builder.Configuration.GetConnectionString("PostgresConnection")!;
        options.UseNpgsql(pgConn);
    }
    else
    {
        options.UseSqlite(defaultConn);
    }
});

// 2. Services Registration
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// 3. Authentication & JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "DirectKarigar_Enterprise_Architect_Master_Key_2026_Bangalore_SuperSecret_SecurityToken!@#$";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "DirectKarigar.WebApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DirectKarigar.Frontend";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// 4. CORS Policy for Angular 19 SPA
const string CorsPolicy = "DirectKarigarCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:4200",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:4200"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 5. Controllers with JSON String Enum Converter & CamelCase
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 6. Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DirectKarigar Core API",
        Version = "v1",
        Description = "Enterprise C2C Hyperlocal Direct Marketplace API - 0% labor commission, UIDAI KYC, and anti-bypass dispute protection."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 7. Auto-Migration & Seed on Startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DbInitializer.SeedAsync(db);
}

// 8. Pipeline Middleware Configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DirectKarigar API v1"));
}

app.UseCors(CorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
