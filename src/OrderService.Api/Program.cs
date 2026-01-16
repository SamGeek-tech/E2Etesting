using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OrderService.Application;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure;
using OrderService.Infrastructure.ExternalServices;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Check if running for contract testing (provider verification)
var isContractTesting = Environment.GetEnvironmentVariable("PACT_PROVIDER_VERIFICATION") == "true"
    || builder.Environment.EnvironmentName == "ContractTesting";

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Order Service API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Clean Architecture layers
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=orders.db";

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

// CONTRACT TESTING: Use mock InventoryClient that always succeeds
if (isContractTesting)
{
    // Override the real InventoryClient with mock for contract testing
    builder.Services.AddScoped<IInventoryClient, MockInventoryClient>();
}

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super_secret_key_that_is_long_enough_123";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "OrderService",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "OrderWeb",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

var app = builder.Build();

// Ensure database is created
OrderService.Infrastructure.DependencyInjection.EnsureDatabaseCreated(app.Services);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();

// CONTRACT TESTING: Override authentication for Pact verification requests
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        
        // Check if it's a test token OR contains Pact regex patterns like "Bearer .*"
        if (!string.IsNullOrEmpty(authHeader) && 
            (authHeader.Contains("test-token") || 
             authHeader.Contains("Bearer .*") || 
             authHeader.Contains("Bearer .+")))
        {
            // Inject a test user identity for contract testing
            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, "user@example.com"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "test-user-id")
            };
            // Use the JWT Bearer authentication scheme so [Authorize] recognizes it
            var identity = new System.Security.Claims.ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
            context.User = new System.Security.Claims.ClaimsPrincipal(identity);
        }
        await next(context);
    });
}

app.UseAuthorization();

// Add Provider State Middleware for Contract Testing
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<OrderService.Api.Middleware.ProviderStateMiddleware>();
}

app.MapControllers();

app.Run();

// Expose Program class for integration/contract testing
public partial class Program { }
