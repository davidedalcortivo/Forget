# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed

- The package tags are listed in alphabetical order, the same order as the repository topics; the tags themselves are the
  same as in 2.1.1.

## [2.1.1] - 2026-09-21

No code changes: the package metadata and the documentation.

### Added

- `CONTRIBUTING.md`, `SECURITY.md`, issue forms and a pull request template (in the repository only, not in the packages).

### Changed

- The README and the summaries of the connection extension classes say "filtering" instead of "dynamic filtering", because
  "dynamic" suggested only the filters built at runtime and left out the expression predicates.
- The package descriptions no longer mention upsert and follow the repository's wording (plain "filtering", which covers
  both expression predicates and `FilterDescriptor`), and the package tags now match the repository topics: `dapper`, `orm`,
  `micro-orm`, `dotnet`, `sql`, `data-access`, plus the provider's own (`sqlserver`, `mysql`, `postgresql` or `oracle`). The
  alternative spellings (`mssql`, `postgres`, `npgsql`, `odp.net`) are gone.

## [2.1.0] - 2026-09-21

### Changed

- An id, a filter value and a value you set with `Update`/`UpdateAsync` no longer have to be of exactly the property's type. Forget still
  never converts it: the value reaches Dapper as it was passed, and is rejected with an `ArgumentException` only when its
  type could lose precision or change meaning for the property, or when the drivers cannot bind it. For a comparison
  (`GetById`, `Delete(id)`, `GetByIdRange`, `DeleteRange(ids)` and `FilterDescriptor`, including the elements of an `In` list)
  the accepted types are: the same type, `byte`, `short`, `int` or `long` for any integer property, those for a `decimal`,
  `byte`, `short` or `int` for a `double`, `byte` or `short` for a `float`, and an enum for its
  underlying type (and the other way round). So `GetById(1L)` and `DeleteRange(new long[] { ... })` now work on an `int` key,
  and `new FilterDescriptor<Product>("Price", 10)` works on a `decimal` property; before, each of them threw. Still rejected:
  a `string` for a number, an `int` or a `long` for a `float`, a `long` for a `double`, a `decimal` or a `double` for an
  integer, and a `float` for a `double` (`1.1f` is converted exactly, but it is not `1.1`, so it would silently compare or
  write another number).
- `sbyte`, `ushort`, `uint` and `ulong` are accepted only for a property of the same type. Tried against the four engines,
  MySQL binds them, while Oracle, PostgreSQL and SQL Server fail with an opaque driver error, so Forget refuses them up
  front with its own message.
- A list of bytes (a `byte[]` used as a list of ids or as the values of an `In` filter) is rejected with an `ArgumentException`:
  every driver binds a `byte[]` as one binary value, so the query used to fail inside the database (a syntax error on MySQL,
  `ORA-00932` on Oracle, `op ANY/ALL (array) requires array on right side` on PostgreSQL, and a syntax error on SQL Server).
  It already failed for a key of type `byte`.
- A write with `Update`/`UpdateAsync` is stricter than a comparison, because a value that does not fit is an error at
  best (MySQL clips it without strict mode, Oracle stores it in a `NUMBER(10)` column and then fails to read the row back
  into an `int`, and PostgreSQL and SQL Server reject it): the type of each value must fit the property's, that is the
  same type or a narrower numeric one. An `int` for a `long` or a `decimal` property is accepted, a `long` for an
  `int` property is not (cast it yourself), and neither is an `int` for a `float`.
- The ids of `GetByIdRange` and `DeleteRange` must all have the same type (a list mixing, say, `int` and `long` throws an
  `ArgumentException`). Before, they all had to be of the key's exact type.
- `Min` and `Max` with a property name and a `TProperty` (`MinAsync<Order, long>("Quantity")`) now accept any `TProperty`
  that can hold every value of the property: a wider numeric type works (`long` for an `int` property, `decimal` for an
  `int` one), a narrower one is still rejected, and so is one that could round the result (`int` for a `decimal` property).

### Fixed

- A `byte[]` as the value of a `FilterDescriptor` on a `byte[]` property threw
  `The type of the provided value 'System.Byte' does not match the type of the property 'Payload' ('System.Byte[]')`,
  because the array was checked element by element against the array type. The same happened with an array column
  (`int[]` on PostgreSQL). A value whose own type fits the property is now accepted as a whole, and only a collection that
  does not fit is checked element by element.

## [2.0.0] - 2026-09-20

### Removed

- **Breaking:** the `preserveDuplicates` and `preserveNulls` parameters of `GetByIdRange`/`GetByIdRangeAsync`, on all four
  providers. Both existed to keep the result aligned with `ids`, which meant pairing every row read back with the id that
  was asked for, and that pairing is what failed (see Fixed).

### Changed

- **Breaking:** `GetByIdRange`/`GetByIdRangeAsync` return `IReadOnlyList<TEntity>` instead of `IReadOnlyList<TEntity?>`. The
  result holds the rows that exist, each once: it is not in the order of `ids`, and an id with no row does not appear in it.
  The identifier is assumed to be unique in the table. Code that relied on the order or on the `null` entries can rebuild
  them from the rows:

  ```csharp
  IReadOnlyList<Product> rows = await connection.GetByIdRangeAsync<Product>(ids);
  Dictionary<int, Product> byId = rows.ToDictionary(p => p.Id);
  List<Product?> inOrder = [.. ids.Select(id => byId.GetValueOrDefault(id))];
  ```

### Fixed

- `GetByIdRange`/`GetByIdRangeAsync` threw `KeyNotFoundException` whenever the database matched an id that .NET did not
  consider equal to the row's key, because each row read back was paired with the requested ids by exact .NET equality. It
  happened with a `string` key under a case-insensitive collation (the default on MySQL and SQL Server: asking for `"abc"`
  when the table holds `"ABC"`) and with a `byte[]` key (two arrays with the same content are different objects). It no
  longer happens: the rows are not paired with the ids any more. Rows the database returns more than once, because two
  ids the database considers equal (`"abc"` and `"ABC"`) fell in different batches, or because the same `byte[]` was
  requested twice, are returned once, compared by their own key (`byte[]` by content).

## [1.0.3] - 2026-09-20

### Changed

- Upsert under concurrency is now documented, on the methods it affects and in the README, and covered by tests on all four
  providers (16 connections upserting the same new keys at the same moment). Single-row upserts succeeded for every caller on
  MySQL, PostgreSQL and SQL Server, and so did multi-row upserts on MySQL and PostgreSQL. Two cases can fail; in both, no key
  ends up with two rows, and Forget does not retry (handling the error is up to the caller):
  - Oracle: a `MERGE` is not atomic against a concurrent insert of the same key, so a session can fail with `ORA-00001`, and a
    multi-row upsert can also be chosen as the victim of a deadlock (`ORA-00060`).
  - SQL Server: a multi-row upsert of overlapping keys can be chosen as the victim of a deadlock (error 1205).
- README: a new section under the mapping attributes says what the entity maps (the columns it declares; the table may
  have more), that types are Dapper's rather than Forget's, and that the column metadata some operations need is read
  once per entity and database and never invalidated.
- README: the example entity marks its `Id` as `[DatabaseGenerated(DatabaseGeneratedOption.Identity)]`. Without it, `Insert`
  on a table with an identity column would try to write the `Id`.

### Fixed

- A predicate that used a property the entity does not map to a column (one marked `[NotMapped]`, for example) threw a bare
  `KeyNotFoundException: The given key 'Computed' was not present in the dictionary`, although the documentation promises
  `NotSupportedException` for an expression the translator cannot handle. It now throws that, naming the property and the entity.
- A predicate that could not be translated or evaluated (`t => t.Name.Length > 3`, or a captured value that throws when read)
  lost the reason it failed: only `Unable to evaluate expression '...'` came out. The original exception is now attached as
  the `InnerException`, and the message says what the expression would have needed to be (a mapped column of the entity, or a
  value that can be computed without it).
- SQL Server: `UpsertRange` deadlocked far too easily when several connections upserted the same new keys at the same moment
  (one of them was killed as the deadlock victim). Its `UPDATE` step took no lock at all and only the `INSERT` step held
  `UPDLOCK, HOLDLOCK`, so two callers both got past the update and then blocked each other on the insert. The `UPDATE`
  now takes the same range locks, as the single-row `Upsert` already did, which makes the deadlock much rarer. It does not
  remove it: on a small table the server reads the target with a scan, and concurrent scans holding range locks can still
  deadlock (see above). `UpdateRange` is unchanged.
- An entity may now map only some of the columns of its table, as with Dapper (audit or legacy columns are common).
  Every operation that reads the column list from the database used to throw `InvalidOperationException: Database table
  schema mismatch` unless the entity mapped every column: `InsertRange`, `UpdateRange` and `UpsertRange` on Oracle
  (even after `LoadDbCache`), `UpdateRange` on PostgreSQL, and `Sum`/`Avg` on SQL Server. PostgreSQL's `UpdateRange` now
  fills the columns the entity does not map with `NULL` in its row values, which the `UPDATE` never reads; Oracle's
  discovery block only reads the columns whose names Forget could map, so a column named with a backslash (which broke
  the block's JSON output) no longer matters. A mapped property whose column does not exist in the table still fails,
  with a message that names the property and the column.
- Oracle: `InsertRange`, `UpdateRange` and `UpsertRange` silently rounded the values written to a `NUMBER` column
  declared without a precision (`12.75` was stored as `13`), and mangled a `NUMBER(*,s)` column (scale used as
  precision). The cast that the multi-row statement discovers per column took the dictionary's `DATA_LENGTH` (22, the
  storage size of any `NUMBER`) for a precision. A length is now used only for `VARCHAR2`, `NVARCHAR2`, `CHAR`,
  `NCHAR` and `RAW`; a `NUMBER` with no precision is cast as plain `NUMBER`, and `NUMBER(*,s)` as `NUMBER(38,s)`.
  The same statement failed with `ORA-12899` on every non-null value written to a `CHAR(n CHAR)` or `NCHAR(n)`
  column, because the cast was padded to `DATA_LENGTH`, which counts bytes; `CHAR` and `NCHAR` are now cast to the
  column's own length in its own unit (`CHAR(n CHAR)`, `CHAR(n BYTE)`, `NCHAR(n)`).
  Single-row operations were not affected.
- PostgreSQL: every collection of enum values bound for `= ANY(@p)` failed, because it was bound as an `enum[]`
  array that Npgsql cannot write. This affected `Contains` over a list of enums (for example
  `kinds.Contains(x.Kind)`), `FilterDescriptor` with `ComparisonOperator.In` over enums, and
  `GetByIdRange`/`DeleteRange` on an entity whose key is an enum. The array is now built from the enum's underlying
  integral type, the same way Dapper binds a single enum value.

### Known issues

- `GetByIdRange`/`GetByIdRangeAsync` with a `string` key throws `KeyNotFoundException` when the database compares strings
  differently from .NET: with a case-insensitive collation (the default on MySQL and SQL Server), asking for `"abc"` when
  the table holds `"ABC"` matches in the database, but the rows read back are then paired with the requested ids by exact
  comparison and the pair is not found. Until this is addressed, ask for the keys exactly as they are stored, or use
  `GetAll`/`GetAllAsync` with a predicate such as `x => ids.Contains(x.Code)`, which leaves the comparison to the database.

## [1.0.2] - 2026-09-18

### Added

- A "Buy Me A Coffee" badge in the README, linking to the project's support page.

## [1.0.1] - 2026-09-18

### Fixed

- The coverage badge in the README used a relative path (`assets/badges/coverage.svg`), which NuGet.org's readme
  renderer does not resolve — it only renders images from a fixed set of trusted absolute domains. Switched to an
  absolute `raw.githubusercontent.com` URL, which both GitHub and NuGet.org render correctly.

## [1.0.0] - 2026-09-18

Initial release.

### Added

- Core engine: expression-based (`Expression<Func<T, bool>>`) and descriptor-based (`FilterDescriptor`/`FilterGroup`)
  filtering, translated per-dialect into parameterized SQL.
- Type-safe CRUD, sorting, paging, and single/range upsert.
- Provider packages for MySQL, Oracle, PostgreSQL and SQL Server, each following that engine's real type and
  syntax constraints (e.g. Oracle's `UNION ALL`/`DUAL`-based multi-row insert with automatic per-column cast
  discovery, since Oracle has no native `VALUES (...), (...)` syntax).

[2.1.1]: https://github.com/davidedalcortivo/Forget/releases/tag/v2.1.1
[2.1.0]: https://github.com/davidedalcortivo/Forget/releases/tag/v2.1.0
[2.0.0]: https://github.com/davidedalcortivo/Forget/releases/tag/v2.0.0
[1.0.3]: https://github.com/davidedalcortivo/Forget/releases/tag/v1.0.3
[1.0.2]: https://github.com/davidedalcortivo/Forget/releases/tag/v1.0.2
[1.0.1]: https://github.com/davidedalcortivo/Forget/releases/tag/v1.0.1
[1.0.0]: https://github.com/davidedalcortivo/Forget/releases/tag/v1.0.0
