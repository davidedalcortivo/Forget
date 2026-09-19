# Forget

[![Build & Test](https://github.com/davidedalcortivo/Forget/actions/workflows/build-test.yml/badge.svg)](https://github.com/davidedalcortivo/Forget/actions/workflows/build-test.yml)
![Core line coverage](https://raw.githubusercontent.com/davidedalcortivo/Forget/main/assets/badges/coverage.svg)
[![Buy Me A Coffee](https://img.shields.io/badge/-Buy_Me_A_Coffee-ffdd00?style=flat&logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/davidedalcortivo)

A thin, provider-aware extension layer on top of [Dapper](https://github.com/DapperLib/Dapper): type-safe CRUD
(single-row or multi-row), dynamic filtering, sorting, and paging against one mapped table at a time — with SQL
generated correctly for the engine you're actually running against, not a lowest-common-denominator translation.
Supports **MySQL**, **Oracle**, **PostgreSQL**, and **SQL Server**.

It sits between raw Dapper and a full ORM: you still map plain classes to tables with a handful of attributes, but
you can forget hand-writing (and hand-maintaining, across 4 dialects) the same `SELECT`/`UPDATE`/`INSERT`/`MERGE`
boilerplate for every entity.

## Why

|                                                    | Dapper           | Forget                              | EF Core                                   |
|----------------------------------------------------|-------------------|--------------------------------------------|--------------------------------------------|
| Single-row CRUD                                      | You write the SQL | Generated, one line per call                | Generated                                   |
| Multi-row update/insert/delete/upsert                | You write the SQL | ✅ Generated as one atomic multi-row statement, with a configurable batch size | Generated, but one statement per row, batched into fewer round trips — not a single multi-row statement |
| Filter built from a compile-time LINQ expression     | You write the SQL | ✅ `Expression<Func<T, bool>>`              | ✅                                           |
| Filter built dynamically at runtime (e.g. from a query string, with no `Expression` in sight) | You write the SQL | ✅ `FilterDescriptor`/`FilterGroup`, by property name | Needs `Expression.Lambda` plumbing or a package like `System.Linq.Dynamic.Core` |
| Upsert on a natural (non-identity) key               | You write the SQL | ✅ `[UpsertKey]`, one call across all 4 providers | Needs manual logic or a third-party package |
| Multi-table joins / result-set mapping to several types | ✅ `splitOn`      | ❌ (see [What this isn't](#what-this-isnt)) | ✅                                           |
| Change tracking, migrations, lazy loading            | ❌                | ❌                                           | ✅                                           |

If you need joins across unrelated entities, keep using Dapper directly for that one query — Forget doesn't
replace it, it removes the repetitive single-table code around it.

## Install

Pick the package for your database; each one pulls in `Forget.Core` and the underlying ADO.NET driver
automatically.

| Database   | Package                                                        | Connection type      |
|------------|------------------------------------------------------------------|-----------------------|
| MySQL      | [`Forget.MySql`](https://www.nuget.org/packages/Forget.MySql)         | `MySqlConnection`     |
| Oracle     | [`Forget.Oracle`](https://www.nuget.org/packages/Forget.Oracle)       | `OracleConnection`    |
| PostgreSQL | [`Forget.PostgreSql`](https://www.nuget.org/packages/Forget.PostgreSql) | `NpgsqlConnection`    |
| SQL Server | [`Forget.SqlServer`](https://www.nuget.org/packages/Forget.SqlServer) | `SqlConnection`       |

```bash
dotnet add package Forget.SqlServer
```

## Quickstart

Map an entity with plain data-annotation attributes:

```csharp
using Forget.Core.Models; // UpsertKeyAttribute, and later FilterDescriptor/FilterGroup/ComparisonOperator/SortDescriptor
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Products")]
public class Product
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [UpsertKey]
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}
```

Then use the connection extension methods — every method below exists identically (same name, same overloads) on
all 4 providers, and each has both a synchronous and an asynchronous overload (`GetAll`/`GetAllAsync`,
`Insert`/`InsertAsync`, and so on); only the `using` and the connection type change.
`[UpsertKey]` (on `Sku` above) makes `Upsert`/`UpsertAsync` match rows by that column instead of the identity
column — see [Upsert on a natural key](#upsert-on-a-natural-key-one-call-a-different-correct-statement-per-engine)
below for what actually runs:

```csharp
using Forget.SqlServer.Extensions;
using Microsoft.Data.SqlClient;

await using SqlConnection connection = new(connectionString);

IReadOnlyList<Product> all = await connection.GetAllAsync<Product>();
// Every method also has a synchronous overload with the same name minus "Async" - the rest of this example
// sticks to async, but this one works identically: IReadOnlyList<Product> all = connection.GetAll<Product>();

Product? product = await connection.GetByIdAsync<Product>(id: 42);

await connection.InsertAsync(new Product { Sku = "SKU-1", Name = "Widget", Category = "Tools", Price = 9.99m, IsActive = true });

await connection.UpdateAsync(product!);

await connection.UpsertAsync(new Product { Sku = "SKU-1", Name = "Widget", Category = "Tools", Price = 12.50m, IsActive = true });

await connection.DeleteAsync<Product>(id: 42);
```

## Dynamic filtering, two ways

A predicate you can write at compile time works exactly like you'd expect:

```csharp
IReadOnlyList<Product> cheapTools = await connection.GetAllAsync<Product>(
    p => p.Category == "Tools" && p.Price < 20 && p.IsActive);
```

But a search endpoint rarely knows its filters at compile time — they come from a query string, a search form,
an API request body.
Raw Dapper gives you nothing here beyond string concatenation, and EF Core needs you to build `Expression` trees
by hand (or add `System.Linq.Dynamic.Core`).
Forget's `FilterDescriptor`/`FilterGroup` build the exact same kind of filter from plain data — a property
**name**, not a property **selector** — so it composes cleanly from untyped input:

```csharp
// PropertyName and Value could come straight from Request.Query, validated against Product's real properties.
FilterGroup<Product> filter = new(
[
    new FilterDescriptor<Product>("Category", "Tools"),
    new FilterDescriptor<Product>("Price", 20m, ComparisonOperator.LessThan),
    new FilterDescriptor<Product>("Name", "wid", ComparisonOperator.Contains, ignoreCase: true)
]);

IReadOnlyList<Product> results = await connection.GetAllAsync(filter);
```

`GetAll`, `GetFirst`, `GetSingle`, `GetPage`, `Update`, `Delete`, `Exists`, `Count`, `Avg`, `Sum`, `Min`, and `Max`
(and their respective asynchronous overloads) all accept either shape — pick whichever fits the call site, they
translate to the same SQL.

### Sorting and paging

```csharp
IReadOnlyList<Product> page = await connection.GetPageAsync<Product>(
    predicate: p => p.IsActive,
    sortDescriptors: [new SortDescriptor<Product>(p => p.Price, SortDirection.Descending)],
    skip: 20,
    take: 10);
```

## Correctness details worth knowing about

Every provider is designed individually at the query-generation level — the same C# call can, and usually does,
produce a genuinely different statement depending on which engine it runs against.
What follows is a handful of concrete examples of that, not an exhaustive list of every place the 4 providers
diverge.

A few smaller, mostly mechanical ones:

- **MySQL** escapes `LIKE` patterns (for `Contains`/`StartsWith`/`EndsWith`) accounting for MySQL's own C-style
  backslash escaping inside string literals — different from the other three engines, and easy to get wrong with
  one shared `ESCAPE` clause.
- **PostgreSQL** translates `Enumerable.Contains`/`ComparisonOperator.In` to `= ANY(@p)` when the collection is
  homogeneously typed (Npgsql needs a genuinely typed array for that), falling back to plain `IN` — letting
  Dapper itself, not the driver, expand the parameter — when it isn't.

And a few bigger ones:

### Upsert on a natural key: one call, a different correct statement per engine

```csharp
await connection.UpsertAsync(new Product { Sku = "SKU-1", Name = "Widget", Category = "Tools", Price = 12.50m, IsActive = true });
```

What actually runs is deliberately *not* the same statement re-parameterized four times:

- **MySQL**: `INSERT INTO ... VALUES (...) AS new ON DUPLICATE KEY UPDATE col = new.col, ...`
- **Oracle**: `MERGE INTO ... USING (SELECT ... FROM DUAL) SOURCE ON (...) WHEN MATCHED THEN UPDATE ... WHEN NOT MATCHED THEN INSERT ...`
  (a `MERGE` is not atomic against a concurrent insert of the same key: if two sessions upsert a key that doesn't
  exist yet at the same moment, one of them can fail with `ORA-00001`. Forget doesn't retry — handle that error and
  call again; the row exists by then, and it is updated. It never leaves a duplicate row.)
- **PostgreSQL**: `INSERT INTO ... VALUES (...) ON CONFLICT (sku) DO UPDATE SET col = EXCLUDED.col, ...`
- **SQL Server**: *not* `MERGE` — a documented, well-known source of concurrency correctness bugs.
  Instead: `UPDATE ... WITH (UPDLOCK, HOLDLOCK) SET ... WHERE ...` followed by a conditional `INSERT` guarded by
  `IF @@ROWCOUNT = 0`, wrapped in a transaction Forget manages for you if you don't supply one.

Each statement is the one that is right for that engine — instead of one statement that happens to parse everywhere
but is subtly wrong (or slow, or unsafe under concurrency) on at least one of them.

Upserts from many connections at once, on the same new keys, are covered by tests on all 4 providers. Single-row
upserts succeeded for every caller on SQL Server, MySQL and PostgreSQL, and so did multi-row upserts on MySQL and
PostgreSQL. Two cases can fail, and in both Forget doesn't retry — handle the error and call again:

- **Oracle**: `ORA-00001`, as described above, and a multi-row upsert can also be chosen as the victim of a deadlock
  (`ORA-00060`).
- **SQL Server**: a multi-row upsert of overlapping keys can be chosen as the victim of a deadlock (error 1205).

No key ever ends up with two rows.

### Avg, on every provider: read as text, converted to `decimal` in C#

Database numeric types can carry more precision than .NET's `decimal` can hold: MySQL's `DECIMAL` goes up to 65
digits, Oracle's `NUMBER` up to 38 significant digits, PostgreSQL's `NUMERIC` is effectively unbounded, and SQL
Server's `AVG` on a `decimal(p,s)` column returns `decimal(38,s)` — while `System.Decimal` only holds about
28-29.
Averaging routinely produces exactly this kind of long, non-terminating result, and reading it as a native
decimal scalar risks the ADO.NET driver itself failing before Forget ever gets a say.

So on all 4 providers, the database returns the average as text instead of a raw decimal, and `Avg`/`AvgAsync`
then converts that text into a `decimal` in C# with its own parser: precision beyond what `decimal` can represent
is rounded off, and only a magnitude that genuinely doesn't fit throws `OverflowException`.
`Sum`/`SumAsync` doesn't go through any of this — summing a column doesn't introduce precision beyond what it
already had, so it reads the native decimal scalar directly.
The exact SQL each provider uses to produce that text is visible via `AvgCommand`, if you want to see it.

### Oracle: no native multi-row `VALUES (...), (...)` — update, insert, and upsert all need a way around it

Oracle has no native multi-row insert syntax.
The common workaround is:

```sql
INSERT INTO products (id, name, price)
SELECT :id0, :name0, :price0 FROM DUAL
UNION ALL
SELECT :id1, :name1, :price1 FROM DUAL
```

which immediately runs into `ORA-01790` ("expression must have same datatype as corresponding expression") the
moment two rows disagree on the inferred type of a bound parameter in the same `SELECT` position — which happens
constantly, since a `NULL` or a numeric literal in one row and a string in another are common in real data.

This isn't only an `InsertRange`/`InsertRangeAsync` problem: `UpdateRange`/`UpdateRangeAsync` and
`UpsertRange`/`UpsertRangeAsync` build on the exact same `UNION ALL`/`DUAL` block as their source rowset
(`UpdateRange`/`UpdateRangeAsync` feeds it into a `MERGE` with only a `WHEN MATCHED` branch;
`UpsertRange`/`UpsertRangeAsync` into a full `MERGE` with both branches) — so all three needed solving, not just
one.
Forget generates this shape automatically, but with every value cast to its column's real type first:

```sql
-- "app" here is whatever schema the entity resolves to (your Oracle user by default, or [Table(Schema = "...")])
INSERT INTO "app"."Products" (
    "Id", "Name", "Price"
)
SELECT CAST(:Id0 AS NUMBER(10)) AS "Id", :Name0 AS "Name", CAST(:Price0 AS NUMBER(18,2)) AS "Price" FROM DUAL
UNION ALL
SELECT CAST(:Id1 AS NUMBER(10)) AS "Id", :Name1 AS "Name", CAST(:Price1 AS NUMBER(18,2)) AS "Price" FROM DUAL
```

The cast expression for each column isn't hard-coded — on the first call for a given entity and connection,
Forget queries Oracle's data dictionary (`ALL_TAB_COLUMNS`) for that table, and for each column *probes*,
via `EXECUTE IMMEDIATE` against a set of candidate type strings (precision+scale, precision-only, length-only,
bare type name, and a `TO_<type>(...)` conversion-function fallback), which cast expression the server actually
accepts for that column right now — then caches the result.
This means it stays correct across Oracle versions and unusual column type definitions without the library
hard-coding assumptions about either.

```csharp
await connection.InsertRangeAsync(products, batchSize: 500);
```

### SQL Server: safe aggregation, and one place we didn't intervene

Summing an `int`/`smallint`/`tinyint` column over enough rows can overflow that type's range, so `Sum`/`Avg` cast
it to `BIGINT` first — the same value, just in a container wide enough not to overflow.

That cast doesn't (and can't, without inventing an arbitrary precision) give `Avg` fractional precision, though:
T-SQL's own `AVG` returns the same exact numeric type as its input, so averaging an integer-typed column still
returns a truncated whole number — `AVG` of `1, 2, 4` is `2`, not `2.33`, identical to what a hand-written
`AVG(column)` query returns.
We looked at "fixing" this and decided against it: unlike a real translation bug (an unnecessary escape character
silently breaking a search, say), there's no single objectively correct alternative here to fall back on — only
an arbitrary policy decision about precision that isn't ours to make.
This behavior is documented on every `Avg`/`AvgAsync`/`AvgCommand` overload and locked in by a regression test,
precisely because it fails silently (a plausible-looking wrong number, no exception) rather than loudly.

## Entity mapping attributes

| Attribute                              | Effect                                                                 |
|------------------------------------------|---------------------------------------------------------------------|
| `[Key]`                                   | Marks the identifier property. If omitted, a property named `Id` is used. |
| `[UpsertKey]`                             | Marks one or more properties as the natural key used to match rows on upsert, instead of the identifier. Multiple properties form a composite key. |
| `[Table(Name = ..., Schema = ...)]`       | Overrides the table name and/or schema (defaults: CLR type name, provider's default schema). |
| `[Column(Name = ...)]`                    | Overrides the SQL column name for a property.                        |
| `[NotMapped]`                             | Excludes a property entirely.                                        |
| `[DatabaseGenerated(...)]`                | A property with `Identity` or `Computed` is never written by the library, only read back. |

### What the entity maps, and what Forget leaves alone

- **The entity maps the columns it declares.** The table can have more columns than the entity (audit or legacy
  columns, say): those are never selected and never written, so a database default applies on insert. A mapped
  property whose column does not exist in the table fails with a message naming the property and the column.
- **Types are Dapper's.** Forget does not map or convert types. Any type Dapper can bind, or that you register a
  `SqlMapper.TypeHandler` for, works the same way through Forget.
- **Column metadata is read once.** A few operations need the table's column types — `Sum`/`Avg` on SQL Server, the
  multi-row writes on Oracle, `UpdateRange` on PostgreSQL. Forget reads them the first time it needs them (or when
  you call `LoadDbCache`/`LoadDbCacheAsync` yourself) and keeps them, per entity and database, for the life of the
  process. Nothing invalidates them: after altering a column, restart the process.

## What this isn't

Forget is deliberately single-table: there is no multi-mapping, no `splitOn`, no join-building across
unrelated entities.
Every generated statement targets exactly one table.
For a query that spans several tables, drop down to Dapper directly on the same connection — Forget doesn't
try to replace it, it exists so that most of your single-table data access code doesn't need to be Dapper's raw
SQL either.

It also doesn't do change tracking, migrations, or lazy loading — if you need those, you likely want EF Core.

## Performance notes

- Per-entity reflection (property/attribute scanning) happens once per `TEntity`, on first use, guarded by the
  CLR's own thread-safe static-constructor initialization — not on every call.
- Property access uses compiled expression-tree getters, not `PropertyInfo.GetValue` reflection.
- Generated SQL is cached as a template per `(entity type, provider)` pair and rendered by substituting only the
  parts that vary per call (the filter, the sort, the parameter values) — not rebuilt from scratch every time.

## Inspecting generated SQL without running it

Every execution method (`GetAll`/`GetAllAsync`, `Upsert`/`UpsertAsync`, `InsertRange`/`InsertRangeAsync`, ...) has
a matching `*Command`/`*Commands` method that builds and returns the SQL text and parameters without executing
them — useful for logging, testing, or executing manually:

```csharp
DbCommandInfo command = connection.UpsertCommand(product);
// command.Sql, command.Parameters
```

## Versioning

This project follows [Semantic Versioning](https://semver.org/): breaking changes bump the major version,
backwards-compatible additions bump the minor version, and fixes bump the patch version.
See [CHANGELOG.md](CHANGELOG.md) for release history.

## License

Apache-2.0 — see [LICENSE](LICENSE).
