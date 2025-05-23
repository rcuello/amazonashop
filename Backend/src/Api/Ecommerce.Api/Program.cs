using System.Text;
using Ecommerce.Application;
using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Domain;
using Ecommerce.Infrastructure.ImageCloudinary;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Ecommerce.Api.Extensions.ServiceCollection;
using Ecommerce.Api.Extensions.ServiceCollection.ApplicationBuilder;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopmentOrLocal();

// ===== CONFIGURACIÓN DE ARCHIVOS DE CONFIGURACIÓN =====
builder.Configuration.AddCustomConfigurationFiles(builder.Environment);
// ======================================================

// Service registration
builder.Services.AddCustomInfrastructureServices(builder.Configuration);
builder.Services.AddCustomEmailService(builder.Configuration);

builder.Services.AddCustomApplicationServices(builder.Configuration);

builder.Services.AddCustomRateLimiting(builder.Configuration);

// ===== CONFIGURACIÓN DE CACHE =====
builder.Services.AddCustomCache();


builder.Services.AddDbContext<EcommerceDbContext>(option =>
{
    var connectionString = builder.Configuration.GetConnectionString("ConnectionString");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("La cadena de conexión 'ConnectionString' no está configurada en appsettings.json");
    }

    option.UseSqlServer(
        connectionString,
        sqlOptions => {
            // Especificar el assembly de migraciones
            sqlOptions.MigrationsAssembly(typeof(EcommerceDbContext).Assembly.FullName);

            // Opciones adicionales
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);

            sqlOptions.CommandTimeout(30); // Tiempo de espera en segundos
        }
    );
});

// ===== CONFIGURACIÓN DE MediatR =====
builder.Services.AddCustomMediatR();

// Para la subida de imagenes
builder.Services.AddScoped<IManageImageService, CloudinaryManageImageService>();

// ===== CONFIGURACIÓN DE Json =====
builder.Services.AddCustomJson(builder.Environment);


IdentityBuilder identityBuilder = builder.Services.AddIdentityCore<Usuario>();
identityBuilder = new IdentityBuilder(identityBuilder.UserType, identityBuilder.Services);

identityBuilder.AddRoles<IdentityRole>().AddDefaultTokenProviders();
identityBuilder.AddClaimsPrincipalFactory<UserClaimsPrincipalFactory<Usuario, IdentityRole>>();

identityBuilder.AddEntityFrameworkStores<EcommerceDbContext>();
identityBuilder.AddSignInManager<SignInManager<Usuario>>();

//builder.Services.TryAddSingleton<ISystemClock, SystemClock>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:key"]!));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

// ===== CONFIGURACIÓN DE Cors =====
builder.Services.AddCustomCors();

// ===== CONFIGURACIÓN DE FileUpload =====
builder.Services.AddCustomFileUpload();

// ===== CONFIGURACIÓN DE OpenApi =====
builder.Services.AddCustomOpenApi();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// ===== Uso DE Middlewares =====
app.UseCustomMiddlewares(isDevelopment);



using (var scope = app.Services.CreateScope())
{
    var service = scope.ServiceProvider;
    var loggerFactory = service.GetRequiredService<ILoggerFactory>();    

    try
    {
        var context = service.GetRequiredService<EcommerceDbContext>();
        var userManager = service.GetRequiredService<UserManager<Usuario>>();
        var roleManager = service.GetRequiredService<RoleManager<IdentityRole>>();

        logger.LogInformation("Starting Migration ...");

        //await context.Database.MigrateAsync();

        //RC: Por ahora Esto creará la base de datos según el modelo actual, pero no mantendrá las migraciones.
        await context.Database.EnsureCreatedAsync();

        logger.LogDebug("Loading Data ...");
        await EcommerceDbContextData.LoadDataAsync(context,userManager,roleManager,loggerFactory);

        logger.LogInformation("Migration Completed !!!");
    }
    catch (Exception ex)
    {
        
        logger.LogError(ex,"Error en la migration");
    }

}

// Precargar plantillas de correo electrónico
//app.UseCustomTemplatePreloading(onlyInProduction: false);

// Alternativa: app.UseTemplatePreloading(); // Solo precarga en producción por defecto

app.Run();

//cd amazonashop
// dotnet run --project src/api
// dotnet run --project Backend/src/Api/Ecommerce.Api 
// dotnet run --project Backend/src/Api/Ecommerce.Api --launch-profile "Local Development"
// dotnet run --project Backend/src/Api/Ecommerce.Api --launch-profile "Development"
// https://localhost:5001/swagger/index.html

//Datos de los usuarios => src\Infrastructure\Persistence\EcommerceDbContextData.cs


