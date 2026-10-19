# Backend TODO - InventoryManagement.Api

> **Objetivo:** Backend completo y robusto para que el frontend solo "llegue y conecte"  
> **Estado actual:** ✅ Fase 0, 1 y 2 completadas (EF Core + DTOs/Validación + Error Handling + Logging)

---

## ✅ YA HECHO (Fase 0 - Fundación)

- [x] Proyecto ASP.NET Core 8.0 Web API
- [x] Entity Framework Core 8.0 + Npgsql (PostgreSQL)
- [x] Entidades: `Category`, `Product` con anotaciones y navegación
- [x] `InventoryDbContext` con Fluent API + índices + FKs
- [x] Seed data (4 categorías, 6 productos)
- [x] Migración inicial `InitialCreate`
- [x] Connection strings en appsettings (Dev/Prod)
- [x] Migración automática en `Program.cs` (Development)
- [x] Endpoints CRUD completos (Minimal APIs)
- [x] Filtros, paginación, búsqueda en GET productos
- [x] Endpoint especial `/low-stock`
- [x] Soft delete (IsActive) en ambas entidades
- [x] Docker Compose para PostgreSQL
- [x] Documentación técnica (`EF_CORE_IMPLEMENTATION.md`)

---

## ✅ FASE 1 - Validación y DTOs (Prioridad ALTA) - **COMPLETADA**

### 1.1 DTOs (Data Transfer Objects)
- [x] Carpeta `DTOs/` creada
- [x] `CategoryDto` (salida) + `CategoryRefDto` (ligera)
- [x] `CreateCategoryDto` / `UpdateCategoryDto` (entrada)
- [x] `ProductDto` (salida completa con Category, IsLowStock, MarginPercentage)
- [x] `ProductListDto` (versión ligera para listados)
- [x] `CreateProductDto` / `UpdateProductDto` (entrada)
- [x] `UpdateStockDto` (actualización parcial de stock)
- [x] `ProductFilterDto` (filtros avanzados: precio, búsqueda, orden, paginación)
- [x] `PagedResult<T>` genérico para respuestas paginadas
- [x] `ApiResponse<T>` wrapper estándar (success, data, message, errors, meta)

### 1.2 Validación robusta (FluentValidation)
- [x] Paquete `FluentValidation.AspNetCore` instalado
- [x] `CreateCategoryValidator` / `UpdateCategoryValidator`
- [x] `CreateProductValidator` / `UpdateProductValidator` (con validación BD: SKU único, CategoryId existe)
- [x] `UpdateStockValidator`
- [x] `ProductFilterValidator`
- [x] Validaciones: SKU único (formato), precio > 0, stock >= 0, CategoryId existe en BD
- [x] Helper `ValidationProblem()` para devolver `ValidationProblemDetails` (RFC 7807)

### 1.3 Mapeo (Mapster)
- [x] Paquetes `Mapster` + `Mapster.DependencyInjection` instalados
- [x] Configuración en `Mapping/MapsterConfig.cs`
- [x] Mapeos: Entity ↔ DTO, Create/Update DTOs → Entity (ignore Id, timestamps, nav props)
- [x] Helper `MapCategoryRef` para evitar problemas de expression trees con nulls
- [x] Endpoints refactorizados para usar DTOs + `.Adapt<T>()` + `.ProjectToType<T>()`

---

## ✅ FASE 2 - Manejo de Errores y Logging (Prioridad ALTA) - **COMPLETADA**

### 2.1 Excepciones y ProblemDetails
- [x] Middleware global `ExceptionHandlingMiddleware` (`Middleware/ExceptionHandlingMiddleware.cs`)
- [x] Clases: `NotFoundException`, `BusinessRuleException`, `ConcurrencyException`, `ValidationException` (`Common/Exceptions/`)
- [x] RFC 7807 ProblemDetails estándar para 404, 409, 422, 500
- [x] Mapear excepciones a códigos HTTP apropiados
- [x] Correlation ID en todos los errores (RequestId)

### 2.2 Logging estructurado
- [x] Instalar `Serilog.AspNetCore` + `Serilog.Sinks.Console` + `Serilog.Sinks.File`
- [x] Configurar en `Program.cs` + `appsettings.json` / `appsettings.Development.json`
- [x] Correlation ID (request tracing) con `Serilog.Enrichers.CorrelationId`
- [x] Log de queries EF Core lentas - `EfCoreSlowQueryInterceptor` (umbral configurable: 500ms prod / 200ms dev)
- [x] Enriquecedores: MachineName, ProcessId, ThreadId, Environment
- [x] File sink: JSON compacto, rolling daily, retención 30 días (prod) / 7 días (dev)

---

## 🟡 FASE 3 - Funcionalidades de Negocio Core (Prioridad MEDIA-ALTA)

### 3.1 Stock y Movimientos
- [ ] Entidad `StockMovement` (Entrada/Salida/Ajuste/Transferencia)
- [ ] Endpoints: `POST /api/products/{id}/movements`
- [ ] Validación: no permitir salida si stock insuficiente
- [ ] Kardex / Historial de movimientos por producto
- [ ] Endpoint: `GET /api/products/{id}/kardex`

### 3.2 Categorías - Jerarquía (opcional)
- [ ] `ParentCategoryId` nullable en Category (autoreferencia)
- [ ] Endpoint árbol: `GET /api/categories/tree`

### 3.3 Búsqueda y Filtros Avanzados
- [ ] Filtro por rango de precios
- [ ] Filtro por rango de fechas (creación/actualización)
- [ ] Ordenamiento dinámico (?sort=price:desc,name:asc)
- [ ] Búsqueda full-text (PostgreSQL tsvector) - opcional

---

## 🟢 FASE 4 - Seguridad y Autenticación (Prioridad ALTA para prod)

### 4.1 Identity + JWT
- [ ] Instalar `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- [ ] Entidades: `ApplicationUser`, `ApplicationRole`
- [ ] `IdentityDbContext` (heredar o separar)
- [ ] JWT Bearer: `Microsoft.AspNetCore.Authentication.JwtBearer`
- [ ] Endpoints: `/api/auth/register`, `/api/auth/login`, `/api/auth/refresh`
- [ ] Roles: `Admin`, `Manager`, `Viewer`

### 4.2 Autorización en endpoints
- [ ] Policies: `RequireAdmin`, `RequireManagerOrAdmin`
- [ ] `[Authorize(Policy = "...")]` en endpoints sensibles
- [ ] Ownership: usuarios solo ven/modifican su data (si multi-tenant)

### 4.3 Refresh Tokens + Revocación
- [ ] Tabla `RefreshTokens` (hash, expiración, revocado)
- [ ] Rotación de refresh tokens
- [ ] Logout (revocar token actual)

---

## 🔵 FASE 5 - Testing y Calidad (Prioridad MEDIA)

### 5.1 Tests Unitarios
- [ ] xUnit + Moq / NSubstitute
- [ ] Tests de validadores (FluentValidation)
- [ ] Tests de lógica de negocio (servicios)

### 5.2 Tests de Integración
- [ ] `Microsoft.AspNetCore.Mvc.Testing` + `TestContainers` (PostgreSQL real)
- [ ] Tests de endpoints CRUD completos
- [ ] Tests de auth (login, forbidden, unauthorized)
- [ ] Tests de validación y edge cases

### 5.3 Cobertura y CI
- [ ] `coverlet` + `ReportGenerator`
- [ ] GitHub Actions / Azure DevOps pipeline
- [ ] Build + Test + Migration check en PR

---

## 🟣 FASE 6 - Observabilidad y Operaciones (Prioridad MEDIA)

### 6.1 Health Checks
- [ ] `Microsoft.Extensions.Diagnostics.HealthChecks`
- [ ] Check: DB connection, migraciones pendientes, disco, memoria
- [ ] Endpoint: `GET /health` + `/health/ready` + `/health/live`

### 6.2 Métricas
- [ ] `Prometheus` + `OpenTelemetry` (opcional)
- [ ] Contadores: requests, errors, latency, DB queries

### 6.3 Versionado de API
- [ ] `Microsoft.AspNetCore.Mvc.Versioning`
- [ ] URL: `/api/v1/...` o Header `Api-Version`

---

## ⚪ FASE 7 - Performance y Escalabilidad (Prioridad BAJA initially)

### 7.1 Caché
- [ ] `Microsoft.Extensions.Caching.StackExchangeRedis`
- [ ] Cache: categorías (poco cambian), productos por ID
- [ ] Invalidation en CUD operations

### 7.2 Paginación con Cursor (opcional)
- [ ] Para listas muy grandes (>10k)
- [ ] `after` / `before` cursor tokens

### 7.3 Background Jobs
- [ ] `Hangfire` o `Quartz`
- [ ] Reportes programados, alertas stock bajo, limpieza logs

---

## 📦 FASE 8 - Preparación Frontend-Ready (Prioridad ALTA al final)

### 8.1 OpenAPI / Swagger mejorado
- [ ] Documentación XML (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`)
- [ ] Ejemplos en request/response
- [ ] Agrupar por tags, ordenar endpoints
- [ ] `Swashbuckle.AspNetCore.Annotations`

### 8.2 Generación de cliente
- [ ] `dotnet tool install -g Microsoft.dotnet-openapi` / `kiota` / `NSwag`
- [ ] Script para generar cliente TypeScript/JavaScript/C#
- [ ] Publicar spec OpenAPI en artifact CI

### 8.3 CORS y Headers
- [ ] Política CORS configurada para dominio frontend
- [ ] Headers de seguridad (HSTS, CSP, etc.)

### 8.4 Rate Limiting
- [ ] `Microsoft.AspNetCore.RateLimiting`
- [ ] Políticas por IP / usuario / endpoint

---

## 📋 CHECKLIST DE "LISTO PARA FRONTEND"

| Criterio | Done? |
|----------|-------|
| ✅ CRUD completo Categorías/Productos | ✅ |
| ✅ Paginación, filtros, búsqueda | ✅ |
| ✅ Validación robusta (FluentValidation) | ✅ |
| ✅ DTOs separados de Entidades | ✅ |
| ✅ Manejo errores global (ProblemDetails) | ✅ (ValidationProblem) |
| ✅ Auth JWT + Roles | ⬜ |
| ✅ Health checks | ⬜ |
| ✅ OpenAPI completo con ejemplos | ⬜ |
| ✅ Cliente generable (TypeScript) | ⬜ |
| ✅ Tests integración pasando | ⬜ |
| ✅ CI/CD pipeline verde | ⬜ |
| ✅ Dockerfile multi-stage para deploy | ⬜ |

---

## 🎯 PRÓXIMO PASO RECOMENDADO

**Iniciar Fase 3:** Stock Movements (Kardex, movimientos entrada/salida)

Incluiría:
1. Entidad `StockMovement` (Tipo: Entrada/Salida/Ajuste/Transferencia, Quantity, Reason, Reference, UserId, Timestamp)
2. Endpoint `POST /api/products/{id}/movements` con validación: no salida si stock insuficiente
3. Endpoint `GET /api/products/{id}/kardex` (historial paginado)
4. Actualizar `Product.StockQuantity` automáticamente al crear movimiento
5. Validaciones FluentValidation para `CreateStockMovementDto`
6. DTOs y Mapster config
7. Migración EF Core

¿Quieres continuar con la **Fase 3**?