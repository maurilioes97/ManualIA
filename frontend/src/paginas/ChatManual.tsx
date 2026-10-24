import { useState, type FormEvent } from 'react'
import { enviarPergunta } from '../api'
import type { PerguntaProcessada, SessaoChat } from '../tipos'

type Propriedades = {
  sessao: SessaoChat
  aoFechar: () => void
}

export function ChatManual({ sessao, aoFechar }: Propriedades) {
  const [pergunta, setPergunta] = useState('')
  const [resultados, setResultados] = useState<PerguntaProcessada[]>([])
  const [enviando, setEnviando] = useState(false)
  const [erro, setErro] = useState('')

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

  return (
    <section>
      <div className="cabecalho-chat">
        <div>
          <h1>Chat do manual</h1>
          <p>{sessao.manualTitulo}</p>
        </div>
        <button type="button" className="secundario" onClick={aoFechar} disabled={enviando}>
          Voltar aos manuais
        </button>
      </div>

      <p>Faça perguntas sobre este manual e confira os trechos usados como referência.</p>

      <div className="historico-chat" aria-live="polite">
        {resultados.map(resultado => (
          <article key={resultado.mensagemId}>
            <div className="balao pergunta-chat">
              <strong>Você</strong>
              <p>{resultado.pergunta}</p>
            </div>
            <div className="balao trechos-chat">
              <strong>{resultado.geradaPorIA ? 'Resposta da IA' : 'Aviso do sistema'}</strong>
              <p className="texto-resposta">{resultado.resposta}</p>
              {resultado.trechos.length > 0 && <h2 className="titulo-referencias">
                {resultado.geradaPorIA ? 'Referências da resposta' : 'Trechos encontrados'}
              </h2>}
              {resultado.trechos.map(trecho => (
                <blockquote key={trecho.chunkId}>
                  <strong>Trecho {trecho.chunkId} — </strong>
                  {trecho.pagina && <span>Página {trecho.pagina} — </span>}
                  {trecho.texto}
                </blockquote>
              ))}
            </div>
          </article>
        ))}
      </div>

      {enviando && <p role="status">Consultando o manual e preparando a resposta...</p>}
      {erro && <div className="mensagem erro" role="alert">{erro}</div>}

      <form className="cartao formulario-pergunta" onSubmit={enviar}>
        <label htmlFor="pergunta">Faça uma pergunta sobre este manual</label>
        <textarea
          id="pergunta"
          value={pergunta}
          onChange={e => setPergunta(e.target.value)}
          maxLength={1000}
          placeholder="Digite sua dúvida..."
          required
          disabled={enviando}
        />
        <button disabled={enviando}>
          {enviando ? 'Preparando resposta...' : 'Enviar pergunta'}
        </button>
      </form>
    </section>
  )
}
