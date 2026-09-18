namespace ManualIA.Api.Models.Entities;

public class Manual
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public int CadastradoPorId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public bool Excluido { get; set; }
    public ArquivoManual? Arquivo { get; set; }
    public List<ChunkManual> Chunks { get; set; } = [];
}
