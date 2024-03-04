using System.Text.Json.Serialization;

namespace RinhaBackend.DbMysql;

public class TransacaoRequest
{
    public int Valor { get; set; }

    public char Tipo { get; set; }

    public string Descricao { get; set; }
}


public class TransacaoResult
{
    public int Limite { get; set; }

    public int Saldo { get; set; }
}

public class ExtratoResult
{
    public Saldo Saldo { get; set; } = new Saldo();

    [JsonPropertyName("ultimas_transacoes")]
    public List<Transacao> UltimasTransacoes { get; set; } = new List<Transacao>();
}

public class Saldo
{
    public int Total { get; set; }

    [JsonPropertyName("data_extrato")]
    public string DataExtrato { get; set; }

    public int Limite { get; set; }
}

public class Transacao
{
    public int Valor { get; set; }

    public char Tipo { get; set; }

    public string Descricao { get; set; }

    [JsonPropertyName("realizado_em")]
    public string RealizadoEm { get; set; }
}

[JsonSerializable(typeof(Saldo))]
[JsonSerializable(typeof(ExtratoResult))]
[JsonSerializable(typeof(Transacao))]
[JsonSerializable(typeof(TransacaoRequest))]
[JsonSerializable(typeof(TransacaoResult))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}