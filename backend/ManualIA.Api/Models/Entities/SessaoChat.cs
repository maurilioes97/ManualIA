namespace ManualIA.Api.Models.Entities;

public class SessaoChat
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int ManualId { get; set; }
    public Manual Manual { get; set; } = null!;
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadaEm { get; set; } = DateTime.UtcNow;
    public List<MensagemChat> Mensagens { get; set; } = [];
}
