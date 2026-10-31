import { useEffect, useState } from 'react'
import {
  abrirChat,
  excluirConversaHistorico,
  listarHistoricoConversas,
  listarManuais,
  obterHistoricoConversa,
} from '../api'
import type {
  ConversaHistorico,
  ConversaHistoricoResumo,
  Manual,
} from '../tipos'
import { ChatManual } from './ChatManual'

const formatadorData = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
})

export function HistoricoConversas() {
  const [conversas, setConversas] = useState<ConversaHistoricoResumo[]>([])
  const [manuais, setManuais] = useState<Manual[]>([])
  const [conversa, setConversa] = useState<ConversaHistorico | null>(null)
  const [carregando, setCarregando] = useState(true)
  const [conversaAbrindo, setConversaAbrindo] = useState<number | null>(null)
  const [conversaExcluindo, setConversaExcluindo] = useState<number | null>(null)
  const [manualAbrindoChat, setManualAbrindoChat] = useState<number | null>(null)
  const [erro, setErro] = useState('')

  async function carregar() {
    setCarregando(true)
    setErro('')
    try {
      const [historico, listaManuais] = await Promise.all([
        listarHistoricoConversas(),
        listarManuais(),
      ])
      setConversas(historico)
      setManuais(listaManuais)
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setCarregando(false)
    }
  }

  useEffect(() => {
    let ativo = true

    Promise.all([listarHistoricoConversas(), listarManuais()])
      .then(([historico, listaManuais]) => {
        if (!ativo) return
        setConversas(historico)
        setManuais(listaManuais)
      })
      .catch(e => {
        if (ativo) setErro((e as Error).message)
      })
      .finally(() => {
        if (ativo) setCarregando(false)
      })

    return () => {
      ativo = false
    }
  }, [])

  async function abrirConversa(id: number) {
    setConversaAbrindo(id)
    setErro('')
    try {
      setConversa(await obterHistoricoConversa(id))
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setConversaAbrindo(null)
    }
  }

  async function iniciarChat(manualId: number) {
    setManualAbrindoChat(manualId)
    setErro('')
    try {
      const sessao = await abrirChat(manualId)
      setConversa({ sessao, interacoes: [] })
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setManualAbrindoChat(null)
    }
  }

  async function excluirConversa(item: ConversaHistoricoResumo) {
    if (!confirm(`Apagar a conversa do manual "${item.manualTitulo}"? Esta ação não poderá ser desfeita.`))
      return

    setConversaExcluindo(item.id)
    setErro('')
    try {
      await excluirConversaHistorico(item.id)
      setConversas(atuais => atuais.filter(conversaAtual => conversaAtual.id !== item.id))
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setConversaExcluindo(null)
    }
  }

  if (conversa) {
    return (
      <ChatManual
        key={conversa.sessao.id}
        sessao={conversa.sessao}
        resultadosIniciais={conversa.interacoes}
        manuais={manuais}
        manualAbrindoChat={manualAbrindoChat}
        aoAbrirChat={iniciarChat}
        aoFechar={() => {
          setConversa(null)
          void carregar()
        }}
      />
    )
  }

  return (
    <section className="pagina-consulta pagina-historico">
      <h1>Histórico de conversas</h1>
      <p>Consulte suas conversas anteriores e continue de onde parou.</p>

      {erro && <div className="mensagem erro" role="alert">{erro}</div>}
      {carregando && <div className="cartao carregando-historico">Carregando histórico...</div>}

      {!carregando && conversas.length === 0 && !erro && (
        <div className="cartao historico-vazio">
          <span aria-hidden="true">♡</span>
          <h2>Nenhuma conversa por aqui</h2>
          <p>Abra um manual e faça uma pergunta para começar.</p>
        </div>
      )}

      <div className="lista lista-historico">
        {conversas.map(item => (
          <article className="cartao cartao-historico" key={item.id}>
            <div className="icone-historico" aria-hidden="true">☵</div>
            <div className="conteudo-historico">
              <h2>{item.manualTitulo}</h2>
              <p className="data-historico">
                Atualizada em {formatadorData.format(new Date(item.atualizadaEm))}
              </p>
              <p className="previa-historico">
                {item.ultimaPergunta ?? 'Conversa ainda sem mensagens.'}
              </p>
              <small>{item.quantidadeMensagens} mensagens</small>
            </div>
            <div className="acoes acoes-historico">
              <button
                type="button"
                disabled={conversaAbrindo !== null || conversaExcluindo !== null}
                onClick={() => void abrirConversa(item.id)}
              >
                {conversaAbrindo === item.id ? 'Abrindo...' : 'Ver conversa'}
              </button>
              <button
                type="button"
                className="perigo"
                disabled={conversaAbrindo !== null || conversaExcluindo !== null}
                onClick={() => void excluirConversa(item)}
              >
                {conversaExcluindo === item.id ? 'Apagando...' : 'Apagar'}
              </button>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}
