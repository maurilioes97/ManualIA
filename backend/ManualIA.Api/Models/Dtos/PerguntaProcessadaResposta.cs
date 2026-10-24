namespace ManualIA.Api.Models.Dtos;

public record PerguntaProcessadaResposta(
    int SessaoId,
    int MensagemId,
    string Pergunta,
    DateTime EnviadaEm,
    List<TrechoEncontradoResposta> Trechos,
    int RespostaId,
    string Resposta,
    bool GeradaPorIA,
    string Motivo);
