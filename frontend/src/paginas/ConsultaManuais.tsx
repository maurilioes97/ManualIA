import { useEffect, useState, type FormEvent } from 'react'
import { abrirChat, listarManuais, obterManual } from '../api'
import type { Manual, SessaoChat } from '../tipos'
import { ChatManual } from './ChatManual'

export function ConsultaManuais() {
  const [termo, setTermo] = useState('')
  const [manuais, setManuais] = useState<Manual[]>([])
  const [selecionado, setSelecionado] = useState<Manual | null>(null)
  const [carregando, setCarregando] = useState(true)
  const [abrindoChat, setAbrindoChat] = useState(false)
  const [chat, setChat] = useState<SessaoChat | null>(null)
  const [erro, setErro] = useState('')

  async function pesquisar(termoBusca = termo) {
    setCarregando(true)
    setErro('')
    setSelecionado(null)

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
      .then(setManuais)
      .catch(e => setErro((e as Error).message))
      .finally(() => setCarregando(false))
  }, [])

  function enviarPesquisa(evento: FormEvent) {
    evento.preventDefault()
    void pesquisar()
  }

  async function verDetalhes(id: number) {
    setErro('')

    try {
      setSelecionado(await obterManual(id))
    } catch (e) {
      setErro((e as Error).message)
    }
  }

  async function iniciarChat() {
    if (!selecionado) return
    setAbrindoChat(true)
    setErro('')

    try {
      setChat(await abrirChat(selecionado.id))
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setAbrindoChat(false)
    }
  }

  if (chat)
    return <ChatManual sessao={chat} aoFechar={() => setChat(null)} />

  return (
    <section>
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

      <div className="lista">
        {manuais.map(manual => (
          <article className="cartao" key={manual.id}>
            <div className="titulo-acoes">
              <div>
                <h2>{manual.titulo}</h2>
                <p>{manual.descricao}</p>
              </div>
              <button
                type="button"
                className="secundario"
                onClick={() => void verDetalhes(manual.id)}
              >
                Ver detalhes
              </button>
            </div>
          </article>
        ))}
      </div>

      {selecionado && (
        <section className="cartao detalhes-manual" aria-live="polite">
          <div className="titulo-acoes">
            <div>
              <h2>{selecionado.titulo}</h2>
              <p>{selecionado.descricao}</p>
            </div>
            <button
              type="button"
              className="secundario"
              onClick={() => setSelecionado(null)}
            >
              Fechar
            </button>
          </div>
          {selecionado.arquivo && (
            <p className="arquivo">
              <strong>{selecionado.arquivo.nome}</strong> —{' '}
              {selecionado.arquivo.tipo},{' '}
              {selecionado.arquivo.quantidadeChunks} trechos disponíveis
            </p>
          )}
          <button type="button" disabled={abrindoChat} onClick={iniciarChat}>
            {abrindoChat ? 'Abrindo chat...' : 'Abrir chat'}
          </button>
        </section>
      )}
    </section>
  )
}
