using Forget.Core.Models;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Text.Json;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// Runs the cast discovery over every Oracle column type at once, without an entity per type: creates a table
    /// with one column of each type, runs the same PL/SQL block Forget runs against it, builds a table out of each
    /// discovered cast (<c>CREATE TABLE AS SELECT</c>) and compares the column Oracle derives from that cast with the
    /// column the cast was discovered for. A cast that changes the type, the precision, the scale or the length of
    /// what it wraps would round, pad or truncate the value on its way into the column.
    /// </summary>
    [Collection(OracleCollection.Name)]
    public class CastFidelityIntegrationTests
    {
        private enum Fit
        {
            Exact,
            MayBeWider
        }

        private readonly record struct Shape(string Type, int Length, int? Precision, int? Scale, int CharLength);

        private static readonly (string Definition, Fit Fit)[] ColumnTypes =
        [
            ("NUMBER", Fit.Exact),
            ("NUMBER(10)", Fit.Exact),
            ("NUMBER(10,2)", Fit.Exact),
            ("NUMBER(*,2)", Fit.Exact),
            ("NUMBER(38)", Fit.Exact),
            ("NUMBER(5,-2)", Fit.Exact),
            ("INTEGER", Fit.Exact),
            ("FLOAT", Fit.Exact),
            ("FLOAT(53)", Fit.Exact),
            ("BINARY_FLOAT", Fit.Exact),
            ("BINARY_DOUBLE", Fit.Exact),
            ("CHAR(5)", Fit.Exact),
            ("CHAR(5 CHAR)", Fit.Exact),
            ("NCHAR(5)", Fit.Exact),
            ("VARCHAR2(50)", Fit.Exact),
            ("VARCHAR2(50 CHAR)", Fit.MayBeWider),
            ("NVARCHAR2(20)", Fit.MayBeWider),
            ("RAW(16)", Fit.Exact),
            ("CLOB", Fit.Exact),
            ("NCLOB", Fit.Exact),
            ("BLOB", Fit.Exact),
            ("DATE", Fit.Exact),
            ("TIMESTAMP(3)", Fit.Exact),
            ("TIMESTAMP(6)", Fit.Exact),
            ("TIMESTAMP(6) WITH TIME ZONE", Fit.Exact),
            ("TIMESTAMP(6) WITH LOCAL TIME ZONE", Fit.Exact),
            ("INTERVAL YEAR(2) TO MONTH", Fit.Exact),
            ("INTERVAL DAY(2) TO SECOND(6)", Fit.Exact),
            ("ROWID", Fit.Exact),
            ("UROWID(100)", Fit.MayBeWider)
        ];

        private readonly OracleFixture _fixture;

        public CastFidelityIntegrationTests(OracleFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private async Task ExecuteAsync(string sql)
        {
            await using OracleCommand command = _fixture.Connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(Ct);
        }

        private async Task<Shape> GetShapeAsync(string table, string column)
        {
            await using OracleCommand command = _fixture.Connection.CreateCommand();
            command.BindByName = true;
            command.CommandText = """
                SELECT DATA_TYPE, DATA_LENGTH, DATA_PRECISION, DATA_SCALE, CHAR_LENGTH
                FROM ALL_TAB_COLUMNS
                WHERE OWNER = 'dbo' AND TABLE_NAME = :TableName AND COLUMN_NAME = :ColumnName
                """;
            command.Parameters.Add("TableName", table);
            command.Parameters.Add("ColumnName", column);

            await using OracleDataReader reader = (OracleDataReader)await command.ExecuteReaderAsync(Ct);
            Assert.True(await reader.ReadAsync(Ct), $"Column {table}.{column} not found.");

            string type = reader.GetString(0);
            int? precision = reader.IsDBNull(2) ? null : reader.GetInt32(2);
            int? scale = reader.IsDBNull(3) ? null : reader.GetInt32(3);

            // NUMBER(*,s) is stored as a NUMBER with no precision and a scale, and is the same type as NUMBER(38,s).
            if (type == "NUMBER" && precision is null && scale is not null)
                precision = 38;

            return new Shape(type, reader.GetInt32(1), precision, scale, reader.GetInt32(4));
        }

        private async Task<List<DbColumnInfo>> DiscoverAsync(string table)
        {
            // The same call GetColumnsImplAsync makes: the discovery block, with the owner and table bound by name.
            string sql = Forget.Oracle.Strategies.SqlBuilderStrategy.Instance.GetColumnsSqlBuilder<Widget>().Render(":SchemaName", ":TableName");

            await using OracleCommand command = _fixture.Connection.CreateCommand();
            command.BindByName = true;
            command.CommandText = sql;
            command.Parameters.Add(new OracleParameter("SchemaName", OracleDbType.Varchar2, "dbo", ParameterDirection.Input));
            command.Parameters.Add(new OracleParameter("TableName", OracleDbType.Varchar2, table, ParameterDirection.Input));

            OracleParameter result = new("result", OracleDbType.Clob, ParameterDirection.Output);
            command.Parameters.Add(result);
            await command.ExecuteNonQueryAsync(Ct);

            return JsonSerializer.Deserialize<List<DbColumnInfo>>(((OracleClob)result.Value).Value, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        [Fact]
        public async Task EveryDiscoveredCast_ProducesAColumnShapedLikeTheOneItWasDiscoveredFor()
        {
            string columns = string.Join(", ", ColumnTypes.Select((t, i) => $"\"c{i:D2}\" {t.Definition}"));
            await ExecuteAsync($"CREATE TABLE \"CastFidelity\" ({columns})");

            List<DbColumnInfo> discovered = await DiscoverAsync("CastFidelity");
            List<string> problems = [];

            for (int i = 0; i < ColumnTypes.Length; i++)
            {
                (string definition, Fit fit) = ColumnTypes[i];
                string name = $"c{i:D2}";
                DbColumnInfo info = discovered.Single(c => c.Name == name);

                if (info.IsCastable != true)
                {
                    problems.Add($"{definition}: no cast was discovered.");
                    continue;
                }

                try
                {
                    await ExecuteAsync("DROP TABLE \"CastFidelityCast\" PURGE");
                }
                catch (OracleException)
                {
                    // Nothing to drop the first time around.
                }

                await ExecuteAsync($"CREATE TABLE \"CastFidelityCast\" AS SELECT {info.CastExpression!.Replace("{}", "NULL")} AS \"X\" FROM DUAL");

                Shape original = await GetShapeAsync("CastFidelity", name);
                Shape cast = await GetShapeAsync("CastFidelityCast", "X");

                bool fits = fit == Fit.Exact
                    ? original == cast
                    : original.Type == cast.Type && cast.Length >= original.Length;

                if (!fits)
                    problems.Add($"{definition}: {info.CastExpression} gives {cast}, expected {(fit == Fit.Exact ? "" : "at least ")}{original}.");
            }

            Assert.True(problems.Count == 0, Environment.NewLine + string.Join(Environment.NewLine, problems));
        }
    }
}
