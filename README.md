# InventoryManagement

[![CI](https://github.com/DHat3r/InventoryManagement/actions/workflows/ci.yml/badge.svg)](https://github.com/DHat3r/InventoryManagement/actions/workflows/ci.yml)

> API REST de gestión de inventario para pymes: catálogo de productos, movimientos de stock con kardex valorizado y control de acceso según el rol de cada persona (Admin, Inventario, Contador, Comprador).

![Endpoints de la API en Swagger UI](docs/swagger.png)

## Funcionalidades

- **Catálogo:** categorías jerárquicas (con vista de árbol) y productos, con búsqueda, filtros por precio y fecha, orden por varios campos y paginación.
- **Stock y kardex:** entradas, salidas y ajustes; historial de movimientos por producto y resumen valorizado. No permite una salida mayor al stock disponible.
- **Alertas:** listado de productos bajo su stock mínimo.
- **Autenticación:** JWT con refresh tokens que rotan en cada uso y se pueden revocar (logout, cambio de contraseña).
- **Roles y permisos:** 4 roles de negocio; cada endpoint exige el rol que corresponde.
- **Auditoría:** cada registro guarda quién lo creó y quién lo modificó; cada movimiento, quién lo registró.
- **Usuarios:** administración de usuarios con validación de RUT chileno.
- **Operación:** health checks, límite de peticiones por minuto, logs estructurados en JSON y errores en formato estándar (RFC 7807).

### Permisos por rol

Se separan dos tipos de dinero: el **valor de ingreso** (costo de compra), que Inventario necesita para revisar lo que recibe, y el **valor de venta** (precio y margen), que es información comercial. Inventario no ve el segundo, para evitar fugas de datos y que se confunda con el costo.

| Acción | Admin | Inventario | Contador | Comprador |
|--------|:-----:|:----------:|:--------:|:---------:|
| Ver productos, stock y costo | ✅ | ✅ | ✅ | — |
| Ver precio de venta y margen (y filtrar u ordenar por precio) | ✅ | — | ✅ | — |
| Crear y editar productos y categorías | ✅ | ✅ | — | — |
| Eliminar productos y categorías | ✅ | — | — | — |
| Registrar entradas | ✅ | ✅ | — | ✅ |
| Registrar salidas y ajustes | ✅ | ✅ | — | — |
| Ver kardex (historial con costo unitario) | ✅ | ✅ | ✅ | — |
| Ver kardex valorizado (resumen) | ✅ | — | ✅ | — |
| Administrar usuarios | ✅ | — | — | — |

Las reglas se aplican en la API (por ejemplo, el precio llega en `null` a Inventario); el panel web solo oculta lo que cada rol no puede usar.

### Endpoints principales

| Recurso | Endpoints |
|---------|-----------|
| Auth | `POST /api/auth/login` · `refresh` · `logout` · `change-password` · `GET /api/auth/me` |
| Categorías | CRUD en `/api/categories` · `GET /api/categories/tree` |
| Productos | CRUD en `/api/products` · `GET /low-stock` · `PATCH /{id}/stock` |
| Kardex | `POST /api/products/{id}/movements` · `GET /{id}/kardex` · `GET /{id}/kardex/summary` |
| Usuarios | CRUD en `/api/users` (solo Admin) |
| Salud | `GET /health` · `/health/ready` · `/health/live` |

La documentación completa, con ejemplos, está en Swagger.

## Stack

| Capa | Tecnología |
|------|------------|
| Backend | C# · ASP.NET Core 8 (Minimal APIs) |
| Frontend | React 19 · TypeScript · Vite · React Router |
| Base de datos | PostgreSQL 16 · Entity Framework Core 8 |
| Validación y mapeo | FluentValidation · Mapster |
| Seguridad | JWT Bearer · BCrypt · rate limiting nativo de .NET |
| Observabilidad | Serilog · Health Checks |
| Documentación | Swagger / OpenAPI (Swashbuckle) |
| Tests | xUnit · FluentAssertions · Testcontainers |
| Infraestructura | Docker · Docker Compose |

## Cómo ejecutarlo

Requisitos: [Docker](https://www.docker.com/) y, para la opción A, el [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone https://github.com/DHat3r/InventoryManagement.git
cd InventoryManagement
cp .env.example .env
```

### Opción A: base en Docker, API local (recomendada para desarrollar)

```bash
docker compose up -d
cd InventoryManagement.Api
dotnet run
```

Swagger queda en `http://localhost:5263/swagger`. Al arrancar en modo Development, la API aplica las migraciones y crea el usuario administrador (configurado en `AdminSeed` de `appsettings.Development.json`).

### Opción B: todo en Docker

```bash
docker compose --profile api up -d --build
```

La API queda en `http://localhost:5263`. Al arrancar aplica las migraciones (`DATABASE_MIGRATE_ON_STARTUP=true` en `.env`) y crea el usuario administrador con las variables `ADMIN_*`. Con el `ASPNETCORE_ENVIRONMENT=Production` por defecto, Swagger está desactivado; para verlo, cámbialo a `Development` en `.env`.

> `POSTGRES_PASSWORD`, `JWT_SECRET` y `ADMIN_PASSWORD` son obligatorios: si faltan en `.env`, `docker compose` se detiene con un error. Los valores de `.env.example` son solo para desarrollo; cámbialos en cualquier otro entorno.

### Frontend (panel web)

Con la API corriendo en `http://localhost:5263` (opción A o B), en otra terminal:

```bash
cd frontend
npm install
npm run dev
```

El panel queda en `http://localhost:5173`: login, listado de productos con búsqueda y filtro de stock bajo, y kardex de cada producto con registro de movimientos. En desarrollo, Vite reenvía las peticiones `/api` a la API (proxy), así que no hace falta configurar CORS.

> Vite no funciona si la ruta de la carpeta contiene `#` (por ejemplo `c#/`). En ese caso usa `npm run build` y `npm run preview` (puerto 4173), o clona el repo en una ruta sin `#`.

## Tests

```bash
cd InventoryManagement.Tests
dotnet test
```

Requiere Docker en ejecución. Hay 51 tests:

- **15 unitarios** de los validadores (FluentValidation sobre EF Core InMemory).
- **36 de integración** que levantan la API completa contra un PostgreSQL real en un contenedor (Testcontainers): auth, categorías, productos, movimientos de stock y permisos por rol (Inventario y Contador).

### Integración continua

Cada push a `main` y cada pull request corren [el workflow de CI](.github/workflows/ci.yml):

- **Backend:** build, chequeo de migraciones pendientes (falla si un cambio en las entidades no tiene migración), tests y reporte de cobertura (69,7 % de líneas, sin contar migraciones). El resumen aparece en la página de cada ejecución y el reporte HTML queda como artifact.
- **Frontend:** `npm ci`, lint y build.

## Arquitectura

La API es el único punto de acceso a los datos: cualquier cliente (el panel React de `frontend/`, Swagger o Postman) se comunica con ella por HTTP, y ella es la única que habla con la base de datos.

```mermaid
flowchart LR
    C["Cliente<br/>frontend · Swagger · Postman"] -->|"HTTP + JWT"| M

    subgraph API["InventoryManagement.Api (.NET 8)"]
        M["Middleware<br/>errores · rate limiting · CORS · auth"] --> E["Endpoints<br/>Minimal APIs"]
        E --> V["Validación<br/>FluentValidation"]
        E --> EF["EF Core"]
    end

    EF --> DB[("PostgreSQL 16<br/>contenedor Docker")]
```

Cada petición pasa por estas capas:

1. **Middleware:** captura errores y los devuelve en formato estándar (ProblemDetails), limita las peticiones por minuto y valida el token JWT.
2. **Endpoints:** comprueban que el rol del usuario tenga permiso, reciben DTOs (nunca las entidades directamente) y aplican las reglas de negocio, por ejemplo rechazar una salida si no hay stock suficiente.
3. **Validación:** revisa los datos antes de tocar la base (formato, SKU único, que la categoría exista).
4. **EF Core:** traduce las operaciones a SQL. Un movimiento de stock y la actualización del inventario se guardan en una misma transacción, para que el kardex nunca quede descuadrado.

En desarrollo, PostgreSQL corre en Docker y la API con `dotnet run`; con `docker-compose --profile api` ambos corren en contenedores.

## Decisiones técnicas

- **Minimal APIs y no controladores:** menos código repetitivo y es el estilo moderno de .NET. Para que no crezca todo en `Program.cs`, cada módulo vive en `Endpoints/` (`CategoryEndpoints`, `ProductEndpoints`, `KardexEndpoints`, `AuthEndpoints`, `UserEndpoints`) como un método de extensión `app.MapXxxEndpoints()`; `Program.cs` queda solo con la configuración.
- **JWT propio y no ASP.NET Identity:** el modelo de usuario necesita RUT y roles de negocio propios, e Identity trae muchas tablas y flujos que no se usan. Hacerlo a mano también deja claro cómo funciona cada paso.
- **Refresh tokens guardados como hash (SHA-256):** si alguien leyera la base de datos, no podría usar los tokens. Cada refresh invalida el token anterior.
- **Testcontainers y no InMemory para integración:** InMemory no se comporta como PostgreSQL (transacciones, índices únicos, claves foráneas). Probar contra la base real evita falsos positivos.
- **Cantidad siempre positiva en los movimientos:** el tipo (entrada, salida, ajuste) define el signo, como en contabilidad. Así los reportes no dependen de interpretar números negativos.
- **DTOs separados de las entidades:** la API no expone el modelo de la base, y cambiarlo no rompe a los clientes.
- **Errores en formato ProblemDetails (RFC 7807):** todos los errores tienen la misma forma y un identificador de correlación, lo que facilita manejarlos en un frontend y rastrearlos en los logs.

## Qué mejoraría con más tiempo

- **Migraciones en un despliegue real:** hoy la API migra al arrancar, lo que basta para una instancia. Con varias instancias conviene un paso de migración aparte (por ejemplo, un migration bundle en el pipeline).
- **Más tests:** servicios (auth, movimientos) y endpoints de usuarios.
- **Frontend:** contenedor propio (nginx) en `docker-compose`, tests de componentes y pantallas de categorías y usuarios.
- **Siguiente etapa:** caché con Redis, módulos de proveedores y pedidos y despliegue público con Swagger visible.

## Autor

Leandro · [GitHub](https://github.com/DHat3r)
