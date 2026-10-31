namespace ManualIA.Api.Models.Dtos;

public record SessaoChatResposta(
    int Id,
    int ManualId,
    string ManualTitulo,
    string ManualTipoArquivo,
    DateTime CriadaEm);
