using Ecommerce.Api.Extensions.ServiceCollection;
using Ecommerce.Api.Extensions.ServiceCollection.ApplicationBuilder;

// =====================================================================================
// INICIALIZACIÓN DEL BUILDER
// =====================================================================================
var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopmentOrLocal();

// =====================================================================================
// CONFIGURACIÓN DE ARCHIVOS DE CONFIGURACIÓN
// =====================================================================================
// Carga archivos adicionales de configuración como ratelimiting.json, features.json, etc.
builder.Configuration.AddCustomConfigurationFiles(builder.Environment);
// ======================================================

// =====================================================================================
// REGISTRO DE SERVICIOS - CAPAS DE ARQUITECTURA
// =====================================================================================

// ----------------------------------------------------
// CAPA DE INFRAESTRUCTURA - REPOSITORIOS Y PERSISTENCIA
// ----------------------------------------------------
// Configuración del patrón Repository y Unit of Work
builder.Services.AddCustomRepositoryServices();

// ----------------------------------------------------
// SERVICIOS DE INFRAESTRUCTURA EXTERNA
// ----------------------------------------------------
// Servicios de correo electrónico (SendGrid, Mailtrap)
builder.Services.AddCustomEmailService(builder.Configuration);

// Servicios de gestión de imágenes (Cloudinary)
builder.Services.AddCustomImageService(builder.Configuration);

// Servicios de pagos (Stripe, PayPal, etc.)
builder.Services.AddCustomPaymentServices(builder.Configuration);

// =====================================================================================
// CAPA DE APLICACIÓN - LÓGICA DE NEGOCIO
// =====================================================================================
// FluentValidation para validación de comandos y queries
builder.Services.AddCustomFluentValidation();

// MediatR para implementar Command Query Responsibility Segregation (CQRS)
builder.Services.AddCustomMediatR();

// AutoMapper para transformación entre DTOs y entidades de dominio
builder.Services.AddCustomAutoMapper();

// =====================================================================================
// SERVICIOS DE INFRAESTRUCTURA WEB
// =====================================================================================

// ----------------------------------------------------
// CONTROL DE TRÁFICO Y SEGURIDAD
// ----------------------------------------------------

// Rate Limiting para prevenir abuso de la API
builder.Services.AddCustomRateLimiting(builder.Configuration);

// ----------------------------------------------------
// RENDIMIENTO Y CACHE
// ----------------------------------------------------
// Memory Cache para optimización de consultas frecuentes
builder.Services.AddCustomCache();

// ----------------------------------------------------
// PERSISTENCIA DE DATOS
// ----------------------------------------------------
// Entity Framework Core con SQL Server
builder.Services.AddCustomDbContext(builder.Configuration);

// ----------------------------------------------------
// SERIALIZACIÓN Y FORMATO DE DATOS
// ----------------------------------------------------
// Configuración JSON para APIs RESTful (camelCase, fechas, etc.)
builder.Services.AddCustomJson(builder.Environment);

// ----------------------------------------------------
// SERVICIOS DE TIEMPO (REQUERIDO PARA .NET 9)
// ----------------------------------------------------
// TimeProvider para funcionalidades dependientes del tiempo
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

// ----------------------------------------------------
// AUTENTICACIÓN Y AUTORIZACIÓN
// ----------------------------------------------------
// JWT Bearer Token + ASP.NET Core Identity
builder.Services.AddCustomAuthentication(builder.Configuration);

// ----------------------------------------------------
// COMUNICACIÓN ENTRE DOMINIOS
// ----------------------------------------------------
// CORS para permitir requests desde frontend (React, Angular, etc.)
builder.Services.AddCustomCors();

// ----------------------------------------------------
// GESTIÓN DE ARCHIVOS
// ----------------------------------------------------
// Configuración para subida de archivos grandes (imágenes, documentos)
builder.Services.AddCustomFileUpload();

// ----------------------------------------------------
// DOCUMENTACIÓN DE API
// ----------------------------------------------------
// OpenAPI/Swagger para documentación interactiva
builder.Services.AddCustomOpenApi();


// =====================================================================================
// CONSTRUCCIÓN Y CONFIGURACIÓN DE LA APLICACIÓN
// =====================================================================================
var app = builder.Build();

// =====================================================================================
// PIPELINE DE MIDDLEWARES
// =====================================================================================
// Configuración del pipeline HTTP en orden específico:
// 1. HTTPS Redirection
// 2. Exception Handling
// 3. Rate Limiting
// 4. Authentication/Authorization
// 5. CORS
// 6. Controllers
app.UseCustomMiddlewares(isDevelopment);

// =====================================================================================
// INICIALIZACIÓN DE BASE DE DATOS
// =====================================================================================
// Ejecuta migraciones y seed de datos inicial de forma segura
await app.UseCustomMigrationsAsync();


// =====================================================================================
// INICIO DE LA APLICACIÓN
// =====================================================================================
app.Run();




// =====================================================================================
// DOCUMENTACIÓN DE EXTENSIONES PERSONALIZADAS
// =====================================================================================
/*
## 🏗️ CONVENCIONES ADOPTADAS PARA LAS EXTENSIONES

### 📋 Nomenclatura Estándar:
   - `AddCustom[Feature]` para ServiceCollection (registro de servicios)
   - `UseCustom[Feature]` para ApplicationBuilder (configuración del pipeline)

### 🔧 ServiceCollection Extensions (Registro de Servicios):
    - AddCustomAuthentication() → JWT Bearer Token + ASP.NET Core Identity
    - AddCustomCache()          → Memory Cache con configuración optimizada
    - AddCustomCors()           → Políticas CORS para desarrollo/producción
    - AddCustomDbContext()      → Entity Framework Core + SQL Server
    - AddCustomFileUpload()     → Gestión de archivos grandes (50MB)
    - AddCustomJson()           → Serialización JSON consistente
    - AddCustomOpenApi()        → Swagger/OpenAPI con seguridad JWT
    - AddCustomRateLimiting()   → Control de tráfico y prevención de abuso

### 🚀 ApplicationBuilder Extensions (Pipeline de Middlewares):
    - UseCustomDevelopment()    → Herramientas de desarrollo (Swagger, logging)
    - UseCustomMiddlewares()    → Pipeline HTTP ordenado y optimizado
    - UseCustomMigrations()     → Migraciones automáticas y seed de datos



/*
# Convenciones Adoptadas para las extensiones
   - Nomenclatura: AddCustom[Feature] para ServiceCollection, UseCustom[Feature] para ApplicationBuilder

## ServiceCollection Extensions

    - AddCustomAuthentication() - JWT y Identity
    - AddCustomCache()          - Memory Cache configurado
    - AddCustomCors()           - Políticas CORS
    - AddCustomDatabase()       - Entity Framework y DbContext
    - AddCustomFileUpload()     - Configuración de archivos grandes
    - AddCustomJson()           - Serialización JSON
    - AddCustomOpenApi()        - Swagger/OpenAPI
    - AddCustomRateLimiting()   - Rate limiting

## ApplicationBuilder Extensions
    - UseCustomDevelopment()    - Configuración de desarrollo
    - UseCustomMiddlewares()    - Pipeline de middlewares
    - UseCustomMigrations()     - Migraciones y seed de datos

TODO:
     // /health endpoint
    app.UseCustomHealthChecks();      
    // OpenTelemetry, Logging Middleware
    app.UseCustomObservability();      
    // EventStoreDB, Kafka + Outbox , RabbitMQ
    builder.Services.AddCustomEventSourcing(); 

    // Application Insights / Elastic APM para monitoreo en producción
    builder.Services.AddCustomApplicationPerformanceMonitoring(builder.Configuration);
    // Elasticsearch, Azure AI search para búsqueda avanzada de productos
    builder.Services.AddCustomSearchEngine(builder.Configuration);
    // ML.NET para recomendaciones de productos
    builder.Services.AddCustomMachineLearning(builder.Configuration);
    // Azure Cognitive Services / OpenAI integration
    builder.Services.AddCustomAIServices(builder.Configuration);
    // SignalR para notificaciones en tiempo real
    builder.Services.AddCustomSignalR(builder.Configuration);
    // Azure Blob Storage / AWS S3 / MinIO
    builder.Services.AddCustomBlobStorage(builder.Configuration);
    // Servicios de notificaciones push
    builder.Services.AddCustomPushNotifications(builder.Configuration);
    // Vault para gestión de secretos (HashiCorp Vault, Azure Key Vault)
    builder.Services.AddCustomSecretManagement(builder.Configuration);
    // OAuth2/OpenID Connect con múltiples proveedores
    builder.Services.AddCustomExternalAuthentication(builder.Configuration);
 */

// =====================================================================================
// COMANDOS DE DESARROLLO ÚTILES
// =====================================================================================
// Navegación al directorio:
// cd amazonashop

// Ejecución del proyecto:
// dotnet run --project src/api
// dotnet run --project Backend/src/Api/Ecommerce.Api 
// dotnet run --project Backend/src/Api/Ecommerce.Api --launch-profile "Local Development"
// dotnet run --project Backend/src/Api/Ecommerce.Api --launch-profile "Development"

// URLs importantes:
// 🌐 API Swagger: https://localhost:5001/swagger/index.html
// 📊 Health Check: https://localhost:5001/health

// Archivos de configuración importantes:
// 👥 Datos de usuarios iniciales: src\Infrastructure\Persistence\EcommerceDbContextData.cs
// ⚙️ Configuración rate limiting: ratelimiting.json
// 🔧 Settings principales: appsettings.json





