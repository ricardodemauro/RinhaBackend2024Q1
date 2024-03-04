using Npgsql;

namespace RinhaBackend.DbPostgresql;

public sealed class DbPostgreSql
{
    readonly string _connectionString;

    readonly HashSet<int> _clientIds = new HashSet<int> { 1, 2, 3, 4, 5 };

    public DbPostgreSql(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    const string _sqlExtrato = @"
SELECT
    A.""NR_LIMITE""
    , A.""NR_SALDO""
    , A.""NR_VALOR""
    , A.""CD_TYPE""
    , A.""DS_TRANSACAO""
    , A.""dt_realizado_formatted""
    , to_char(now(), 'YYYY-MM-DD""T""HH:MI:SS:MS""Z""'::text) AS DT_EXTRATO
FROM ""TRANSACAO_VW"" A
WHERE A.""ID_CLIENTE"" = @P_ID_CLIENTE
ORDER BY A.""DT_REALIZADO"" DESC
LIMIT 10;
";

    internal async Task<IResult> Extrato(int id, CancellationToken cancellationToken)
    {
        if (_clientIds.Contains(id) == false) return Results.NotFound();

        using var conn = new NpgsqlConnection(_connectionString);

        using var cmd = new NpgsqlCommand(_sqlExtrato, conn);
        cmd.Parameters.AddWithValue("@P_ID_CLIENTE", id);

        await conn.OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        await reader.ReadAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        int idCli = reader.GetOrdinal("NR_LIMITE");

        var saldo = new Saldo
        {
            DataExtrato = reader.GetString(6),
            Limite = reader.GetInt32(0),
            Total = reader.GetInt32(1),
        };

        var transacoes = new List<Transacao>(10);

        do
        {
            var transacaoType = reader.GetChar(3);
            if (transacaoType == 'S') continue;

            var t = new Transacao
            {
                Descricao = reader.GetString(4),
                RealizadoEm = reader.GetString(5),
                Tipo = transacaoType,
                Valor = reader.GetInt32(2)
            };
            transacoes.Add(t);
        } while (await reader.ReadAsync(cancellationToken));


        var extrato = new ExtratoResult
        {
            Saldo = saldo,
            UltimasTransacoes = transacoes
        };

        return Results.Ok(extrato);
    }

    internal async Task<IResult> Transacao(int id, TransacaoRequest transacao, CancellationToken cancellationToken)
    {
        if (_clientIds.Contains(id) == false) return Results.NotFound();

        cancellationToken.ThrowIfCancellationRequested();

        using var conn = new NpgsqlConnection(_connectionString);

        using var cmd = new NpgsqlCommand("select FN_criar_transacao_v2($1, $2, $3, $4)", conn)
        {
            Parameters =
            {
                new NpgsqlParameter<int>() { Value = id },
                new NpgsqlParameter<int>() { Value = transacao.Tipo == 'c' ? transacao.Valor : transacao.Valor * -1 },
                new NpgsqlParameter<char>() { Value = transacao.Tipo },
                new NpgsqlParameter<string>() { Value = transacao.Descricao }
            }
        };

        cmd.CommandType = System.Data.CommandType.Text;

        await conn.OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        await reader.ReadAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var record = reader.GetFieldValue<object[]>(0);
        if (record.Length == 1) throw new InvalidOperationException("Invalid failure code.");

        var resu = (int)record[0];

        await conn.CloseAsync();

        if (resu == 1)
        {
            var (saldo, limite) = ((int)record[1], (int)record[2]);
            return Results.Ok(new TransacaoResult { Limite = limite, Saldo = saldo });
        }

        return Results.UnprocessableEntity();
    }
}
