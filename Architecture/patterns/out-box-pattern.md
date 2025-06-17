El **Outbox Pattern** es un patrón de diseño que resuelve el problema de mantener consistencia entre operaciones de base de datos y el envío de mensajes/eventos en sistemas distribuidos.

## El Problema que Resuelve

Imagina que necesitas:
1. Guardar datos en la base de datos
2. Enviar un mensaje/evento (email, message queue, webhook, etc.)

**Problema:** ¿Qué pasa si la operación 1 funciona pero la 2 falla? Quedas en un estado inconsistente.

## Cómo Funciona el Outbox Pattern

En lugar de enviar mensajes directamente, los guardas en una tabla "outbox" dentro de la misma transacción de base de datos. Luego, un proceso separado lee y procesa estos mensajes.

## Implementación en C#

### 1. **Modelo del Outbox**
```csharp
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Payload { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
    public int RetryCount { get; set; }
}
```

### 2. **Servicio Principal con Outbox**
```csharp
public class OrderService
{
    private readonly ApplicationDbContext _context;
    
    public async Task CreateOrderAsync(CreateOrderCommand command)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // 1. Operación principal
            var order = new Order 
            { 
                CustomerId = command.CustomerId,
                Amount = command.Amount,
                Status = OrderStatus.Created
            };
            
            _context.Orders.Add(order);
            
            // 2. Agregar mensaje al outbox
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "OrderCreated",
                Payload = JsonSerializer.Serialize(new OrderCreatedEvent 
                { 
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    Amount = order.Amount 
                }),
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            };
            
            _context.OutboxMessages.Add(outboxMessage);
            
            // 3. Guardar todo en una sola transacción
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### 3. **Procesador del Outbox**
```csharp
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync();
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }
        }
    }
    
    private async Task ProcessOutboxMessagesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        
        var unprocessedMessages = await context.OutboxMessages
            .Where(m => !m.IsProcessed && m.RetryCount < 3)
            .OrderBy(m => m.CreatedAt)
            .Take(100)
            .ToListAsync();
            
        foreach (var message in unprocessedMessages)
        {
            try
            {
                // Publicar el mensaje
                await messagePublisher.PublishAsync(message.Type, message.Payload);
                
                // Marcar como procesado
                message.IsProcessed = true;
                message.ProcessedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message {MessageId}", message.Id);
                message.RetryCount++;
            }
        }
        
        await context.SaveChangesAsync();
    }
}
```

### 4. **Publisher de Mensajes**
```csharp
public interface IMessagePublisher
{
    Task PublishAsync(string messageType, string payload);
}

public class MessagePublisher : IMessagePublisher
{
    private readonly IServiceBus _serviceBus;
    private readonly IEmailService _emailService;
    
    public async Task PublishAsync(string messageType, string payload)
    {
        switch (messageType)
        {
            case "OrderCreated":
                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(payload);
                await _serviceBus.PublishAsync(orderEvent);
                break;
                
            case "SendWelcomeEmail":
                var emailEvent = JsonSerializer.Deserialize<SendWelcomeEmailEvent>(payload);
                await _emailService.SendWelcomeEmailAsync(emailEvent.Email);
                break;
                
            default:
                throw new ArgumentException($"Unknown message type: {messageType}");
        }
    }
}
```

## Configuración en Program.cs
```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IMessagePublisher, MessagePublisher>();
builder.Services.AddHostedService<OutboxProcessor>();
```

## Ventajas del Outbox Pattern

1. **Consistencia:** Garantiza que los mensajes se envían solo si la operación principal fue exitosa
2. **Durabilidad:** Los mensajes se persisten y no se pierden
3. **Retry automático:** Puede reintentar mensajes fallidos
4. **Orden:** Mantiene el orden de los eventos
5. **Observabilidad:** Fácil de monitorear y hacer debugging

## Variantes y Mejoras

### **Con Entity Framework Interceptors**
```csharp
public class OutboxInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        
        // Detectar entidades que generan eventos
        var domainEvents = context.ChangeTracker.Entries<IDomainEntity>()
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();
            
        // Convertir a mensajes outbox
        foreach (var domainEvent in domainEvents)
        {
            var outboxMessage = new OutboxMessage
            {
                Type = domainEvent.GetType().Name,
                Payload = JsonSerializer.Serialize(domainEvent),
                CreatedAt = DateTime.UtcNow
            };
            
            context.Set<OutboxMessage>().Add(outboxMessage);
        }
        
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

### **Con Polly para Retry**
```csharp
private static readonly AsyncRetryPolicy _retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, delay, retryCount, context) =>
        {
            Console.WriteLine($"Retry {retryCount} after {delay} seconds");
        });

// Usar en el procesador
await _retryPolicy.ExecuteAsync(async () =>
{
    await messagePublisher.PublishAsync(message.Type, message.Payload);
});
```

El Outbox Pattern es especialmente útil en arquitecturas de microservicios y sistemas distribuidos donde necesitas garantizar la consistencia eventual entre diferentes servicios.