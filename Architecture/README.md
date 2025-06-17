## **CATEGORÍAS PRINCIPALES DE PATRONES**

### **1. PATRONES DE ARQUITECTURA DE APLICACIÓN**

**Estructurales:**
- **Layered (N-Tier):** Capas separadas (Presentation → Business → Data)
- **Hexagonal (Ports & Adapters):** Núcleo de negocio aislado de dependencias externas
- **Clean Architecture:** Dependencias apuntan hacia adentro
- **Onion Architecture:** Similar a Clean, pero con capas en forma de cebolla

**De Componentes:**
- **Component-Based:** Aplicación dividida en componentes reutilizables
- **Plugin/Microkernel:** Núcleo mínimo + plugins extensibles
- **Modular Monolith:** Monolito organizado en módulos bien definidos

### **2. PATRONES DE SISTEMAS DISTRIBUIDOS**

**De Comunicación:**
- **Client-Server:** Cliente solicita, servidor responde
- **Peer-to-Peer:** Nodos equivalentes que se comunican directamente
- **Message Bus/Broker:** Comunicación a través de intermediario
- **Request-Response:** Comunicación síncrona bidireccional
- **Publish-Subscribe:** Productores publican, consumidores se suscriben

**De Descomposición:**
- **Microservices:** Servicios pequeños e independientes
- **Service-Oriented Architecture (SOA):** Servicios como unidades de funcionalidad
- **Serverless/FaaS:** Funciones ejecutadas bajo demanda

### **3. PATRONES DE MANEJO DE DATOS**

**De Persistencia:**
- **Repository Pattern:** Abstracción de acceso a datos
- **Unit of Work:** Mantiene lista de objetos afectados por transacción
- **Data Access Object (DAO):** Interfaz para operaciones de base de datos
- **Active Record:** Objetos que se persisten a sí mismos

**De Consistencia:**
- **CQRS (Command Query Responsibility Segregation):** Separar lecturas de escrituras
- **Event Sourcing:** Almacenar eventos en lugar de estados
- **Saga Pattern:** Transacciones distribuidas
- **Outbox Pattern:** Consistencia en mensajería

### **4. PATRONES DE PROCESAMIENTO**

**De Flujo de Datos:**
- **Pipe and Filter:** Datos fluyen a través de filtros conectados
- **MapReduce:** Mapear datos → Reducir resultados
- **Stream Processing:** Procesamiento continuo de flujo de datos
- **Batch Processing:** Procesamiento por lotes

**De Eventos:**
- **Event-Driven Architecture:** Reaccionar a eventos
- **Event Streaming:** Flujo continuo de eventos
- **CQRS + Event Sourcing:** Combinación poderosa para sistemas complejos

### **5. PATRONES DE ESCALABILIDAD Y PERFORMANCE**

**De Escalabilidad:**
- **Load Balancer:** Distribuir carga entre instancias
- **Sharding:** Dividir datos horizontalmente
- **Read Replicas:** Copias de solo lectura
- **Circuit Breaker:** Prevenir cascadas de fallos

**De Cache:**
- **Cache-Aside:** Aplicación maneja cache
- **Write-Through:** Escribir a cache y DB simultáneamente  
- **Write-Behind:** Escribir a cache primero, DB después
- **Refresh-Ahead:** Refrescar cache antes de expiración

### **6. PATRONES DE INTEGRACIÓN**

**Enterprise Integration Patterns:**
- **Message Channel:** Canal para enviar mensajes
- **Message Router:** Dirigir mensajes según contenido
- **Message Translator:** Traducir entre formatos
- **Message Filter:** Filtrar mensajes no deseados
- **Aggregator:** Combinar mensajes relacionados

## **EJEMPLO PRÁCTICO: E-COMMERCE**

```csharp
// Combinando múltiples patrones
public class ECommerceSystem
{
    // 1. Clean Architecture (capas)
    // 2. CQRS (separar comandos de queries)
    // 3. Event Sourcing (historial de eventos)
    // 4. Outbox Pattern (consistencia)
    // 5. Repository Pattern (datos)
    
    public class OrderCommandHandler
    {
        private readonly IOrderRepository _repository;
        private readonly IEventStore _eventStore;
        private readonly IOutboxService _outbox;
        
        public async Task<Result> CreateOrder(CreateOrderCommand command)
        {
            using var transaction = await _context.BeginTransactionAsync();
            
            // Domain logic
            var order = Order.Create(command.CustomerId, command.Items);
            
            // Repository pattern
            await _repository.SaveAsync(order);
            
            // Event sourcing
            await _eventStore.SaveEventsAsync(order.Id, order.Events);
            
            // Outbox pattern
            await _outbox.PublishEventsAsync(order.Events);
            
            await transaction.CommitAsync();
            return Result.Success();
        }
    }
}
```

## **GUÍA DE APRENDIZAJE PROGRESIVO**

**Nivel Básico:**
1. Layered Architecture
2. MVC/MVP/MVVM
3. Repository Pattern
4. Factory Pattern

**Nivel Intermedio:**
5. Clean Architecture
6. CQRS
7. Event-Driven Architecture
8. Microservices basics

**Nivel Avanzado:**
9. Event Sourcing
10. Saga Pattern
11. Distributed patterns
12. Performance patterns

## **RECURSOS PARA PROFUNDIZAR**

**Libros fundamentales:**
- "Patterns of Enterprise Application Architecture" - Martin Fowler
- "Building Microservices" - Sam Newman
- "Clean Architecture" - Robert C. Martin
- "Enterprise Integration Patterns" - Hohpe & Woolf

**Patrones específicos por tecnología:**
- **.NET:** Microsoft Architecture Guides
- **Java:** Spring patterns, Java EE patterns  
- **Node.js:** Express patterns, NestJS patterns
- **Cloud:** AWS/Azure/GCP patterns

¿Te gustaría que profundice en alguna categoría específica o que te muestre implementaciones prácticas de algunos patrones en particular?