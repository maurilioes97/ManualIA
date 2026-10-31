using System.Net;
using System.Text;
using System.Text.Json;
using ManualIA.Api.Data;
using ManualIA.Api.Models.Dtos;
using ManualIA.Api.Models.Entities;
using ManualIA.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

// Testes executáveis sem pacotes de teste adicionais. Nenhuma chamada externa é feita.
var trechos = new List<TrechoEncontradoResposta>
{
    new(10, 1, 2, "Desligue o motor antes da manutenção."),
    new(20, 2, null, "Inspecione o filtro mensalmente.")
};
var total = 0;
void Verificar(bool condicao, string descricao)
{
    if (!condicao) throw new Exception("FALHOU: " + descricao);
    total++;
    Console.WriteLine("OK: " + descricao);
}

static string Envelope(string conteudo, string motivo = "STOP") => JsonSerializer.Serialize(new
{
    candidates = new[] { new { finishReason = motivo, content = new { parts = new[] { new { text = conteudo } } } } }
});
static HttpResponseMessage Retorno(string corpo, HttpStatusCode status = HttpStatusCode.OK) => new(status)
{
    Content = new StringContent(corpo, Encoding.UTF8, "application/json")
};
static ServicoIA Criar(HttpClient cliente, string? chave = "chave-ficticia", string? modelo = null) => new(cliente,
    new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["GEMINI_API_KEY"] = chave,
        ["Gemini:Modelo"] = modelo,
        ["Gemini:TimeoutSegundos"] = "1"
    }).Build(), NullLogger<ServicoIA>.Instance);

var handler = new TransporteSimulado();
using var cliente = new HttpClient(handler);
var servico = Criar(cliente);
handler.Responder = async (pedido, _) =>
{
    Verificar(pedido.RequestUri!.Host == "generativelanguage.googleapis.com" &&
        pedido.RequestUri.AbsolutePath.EndsWith("gemini-3.5-flash-lite:generateContent"), "Destino e modelo padrão Gemini");
    Verificar(pedido.Headers.GetValues("x-goog-api-key").Single() == "chave-ficticia" &&
        !pedido.RequestUri.Query.Contains("key"), "Chave no cabeçalho, fora da URL");
    using var json = JsonDocument.Parse(await pedido.Content!.ReadAsStringAsync());
    var entrada = json.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString()!;
    using var dados = JsonDocument.Parse(entrada);
    Verificar(dados.RootElement.EnumerateObject().Count() == 2 &&
        dados.RootElement.GetProperty("trechos").GetArrayLength() == 2 &&
        dados.RootElement.GetProperty("pergunta").GetString() == "Como fazer manutenção?",
        "Entrada contém somente pergunta e trechos");
    return Retorno(Envelope("""{"podeResponder":true,"resposta":"Desligue o motor. [Trecho 10]","referencias":[10]}"""));
};
var resultado = await servico.GerarResposta("Como fazer manutenção?", trechos);
Verificar(resultado.GeradaPorIA && resultado.Trechos.Count == 1 && resultado.Trechos[0].ChunkId == 10,
    "Resposta válida usa apenas as referências selecionadas");
Verificar(resultado.Conteudo == "Desligue o motor.",
    "Referência numérica não aparece no texto da resposta");

var chamadasAntesSaudacao = handler.Chamadas;
resultado = await servico.GerarResposta("Olá!", trechos);
Verificar(resultado.GeradaPorIA && resultado.Motivo == "saudacao" && resultado.Trechos.Count == 0 &&
    resultado.Conteudo.Contains("Simpática") && handler.Chamadas == chamadasAntesSaudacao,
    "Saudação recebe resposta local sem consumir API");

handler.Responder = (pedido, _) =>
{
    Verificar(pedido.RequestUri!.AbsolutePath.EndsWith("modelo-teste:generateContent"),
        "Modelo explicitamente configurado prevalece sobre o padrão");
    return Task.FromResult(Retorno(Envelope("""{"podeResponder":true,"resposta":"Desligue o motor.","referencias":[10]}""")));
};
await Criar(cliente, modelo: "modelo-teste").GerarResposta("Pergunta", trechos);

var chamadas = handler.Chamadas;
Verificar((await servico.GerarResposta("Sem fonte", [])).Motivo == "sem_trechos", "Sem fontes: mensagem local");
Verificar((await Criar(cliente, null).GerarResposta("Pergunta", trechos)).Motivo == "nao_configurada", "Sem chave: trechos disponíveis");
Verificar(handler.Chamadas == chamadas, "Sem chave ou fontes não consome API");

foreach (var caso in new[]
{
    (HttpStatusCode.TooManyRequests, "limite_atingido"),
    (HttpStatusCode.NotFound, "modelo_indisponivel"),
    (HttpStatusCode.Unauthorized, "falha_ia"),
    (HttpStatusCode.InternalServerError, "falha_ia")
})
{
    handler.Responder = (_, _) => Task.FromResult(Retorno("erro externo", caso.Item1));
    chamadas = handler.Chamadas;
    resultado = await servico.GerarResposta("Pergunta", trechos);
    Verificar(!resultado.GeradaPorIA && resultado.Motivo == caso.Item2 && resultado.Trechos.Count == 2 &&
        handler.Chamadas == chamadas + 1, $"HTTP {(int)caso.Item1}: preserva trechos sem repetir chamada");
}

foreach (var corpo in new[]
{
    Envelope("""{"podeResponder":true,"resposta":"Inventada","referencias":[999]}"""),
    Envelope("""{"podeResponder":false,"resposta":"","referencias":[]}"""),
    Envelope("""{"podeResponder":true,"resposta":"","referencias":[10]}"""),
    Envelope("""{"podeResponder":true,"resposta":"Sem fonte","referencias":[]}"""),
    Envelope("{}"),
    Envelope("incompleto", "MAX_TOKENS"),
    """{"promptFeedback":{"blockReason":"SAFETY"}}""",
    """{"candidates":[]}""",
    "json inválido"
})
{
    handler.Responder = (_, _) => Task.FromResult(Retorno(corpo));
    resultado = await servico.GerarResposta("Pergunta", trechos);
    Verificar(!resultado.GeradaPorIA && resultado.Trechos.Count == 2, "Resposta inválida/recusada preserva fontes");
}
handler.Responder = (_, _) => throw new HttpRequestException("Erro de rede simulado");
Verificar((await servico.GerarResposta("Pergunta", trechos)).Motivo == "falha_ia", "Falha de conexão");
handler.Responder = async (_, token) => { await Task.Delay(5000, token); return Retorno("{}"); };
Verificar((await servico.GerarResposta("Pergunta", trechos)).Motivo == "falha_ia", "Timeout com alternativa local");
using var cancelamento = new CancellationTokenSource();
cancelamento.Cancel();
try
{
    await servico.GerarResposta("Pergunta", trechos, cancelamento.Token);
    Verificar(false, "Cancelamento deve ser propagado");
}
catch (OperationCanceledException) { Verificar(true, "Cancelamento do cliente propagado"); }

if (args.Contains("--banco"))
{
    // Executar da raiz do repositório. Fixtures novas e rollback, sem ler manuais reais.
    var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath("backend/ManualIA.Api/appsettings.json")).Build();
    await using var banco = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(config.GetConnectionString("Banco")).Options);
    await using var transacao = await banco.Database.BeginTransactionAsync();
    var usuario = new Usuario { Nome = "Teste CDU07", EmailNormalizado = Guid.NewGuid().ToString(),
        Perfil = new Perfil { Nome = "Teste-" + Guid.NewGuid() } };
    banco.Usuarios.Add(usuario);
    await banco.SaveChangesAsync();
    var manual = new Manual { Titulo = "Fixture CDU07", Descricao = "Somente teste", CadastradoPorId = usuario.Id,
        Arquivo = new ArquivoManual { QuantidadeChunks = 1 },
        Chunks = [new ChunkManual { Ordem = 1, Pagina = 1, Texto = "Desligue o motor antes da manutenção." }] };
    var outro = new Manual { Titulo = "Outro manual", CadastradoPorId = usuario.Id,
        Chunks = [new ChunkManual { Ordem = 1, Texto = "SEGREDO_OUTRO_MANUAL: manutenção." }] };
    banco.Manuais.AddRange(manual, outro);
    await banco.SaveChangesAsync();
    var chat = new ChatService(banco, servico);
    var abertura = await chat.AbrirSessao(manual.Id, usuario.Id);
    Verificar(abertura.Sucesso, "Abertura de sessão com manual disponível");
    handler.Responder = async (pedido, _) =>
    {
        var corpo = await pedido.Content!.ReadAsStringAsync();
        Verificar(!corpo.Contains("SEGREDO_OUTRO_MANUAL"), "Não envia chunks de outro manual");
        return Retorno(Envelope(JsonSerializer.Serialize(new { podeResponder = true,
            resposta = "Desligue o motor.", referencias = new[] { manual.Chunks[0].Id } })));
    };
    var pergunta = await chat.EnviarPergunta(abertura.Dados!.Id, usuario.Id, "Como fazer manutenção?");
    Verificar(pergunta.Sucesso && pergunta.Dados!.GeradaPorIA, "Chat retorna resposta gerada");
    var mensagens = await banco.MensagensChat.AsNoTracking().Where(x => x.SessaoChatId == abertura.Dados.Id).ToListAsync();
    Verificar(mensagens.Count == 2 && mensagens.Any(x => x.Papel == "Usuario") &&
        mensagens.Any(x => x.Id == pergunta.Dados!.RespostaId && x.ReferenciasJson!.Contains("Desligue")),
        "Pergunta, resposta e snapshot persistidos no PostgreSQL");
    chamadas = handler.Chamadas;
    Verificar((await chat.EnviarPergunta(abertura.Dados.Id, -1, "manutenção")).Status == 404, "Bloqueia conversa de outro usuário");
    Verificar((await chat.EnviarPergunta(abertura.Dados.Id, usuario.Id, " ")).Status == 400, "Bloqueia pergunta vazia");
    Verificar((await chat.EnviarPergunta(abertura.Dados.Id, usuario.Id, "astronomia")).Dados!.Motivo == "sem_trechos",
        "Sem correspondência registra aviso do sistema");
    Verificar(handler.Chamadas == chamadas, "Validações e ausência de fonte não chamam Gemini");
    handler.Responder = (_, _) => Task.FromResult(Retorno("quota", HttpStatusCode.TooManyRequests));
    pergunta = await chat.EnviarPergunta(abertura.Dados.Id, usuario.Id, "manutenção");
    Verificar(pergunta.Dados!.Motivo == "limite_atingido" &&
        await banco.MensagensChat.CountAsync(x => x.SessaoChatId == abertura.Dados.Id) == 6,
        "Quota esgotada também persiste pergunta e aviso");
    manual.Excluido = true;
    await banco.SaveChangesAsync();
    Verificar((await chat.EnviarPergunta(abertura.Dados.Id, usuario.Id, "manutenção")).Status == 409,
        "Manual indisponível bloqueia pergunta");
    await transacao.RollbackAsync();
    Console.WriteLine("Fixtures de banco revertidas por rollback.");
}
Console.WriteLine($"{total} verificações passaram; nenhuma chamada real ao Gemini.");

sealed class TransporteSimulado : HttpMessageHandler
{
    public int Chamadas { get; private set; }
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Responder { get; set; }
        = (_, _) => throw new Exception("Chamada inesperada");
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Chamadas++;
        return Responder(request, cancellationToken);
    }
}
