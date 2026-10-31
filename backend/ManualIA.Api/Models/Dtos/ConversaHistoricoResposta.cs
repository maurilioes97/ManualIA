namespace ManualIA.Api.Models.Dtos;

public record ConversaHistoricoResumoResposta(
    int Id,
    int ManualId,
    string ManualTitulo,
    DateTime CriadaEm,
    DateTime AtualizadaEm,
    int QuantidadeMensagens,
    string? UltimaPergunta);

public record ConversaHistoricoResposta(
    SessaoChatResposta Sessao,
    List<PerguntaProcessadaResposta> Interacoes);
