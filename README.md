# Sazón Caleño: sistema de órdenes para restaurante

Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026-B

**Integrantes:** Samuel Darío Giraldo P. · Angel David Gómez G.

Sistema para un restaurante pequeño: los meseros toman pedidos desde el celular o la tablet, la cocina los atiende
en orden de llegada, la caja cobra con impuesto y propina, y el administrador gestiona el menú, las mesas, los usuarios
y los reportes de ventas. La **aplicación web** y la **aplicación de escritorio** comparten la misma biblioteca de
negocio y la misma base de datos **PostgreSQL en la nube**.

**App web publicada:** https://sazon-caleno.onrender.com (PC, tablet y celular, desde cualquier red).
> El plan gratuito de Render duerme el servidor tras 15 min sin uso: la primera visita puede tardar ~1 minuto.

## Arquitectura

```
Restaurante.slnx
├── src/
│   ├── Restaurante.Core       Biblioteca de clases (dominio + reglas de negocio + datos)
│   │   ├── Dominio/           Entidades: Mesa, Producto, Orden, DetalleOrden, Factura, enums, excepción de negocio
│   │   ├── Datos/             RestauranteDbContext (EF Core + Identity), repositorio genérico, migraciones, datos iniciales
│   │   ├── Servicios/         ServicioMenu, ServicioMesas, ServicioOrdenes, ServicioFacturacion, ServicioReportes
│   │   └── Seguridad/         Usuario (IdentityUser), Roles, ServicioAutenticacion (para escritorio)
│   ├── Restaurante.Web        ASP.NET Core MVC + Identity + Bootstrap 5 (responsive)
│   └── Restaurante.Desktop    Windows Forms: cocina, caja y reportes
├── tools/
│   └── Restaurante.Comparativas   Consola que mide las estructuras de datos elegidas frente a sus alternativas
└── docs/
    └── ESTRUCTURAS_DE_DATOS.md    Justificación y resultados de las comparativas
```

```
   Web MVC (meseros, admin)        Escritorio WinForms (cocina, caja)
             │                                   │
             └──────────► Restaurante.Core ◄─────┘
                         (dominio + servicios)
                                  │  EF Core (Npgsql)
                                  ▼
             PostgreSQL en la nube (Neon) ── web publicada en Render (Docker)
```

**Base de datos.** `ConexionBaseDatos` elige el proveedor según la cadena de conexión:
- **PostgreSQL (Neon)** si hay cadena: variable de entorno `SAZON_DB`, `ConnectionStrings:Restaurante` en
  `appsettings.json`, o un archivo `conexion.txt` junto al ejecutable. Acepta la URL que entrega Neon
  (`postgresql://usuario:clave@servidor/base?sslmode=require`). Se aplican las migraciones de EF Core.
- **SQLite local** (`%LOCALAPPDATA%\RestauranteApp\restaurante.db`) si no hay cadena: modo sin internet;
  el esquema se crea desde el modelo.

Así la web en la nube, la web local y el escritorio en cualquier PC ven los mismos pedidos en tiempo real.

## Tecnologías
- .NET 10 / C# 14
- ASP.NET Core MVC con ASP.NET Core Identity (inicio de sesión y roles)
- Entity Framework Core 10 con PostgreSQL (Npgsql, Neon) y migraciones; SQLite como respaldo local
- Docker + Render para publicar la web
- Windows Forms (.NET 10)
- Bootstrap 5 y Bootstrap Icons (diseño responsive para celular, tablet y PC)

## Roles

| Rol | Web | Escritorio |
|---|---|---|
| **Admin** | Todo: tablero, órdenes, mesas, caja, cocina, menú, reportes, usuarios | Cocina, Caja, Reportes |
| **Mesero** | Tomar órdenes, ver el plano de mesas, entregar, cobrar | Caja |
| **Cocina** | Cola de cocina (tomar siguiente, marcar lista) | Cocina |

Usuarios de prueba (se crean solos la primera vez que se ejecuta cualquiera de las dos apps):

| Correo | Contraseña | Rol |
|---|---|---|
| admin@restaurante.com | Admin123* | Admin |
| mesero@restaurante.com | Mesero123* | Mesero |
| mesera@restaurante.com | Mesero123* | Mesero |
| cocina@restaurante.com | Cocina123* | Cocina |

## Flujo de una orden

```
Pendiente ──► En preparación ──► Lista ──► Entregada ──► Pagada
    │
    └──► Cancelada
```
- **Mesero (web):** crea la orden para una mesa libre → la mesa queda *Ocupada*.
- **Cocina (web o escritorio):** "Tomar siguiente" saca la orden más antigua de la cola (FIFO) → *Lista*.
- **Mesero:** la entrega en la mesa → *Entregada*.
- **Caja:** cobra (impoconsumo 8 % + propina voluntaria 10 %) → se genera la factura, la orden pasa a *Pagada* y la mesa se libera.

Cualquier transición inválida (por ejemplo cobrar una orden que aún está en cocina) la bloquea el dominio con una `ReglaNegocioException`.

## Cómo ejecutar

### Opción 0: en la nube (nada que instalar)
Abra https://sazon-caleno.onrender.com en el PC, la tablet o el celular. El escritorio del paquete de Releases
ya viene conectado a la misma base de datos en la nube.

### Opción A: ejecutables listos (no requiere instalar .NET)
1. Descargue `SazonCaleno-App.zip` desde [Releases](https://github.com/giraldosamuel005-jpg/Sazon-cale-o-miniproyecto2/releases) y descomprímalo (Windows 10/11 de 64 bits).
2. **Web:** doble clic en `Abrir app web.bat` (no cierre la ventana negra; el navegador se abre en `http://localhost:5186`).
3. **Escritorio:** doble clic en `Abrir app escritorio.bat`.
4. **Celular/tablet:** `Abrir app web (celular por WiFi).bat` y abra en el celular la dirección que muestra la ventana.

Si aparece "Windows protegió su PC": *Más información* → *Ejecutar de todas formas*.

### Opción B: desde el código fuente
Requisitos: SDK de .NET 10 y Windows (para la app de escritorio).

```bash
# Web (http://localhost:5186)
dotnet run --project src/Restaurante.Web

# Escritorio
dotnet run --project src/Restaurante.Desktop

# Comparativas de estructuras de datos
dotnet run -c Release --project tools/Restaurante.Comparativas
```

Sin cadena de conexión usa SQLite local; para usar PostgreSQL defina la variable `SAZON_DB` con la URL de Neon.
La base de datos se crea sola con migraciones y datos de ejemplo (10 mesas, 15 productos y una semana de ventas).
En modo local, para empezar de cero borre la carpeta `%LOCALAPPDATA%\RestauranteApp`.

Para ver la web desde el celular en la misma red Wi‑Fi:
```bash
dotnet run --project src/Restaurante.Web --urls http://0.0.0.0:5186
```
y abra `http://<IP-del-PC>:5186` en el celular.

Para crear una nueva migración después de cambiar el modelo (las migraciones son de PostgreSQL):
```bash
dotnet tool restore
SAZON_DB="Host=localhost;Database=x" dotnet dotnet-ef migrations add NombreCambio --project src/Restaurante.Core --startup-project src/Restaurante.Web --output-dir Datos/Migraciones
```

## Características de C# y .NET utilizadas
- **POO:** clase abstracta `EntidadBase` con herencia, encapsulamiento de reglas dentro de `Orden`, propiedades calculadas, sobrescritura de `ToString`, polimorfismo en `UserClaimsPrincipalFactory`.
- **Interfaces y genéricos:** `IRepositorio<T>` / `Repositorio<T> where T : EntidadBase`.
- **Delegados y eventos:** `ServicioOrdenes.EstadoCambiado` (`EventHandler<CambioEstadoEventArgs>`), al que se suscribe el registro de eventos de la cocina en escritorio.
- **LINQ:** consultas a EF Core (`Where`, `Include`, `OrderBy`) y LINQ to Objects (`GroupBy`, `Sum`, `ToDictionary`, `ToHashSet`).
- **async/await** en todos los servicios y en los eventos de WinForms.
- **Records**, `init`/`required`, **pattern matching** (`is not`, `or`, `switch` expressions), **tuplas** (intercambio de fechas en reportes), **métodos de extensión** (`Pesos()`, `Nombre()`), **excepciones personalizadas**, **nullable reference types**, **expresiones lambda** y `Expression<Func<T,bool>>`.
- **Estructuras de datos:** `List`, `Dictionary`, `Queue`, `HashSet`, `SortedDictionary`, `StringBuilder` (ver [docs/ESTRUCTURAS_DE_DATOS.md](docs/ESTRUCTURAS_DE_DATOS.md)).
- **ASP.NET Core:** MVC, inyección de dependencias, filtros globales de autorización, `[Authorize(Roles=…)]`, tag helpers, validación con DataAnnotations, antiforgery, localización `es-CO`.
- **Identity:** `UserManager`, `SignInManager`, roles, claims personalizados y el mismo `PasswordHasher` reutilizado en escritorio.
- **EF Core:** Code First, migraciones, dos proveedores (PostgreSQL y SQLite) con el mismo modelo, llaves de sesión de Data Protection en la base, relaciones 1‑N y 1‑1, índices, conversión de tipos, carga de datos iniciales.
- **WinForms:** controles creados por código, `DataGridView` con enlace de datos, `Timer`, un control propio dibujado con GDI+ (`GraficoBarras`) y formularios modales.
