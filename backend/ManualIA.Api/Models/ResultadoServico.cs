namespace ManualIA.Api.Models;

public class ResultadoServico<T>
{
    public T? Dados { get; private init; }
    public string? Erro { get; private init; }
    public int Status { get; private init; } = 200;
    public bool Sucesso => Erro is null;

    public static ResultadoServico<T> ComSucesso(T dados) => new() { Dados = dados };

    public static ResultadoServico<T> ComErro(string erro, int status) =>
        new() { Erro = erro, Status = status };
}
