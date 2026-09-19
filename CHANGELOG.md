# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [1.0.3] - 2026-09-20

### Changed

- Oracle: the documentation of `Upsert`, `UpsertAsync`, `UpsertRange` and `UpsertRangeAsync`, and the README, now say what
  happens when several sessions upsert the same new key at the same moment: a `MERGE` is not atomic against a concurrent
  insert of the same key, so one of them can fail with `ORA-00001`, and never leaves a duplicate row. Forget does not retry;
  handling that error is up to the caller. The other three providers are unaffected: concurrent upserts of the same key
  succeed for every caller (covered by tests).
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
- SQL Server: `UpsertRange` could deadlock when several connections upserted the same new keys at the same moment (one of
  them was killed as the deadlock victim). Its `UPDATE` step took no lock at all and only the `INSERT` step held
  `UPDLOCK, HOLDLOCK`, so two callers both got past the update and then blocked each other on the insert. The `UPDATE`
  now takes the same range locks, as the single-row `Upsert` already did. `UpdateRange` is unchanged.
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
  differently from .NET: with a case-insensitive collation (the default on SQL Server and MySQL), asking for `"abc"` when
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
- Provider packages for SQL Server, MySQL, PostgreSQL and Oracle, each following that engine's real type and
  syntax constraints (e.g. Oracle's `UNION ALL`/`DUAL`-based multi-row insert with automatic per-column cast
  discovery, since Oracle has no native `VALUES (...), (...)` syntax).

[1.0.3]: https://github.com/davidedalcortivo/Forget/releases/tag/v1.0.3
[1.0.2]: https://github.com/davidedalcortivo/Forget/releases/tag/v1.0.2
[1.0.1]: https://github.com/davidedalcortivo/Forget/releases/tag/v1.0.1
[1.0.0]: https://github.com/davidedalcortivo/Forget/releases/tag/v1.0.0
