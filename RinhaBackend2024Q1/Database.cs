using Microsoft.AspNetCore.Connections;
using MySqlConnector;

namespace RinhaBackend2024Q1;

public sealed class DbService
{
    readonly string _connectionString;

    readonly HashSet<int> _clientIds = new HashSet<int> { 1, 2, 3, 4, 5 };

    public DbService(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    const string _sqlExtrato = @"
SELECT
    A.NR_LIMITE
    , A.NR_SALDO
    , A.NR_VALOR
    , A.CD_TYPE
    , A.DS_TRANSACAO
    , A.DT_REALIZADO_FORMATTED
FROM TRANSACAO_VW A
WHERE A.ID_CLIENTE = @P_ID_CLIENTE
ORDER BY A.DT_REALIZADO DESC
LIMIT 10;
";

    internal async Task<IResult> Extrato(int id, CancellationToken cancellationToken)
    {
        if (_clientIds.Contains(id) == false) return Results.NotFound();

        using var conn = new MySqlConnection(_connectionString);

        using var cmd = new MySqlCommand(_sqlExtrato, conn);
        cmd.Parameters.AddWithValue("@P_ID_CLIENTE", id);

        await conn.OpenAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        using var reader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection, cancellationToken);

        await reader.ReadAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var saldo = new Saldo
        {
            DataExtrato = DateTime.Now,
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

    internal async Task<IResult> Transacao(int id, TransacaoRequest transacao)
    {
        if (_clientIds.Contains(id) == false) return Results.NotFound();

        using var conn = new MySqlConnection(_connectionString);

        using var cmd = new MySqlCommand("CRIAR_TRANSACAO", conn);

        cmd.CommandType = System.Data.CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@P_ID_CLIENTE", id);
        cmd.Parameters.AddWithValue("@P_NR_VALUE", transacao.Tipo == 'c' ? transacao.Valor : transacao.Valor * -1);
        cmd.Parameters.AddWithValue("@P_CD_TYPE", transacao.Tipo);
        cmd.Parameters.AddWithValue("@P_DS_TRANSACAO", transacao.Descricao);

        var pOut = cmd.CreateParameter();
        pOut.Direction = System.Data.ParameterDirection.Output;
        pOut.ParameterName = "@P_OUT_RESULT";
        pOut.DbType = System.Data.DbType.Int32;
        cmd.Parameters.Add(pOut);

        var pOutLimite = cmd.CreateParameter();
        pOutLimite.Direction = System.Data.ParameterDirection.Output;
        pOutLimite.ParameterName = "@P_OUT_LIMITE";
        pOutLimite.DbType = System.Data.DbType.Int32;
        cmd.Parameters.Add(pOutLimite);

        var pOutSaldo = cmd.CreateParameter();
        pOutSaldo.Direction = System.Data.ParameterDirection.Output;
        pOutSaldo.ParameterName = "@P_OUT_SALDO";
        pOutSaldo.DbType = System.Data.DbType.Int32;
        cmd.Parameters.Add(pOutSaldo);

        await conn.OpenAsync();

        await cmd.ExecuteNonQueryAsync();

        var resu = (int)pOut.Value;

        await conn.CloseAsync();

        if (resu == 1) return Results.Ok(new { limite = (int)pOutLimite.Value, saldo = (int)pOutSaldo.Value });

        return Results.UnprocessableEntity();
    }
}
