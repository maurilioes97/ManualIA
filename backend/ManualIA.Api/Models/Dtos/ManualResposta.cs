namespace ManualIA.Api.Models.Dtos;

public record ManualResposta(
    int Id,
    string Titulo,
    string Descricao,
    DateTime CriadoEm,
    DateTime AtualizadoEm,
    ArquivoResposta? Arquivo);
