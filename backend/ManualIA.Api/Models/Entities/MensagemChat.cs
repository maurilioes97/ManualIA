namespace ManualIA.Api.Models.Entities;

public class MensagemChat
{
    public int Id { get; set; }
    public int SessaoChatId { get; set; }
    public SessaoChat SessaoChat { get; set; } = null!;
    public string Papel { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
    public string? ReferenciasJson { get; set; }
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
}
