namespace ManualIA.Api.Models;

public record ArquivoResposta(
    string Nome,
    string Tipo,
    long Tamanho,
    DateTime EnviadoEm,
    int QuantidadeChunks);
