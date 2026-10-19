# 📦 HANDOFF - InventoryManagement.Api

> **Proyecto:** Backend ASP.NET Core 8.0 para Manejo de Inventario  
> **Estado actual:** ✅ Fase 0, 1, 2, 3 y **Fase 4 completadas** y probadas  
> **Próximo:** Fase 5 - Tests y Calidad  
> **Fecha:** 19/10/2026

---

## 🎯 Resumen rápido

| Fase | Estado | Qué incluye |
|------|--------|-------------|
| **0 - Fundación** | ✅ | EF Core + PostgreSQL + Entidades + Migración + CRUD básico |
| **1 - DTOs/Validación/Mapeo** | ✅ | FluentValidation + Mapster + DTOs + ApiResponse + ProblemDetails |
| **2 - Error Handling + Logging** | ✅ | Middleware global + Excepciones dominio + Serilog + Correlation ID |
| **3 - Stock Movements** | ✅ **COMPLETADA** | StockMovement entity, Kardex, movimientos entrada/salida/ajuste |
| **4 - Auth JWT** | ✅ **COMPLETADA** | JWT + Roles + Refresh tokens + Rotación + Auditoría + Admin Seed |
| **5 - Tests** | 🔄 **SIGUIENTE** | xUnit + TestContainers + Integration tests |
| **6 - Observabilidad** | ⏳ | Health checks + Métricas + Versionado |
| **7 - Performance** | ⏳ | Redis cache + Background jobs |
| **8 - Frontend-Ready** | ⏳ | OpenAPI mejorado + Cliente TS + CORS + Rate limiting |

---

## 🚀 Setup en nuevo computador

### 1. Prerrequisitos
```bash
# .NET 8 SDK
dotnet --version  # Debe ser 8.0.x

# Docker Desktop (para PostgreSQL)
docker --version

# Git
git --version
```

### 2. Clonar y restaurar
```bash
cd <tu-carpeta-proyectos>
git clone <repo-url>  # o copiar la carpeta completa
cd Manejo\ de\ Inventario/InventoryManagement.Api

# Restaurar paquetes
dotnet restore

# Verificar build
dotnet build
```

### 3. Levantar PostgreSQL
```bash
cd ..  # Raíz del proyecto (donde está docker-compose.yml)
docker-compose up -d

# Verificar
docker ps
# Debe salir: inventory-postgres  (puerto 5432)
```

### 4. Aplicar migraciones
```bash
cd InventoryManagement.Api
dotnet ef database update

# Verificar
dotnet ef migrations list
```

### 5. Ejecutar API
```bash
dotnet run --urls "http://localhost:5263"

# Swagger UI: http://localhost:5263/swagger
```

---

## 📁 Estructura actual del proyecto

```
Manejo de Inventario/
├── docker-compose.yml                 # PostgreSQL 16
├── InventoryManagement.sln
├── BACKEND_TODO.md                    # ✅ Checklist actualizado
├── EF_CORE_IMPLEMENTATION.md          # ✅ Docs técnico completo (Fase 0 + 1)
├── HANDOFF.md                         # 👉 ESTE ARCHIVO
└── InventoryManagement.Api/
    ├── InventoryManagement.Api.csproj
    ├── Program.cs                     # ✅ Minimal APIs completas
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── Common/
    │   └── ApiResponse.cs             # ApiResponse<T>, PagedResult<T>
    ├── DTOs/
    │   ├── CategoryDtos.cs            # CategoryDto, Create/Update, CategoryRefDto
    │   ├── ProductDtos.cs             # ProductDto, ProductListDto, Create/Update, UpdateStock, ProductFilterDto
    │   └── StockMovementDtos.cs       # CreateStockMovementDto, StockMovementDto, StockMovementFilterDto, KardexSummaryDto
    ├── Validators/
    │   ├── CategoryValidators.cs      # Create/Update validators
    │   ├── ProductValidators.cs       # Create/Update/Stock/Filter validators (con validación BD)
    │   └── StockMovementValidators.cs # CreateStockMovementValidator, StockMovementFilterValidator
    ├── Mapping/
    │   └── MapsterConfig.cs           # Config Mapster completa (Entity↔DTO)
    ├── Models/
    │   ├── Category.cs                # Entidad + navegación Products
    │   ├── Product.cs                 # Entidad + FK CategoryId + navegación Category
    │   ├── MovementType.cs            # enum: Entrada=1, Salida=2, Ajuste=3
    │   ├── StockMovement.cs           # Entidad Kardex (trazabilidad contable)
    │   └── InventoryDbContext.cs      # DbContext + Fluent API + Seed data
    └── Migrations/
        ├── 20261019145915_InitialCreate.cs
        ├── 20261019214749_AddStockMovements.cs
        └── InventoryDbContextModelSnapshot.cs
```

---

## ✅ Qué YA funciona (verificado)

### Endpoints probados

```bash
# Categorías
GET    /api/categories                    # Lista (solo activas)
GET    /api/categories/{id}               # Por ID
POST   /api/categories                    # Crear (valida nombre único)
PUT    /api/categories/{id}               # Actualizar
DELETE /api/categories/{id}               # Soft delete (IsActive=false)

# Productos
GET    /api/products                      # Paginado + filtros + orden
        ?page=1&pageSize=20&categoryId=1&isActive=true
        &search=laptop&minPrice=10&maxPrice=100
        &lowStockOnly=false&sortBy=price&sortDesc=true
GET    /api/products/{id}                 # Completo (con Category, Margin%, IsLowStock)
GET    /api/products/low-stock            # Solo stock ≤ MinStockLevel
POST   /api/products                      # Crear (valida SKU único, CategoryId existe, precio>0, etc)
PUT    /api/products/{id}                 # Actualizar completo
PATCH  /api/products/{id}/stock           # Actualizar solo stock { "quantity": 10 }
DELETE /api/products/{id}                 # Soft delete

# Stock Movements / Kardex (Fase 3) ✅
POST   /api/products/{id}/movements       # Registrar movimiento (Entrada/Salida/Ajuste)
GET    /api/products/{id}/kardex          # Historial paginado + filtros
        ?page=1&pageSize=50&type=Salida&dateFrom=2026-01-01&reference=FV-
GET    /api/products/{id}/kardex/summary  # Resumen valorado (para contador)

# Auth / Users (Fase 4) ✅
POST   /api/auth/login                    # Login → JWT + RefreshToken
POST   /api/auth/refresh                  # Renovar access token (rotación)
POST   /api/auth/logout                   # Revocar refresh token
GET    /api/auth/me                       # Info usuario actual
POST   /api/auth/change-password          # Cambiar password (revoca tokens)
GET    /api/users                         # Listar usuarios (Admin, paginado)
GET    /api/users/{id}                    # Ver usuario (Admin)
POST   /api/users                         # Crear usuario (Admin, valida RUT)
PUT    /api/users/{id}                    # Actualizar usuario (Admin)
DELETE /api/users/{id}                    # Soft delete (Admin)
```

### Respuestas estandarizadas

```json
// Éxito (200/201)
{
  "success": true,
  "data": { "id": 1, "name": "...", "sku": "...", "price": 100, ... },
  "message": "Producto creado",
  "errors": [],
  "meta": null
}

// Lista paginada
{
  "success": true,
  "data": {
    "data": [...],
    "totalCount": 25,
    "page": 1,
    "pageSize": 20,
    "totalPages": 2,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "message": null,
  "errors": [],
  "meta": {
    "pagination": { "totalCount": 25, "page": 1, "pageSize": 20, "totalPages": 2, ... }
  }
}

// Error validación (400) - RFC 7807
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["El nombre es obligatorio"],
    "SKU": ["El SKU solo puede contener letras mayúsculas, números y guiones"],
    "Price": ["El precio debe ser mayor a 0"],
    "CategoryId": ["La categoría seleccionada no existe"]
  }
}

// No encontrado (404) - RFC 7807 ProblemDetails
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Recurso no encontrado",
  "status": 404,
  "detail": "Category con clave '999' no encontrado",
  "instance": "/api/categories/999",
  "correlationId": "0HNPE4RR2A1MD:00000001",
  "resourceName": "Category",
  "resourceKey": 999
}

// Error interno (500) - RFC 7807 ProblemDetails
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Error interno del servidor",
  "status": 500,
  "detail": "Ocurrió un error inesperado. Por favor intente más tarde.",
  "instance": "/api/products",
  "correlationId": "0HNPE4RR2A1MD:00000002"
}
```

### Validaciones implementadas

| Regla | Dónde |
|-------|-------|
| Nombre obligatorio, ≤100/200 chars, solo letras/números/espacios/guiones/apóstrofes | Category/Product validators |
| Descripción ≤500/1000 chars | Validators |
| SKU: formato `^[A-Z0-9\-]+$` + **único en BD** | CreateProductValidator / UpdateProductValidator |
| Precio > 0, ≤ 999,999.99 | Validators |
| Costo ≥ 0 | Validators |
| Stock ≥ 0, MinStockLevel ≥ 0 | Validators |
| UnitOfMeasure ≤ 50 chars | Validators |
| CategoryId > 0 + **existe en BD** | MustAsync en validators |
| Página > 0, PageSize 1-100 | ProductFilterValidator |
| MinPrice ≤ MaxPrice | ProductFilterValidator |

---

## 🔑 Archivos clave para entender el código

| Archivo | Qué hace |
|---------|----------|
| `Program.cs` | **Punto de entrada** - DI, pipeline, TODOS los endpoints |
| `Models/InventoryDbContext.cs` | DbContext + Fluent API + Seed data |
| `Mapping/MapsterConfig.cs` | Configuración mapeo Entity↔DTO (incluye helpers para nulls) |
| `Common/ApiResponse.cs` | Wrappers `ApiResponse<T>`, `PagedResult<T>` |
| `Validators/ProductValidators.cs` | Validaciones con acceso a BD (SKU único, CategoryId) |
| `DTOs/ProductDtos.cs` | Todos los DTOs de producto (entrada/salida/filtros) |

---

## ⚙️ Paquetes NuGet instalados

```xml
<!-- Core -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.21" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />

<!-- EF Core + PostgreSQL -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.10" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10" />

<!-- Fase 1 -->
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="Mapster" Version="7.4.0" />
<PackageReference Include="Mapster.DependencyInjection" Version="1.0.1" />

<!-- Fase 2 -->
<PackageReference Include="Serilog.AspNetCore" Version="8.0.1" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
<PackageReference Include="Serilog.Enrichers.CorrelationId" Version="3.0.1" />
<PackageReference Include="Serilog.Enrichers.Environment" Version="3.0.1" />
<PackageReference Include="Serilog.Enrichers.Process" Version="3.0.0" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="3.0.0" />
<PackageReference Include="Serilog.Formatting.Compact" Version="2.0.0" />
```

---

## 🔧 Comandos útiles

```bash
# Compilar
dotnet build

# Ejecutar (dev)
dotnet run --urls "http://localhost:5263"

# Migraciones
dotnet ef migrations add NombreMigracion
dotnet ef database update
dotnet ef migrations remove          # Revertir última
dotnet ef migrations list
dotnet ef migrations script          # Generar SQL

# Docker
docker-compose up -d                 # Levantar PostgreSQL
docker-compose down                  # Bajar
docker-compose logs -f postgres      # Ver logs

# Limpieza
dotnet clean
rm -rf bin obj
dotnet restore
dotnet build
```

---

## 🎯 PRÓXIMO PASO: Fase 2 - Error Handling + Logging

### Qué implementar

| Tarea | Detalle |
|-------|---------|
| **1. ExceptionHandlingMiddleware** | Middleware global que captura excepciones y devuelve ProblemDetails RFC 7807 |
| **2. Excepciones de dominio** | `NotFoundException`, `BusinessRuleException`, `ConcurrencyException`, `ValidationException` |
| **3. ProblemDetails unificado** | 404, 409 (conflicto), 422, 500 todos con mismo formato |
| **4. Serilog** | Console + File (rolling), JSON output, Correlation ID (RequestId) |
| **5. Log queries lentas** | Interceptor EF Core o `DbCommandInterceptor` con umbral configurable |

### Estructura sugerida para Fase 2

```
InventoryManagement.Api/
├── Common/
│   ├── ApiResponse.cs
│   └── Exceptions/
│       ├── NotFoundException.cs
│       ├── BusinessRuleException.cs
│       ├── ConcurrencyException.cs
│       └── ValidationException.cs
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
├── Logging/
│   └── EfCoreLoggingInterceptor.cs    # Para queries lentas
└── Program.cs                          # Registrar middleware + Serilog
```

### Fase 2 - IMPLEMENTADA ✅

#### Código agregado:

**Excepciones de dominio** (`Common/Exceptions/`):
- `NotFoundException` - 404 con resourceName/key
- `BusinessRuleException` - 409 Conflict
- `ConcurrencyException` - 409 Conflict  
- `ValidationException` - 400 con diccionario de errores

**Middleware global** (`Middleware/ExceptionHandlingMiddleware.cs`):
- Captura TODAS las excepciones
- Mapea a ProblemDetails RFC 7807 unificado
- Incluye `correlationId` (RequestId) en todos los errores
- Loggea excepción completa con contexto

**Serilog configurado** (`Program.cs` + `appsettings.json`):
- Console (formato legible) + File (JSON compacto, rolling daily)
- Enriquecedores: CorrelationId, MachineName, ProcessId, ThreadId, Environment
- Request logging con correlationId automático

**Slow Query Interceptor** (`Logging/EfCoreSlowQueryInterceptor.cs`):
- Umbral configurable (`EfCore:SlowQueryThresholdMs`: 500ms prod, 200ms dev)
- Loggea query + parámetros + duración + contexto

#### Estructura final:
```
InventoryManagement.Api/
├── Common/Exceptions/          # ✅ FASE 2
│   ├── NotFoundException.cs
│   ├── BusinessRuleException.cs
│   ├── ConcurrencyException.cs
│   └── ValidationException.cs
├── Middleware/                 # ✅ FASE 2
│   └── ExceptionHandlingMiddleware.cs
├── Logging/                    # ✅ FASE 2
│   └── EfCoreSlowQueryInterceptor.cs
```

---

## 📋 Checklist rápido para continuar

- [x] Clonar/copiar proyecto en nuevo computador
- [x] `dotnet restore` + `dotnet build` → ✅ compila
- [x] `docker-compose up -d` → PostgreSQL corriendo
- [x] `dotnet ef database update` → BD sincronizada
- [x] `dotnet run` → API en http://localhost:5263/swagger
- [x] Probar endpoints básicos (GET /api/categories, GET /api/products)
- [x] **Fase 2 COMPLETA**: ExceptionHandlingMiddleware + Serilog + ProblemDetails RFC 7807
- [ ] **Iniciar Fase 3**: StockMovement entity, Kardex, movimientos entrada/salida

---

## 📚 Documentación de referencia

| Archivo | Qué contiene |
|---------|--------------|
| `BACKEND_TODO.md` | Checklist completo por fases (actualizado) |
| `EF_CORE_IMPLEMENTATION.md` | Docs técnico detallado (Fase 0 + 1 con código) |
| `HANDOFF.md` | 👉 Este archivo |

---

## 💡 Notas para sub-agentes

Si usas sub-agentes en el nuevo computador:

1. **Explorar código**: `grep -r "FluentValidation" --include="*.cs"` para ver validators
2. **Entender Mapster**: `Mapping/MapsterConfig.cs` tiene toda la config
3. **Endpoints**: Todo en `Program.cs` (Minimal APIs, no Controllers)
4. **BD**: Entidades en `Models/`, DbContext con Fluent API
5. **Tests**: No hay tests aún (Fase 5)

---

## 🆘 Troubleshooting común

| Problema | Solución |
|----------|----------|
| `dotnet ef` no encontrado | `dotnet tool install --global dotnet-ef` |
| Puerto 5432 ocupado | Cambiar en `docker-compose.yml` o matar proceso |
| Migración falla | `dotnet ef migrations remove` + `dotnet ef migrations add Nombre` |
| Build falla por archivo bloqueado | `taskkill /F /IM InventoryManagement.Api.exe` (Windows) |
| SKU no mapea | Ver `MapsterConfig.cs` - mapeo explícito para SKU/Description/etc |

---

**¿Listo para Fase 2?** El código base está sólido, probado y documentado. ¡A por el Error Handling global! 🚀