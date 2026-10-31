using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ManualIA.Api.Data;
using ManualIA.Api.Models;
using ManualIA.Api.Models.Dtos;
using ManualIA.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ManualIA.Api.Services;

public partial class ChatService(AppDbContext banco, ServicoIA servicoIA)
{
    private static readonly HashSet<string> PalavrasIgnoradas = new(StringComparer.Ordinal)
    {
        "a", "ao", "aos", "as", "com", "como", "da", "das", "de", "do", "dos",
        "e", "em", "esta", "este", "eu", "manual", "na", "nas", "no", "nos", "o",
        "os", "ou", "para", "por", "qual", "quais", "que", "se", "sem", "um", "uma"
    };

    public async Task<List<ConversaHistoricoResumoResposta>> ListarHistorico(int usuarioId)
        => await banco.SessoesChat.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.AtualizadaEm)
            .Select(x => new ConversaHistoricoResumoResposta(
                x.Id,
                x.ManualId,
                x.Manual.Titulo,
                x.CriadaEm,
                x.AtualizadaEm,
                x.Mensagens.Count,
                x.Mensagens
                    .Where(m => m.Papel == "Usuario")
                    .OrderByDescending(m => m.CriadaEm)
                    .Select(m => m.Conteudo)
                    .FirstOrDefault()))
            .ToListAsync();

    public async Task<ResultadoServico<ConversaHistoricoResposta>> ObterHistorico(
        int sessaoId, int usuarioId)
    {
        var sessao = await banco.SessoesChat.AsNoTracking()
            .Include(x => x.Manual).ThenInclude(x => x.Arquivo)
            .Include(x => x.Mensagens)
            .SingleOrDefaultAsync(x => x.Id == sessaoId && x.UsuarioId == usuarioId);

        if (sessao is null)
            return ResultadoServico<ConversaHistoricoResposta>.ComErro(
                "Conversa não encontrada.", 404);

        var mensagens = sessao.Mensagens.OrderBy(x => x.CriadaEm).ThenBy(x => x.Id).ToList();
        var interacoes = new List<PerguntaProcessadaResposta>();

        for (var indice = 0; indice < mensagens.Count; indice++)
        {
            var pergunta = mensagens[indice];
            if (pergunta.Papel != "Usuario") continue;

            var resposta = mensagens.Skip(indice + 1)
                .FirstOrDefault(x => x.Papel is "Assistente" or "Sistema");
            if (resposta is null) continue;

            var referencias = DesserializarReferencias(resposta.ReferenciasJson);
            interacoes.Add(new PerguntaProcessadaResposta(
                sessao.Id,
                pergunta.Id,
                pergunta.Conteudo,
                pergunta.CriadaEm,
                referencias.Trechos,
                resposta.Id,
                resposta.Conteudo,
                referencias.GeradaPorIA,
                referencias.Motivo));
        }

        var dadosSessao = new SessaoChatResposta(
            sessao.Id,
            sessao.ManualId,
            sessao.Manual.Titulo,
            sessao.Manual.Arquivo?.TipoArquivo ?? string.Empty,
            sessao.CriadaEm);

        return ResultadoServico<ConversaHistoricoResposta>.ComSucesso(
            new ConversaHistoricoResposta(dadosSessao, interacoes));
    }

    public async Task<ResultadoServico<bool>> ExcluirConversa(int sessaoId, int usuarioId)
    {
        var sessao = await banco.SessoesChat
            .Include(x => x.Mensagens)
            .SingleOrDefaultAsync(x => x.Id == sessaoId && x.UsuarioId == usuarioId);

        if (sessao is null)
            return ResultadoServico<bool>.ComErro("Conversa não encontrada.", 404);

        banco.MensagensChat.RemoveRange(sessao.Mensagens);
        banco.SessoesChat.Remove(sessao);
        await banco.SaveChangesAsync();
        return ResultadoServico<bool>.ComSucesso(true);
    }

    public async Task<ResultadoServico<SessaoChatResposta>> AbrirSessao(
        int manualId, int usuarioId)
    {
        var manual = await banco.Manuais
            .Where(x => x.Id == manualId && !x.Excluido &&
                x.Arquivo != null && x.Arquivo.QuantidadeChunks > 0)
            .Select(x => new { x.Id, x.Titulo, x.Arquivo!.TipoArquivo })
            .SingleOrDefaultAsync();

        if (manual is null)
            return ResultadoServico<SessaoChatResposta>.ComErro(
                "Manual não encontrado ou indisponível para consulta.", 404);

        var sessao = new SessaoChat
        {
            UsuarioId = usuarioId,
            ManualId = manual.Id
        };

        banco.SessoesChat.Add(sessao);
        await banco.SaveChangesAsync();

        return ResultadoServico<SessaoChatResposta>.ComSucesso(new SessaoChatResposta(
            sessao.Id, manual.Id, manual.Titulo, manual.TipoArquivo, sessao.CriadaEm));
    }

    public async Task<ResultadoServico<PerguntaProcessadaResposta>> EnviarPergunta(
        int sessaoId, int usuarioId, string pergunta, CancellationToken cancelamento = default)
    {
        pergunta = pergunta.Trim();
        if (pergunta.Length == 0)
            return ResultadoServico<PerguntaProcessadaResposta>.ComErro("Informe uma pergunta.", 400);
        if (pergunta.Length > 1000)
            return ResultadoServico<PerguntaProcessadaResposta>.ComErro("A pergunta deve ter no máximo 1000 caracteres.", 400);

        var sessao = await banco.SessoesChat
            .Include(x => x.Manual).ThenInclude(x => x.Arquivo)
            .SingleOrDefaultAsync(x => x.Id == sessaoId && x.UsuarioId == usuarioId, cancelamento);

        if (sessao is null)
            return ResultadoServico<PerguntaProcessadaResposta>.ComErro("Conversa não encontrada.", 404);

        if (sessao.Manual.Excluido || sessao.Manual.Arquivo is null ||
            !await banco.ChunksManuais.AnyAsync(x => x.ManualId == sessao.ManualId, cancelamento))
            return ResultadoServico<PerguntaProcessadaResposta>.ComErro(
                "O manual desta conversa não está mais disponível.", 409);

        var mensagem = new MensagemChat
        {
            SessaoChatId = sessao.Id,
            Papel = "Usuario",
            Conteudo = pergunta
        };
        var termos = ExtrairTermos(pergunta);

        var chunks = await banco.ChunksManuais.AsNoTracking()
            .Where(x => x.ManualId == sessao.ManualId)
            .Select(x => new { x.Id, x.Ordem, x.Pagina, x.Texto })
            .ToListAsync(cancelamento);

        var trechos = chunks
            .Select(x => new
            {
                Chunk = x,
                Pontuacao = CalcularPontuacao(x.Texto, termos)
            })
            .Where(x => x.Pontuacao > 0)
            .OrderByDescending(x => x.Pontuacao)
            .ThenBy(x => x.Chunk.Ordem)
            .Take(5)
            .Select(x => new TrechoEncontradoResposta(
                x.Chunk.Id, x.Chunk.Ordem, x.Chunk.Pagina, x.Chunk.Texto))
            .ToList();

        var geracao = await servicoIA.GerarResposta(pergunta, trechos, cancelamento);
        var resposta = new MensagemChat
        {
            SessaoChatId = sessao.Id,
            Papel = geracao.GeradaPorIA ? "Assistente" : "Sistema",
            Conteudo = geracao.Conteudo,
            // Snapshot preserva os trechos mesmo após a substituição do arquivo.
            ReferenciasJson = JsonSerializer.Serialize(new
            {
                geracao.Motivo, geracao.GeradaPorIA, geracao.Trechos
            }, JsonSerializerOptions.Web)
        };
        sessao.AtualizadaEm = resposta.CriadaEm;
        banco.MensagensChat.AddRange(mensagem, resposta);
        // Um único SaveChanges grava pergunta e resposta atomicamente.
        await banco.SaveChangesAsync(cancelamento);

        return ResultadoServico<PerguntaProcessadaResposta>.ComSucesso(
            new PerguntaProcessadaResposta(
                sessao.Id, mensagem.Id, mensagem.Conteudo, mensagem.CriadaEm,
                geracao.Trechos, resposta.Id, resposta.Conteudo, geracao.GeradaPorIA, geracao.Motivo));
    }

    private static List<string> ExtrairTermos(string texto) => PalavraRegex()
        .Matches(Normalizar(texto))
        .Select(x => x.Value)
        .Where(x => x.Length >= 3 && !PalavrasIgnoradas.Contains(x))
        .Distinct(StringComparer.Ordinal)
        .ToList();

    private static int CalcularPontuacao(string texto, List<string> termos)
    {
        var normalizado = Normalizar(texto);
        return termos.Sum(termo => Regex.Matches(
            normalizado, $@"\b{Regex.Escape(termo)}\w*", RegexOptions.CultureInvariant).Count);
    }

    private static string Normalizar(string texto)
    {
        var decomposicao = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var resultado = new StringBuilder(decomposicao.Length);

        foreach (var caractere in decomposicao)
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                resultado.Append(caractere);

        return resultado.ToString().Normalize(NormalizationForm.FormC);
    }

    private static ReferenciasHistorico DesserializarReferencias(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new(false, "historico", []);

        try
        {
            return JsonSerializer.Deserialize<ReferenciasHistorico>(json, JsonSerializerOptions.Web)
                ?? new ReferenciasHistorico(false, "historico", []);
        }
        catch (JsonException)
        {
            return new ReferenciasHistorico(false, "historico", []);
        }
    }

    private sealed record ReferenciasHistorico(
        bool GeradaPorIA,
        string Motivo,
        List<TrechoEncontradoResposta> Trechos);

    [GeneratedRegex(@"[a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex PalavraRegex();
}
