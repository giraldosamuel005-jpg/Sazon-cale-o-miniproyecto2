# Estructuras de datos: justificación y comparativas

Cada estructura del sistema se eligió según **la operación que más se repite** en ese punto del negocio.
Las cifras salen del proyecto `tools/Restaurante.Comparativas` (`dotnet run -c Release --project tools/Restaurante.Comparativas`),
medidas con `Stopwatch` (mejor de 5 ejecuciones). Los milisegundos varían según el equipo; lo que importa es la proporción.

## Resumen

| # | Dónde se usa | Operación dominante | Estructura elegida | Alternativas descartadas |
|---|---|---|---|---|
| 1 | `ServicioMenu.ObtenerIndiceAsync`, `ServicioOrdenes.CrearAsync` | Buscar un producto por Id al armar la orden | `Dictionary<int, Producto>` | `List<Producto>.Find` |
| 2 | `ServicioOrdenes.ObtenerColaCocinaAsync` / `TomarSiguienteAsync`, cocina web y escritorio | Atender la orden más antigua primero | `Queue<Orden>` (FIFO) | `List.RemoveAt(0)`, `Stack` |
| 3 | `ServicioOrdenes.CrearAsync` (mesas con orden abierta), `SesionUsuario.Roles` | Preguntar "¿contiene X?" | `HashSet<int>` / `HashSet<string>` | `List.Contains` |
| 4 | `ServicioMenu.ObtenerMenuPorCategoriaAsync`, `ServicioReportes.VentasPorDiaAsync` | Recorrer grupos siempre en el mismo orden | `SortedDictionary<K, V>` | `List` + `GroupBy/OrderBy` en cada consulta |
| 5 | `Orden.Detalles` | Recorrer las líneas en el orden en que se pidieron | `List<DetalleOrden>` | Arreglo, `LinkedList` |
| 6 | `Orden.Transiciones` (máquina de estados) | ¿Puede la orden pasar de A a B? | `Dictionary<EstadoOrden, HashSet<EstadoOrden>>` | `if`/`switch` anidados |
| 7 | `ServicioReportes.ResumenAsync` | Acumular el total por método de pago | `Dictionary<MetodoPago, decimal>` | Tres variables sueltas o `List` de pares |
| 8 | `ReciboForm.Generar` (escritorio) | Concatenar muchas líneas de texto | `StringBuilder` | `string +=` |

## Resultados medidos

| Caso | Estructura | Tiempo (ms) |
|---|---|---:|
| 1. Buscar producto por Id (200.000 búsquedas en 2.000 productos) | `List.Find` O(n) | 380,03 |
| | **`Dictionary` O(1)** ✔ | **1,19** |
| 2. Atender en orden de llegada (50.000 órdenes) | `List.RemoveAt(0)` O(n) | 177,13 |
| | `Stack.Pop` (rápido, pero **orden incorrecto**: LIFO) | 2,67 |
| | **`Queue.Dequeue` O(1)** ✔ | **3,07** |
| 3. ¿Mesa ocupada? (200.000 consultas sobre 5.000 mesas) | `List.Contains` O(n) | 44,87 |
| | **`HashSet.Contains` O(1)** ✔ | **1,38** |
| 4. Pintar la carta por categoría (20.000 veces, 400 productos) | `List` + `GroupBy/OrderBy` cada vez | 86,88 |
| | **`SortedDictionary`** (agrupado y ordenado una sola vez) ✔ | **0,54** |
| 5. Sumar el total de una orden (20 líneas × 200.000) | `LinkedList` | 39,53 |
| | **`List`** ✔ | **38,53** |

## Justificación detallada

### 1. `Dictionary<int, Producto>`: índice del menú
Al crear una orden llegan pares *(ProductoId, cantidad)*. Por cada uno hay que obtener el producto
(precio, disponibilidad). Con una lista, cada búsqueda recorre los elementos (O(n)); con un diccionario,
la búsqueda por llave es O(1) en promedio gracias a la tabla hash. **Resultado: ~320× más rápido.**
Además `TryGetValue` permite validar en una sola operación que el producto existe y está disponible.

### 2. `Queue<Orden>`: cola de cocina
La regla del negocio es *"la primera orden en llegar es la primera en prepararse"*: eso es exactamente FIFO.
- `List.RemoveAt(0)` funciona, pero desplaza todos los elementos en cada extracción (O(n)): **~58× más lento**.
- `Stack` es igual de rápido pero es LIFO: atendería primero a la última mesa que pidió. **Viola la regla**.
- `Queue` ofrece `Peek()` (ver quién sigue sin sacarlo, se usa para el botón "Tomar siguiente · Mesa 5")
  y `Dequeue()` en O(1). Además, el nombre de la estructura documenta la regla del negocio en el código.

### 3. `HashSet`: pertenencia
Antes de abrir una orden se verifica que la mesa no tenga otra abierta; en la sesión de escritorio se pregunta
constantemente si el usuario tiene un rol. `HashSet.Contains` es O(1) y además **impide duplicados**
(un rol no puede aparecer dos veces). **~32× más rápido** que `List.Contains`.

### 4. `SortedDictionary`: datos agrupados que siempre se muestran ordenados
La carta siempre se muestra en el orden Entradas → Platos fuertes → Bebidas → Postres (orden del `enum`).
`SortedDictionary` mantiene las llaves ordenadas al insertar (árbol rojo-negro, O(log n)), así que recorrerlo
ya entrega el orden correcto sin reordenar en cada vista. En reportes, `VentasPorDiaAsync` usa
`SortedDictionary<DateTime, decimal>` para que los días salgan cronológicos en el gráfico, incluso los días sin ventas.
Frente a agrupar y ordenar en cada pintada: **~160× más rápido**.
*¿Por qué no `SortedList`?* Para pocas llaves sería equivalente; `SortedDictionary` inserta en O(log n) sin desplazar
arreglos, que es mejor si las categorías crecieran.

### 5. `List<DetalleOrden>`: líneas de la orden
Una orden tiene pocas líneas (normalmente < 30) que se recorren completas y **en el orden en que se pidieron**
(así se imprimen en la factura). `List` guarda los elementos en memoria contigua, crece sola y es lo que EF Core
espera para colecciones de navegación. `LinkedList` no aporta nada (no insertamos en medio) y usa más memoria por nodo;
un arreglo no puede crecer cuando el mesero agrega productos.

### 6. Máquina de estados con `Dictionary<EstadoOrden, HashSet<EstadoOrden>>`
```csharp
[EstadoOrden.Pendiente]     = { EnPreparacion, Cancelada },
[EstadoOrden.EnPreparacion] = { Lista },
[EstadoOrden.Lista]         = { Entregada },
[EstadoOrden.Entregada]     = { Pagada },
```
Validar una transición es `Transiciones[actual].Contains(nuevo)`: dos operaciones O(1). Comparado con `if`/`switch`
anidados, la regla queda **declarada como datos** en un solo lugar, es fácil de leer en la sustentación y se reutiliza
para mostrar en la web los botones válidos (`SiguientesEstados()`).

### 7. `Dictionary<MetodoPago, decimal>`: totales por método de pago
Se inicializa con todas las llaves del `enum` en 0 y se acumula `porMetodo[f.MetodoPago] += f.Total` recorriendo las
facturas una sola vez (O(n)). Si se agrega un método de pago nuevo al `enum`, el reporte lo incluye sin cambiar código.

### 8. `StringBuilder`: recibo del escritorio
Un `string` es inmutable: cada `+=` crea una cadena nueva y copia la anterior. Para armar la tirilla
(~25 líneas) `StringBuilder` va agregando al mismo búfer.

## Otras decisiones relacionadas
- **`record` para datos de transporte** (`ItemPedido`, `ResumenVentas`, `ProductoVendido`, `SesionUsuario`): son inmutables y se comparan por valor, ideales para pasar datos entre capas.
- **`IQueryable` + `Include`** en los servicios: el filtrado (`Where`) se traduce a SQL y lo hace SQLite. Solo las agregaciones de dinero se hacen en memoria con LINQ to Objects, porque SQLite no tiene tipo `decimal`.
- **Índices en la base de datos** (`HasIndex`) sobre `Orden.Estado` y `Factura.CreadoEn`, que son las columnas por las que cocina, caja y reportes filtran siempre. Es la misma idea del `Dictionary`, pero en el motor de base de datos.
