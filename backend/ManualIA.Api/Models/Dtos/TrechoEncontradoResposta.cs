namespace ManualIA.Api.Models.Dtos;

public record TrechoEncontradoResposta(
    int ChunkId,
    int Ordem,
    int? Pagina,
    string Texto);
