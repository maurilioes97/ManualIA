namespace ManualIA.Api.Models;

public class ArquivoManual
{
    public int Id { get; set; }
    public int ManualId { get; set; }
    public Manual Manual { get; set; } = null!;
    public string NomeOriginal { get; set; } = string.Empty;
    public string CaminhoArquivo { get; set; } = string.Empty;
    public string TipoArquivo { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;
    public int QuantidadeChunks { get; set; }
}
