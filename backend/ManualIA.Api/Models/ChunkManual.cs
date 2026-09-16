namespace ManualIA.Api.Models;

public class ChunkManual
{
    public int Id { get; set; }
    public int ManualId { get; set; }
    public Manual Manual { get; set; } = null!;
    public string Texto { get; set; } = string.Empty;
    public int Ordem { get; set; }
    public int? Pagina { get; set; }
}
