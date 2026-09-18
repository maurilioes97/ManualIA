import { useEffect, useState, type FormEvent } from 'react'
import {
  criarManual,
  editarManual,
  enviarArquivo,
  excluirManual,
  listarManuais,
} from '../api'
import type { Manual } from '../tipos'

export function Manuais() {
  const [manuais, setManuais] = useState<Manual[]>([])
  const [titulo, setTitulo] = useState('')
  const [descricao, setDescricao] = useState('')
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [arquivos, setArquivos] = useState<Record<number, File>>({})
  const [mensagem, setMensagem] = useState('')
  const [erro, setErro] = useState('')

  async function carregar() {
    try {
      setManuais(await listarManuais())
    } catch (e) {
      setErro((e as Error).message)
    }
  }

  useEffect(() => {
    listarManuais()
      .then(setManuais)
      .catch(e => setErro((e as Error).message))
  }, [])

  function cancelar() {
    setTitulo('')
    setDescricao('')
    setEditandoId(null)
  }

  function iniciarEdicao(manual: Manual) {
    setTitulo(manual.titulo)
    setDescricao(manual.descricao)
    setEditandoId(manual.id)
  }

  async function salvar(evento: FormEvent) {
    evento.preventDefault()
    setErro('')
    setMensagem('')

    try {
      if (editandoId) {
        await editarManual(editandoId, { titulo, descricao })
        setMensagem('Manual atualizado.')
      } else {
        await criarManual({ titulo, descricao })
        setMensagem('Manual cadastrado.')
      }

      cancelar()
      await carregar()
    } catch (e) {
      setErro((e as Error).message)
    }
  }

  async function excluir(manual: Manual) {
    if (!confirm(`Excluir o manual ${manual.titulo}?`)) return

    try {
      await excluirManual(manual.id)
      setMensagem('Manual excluído.')
      await carregar()
    } catch (e) {
      setErro((e as Error).message)
    }
  }

  async function enviar(manual: Manual) {
    const arquivo = arquivos[manual.id]

    if (!arquivo) {
      setErro('Selecione um arquivo.')
      return
    }

    const substituir = !!manual.arquivo

    if (
      substituir &&
      !confirm('Este manual já possui arquivo. Deseja substituí-lo?')
    ) {
      return
    }

    setErro('')
    setMensagem('')

    try {
      await enviarArquivo(manual.id, arquivo, substituir)
      setMensagem('Arquivo processado e salvo.')

      setArquivos(arquivosAtuais => {
        const novosArquivos = { ...arquivosAtuais }
        delete novosArquivos[manual.id]
        return novosArquivos
      })

      await carregar()
    } catch (e) {
      setErro((e as Error).message)
    }
  }

  return (
    <section>
      <h1>Manuais</h1>

      {erro && <div className="mensagem erro">{erro}</div>}
      {mensagem && <div className="mensagem sucesso">{mensagem}</div>}

      <form className="cartao formulario" onSubmit={salvar}>
        <h2>{editandoId ? 'Editar manual' : 'Cadastrar manual'}</h2>

        <label>
          Título
          <input
            value={titulo}
            onChange={e => setTitulo(e.target.value)}
            maxLength={150}
            required
          />
        </label>

        <label>
          Descrição
          <textarea
            value={descricao}
            onChange={e => setDescricao(e.target.value)}
            maxLength={1000}
            required
          />
        </label>

        <div className="acoes">
          <button>Salvar</button>

          {editandoId && (
            <button type="button" className="secundario" onClick={cancelar}>
              Cancelar
            </button>
          )}
        </div>
      </form>

      <div className="lista">
        {manuais.map(manual => (
          <article className="cartao" key={manual.id}>
            <div className="titulo-acoes">
              <div>
                <h2>{manual.titulo}</h2>
                <p>{manual.descricao}</p>
              </div>

              <div className="acoes">
                <button
                  className="secundario"
                  onClick={() => iniciarEdicao(manual)}
                >
                  Editar
                </button>
                <button className="perigo" onClick={() => excluir(manual)}>
                  Excluir
                </button>
              </div>
            </div>

            {manual.arquivo ? (
              <p className="arquivo">
                <strong>{manual.arquivo.nome}</strong> — {manual.arquivo.tipo},{' '}
                {(manual.arquivo.tamanho / 1024).toFixed(1)} KB,{' '}
                {manual.arquivo.quantidadeChunks} trechos
              </p>
            ) : (
              <p>Nenhum arquivo enviado.</p>
            )}

            <div className="envio">
              <input
                type="file"
                accept=".pdf,.docx"
                onChange={e => {
                  const arquivoSelecionado = e.target.files?.[0]

                  if (arquivoSelecionado) {
                    setArquivos({
                      ...arquivos,
                      [manual.id]: arquivoSelecionado,
                    })
                  }
                }}
              />
              <button type="button" onClick={() => enviar(manual)}>
                {manual.arquivo ? 'Substituir arquivo' : 'Enviar arquivo'}
              </button>
            </div>
          </article>
        ))}

        {manuais.length === 0 && <p>Nenhum manual cadastrado.</p>}
      </div>
    </section>
  )
}
