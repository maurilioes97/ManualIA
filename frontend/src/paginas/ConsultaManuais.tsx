import { useEffect, useState, type FormEvent } from 'react'
import { abrirChat, listarManuais } from '../api'
import type { Manual, SessaoChat } from '../tipos'
import { ChatManual } from './ChatManual'

export function ConsultaManuais() {
  const [termo, setTermo] = useState('')
  const [manuais, setManuais] = useState<Manual[]>([])
  const [todosManuais, setTodosManuais] = useState<Manual[]>([])
  const [carregando, setCarregando] = useState(true)
  const [manualAbrindoChat, setManualAbrindoChat] = useState<number | null>(null)
  const [chat, setChat] = useState<SessaoChat | null>(null)
  const [erro, setErro] = useState('')

  async function pesquisar(termoBusca = termo) {
    setCarregando(true)
    setErro('')

    try {
      setManuais(await listarManuais(termoBusca))
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setCarregando(false)
    }
  }

  useEffect(() => {
    listarManuais()
      .then(lista => {
        setManuais(lista)
        setTodosManuais(lista)
      })
      .catch(e => setErro((e as Error).message))
      .finally(() => setCarregando(false))
  }, [])

  function enviarPesquisa(evento: FormEvent) {
    evento.preventDefault()
    void pesquisar()
  }

  async function iniciarChat(manualId: number) {
    setManualAbrindoChat(manualId)
    setErro('')

    try {
      setChat(await abrirChat(manualId))
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setManualAbrindoChat(null)
    }
  }

  if (chat)
    return (
      <ChatManual
        key={chat.id}
        sessao={chat}
        manuais={todosManuais}
        manualAbrindoChat={manualAbrindoChat}
        aoAbrirChat={iniciarChat}
        aoFechar={() => setChat(null)}
      />
    )

  return (
    <section className="pagina-consulta">
      <h1>Consultar manuais</h1>
      <p>Pesquise pelo título ou por uma palavra relacionada ao manual.</p>

      <form className="cartao pesquisa-manuais" onSubmit={enviarPesquisa}>
        <label htmlFor="termo-manual">Termo de busca</label>
        <div className="acoes-pesquisa">
          <input
            id="termo-manual"
            value={termo}
            onChange={e => setTermo(e.target.value)}
            placeholder="Ex.: manutenção, segurança, equipamento..."
            maxLength={150}
          />
          <button disabled={carregando}>
            {carregando ? 'Pesquisando...' : 'Pesquisar'}
          </button>
          {termo && (
            <button
              type="button"
              className="secundario"
              onClick={() => {
                setTermo('')
                void pesquisar('')
              }}
            >
              Limpar
            </button>
          )}
        </div>
      </form>

      {erro && <div className="mensagem erro">{erro}</div>}

      {!carregando && !erro && (
        <p className="resumo-pesquisa">
          {manuais.length === 0
            ? 'Nenhum manual disponível foi encontrado.'
            : `${manuais.length} ${manuais.length === 1 ? 'manual encontrado' : 'manuais encontrados'}.`}
        </p>
      )}

      <div className="lista grade-manuais">
        {manuais.map(manual => (
          <article className="cartao cartao-manual" key={manual.id}>
            <div className="cabecalho-cartao-manual">
              <span className="icone-manual" aria-hidden="true">📖</span>
              <div className="texto-cartao-manual">
                <h2>{manual.titulo}</h2>
                <p>{manual.descricao}</p>
              </div>
            </div>
            {manual.arquivo && (
              <div className="arquivo arquivo-cartao-manual">
                <span className="tipo-arquivo" aria-hidden="true">{manual.arquivo.tipo}</span>
                <span>
                  <strong>{manual.arquivo.nome}</strong>
                  <span aria-hidden="true"> • </span>
                  {manual.arquivo.quantidadeChunks} trechos disponíveis
                </span>
              </div>
            )}
            <div className="acoes-cartao-manual">
              <button
                type="button"
                disabled={manualAbrindoChat !== null}
                onClick={() => void iniciarChat(manual.id)}
              >
                {manualAbrindoChat === manual.id ? 'Abrindo chat...' : 'Abrir chat'}
                <span className="icone-chat" aria-hidden="true">♥</span>
              </button>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}
