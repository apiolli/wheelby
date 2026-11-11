using Host.WebAPI.ErrorHandling;
using Shared.Application;
using Scalar.AspNetCore;
using AccessControl.Infrastructure;
using Notifications.Infrastructure;
using Permissions.Infrastructure;
using Wheelby.Infrastructure;
using Documents.Infrastructure;
using Host.WebAPI.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddProblemDetails();
builder.Services.AddSharedApplication();  
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// DI de cada modulo
builder.Services.AddAccessControl(builder.Configuration);       
builder.Services.AddNotifications(builder.Configuration);
builder.Services.AddPermissions(builder.Configuration);
builder.Services.AddWheelby(builder.Configuration);
builder.Services.AddDocuments(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
// CORS va antes de la autenticación: así las preflight OPTIONS reciben sus cabeceras.
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Aqui lo que hago es aplicar las migraciones de cada modulo al iniciar
await app.Services.ApplyNotificationsMigrationsAsync();
await app.Services.ApplyAccessControlMigrationsAsync();
await app.Services.ApplyPermissionsMigrationsAsync();
await app.Services.ApplyWheelbyMigrationsAsync();
await app.Services.ApplyDocumentsMigrationsAsync();
await app.Services.SeedAdministratorAsync();

// En el contenedor se corre en Production con Scalar activo (OpenApi__Enabled=true) para la revisión.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();                
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Wheelby API");
    });                              
}

// Mapeo de endpoints
app.MapAccessControlEndpoints();
app.MapPermissionsEndpoints();
app.MapWheelbyEndpoints();
app.MapDocumentsEndpoints();


app.Run();