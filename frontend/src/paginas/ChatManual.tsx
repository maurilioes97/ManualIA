import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type FormEvent,
  type KeyboardEvent,
} from 'react'
import { enviarPergunta, obterPdfManual } from '../api'
import type { Manual, PerguntaProcessada, SessaoChat } from '../tipos'

type Propriedades = {
  sessao: SessaoChat
  manuais: Manual[]
  manualAbrindoChat: number | null
  aoAbrirChat: (manualId: number) => Promise<void>
  aoFechar: () => void
}

function TextoGradual({
  texto,
  aoAtualizar,
}: {
  texto: string
  aoAtualizar: () => void
}) {
  const palavras = useMemo(() => texto.match(/\S+\s*/g) ?? [texto], [texto])
  const reduzirMovimento = window.matchMedia('(prefers-reduced-motion: reduce)').matches
  const [quantidadeVisivel, setQuantidadeVisivel] = useState(
    reduzirMovimento ? palavras.length : 0,
  )

  useEffect(() => {
    if (reduzirMovimento) return

    const intervalo = window.setInterval(() => {
      setQuantidadeVisivel(atual => {
        if (atual >= palavras.length) {
          window.clearInterval(intervalo)
          return atual
        }
        return atual + 1
      })
    }, 55)

    return () => window.clearInterval(intervalo)
  }, [palavras, reduzirMovimento])

  useEffect(() => {
    aoAtualizar()
  }, [quantidadeVisivel, aoAtualizar])

  return (
    <p className="texto-resposta" aria-label={texto}>
      <span aria-hidden="true">
        {palavras.slice(0, quantidadeVisivel).join('')}
      </span>
      {quantidadeVisivel < palavras.length && (
        <span className="cursor-digitacao" aria-hidden="true" />
      )}
    </p>
  )
}

export function ChatManual({
  sessao,
  manuais,
  manualAbrindoChat,
  aoAbrirChat,
  aoFechar,
}: Propriedades) {
  const [pergunta, setPergunta] = useState('')
  const [resultados, setResultados] = useState<PerguntaProcessada[]>([])
  const [paginasVisiveis, setPaginasVisiveis] = useState<Set<number>>(new Set())
  const [enviando, setEnviando] = useState(false)
  const [abrindoManual, setAbrindoManual] = useState(false)
  const [manualAberto, setManualAberto] = useState(false)
  const [urlManual, setUrlManual] = useState('')
  const [erro, setErro] = useState('')
  const fimDoChat = useRef<HTMLDivElement>(null)
  const campoPergunta = useRef<HTMLTextAreaElement>(null)

  useEffect(() => {
    fimDoChat.current?.scrollIntoView({ behavior: 'smooth', block: 'end' })
  }, [resultados, enviando, erro])

  useEffect(() => {
    if (!enviando) campoPergunta.current?.focus()
  }, [enviando])

  useEffect(() => {
    return () => {
      if (urlManual) URL.revokeObjectURL(urlManual)
    }
  }, [urlManual])

  function alternarPaginas(mensagemId: number) {
    setPaginasVisiveis(atuais => {
      const novos = new Set(atuais)
      if (novos.has(mensagemId)) novos.delete(mensagemId)
      else novos.add(mensagemId)
      return novos
    })
  }

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    if (enviando) return
    if (!pergunta.trim()) {
      setErro('Informe uma pergunta.')
      return
    }

    setEnviando(true)
    setErro('')

    try {
      const resultado = await enviarPergunta(sessao.id, pergunta)
      setResultados(atuais => [...atuais, resultado])
      setPergunta('')
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setEnviando(false)
    }
  }

  async function alternarManual() {
    if (manualAberto) {
      setManualAberto(false)
      return
    }
    if (urlManual) {
      setManualAberto(true)
      return
    }

    setAbrindoManual(true)
    setErro('')
    try {
      const arquivo = await obterPdfManual(sessao.manualId)
      setUrlManual(URL.createObjectURL(arquivo))
      setManualAberto(true)
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setAbrindoManual(false)
    }
  }

  function tratarTecla(evento: KeyboardEvent<HTMLTextAreaElement>) {
    if (evento.key !== 'Enter' || evento.shiftKey || evento.nativeEvent.isComposing)
      return

    evento.preventDefault()
    if (!evento.repeat && !enviando) evento.currentTarget.form?.requestSubmit()
  }

  const outrosManuais = manuais.filter(manual => manual.id !== sessao.manualId)

  return (
    <section className="chat-pagina">
      <div className="chat-layout">
      <aside className="manuais-laterais" aria-label="Outros manuais disponíveis">
        <div className="titulo-manuais-laterais">
          <span aria-hidden="true">▤</span>
          <div>
            <h2>Outros manuais</h2>
            <p>Abra uma nova conversa</p>
          </div>
        </div>
        <div className="lista-manuais-laterais">
          {outrosManuais.map(manual => (
            <button
              type="button"
              className="manual-lateral"
              key={manual.id}
              disabled={manualAbrindoChat !== null || enviando}
              onClick={() => void aoAbrirChat(manual.id)}
            >
              <strong>{manual.titulo}</strong>
              <span>
                {manualAbrindoChat === manual.id ? 'Abrindo...' : 'Abrir chat →'}
              </span>
            </button>
          ))}
          {outrosManuais.length === 0 && (
            <p className="sem-outros-manuais">Não há outros manuais disponíveis.</p>
          )}
        </div>
      </aside>
      <div className="chat-painel">
      <div className="cabecalho-chat">
        <div className="apresentacao-chat">
          <img className="avatar-simpatica avatar-grande" src="/simpatica-avatar.png" alt="Simpática" />
          <div>
            <h1>Simpática <span>— Assistente de Manuais</span></h1>
            <p>{sessao.manualTitulo}</p>
          </div>
        </div>
        <div className="acoes cabecalho-chat-acoes">
          {sessao.manualTipoArquivo?.toUpperCase() === 'PDF' && (
            <button
              type="button"
              onClick={() => void alternarManual()}
              disabled={abrindoManual}
            >
              {abrindoManual ? 'Abrindo manual...' : manualAberto ? 'Fechar manual' : 'Abrir manual'}
            </button>
          )}
          <button type="button" className="secundario" onClick={aoFechar} disabled={enviando}>
            Voltar aos manuais
          </button>
        </div>
      </div>

      {manualAberto && urlManual && (
        <section className="visualizador-manual" aria-label="Visualização do manual">
          <iframe src={urlManual} title={`Manual ${sessao.manualTitulo}`} />
        </section>
      )}

      <div className="historico-chat" aria-live="polite">
        <article className="mensagem-boas-vindas">
          <div className="balao trechos-chat">
            <div className="identidade-resposta">
              <img className="avatar-simpatica" src="/simpatica-avatar.png" alt="" />
              <strong>Simpática</strong>
            </div>
            <p className="texto-resposta">
              Oi, boas-vindas à Simpática! Encontre informações neste manual e converse comigo de um jeito simples.
            </p>
          </div>
        </article>
        {resultados.map(resultado => {
          const exibindoPaginas = paginasVisiveis.has(resultado.mensagemId)
          const idPaginas = `paginas-${resultado.mensagemId}`
          const paginas = [...new Set(
            resultado.trechos
              .map(trecho => trecho.pagina)
              .filter((pagina): pagina is number => pagina !== null),
          )].sort((a, b) => a - b)

          return <article key={resultado.mensagemId}>
            <div className="balao pergunta-chat">
              <strong>Você</strong>
              <p>{resultado.pergunta}</p>
            </div>
            <div className="balao trechos-chat">
              <div className="identidade-resposta">
                <img className="avatar-simpatica" src="/simpatica-avatar.png" alt="" />
                <strong>{resultado.geradaPorIA ? 'Simpática' : 'Aviso da Simpática'}</strong>
              </div>
              <TextoGradual
                texto={resultado.resposta}
                aoAtualizar={() => fimDoChat.current?.scrollIntoView({ block: 'end' })}
              />
              {resultado.trechos.length > 0 && (
                <>
                  <button
                    type="button"
                    className="secundario botao-paginas"
                    aria-expanded={exibindoPaginas}
                    aria-controls={idPaginas}
                    onClick={() => alternarPaginas(resultado.mensagemId)}
                  >
                    {exibindoPaginas
                      ? 'Ocultar páginas'
                      : paginas.length > 0
                        ? `Exibir páginas (${paginas.length})`
                        : 'Exibir origem'}
                  </button>
                  {exibindoPaginas && (
                    <div id={idPaginas} className="referencias-chat">
                      <h2 className="titulo-referencias">
                        Páginas consultadas
                      </h2>
                      {paginas.length > 0 ? (
                        <ul className="lista-paginas">
                          {paginas.map(pagina => (
                            <li key={pagina}>Página {pagina}</li>
                          ))}
                        </ul>
                      ) : (
                        <p className="pagina-nao-identificada">
                          O arquivo não possui numeração de página identificada.
                        </p>
                      )}
                    </div>
                  )}
                </>
              )}
            </div>
          </article>
        })}
        {enviando && (
          <div className="indicador-pensando" role="status">
            <img className="avatar-simpatica" src="/simpatica-avatar.png" alt="" />
            <span>Pensando<span className="pontos-pensando" aria-hidden="true">...</span></span>
          </div>
        )}
        <div ref={fimDoChat} aria-hidden="true" />
      </div>

      {erro && <div className="mensagem erro" role="alert">{erro}</div>}

      <form className="cartao formulario-pergunta" onSubmit={enviar}>
        <label className="somente-leitor" htmlFor="pergunta">Faça uma pergunta sobre este manual</label>
        <textarea
          ref={campoPergunta}
          id="pergunta"
          value={pergunta}
          onChange={e => setPergunta(e.target.value)}
          onKeyDown={tratarTecla}
          maxLength={1000}
          placeholder="Digite sua mensagem..."
          required
          disabled={enviando}
        />
        <button className="botao-enviar" disabled={enviando} aria-label="Enviar pergunta" title="Enviar (Enter)">
          <span aria-hidden="true">{enviando ? '…' : '➤'}</span>
        </button>
      </form>
      <small className="dica-envio">Enter envia • Shift + Enter pula uma linha</small>
      </div>
      </div>
    </section>
  )
}
