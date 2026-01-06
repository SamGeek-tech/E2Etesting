using InventoryService.Application;
using InventoryService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Inventory Service API", Version = "v1" });
});

// Clean Architecture layers
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=inventory.db";

builder.Services.AddApplication();  // InventoryService.Application
builder.Services.AddInfrastructure(connectionString);  // InventoryService.Infrastructure

var app = builder.Build();

// Ensure database is created
InventoryService.Infrastructure.DependencyInjection.EnsureDatabaseCreated(app.Services);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
