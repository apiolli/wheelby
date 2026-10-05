using Host.WebAPI.ErrorHandling;
using Shared.Application;
using Scalar.AspNetCore;
using AccessControl.Infrastructure;
using Notifications.Infrastructure;
using Wheelby.Infrastructure;
using Host.WebAPI.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddProblemDetails();
builder.Services.AddSharedApplication();  
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// DI de cada modulo
builder.Services.AddAccessControl(builder.Configuration);       
builder.Services.AddNotifications(builder.Configuration);
builder.Services.AddWheelby(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

// Aqui lo que hago es aplicar las migraciones de cada modulo al iniciar
await app.Services.ApplyNotificationsMigrationsAsync();
await app.Services.ApplyAccessControlMigrationsAsync();
await app.Services.ApplyWheelbyMigrationsAsync();
await app.Services.SeedAdministratorAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Wheelby API");
    });                              
}

// Mapeo de endpoints
app.MapAccessControlEndpoints();


app.Run();