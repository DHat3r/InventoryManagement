# 📦 HANDOFF - InventoryManagement.Api

> **Proyecto:** Backend ASP.NET Core 8.0 para Manejo de Inventario  
> **Estado actual:** ✅ Fase 0 y Fase 1 completadas y probadas  
> **Próximo:** Fase 2 - Error Handling Global + Logging (Serilog)  
> **Fecha:** 19/10/2026

---

## 🎯 Resumen rápido

| Fase | Estado | Qué incluye |
|------|--------|-------------|
| **0 - Fundación** | ✅ | EF Core + PostgreSQL + Entidades + Migración + CRUD básico |
| **1 - DTOs/Validación/Mapeo** | ✅ | FluentValidation + Mapster + DTOs + ApiResponse + ProblemDetails |
| **2 - Error Handling + Logging** | 🔄 **SIGUIENTE** | Middleware global + Excepciones dominio + Serilog + Correlation ID |
| **3 - Stock Movements** | ⏳ | StockMovement entity, Kardex, movimientos entrada/salida |
| **4 - Auth JWT** | ⏳ | Identity + JWT + Roles + Refresh tokens |
| **5 - Tests** | ⏳ | xUnit + TestContainers + Integration tests |
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
    │   └── ProductDtos.cs             # ProductDto, ProductListDto, Create/Update, UpdateStock, ProductFilterDto
    ├── Validators/
    │   ├── CategoryValidators.cs      # Create/Update validators
    │   └── ProductValidators.cs       # Create/Update/Stock/Filter validators (con validación BD)
    ├── Mapping/
    │   └── MapsterConfig.cs           # Config Mapster completa (Entity↔DTO)
    ├── Models/
    │   ├── Category.cs                # Entidad + navegación Products
    │   ├── Product.cs                 # Entidad + FK CategoryId + navegación Category
    │   └── InventoryDbContext.cs      # DbContext + Fluent API + Seed data
    └── Migrations/
        ├── 20261019145915_InitialCreate.cs
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

// No encontrado (404)
{
  "success": false,
  "data": null,
  "message": "Producto no encontrado",
  "errors": ["Producto no encontrado"],
  "meta": null
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

### Paquetes a instalar para Fase 2

```bash
dotnet add package Serilog.AspNetCore --version 8.0.1
dotnet add package Serilog.Sinks.Console --version 6.0.0
dotnet add package Serilog.Sinks.File --version 6.0.0
dotnet add package Serilog.Enrichers.CorrelationId --version 3.0.1
```

---

## 📋 Checklist rápido para continuar

- [ ] Clonar/copiar proyecto en nuevo computador
- [ ] `dotnet restore` + `dotnet build` → ✅ compila
- [ ] `docker-compose up -d` → PostgreSQL corriendo
- [ ] `dotnet ef database update` → BD sincronizada
- [ ] `dotnet run` → API en http://localhost:5263/swagger
- [ ] Probar endpoints básicos (GET /api/categories, GET /api/products)
- [ ] **Iniciar Fase 2**: ExceptionHandlingMiddleware + Serilog

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