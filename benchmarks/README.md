# Benchmarks

Forget against hand-written Dapper and EF Core, on PostgreSQL, with [BenchmarkDotNet](https://benchmarkdotnet.org/).
You need the .NET 10 SDK and Docker: every benchmark starts its own PostgreSQL 16 container (Testcontainers) with a table
of 10,000 rows.

```bash
dotnet run -c Release --project benchmarks/Forget.Benchmarks -- --filter "*"
```

The whole run takes about 45 minutes, most of it `RangeBenchmarks` (the 10,000-row writes, run the naive way, are slow by
design — see below). Use `--filter "*SingleRow*"` (or `*Filter*`, `*Range*`, `*Generation*`) to run one group, and
`--job short` for a quick look that is not precise enough to quote.

## What is measured

| Group | Operations | Variants |
|---|---|---|
| `SingleRowBenchmarks` | `GetById`, `Insert`, `Update` | Dapper, Forget, EF Core |
| `FilterBenchmarks` | `GetAll` with a filter, `GetAll` with `Contains` on 10 ids, `GetAll` with `StartsWith`, `GetPage` with a filter, a sort and a page | Dapper, Forget, EF Core |
| `RangeBenchmarks` | `GetByIdRange`, `InsertRange`, `UpdateRange`, with 100, 1000 and 10,000 rows, out of a table of 50,000 | Dapper, Forget, EF Core |
| `GenerationBenchmarks` | Building the command of `GetById` (and its typed overload), `GetAll` and `GetPage`, without a database | Forget |
| `RangeGenerationBenchmarks` | Building the commands of `GetByIdRange` and `DeleteRange` (and their typed overloads) for 100, 1000 and 10,000 ids, without a database | Forget |

For the writes, Dapper sends one statement per row, which is what `Execute` does with a list. A bulk load (such as
`COPY`, or one statement with an array per column) is a different operation and is not compared here. `GetByIdRange` has
four Forget variants: `GetByIdRange_Forget` uses the default `batchSize` (500 — what a caller gets without passing one),
and `GetByIdRange_Forget_OneRoundTrip` sets `batchSize` to the id count, so it always does one round trip like Dapper's
and EF Core's own query. The two `_Typed` variants make the same two calls with `GetByIdRange<Product, int>`, which takes
the ids as an `int` sequence instead of a non-generic one, the way Dapper and EF Core are given them; see below for why
they differ.

## How it is kept fair

- Every library uses the same open connection. EF Core creates a new `DbContext` for each operation, as an application
  does, and reads without change tracking.
- The filters use a value captured from a variable, as a filter built from a request does, not a literal.
- Writes commit: Dapper and Forget inside an explicit transaction, EF Core through `SaveChanges`, which has its own.
- Before measuring, the setup runs every variant once and stops if it does not return, or store, the same rows as the
  Dapper baseline. A benchmark that compares different work does not run.

## Docker inside WSL 2

If Docker runs inside a WSL 2 distribution and the benchmarks run on Windows, the connection to `localhost` goes through a
relay that adds latency and, when a response is larger than about 64 KB, stalls it for about 40 ms. That is a cost of the
relay, not of the library, and it hits whichever library reads the response too slowly. Set `FORGET_BENCHMARK_HOST` to the
address of the WSL machine (`wsl hostname -I`) so that the benchmarks connect to it directly:

```bash
FORGET_BENCHMARK_HOST=$(wsl hostname -I | awk '{print $1}') dotnet run -c Release --project benchmarks/Forget.Benchmarks -- --filter "*"
```

## Reading the results

The times depend on the machine, on Docker and on the disk under it, so compare the ratios and the allocated memory, not
the absolute values. The reads spend most of their time waiting for the database, which is why the differences between
libraries are small there; `GenerationBenchmarks` is the place to see what Forget itself costs. The writes are the most
sensitive to the disk and vary the most from one run to the next.

### GetByIdRange: reads don't need batching, so Forget's own batch is what costs here

`GetByIdRange_Forget`'s number gets worse as the id count grows — about 1.05x Dapper at 100, 1.3x at 1000, 1.7x at
10,000 — while EF Core's gets *better*, from about 1.3x down to 1.0x. The reason is that `GetByIdRange_Forget` batches at
500 ids by default (a batch smaller than the whole call exists both to stay under other engines' parameter limits — SQL
Server accepts at most 2100 per command — and to avoid handing the database one statement with thousands of parameters),
so more ids means more round trips (2 at 1000, 20 at 10,000), while Dapper's and EF Core's own `= ANY(...)`/`IN (...)`
send them all in a single query regardless of count. `GetByIdRange_Forget_OneRoundTrip` (`batchSize` set to the id count)
removes that variable: about 1.07x, 1.04x and 1.07x at 100, 1000 and 10,000, so the round trips are most of the gap at
1000 and 10,000. The exact cost of an extra round trip also varies a bit with the network path of the day (WSL 2, see
below), which is why this specific number is the least stable one in the whole suite.

### GetByIdRange and DeleteRange with typed ids: the work on the client side nearly disappears

`GetByIdRange<TEntity, TKey>` and `DeleteRange<TEntity, TKey>` take the ids as a sequence of `TKey`, so Forget does not
have to find out their type one boxed id at a time. Each checks `TKey` once against the type of the key, copies the ids
once into a `TKey[]` and hands that array to the driver as the parameter when one batch covers all of it, or a slice of
it for each batch otherwise. The non-generic overloads build a list of boxed ids, check the type of each one and rebuild
a typed array with reflection. `RangeGenerationBenchmarks` measures only that work, without a database, which is why its
numbers are steady: for 100, 1000 and 10,000 ids, building the commands takes

| | 100 ids | 1000 ids | 10,000 ids |
|---|---|---|---|
| `GetByIdRange`, non-generic | 3,372 ns (8.4 KB) | 26,171 ns (47.1 KB) | 352,760 ns (533 KB) |
| `GetByIdRange`, typed | 494 ns (3.8 KB) | 571 ns (7.4 KB) | 2,832 ns (42.5 KB) |
| `DeleteRange`, non-generic | 3,059 ns (7.1 KB) | 26,033 ns (45.8 KB) | 386,997 ns (532 KB) |
| `DeleteRange`, typed | 356 ns (2.6 KB) | 706 ns (6.1 KB) | 2,843 ns (41.2 KB) |

that is, the typed overloads are 7x to 136x faster and allocate 55% to 92% less, and the gap widens with the id count in
both directions, because the non-generic overload's cost grows with the id count while the typed one barely moves.

`GetByIdRange`'s non-generic overload also stopped copying the ids into a `HashSet` before building the commands, since
the database returns a row once however many times its id is listed and the rows that come back are deduplicated by id
when the ids are split over more than one query: that alone accounts for part of its numbers above being lower than they
would otherwise be. `DeleteRange` never had that `HashSet` (there is no row to deduplicate for a delete), so this does
not apply to it.

Against the database, that is a smaller share of the total, and shows only when the round trips are equalised. For
`GetByIdRange` with one round trip the typed call was measured at 1.04x, 1.02x and 1.04x Dapper at 100, 1000 and 10,000
ids, against 1.07x, 1.04x and 1.07x for the non-generic one (the differences at 100 and 1000 are inside the noise; at
10,000 the gap to Dapper goes from about 0.9 to about 0.5 ms), and it allocates less than EF Core: 3.4 MB against 3.6 at
10,000. With the default batch of 500 the round trips dominate and the typed call is no faster. What is left of the gap
to Dapper is the deduplication of the rows that come back and the fixed cost of building the command, and how much each
is has not been measured. `DeleteRange` was not measured end to end against Dapper and EF Core (there is no
`DeleteRange` group in `RangeBenchmarks`), only with `RangeGenerationBenchmarks`.

### GetById and Delete with typed ids: no gain, and that is expected

`GetById<TEntity, TKey>` and `Delete<TEntity, TKey>` take one identifier, not a sequence, so there is no per-identifier
loop for the typed overload to skip: checking the type of `TKey` once is the same amount of work as checking the type of
one boxed value once. Measured with `GenerationBenchmarks`, `GetById_Typed` is 204 ns against 194 ns for `GetById`, and
both allocate the same 1.45 KB — no real difference, well inside the noise (the standard deviation on either one is
already about 30 ns). The typed overloads exist for consistency with `GetByIdRange`/`DeleteRange`, and for a caller who
already holds the id as a `TKey` and wants to say so, not for a measurable speed gain here.

### InsertRange and UpdateRange: EF Core falls further behind Forget as the row count grows

EF Core still generates one statement per row for these — the "How it is kept fair" section above doesn't change that —
but it batches many of them into few round trips (checked by counting `DbCommand` executions: 1 round trip for 1000
rows, 10 for 10,000 — a chunk of 1000 statements per round trip, not one round trip per row). That is why it is nowhere
near the per-row Dapper baseline. But it is not free either: EF Core's time relative to Forget's (default `batchSize`)
gets worse as rows grow — roughly 1.9x at 100 rows, 2.0x at 1000, 2.3x at 10,000 for `InsertRange`, and 2.7x, 2.2x, 3.0x
for `UpdateRange`. Round trips don't explain this one: at 10,000 rows EF Core makes *fewer* round trips than Forget's
default (10 against 20), yet is still markedly slower. What differs is the number of separate SQL statements the
database has to parse and plan — 10,000 single-row statements for EF Core against 20 multi-row statements (500 rows
each) for Forget — and that cost keeps growing with the row count in a way the round trip count does not. Forget itself
stays close to a flat 13x–16x faster than the per-row Dapper baseline at every count tried, batched or not.
