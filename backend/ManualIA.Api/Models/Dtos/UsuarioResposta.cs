namespace ManualIA.Api.Models;

public record UsuarioResposta(
    int Id,
    string Nome,
    string Email,
    int PerfilId,
    string Perfil,
    bool Ativo);
