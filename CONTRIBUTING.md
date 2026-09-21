# Contributing to Forget

Thanks for taking the time. Forget is a small library with one maintainer, so a short issue that reproduces a problem
is worth more than a large pull request that arrives without a conversation first.

## Before you start

- **A bug or a question:** open an issue with the provider, the database version, and the smallest entity and call that
  show the problem. Say what you expected and what happened (the exception, or the SQL and the result).
- **New behavior:** open an issue first, so the shape can be agreed before you write it. Forget stays thin on purpose:
  it does not map or convert types (that is Dapper's job), and it does not do joins, change tracking or migrations
  (see [What this isn't](README.md#what-this-isnt)).
- **A security problem:** do not open a public issue. See [SECURITY.md](SECURITY.md).

## Building and testing

You need the .NET 8 SDK and Docker. The integration tests start real databases with Testcontainers (SQL Server 2022,
MySQL 8.4, PostgreSQL 16 and Oracle XE 21), so the first run pulls the images, and Oracle is the slow one.

```bash
dotnet build
dotnet test --project tests/Forget.Tests
```

The test runner is Microsoft Testing Platform (see `global.json`). To run one provider, or one class:

```bash
dotnet test --project tests/Forget.Tests -- --filter-namespace "Forget.Tests.SqlServer"
dotnet test --project tests/Forget.Tests -- --filter-class "*KeyedRangeIntegrationTests"
```

## How the code is laid out

- `src/Forget.Core`: the part that does not depend on a database: translating expressions and filters, the entity and
  column caches, and the base strategies.
- `src/Forget.SqlServer`, `Forget.MySql`, `Forget.PostgreSql`, `Forget.Oracle`: each provider's dialect, its SQL
  builders, and the connection extension methods (`DbConnectionExtensions*.cs`: synchronous, asynchronous, and the
  `Commands` overloads that return the SQL without running it).
- `tests/Forget.Tests`: `Core` (no database) and one folder per provider. Each provider has its own copy of the test
  entities, because they diverge where an engine's types require it.

## Conventions

- The same operations, with the same names, exist on all four providers, each with a synchronous and an asynchronous
  overload. A change to one provider's extension methods usually applies to all four.
- The public API has XML documentation. Keep it short, and say what Forget decides, not what Dapper or the driver does.
- Comments only where the code cannot say why. Nullable reference types are on, and the build has no warnings
  (`dotnet build --no-incremental` shows the analyzer ones too).
- Anything a provider does differently is verified against a real engine, not by reading its documentation.

## Making a change

1. Write the test first and see it fail, on the providers the change concerns. A provider's test classes share one
   container, so a test uses its own table or its own range of ids.
2. Make the change.
3. Add an entry under `[Unreleased]` in [CHANGELOG.md](CHANGELOG.md), and update the README when users can see the
   difference.
4. Run the whole suite.

The project follows [Semantic Versioning](https://semver.org/): accepting more input is a minor version, and changing
what an existing call returns or throws for input that worked is a major one. Contributors do not change `<Version>`:
the maintainer tags a release, and the release workflow runs the full suite before it publishes anything.

## License

By contributing you agree that your contribution is licensed under the Apache License 2.0 (see [LICENSE](LICENSE)).
