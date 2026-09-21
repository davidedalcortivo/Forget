# Benchmarks

Forget against hand-written Dapper and EF Core, on PostgreSQL, with [BenchmarkDotNet](https://benchmarkdotnet.org/).
You need the .NET 10 SDK and Docker: every benchmark starts its own PostgreSQL 16 container (Testcontainers) with a table
of 10,000 rows.

```bash
dotnet run -c Release --project benchmarks/Forget.Benchmarks -- --filter "*"
```

The whole run takes about half an hour. Use `--filter "*SingleRow*"` (or `*Filter*`, `*Range*`, `*Generation*`) to run one
group, and `--job short` for a quick look that is not precise enough to quote.

## What is measured

| Group | Operations | Variants |
|---|---|---|
| `SingleRowBenchmarks` | `GetById`, `Insert`, `Update` | Dapper, Forget, EF Core |
| `FilterBenchmarks` | `GetAll` with a filter, `GetAll` with `Contains` on 10 ids, `GetAll` with `StartsWith`, `GetPage` with a filter, a sort and a page | Dapper, Forget, EF Core |
| `RangeBenchmarks` | `GetByIdRange`, `InsertRange`, `UpdateRange`, with 100 and 1000 rows | Dapper, Forget, EF Core |
| `GenerationBenchmarks` | Building the command of `GetById`, `GetAll` and `GetPage`, without a database | Forget |

For the writes, Dapper sends one statement per row, which is what `Execute` does with a list. A bulk load (such as
`COPY`, or one statement with an array per column) is a different operation and is not compared here.

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
