# Prueba Sub Agentica - InventoryManagement.Api

> **Fecha:** 19/10/2026  
> **Propósito:** Demostrar el concepto de subagents definidos en `opencode.json`  
> **Ejecutado por:** Subagent `planner` (simulado por el AI assistant)

---

## 🤖 Concepto de Subagents en este Proyecto

Los subagents **no son procesos separados** ni requieren CLI externa. Son **roles especializados** que el AI assistant asume según la tarea:

| Subagent | Rol | Cuándo se activa |
|----------|-----|------------------|
| `implementer` | Escribe código, crea archivos, hace `dotnet build` | "Implementa X", "Crea Y" |
| `tester` | Crea/ejecuta tests, coverage, TestContainers | "Testea esto", "Cobertura" |
| `planner` | Descompone features, actualiza docs, prioriza | "Planifica Fase 3", "Actualiza TODO" |
| `reviewer` | Audita código: N+1, índices, seguridad, migraciones | "Revisa código", "Code review" |
| `devops` | Docker, CI/CD, health checks, observabilidad | "Configura CI/CD", "Dockerfile" |

---

## 📋 Workflow Definido en opencode.json

```json
"workflows": {
  "new-feature": {
    "description": "Flujo completo para nueva feature",
    "steps": [
      { "task": "phase-3-stock-movements", "subagent": "planner" },
      { "task": "phase-3-stock-movements", "subagent": "implementer" },
      { "task": "run-tests", "subagent": "tester" },
      { "task": "code-review", "subagent": "reviewer" },
      { "task": "update-docs", "subagent": "planner" }
    ]
  },
  "phase-2-complete": {
    "description": "Completar Fase 2 end-to-end",
    "steps": [
      { "task": "phase-2-error-handling", "subagent": "implementer" },
      { "task": "run-tests", "subagent": "tester" },
      { "task": "code-review", "subagent": "reviewer" },
      { "task": "update-docs", "subagent": "planner" }
    ]
  }
}
```

---

## ✅ Lo Que Se Ejecutó Realmente (Fase 2)

Cuando pediste **"haz las 5 cosas que tiene ahí"**, el AI assistant (yo) ejecutó secuencialmente:

### 1. **Planner** (análisis inicial)
- Leyó `HANDOFF.md`, `BACKEND_TODO.md`, `EF_CORE_IMPLEMENTATION.md`
- Identificó: Fase 2 = siguiente paso

### 2. **Implementer** (código)
- Creó `Common/Exceptions/*.cs` (4 excepciones)
- Creó `Middleware/ExceptionHandlingMiddleware.cs`
- Instaló 8 paquetes Serilog via `dotnet add package`
- Creó `Logging/EfCoreSlowQueryInterceptor.cs`
- Actualizó `Program.cs` + `appsettings.json` + `appsettings.Development.json`
- Corrigió connection strings
- `dotnet build` ✅

### 3. **DevOps** (infraestructura)
- Creó `.env` con credenciales PostgreSQL
- `docker-compose up -d` → BD levantada
- Instaló .NET 8 Runtime + ASP.NET Core 8 Runtime
- `dotnet run` → API en http://localhost:5263

### 4. **Tester** (verificación manual)
- `GET /api/categories` → 200 OK
- `GET /api/products` → 200 OK (paginado)
- `GET /api/categories/999` → 404 ProblemDetails + correlationId
- `POST /api/categories` (inválido) → 400 ValidationProblemDetails
- Verificó logs Serilog con CorrelationId

### 5. **Planner** (documentación)
- Actualizó `HANDOFF.md` (estado, estructura, paquetes, checklist)
- Actualizó `BACKEND_TODO.md` (Fase 2 ✅, próximo → Fase 3)
- Actualizó `EF_CORE_IMPLEMENTATION.md` (sección completa Fase 2 con código)

---

## 🔄 Prueba de Invocación Explícita

Si quieres invocar un subagent explícitamente, usa estos patrones:

```bash
# En este chat (no CLI):
> "Planifica la Fase 3"           → Activa planner
> "Implementa StockMovement"      → Activa implementer  
> "Haz code review"               → Activa reviewer
> "Configura GitHub Actions"      → Activa devops
> "Escribe tests de integración"  → Activa tester
```

---

## 📁 Archivos Creados/Modificados en Esta Prueba

```
InventoryManagement/
├── opencode.json                    # Definición de subagents + tasks + workflows
├── PRUEBA_SUB_AGENTICA.md           # ← ESTE ARCHIVO (creado por planner)
├── HANDOFF.md                       # Actualizado por planner
├── BACKEND_TODO.md                  # Actualizado por planner
├── EF_CORE_IMPLEMENTATION.md        # Actualizado por planner
├── .env                             # Creado por devops
└── InventoryManagement.Api/
    ├── Common/Exceptions/           # Creado por implementer
    │   ├── NotFoundException.cs
    │   ├── BusinessRuleException.cs
    │   ├── ConcurrencyException.cs
    │   └── ValidationException.cs
    ├── Middleware/
    │   └── ExceptionHandlingMiddleware.cs  # Creado por implementer
    ├── Logging/
    │   └── EfCoreSlowQueryInterceptor.cs   # Creado por implementer
    ├── appsettings.json             # Actualizado por implementer
    ├── appsettings.Development.json # Actualizado por implementer
    └── Program.cs                   # Actualizado por implementer
```

---

## 🎯 Conclusión

**Los subagents SÍ funcionan** - pero como **roles cognitivos del AI assistant**, no como procesos externos.

| Mito | Realidad |
|------|----------|
| "Necesito instalar CLI" | ❌ Ya estás DENTRO de OpenCode (este chat) |
| "Se ejecutan solos" | ❌ El AI los "encarna" según tu pedido |
| "Son magic" | ✅ Son prompts especializados + herramientas (read/write/shell/glob/grep) |

---

## 🚀 Próximo Uso Real

Para **Fase 3 - Stock Movements**:

```
Tú: "Inicia Fase 3"
Yo (planner): Descompone en tareas atómicas → checklist
Yo (implementer): Entity + Migration + DTOs + Validators + Endpoints + Mapster
Yo (tester): Tests unitarios + integración (cuando existan)
Yo (reviewer): N+1 queries, índices, concurrencia
Yo (planner): Actualiza 3 archivos .md
```

---

*Documento generado por subagent `planner` como demostración del sistema de agentes de OpenCode.*