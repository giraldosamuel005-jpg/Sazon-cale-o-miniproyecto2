// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

// =====================================================================================
//  Comparativas de estructuras de datos — Miniproyecto 2 (Sazón Caleño)
//  Mide con Stopwatch las alternativas que se evaluaron para cada caso del sistema
//  y muestra por qué se eligió cada estructura. Ejecutar en Release:
//      dotnet run -c Release --project tools/Restaurante.Comparativas
// =====================================================================================
using System.Diagnostics;
using Restaurante.Core.Dominio;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("Comparativas de estructuras de datos - Sazón Caleño\n");

var resultados = new List<(string Caso, string Estructura, double Ms, bool Elegida)>();

// Ejecuta la acción varias veces y devuelve el mejor tiempo (reduce ruido del JIT/GC).
static double Medir(Action accion, int repeticiones = 5)
{
    accion(); // calentamiento
    double mejor = double.MaxValue;
    for (int i = 0; i < repeticiones; i++)
    {
        var sw = Stopwatch.StartNew();
        accion();
        sw.Stop();
        mejor = Math.Min(mejor, sw.Elapsed.TotalMilliseconds);
    }
    return mejor;
}

var azar = new Random(42);

// -------------------------------------------------------------------------------------
// CASO 1: Buscar productos por Id al armar una orden  ->  Dictionary<int, Producto>
// -------------------------------------------------------------------------------------
const int productos = 2_000, busquedas = 200_000;
var listaProductos = Enumerable.Range(1, productos)
    .Select(i => new Producto { Id = i, Nombre = $"Producto {i}", Precio = 1000 + i })
    .ToList();
var indice = listaProductos.ToDictionary(p => p.Id);
var ids = Enumerable.Range(0, busquedas).Select(_ => azar.Next(1, productos + 1)).ToArray();

resultados.Add(("1. Buscar producto por Id", "List.Find  O(n)", Medir(() =>
{
    foreach (var id in ids) _ = listaProductos.Find(p => p.Id == id);
}, 3), false));
resultados.Add(("1. Buscar producto por Id", "Dictionary O(1)", Medir(() =>
{
    foreach (var id in ids) _ = indice[id];
}), true));

// -------------------------------------------------------------------------------------
// CASO 2: Cola de cocina (sacar la orden más antigua)  ->  Queue<Orden>
// -------------------------------------------------------------------------------------
const int ordenes = 50_000;
Orden[] CrearOrdenes() => Enumerable.Range(1, ordenes).Select(i => new Orden { Id = i }).ToArray();

resultados.Add(("2. Atender en orden de llegada", "List.RemoveAt(0) O(n)", Medir(() =>
{
    var lista = new List<Orden>(CrearOrdenes());
    while (lista.Count > 0) lista.RemoveAt(0); // desplaza todos los elementos cada vez
}, 3), false));
resultados.Add(("2. Atender en orden de llegada", "Stack.Pop (LIFO: orden incorrecto)", Medir(() =>
{
    var pila = new Stack<Orden>(CrearOrdenes());
    while (pila.Count > 0) pila.Pop(); // rápido, pero atiende primero al último que llegó
}), false));
resultados.Add(("2. Atender en orden de llegada", "Queue.Dequeue O(1)", Medir(() =>
{
    var cola = new Queue<Orden>(CrearOrdenes());
    while (cola.Count > 0) cola.Dequeue();
}), true));

// -------------------------------------------------------------------------------------
// CASO 3: ¿La mesa ya tiene una orden abierta? / ¿El usuario tiene el rol X?  ->  HashSet
// -------------------------------------------------------------------------------------
const int mesas = 5_000, consultas = 200_000;
var ocupadasLista = Enumerable.Range(1, mesas).Where(i => i % 2 == 0).ToList();
var ocupadasSet = ocupadasLista.ToHashSet();
var preguntas = Enumerable.Range(0, consultas).Select(_ => azar.Next(1, mesas + 1)).ToArray();

resultados.Add(("3. ¿Mesa ocupada?", "List.Contains O(n)", Medir(() =>
{
    foreach (var m in preguntas) _ = ocupadasLista.Contains(m);
}, 3), false));
resultados.Add(("3. ¿Mesa ocupada?", "HashSet.Contains O(1)", Medir(() =>
{
    foreach (var m in preguntas) _ = ocupadasSet.Contains(m);
}), true));

// -------------------------------------------------------------------------------------
// CASO 4: Menú agrupado y ordenado por categoría  ->  SortedDictionary
// (se arma una vez y se recorre muchas veces al pintar la carta)
// -------------------------------------------------------------------------------------
var menuPlano = Enumerable.Range(1, 400)
    .Select(i => new Producto { Id = i, Categoria = (CategoriaProducto)azar.Next(0, 4) })
    .ToList();
var menuOrdenado = new SortedDictionary<CategoriaProducto, List<Producto>>();
foreach (var p in menuPlano)
{
    if (!menuOrdenado.TryGetValue(p.Categoria, out var l)) menuOrdenado[p.Categoria] = l = new();
    l.Add(p);
}
const int pintadas = 20_000;

resultados.Add(("4. Pintar la carta por categoría", "List + GroupBy/OrderBy cada vez", Medir(() =>
{
    for (int i = 0; i < pintadas; i++)
        foreach (var g in menuPlano.GroupBy(p => p.Categoria).OrderBy(g => g.Key)) _ = g.Count();
}, 3), false));
resultados.Add(("4. Pintar la carta por categoría", "SortedDictionary (ya ordenado)", Medir(() =>
{
    for (int i = 0; i < pintadas; i++)
        foreach (var (_, lista) in menuOrdenado) _ = lista.Count;
}), true));

// -------------------------------------------------------------------------------------
// CASO 5: Líneas de una orden  ->  List<DetalleOrden>
// Pocas líneas (< 30), se recorren completas y en el orden en que se pidieron.
// Un arreglo no crece; una LinkedList ocupa más memoria y es más lenta de recorrer.
// -------------------------------------------------------------------------------------
const int lineas = 20, vueltas = 200_000;
var detallesLista = Enumerable.Range(1, lineas).Select(i => new DetalleOrden { Cantidad = 2, PrecioUnitario = 1000 * i }).ToList();
var detallesEnlazada = new LinkedList<DetalleOrden>(detallesLista);

resultados.Add(("5. Sumar el total de una orden", "LinkedList", Medir(() =>
{
    for (int i = 0; i < vueltas; i++) { decimal t = 0; foreach (var d in detallesEnlazada) t += d.Subtotal; }
}), false));
resultados.Add(("5. Sumar el total de una orden", "List (memoria contigua)", Medir(() =>
{
    for (int i = 0; i < vueltas; i++) { decimal t = 0; foreach (var d in detallesLista) t += d.Subtotal; }
}), true));

// -------------------------------------------------------------------------------------
// Reporte
// -------------------------------------------------------------------------------------
Console.WriteLine($"{"Caso",-34} {"Estructura",-36} {"Tiempo (ms)",12}");
Console.WriteLine(new string('-', 86));
string? casoAnterior = null;
foreach (var (caso, estructura, ms, elegida) in resultados)
{
    if (casoAnterior is not null && caso != casoAnterior) Console.WriteLine();
    Console.ForegroundColor = elegida ? ConsoleColor.Green : ConsoleColor.Gray;
    Console.WriteLine($"{(caso == casoAnterior ? "" : caso),-34} {estructura + (elegida ? "  ✔" : ""),-36} {ms,12:N2}");
    Console.ResetColor();
    casoAnterior = caso;
}

Console.WriteLine("\n✔ = estructura usada en el sistema. Ver docs/ESTRUCTURAS_DE_DATOS.md para la justificación.");
