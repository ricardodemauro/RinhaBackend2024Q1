namespace RinhaBackend.DbPostgresql;

public static class Validators
{
    public static bool IsValid(this TransacaoRequest rq)
    {
        if (string.IsNullOrEmpty(rq.Descricao)) return false;
        if (!(rq.Descricao.Length >= 1 && rq.Descricao.Length <= 10)) return false;
        if (!(rq.Tipo == 'c' || rq.Tipo == 'd')) return false;

        return true;
    }
}
