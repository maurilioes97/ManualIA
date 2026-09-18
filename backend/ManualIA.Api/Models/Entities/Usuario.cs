namespace ManualIA.Api.Models.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EmailNormalizado { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public int PerfilId { get; set; }
    public Perfil Perfil { get; set; } = null!;
    public int? CadastradoPorId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public bool Ativo { get; set; } = true;
}
