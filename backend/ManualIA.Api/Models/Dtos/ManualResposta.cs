namespace ManualIA.Api.Models;

public record ManualResposta(
    int Id,
    string Titulo,
    string Descricao,
    DateTime CriadoEm,
    DateTime AtualizadoEm,
    ArquivoResposta? Arquivo);
