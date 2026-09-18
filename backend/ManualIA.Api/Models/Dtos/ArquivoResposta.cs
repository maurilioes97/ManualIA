namespace ManualIA.Api.Models.Dtos;

public record ArquivoResposta(
    string Nome,
    string Tipo,
    long Tamanho,
    DateTime EnviadoEm,
    int QuantidadeChunks);
