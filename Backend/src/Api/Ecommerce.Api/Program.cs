using Ecommerce.Api.Extensions.ServiceCollection;
using Ecommerce.Api.Extensions.ServiceCollection.ApplicationBuilder;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopmentOrLocal();

// ===== CONFIGURACI�N DE ARCHIVOS DE CONFIGURACI�N =====
builder.Configuration.AddCustomConfigurationFiles(builder.Environment);
// ======================================================

// Service registration
// ===== CONFIGURACI�N DE Repositories =====
builder.Services.AddCustomRepositoryServices();
builder.Services.AddCustomEmailService(builder.Configuration);
builder.Services.AddCustomImageService(builder.Configuration);
builder.Services.AddCustomPaymentServices(builder.Configuration);
// ===== CONFIGURACI�N DE Fluent =====
builder.Services.AddCustomFluentValidation();
// ===== CONFIGURACI�N DE MediatR (CQRS) =====
builder.Services.AddCustomMediatR();
// ===== CONFIGURACI�N DE AutoMapper =====
builder.Services.AddCustomAutoMapper();

// ===== CONFIGURACI�N DE RateLimit =====
builder.Services.AddCustomRateLimiting(builder.Configuration);

// ===== CONFIGURACI�N DE CACHE =====
builder.Services.AddCustomCache();

// ===== CONFIGURACI�N DE Database Context =====
builder.Services.AddCustomDbContext(builder.Configuration);

// ===== CONFIGURACI�N DE Json =====
builder.Services.AddCustomJson(builder.Environment);

//builder.Services.TryAddSingleton<ISystemClock, SystemClock>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

builder.Services.AddCustomAuthentication(builder.Configuration);

// ===== CONFIGURACI�N DE Cors =====
builder.Services.AddCustomCors();

// ===== CONFIGURACI�N DE FileUpload =====
builder.Services.AddCustomFileUpload();

// ===== CONFIGURACI�N DE OpenApi =====
builder.Services.AddCustomOpenApi();

var app = builder.Build();

app.UseCustomMiddlewares(isDevelopment);

await app.UseCustomMigrationsAsync();
/*
// ===== Uso DE OpenApi =====
app.UseCustomOpenApi(isDevelopment);

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();

// Usar Rate Limiting
app.UseCustomRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseCustomCors();

app.MapControllers();
*/



//using (var scope = app.Services.CreateScope())
//{
//    var service = scope.ServiceProvider;
//    var loggerFactory = service.GetRequiredService<ILoggerFactory>();    

//    try
//    {
//        var context = service.GetRequiredService<EcommerceDbContext>();
//        var userManager = service.GetRequiredService<UserManager<Usuario>>();
//        var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();

//        logger.LogInformation("Starting Migration ...");

//        //await context.Database.MigrateAsync();

//        //RC: Por ahora Esto crear� la base de datos seg�n el modelo actual, pero no mantendr� las migraciones.
//        await context.Database.EnsureCreatedAsync();

//        logger.LogDebug("Loading Data ...");
//        await EcommerceDbContextData.LoadDataAsync(context,userManager,roleManager,loggerFactory);

//        logger.LogInformation("Migration Completed !!!");
//    }
//    catch (Exception ex)
//    {

//        logger.LogError(ex,"Error en la migration");
//    }

//}





app.Run();


/*
# Convenciones Adoptadas para las extensiones
   - Nomenclatura: AddCustom[Feature] para ServiceCollection, UseCustom[Feature] para ApplicationBuilder

## ServiceCollection Extensions

    - AddCustomAuthentication() - JWT y Identity
    - AddCustomCache()          - Memory Cache configurado
    - AddCustomCors()           - Pol�ticas CORS
    - AddCustomDatabase()       - Entity Framework y DbContext
    - AddCustomFileUpload()     - Configuraci�n de archivos grandes
    - AddCustomJson()           - Serializaci�n JSON
    - AddCustomOpenApi()        - Swagger/OpenAPI
    - AddCustomRateLimiting()   - Rate limiting

## ApplicationBuilder Extensions
    - UseCustomDevelopment()    - Configuraci�n de desarrollo
    - UseCustomMiddlewares()    - Pipeline de middlewares
    - UseCustomMigrations()     - Migraciones y seed de datos
 */

//cd amazonashop
// dotnet run --project src/api
// dotnet run --project Backend/src/Api/Ecommerce.Api 
// dotnet run --project Backend/src/Api/Ecommerce.Api --launch-profile "Local Development"
// dotnet run --project Backend/src/Api/Ecommerce.Api --launch-profile "Development"
// https://localhost:5001/swagger/index.html

//Datos de los usuarios => src\Infrastructure\Persistence\EcommerceDbContextData.cs



