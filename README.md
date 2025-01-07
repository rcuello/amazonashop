## Crear archive global.json 

```bash
dotnet new globaljson --sdk-version 8.0.403 --force
```

## Crear solución
```bash
dotnet new sln --name EcommerceSolution
```

## Crear Capa domain
```bash
dotnet new classlib -o src/Core/Ecommerce.Domain
```

## Crear Capa aplicación
```bash
dotnet new classlib -o src/Core/Ecommerce.Application
```

## Crear Capa infrastructure
```bash
dotnet new classlib -o src/Infrastructure --name Ecommerce.Infrastructure
```

## Crear Capa API
```bash
dotnet new webapi -o src/Api --name Ecommerce.Api
```
---

# Agregar referencias de los proyectos a la solución

## Referencia de infrastructure
```bash
 dotnet sln add src/Infrastructure/Ecommerce.Infrastructure.csproj
```

## Referencia de domain
```bash
 dotnet sln add src/Core/Ecommerce.Domain/Ecommerce.Domain.csproj
```

## Referencia de domain
```bash
 dotnet sln add src/Core/Ecommerce.Application/Ecommerce.Application.csproj
```

## Referencia de API
```bash
 dotnet sln add src/Api/Ecommerce.Api.csproj
```

---

# Referencia entre proyectos

## Infrastructure => Application

```bash
 dotnet add src/Infrastructure/Ecommerce.Infrastructure.csproj reference src/Core/Ecommerce.Application/Ecommerce.Application.csproj 
```

## Application => Domain

```bash
 dotnet add src/Core/Ecommerce.Application/Ecommerce.Application.csproj reference src/Core/Ecommerce.Domain/Ecommerce.Domain.csproj
```

## API => Application

```bash
 dotnet add src/Api/Ecommerce.Api.csproj reference src/Core/Ecommerce.Application/Ecommerce.Application.csproj
```

## API => Infrastructure
```bash
 dotnet add src/Api/Ecommerce.Api.csproj reference src/Infrastructure/Ecommerce.Infrastructure.csproj
```