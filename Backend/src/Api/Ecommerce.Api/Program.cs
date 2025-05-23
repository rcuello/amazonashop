using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Ecommerce.Api.Middlewares;
using Ecommerce.Application;
using Ecommerce.Application.Contracts.Infrastructure;
using Ecommerce.Application.Features.Products.Queries.GetProductList;
using Ecommerce.Domain;
using Ecommerce.Infrastructure.ImageCloudinary;
using Ecommerce.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Ecommerce.Application.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Service registration
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);

// ===== CONFIGURACIÓN DE CACHE =====
// 1. Memory Cache (para datos frecuentemente accedidos y de corta duración)
builder.Services.AddMemoryCache(options =>
{
    // OPCIÓN A: Sin SizeLimit (no requerirá Size en cada entrada)
    options.SizeLimit = null; // Comentar o eliminar SizeLimit

    // OPCIÓN B: Con SizeLimit (requerirá Size en cada entrada)
    // Sino es especificado probablemente se lance el error:
    // System.InvalidOperationException: 'Cache entry must specify a value for Size when SizeLimit is set.'
    //options.SizeLimit = 1024; // Límite de entradas en caché

    options.CompactionPercentage = 0.25; // 25% de compactación cuando se alcanza el límite
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5); // Escaneo cada 5 minutos
});


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

builder.Services.AddMediatR(typeof(GetProductListQueryHandler).Assembly);

// Para la subida de imagenes
builder.Services.AddScoped<IManageImageService, CloudinaryManageImageService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddSingleton(serviceProvider =>
{
    var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
    return new System.Text.Json.JsonSerializerOptions
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = environment.IsDevelopment(),
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };
});



builder.Services.AddControllers(opt =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    opt.Filters.Add(new AuthorizeFilter(policy));
}).AddJsonOptions(x =>
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllPolicy", builder => builder.AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
});

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = 52428800; // 50MB en bytes
    options.MultipartBodyLengthLimit = 52428800; // 50MB en bytes
    options.MultipartHeadersLengthLimit = 52428800; // 50MB en bytes
});

builder.Services.AddOpenApi();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

var rateLimitConfig = app.Services.GetRequiredService<IOptions<RateLimitConfiguration>>().Value;

logger.LogInformation("Rate Limiting {Status}. Environment: {Environment}",
    rateLimitConfig.Enabled ? "Enabled" : "Disabled",
    builder.Environment.EnvironmentName);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Ecommerce API");        
    });

    app.MapGet("/", context =>
    {
        context.Response.Redirect("/swagger");
        return Task.CompletedTask;
    });
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors("AllowAllPolicy");

app.MapControllers();



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
app.UseTemplatePreloading(onlyInProduction: false);
// Alternativa: app.UseTemplatePreloading(); // Solo precarga en producción por defecto

app.Run();

//cd amazonashop/Backend
// dotnet run --project src/api
// dotnet run --project src/api --launch-profile "https"
// dotnet run --project Backend/src/Api/Ecommerce.Api --launch-profile "https"
// https://localhost:5001/swagger/index.html

//Datos de los usuarios => src\Infrastructure\Persistence\EcommerceDbContextData.cs


