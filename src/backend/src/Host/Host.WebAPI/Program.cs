using Host.WebAPI.ErrorHandling;
using Shared.Application;
using Scalar.AspNetCore;
using AccessControl.Infrastructure;
using Notifications.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSharedApplication();  
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


// DI de cada modulo
builder.Services.AddAccessControl(builder.Configuration);       
builder.Services.AddNotifications(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

// Aqui lo que hago es aplicar las migraciones de cada modulo al iniciar
await app.Services.ApplyNotificationsMigrationsAsync();
await app.Services.ApplyAccessControlMigrationsAsync();

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