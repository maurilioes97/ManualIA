import { useEffect, useState, type FormEvent } from 'react'
import { criarUsuario, editarUsuario, excluirUsuario, listarPerfis, listarUsuarios } from '../api'
import type { Perfil, Usuario } from '../tipos'

const formularioVazio = { nome: '', email: '', senha: '', perfilId: 0 }

export function Usuarios() {
  const [usuarios, setUsuarios] = useState<Usuario[]>([])
  const [perfis, setPerfis] = useState<Perfil[]>([])
  const [formulario, setFormulario] = useState(formularioVazio)
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [mensagem, setMensagem] = useState('')
  const [erro, setErro] = useState('')

  async function carregar() {
    try {
      const [listaUsuarios, listaPerfis] = await Promise.all([listarUsuarios(), listarPerfis()])
      setUsuarios(listaUsuarios)
      setPerfis(listaPerfis)
      if (!formulario.perfilId && listaPerfis.length) setFormulario(f => ({ ...f, perfilId: listaPerfis[0].id }))
    } catch (e) { setErro((e as Error).message) }
  }

  useEffect(() => {
    Promise.all([listarUsuarios(), listarPerfis()]).then(([listaUsuarios, listaPerfis]) => {
      setUsuarios(listaUsuarios)
      setPerfis(listaPerfis)
      if (listaPerfis.length) setFormulario(f => ({ ...f, perfilId: listaPerfis[0].id }))
    }).catch(e => setErro((e as Error).message))
  }, [])

  function alterar(campo: string, valor: string | number) { setFormulario({ ...formulario, [campo]: valor }) }
  function cancelar() {
    setEditandoId(null)
    setFormulario({ ...formularioVazio, perfilId: perfis[0]?.id ?? 0 })
  }
  function iniciarEdicao(usuario: Usuario) {
    setEditandoId(usuario.id)
    setFormulario({ nome: usuario.nome, email: usuario.email, senha: '', perfilId: usuario.perfilId })
  }

  async function salvar(evento: FormEvent) {
    evento.preventDefault(); setErro(''); setMensagem('')
    try {
      if (editandoId) {
        await editarUsuario(editandoId, { nome: formulario.nome, email: formulario.email, perfilId: formulario.perfilId })
        setMensagem('Usuário atualizado.')
      } else {
        await criarUsuario(formulario)
        setMensagem('Usuário cadastrado.')
      }
      cancelar(); await carregar()
    } catch (e) { setErro((e as Error).message) }
  }

  async function excluir(usuario: Usuario) {
    if (!confirm(`Excluir o usuário ${usuario.nome}?`)) return
    setErro(''); setMensagem('')
    try { await excluirUsuario(usuario.id); setMensagem('Usuário excluído.'); await carregar() }
    catch (e) { setErro((e as Error).message) }
  }

  return (
    <section>
      <h1>Usuários</h1>
      {erro && <div className="mensagem erro">{erro}</div>}
      {mensagem && <div className="mensagem sucesso">{mensagem}</div>}
      <form className="cartao formulario" onSubmit={salvar}>
        <h2>{editandoId ? 'Editar usuário' : 'Cadastrar usuário'}</h2>
        <label>Nome<input value={formulario.nome} onChange={e => alterar('nome', e.target.value)} maxLength={100} required /></label>
        <label>E-mail<input type="email" value={formulario.email} onChange={e => alterar('email', e.target.value)} required /></label>
        {!editandoId && <label>Senha inicial<input type="password" value={formulario.senha} onChange={e => alterar('senha', e.target.value)} minLength={6} required /></label>}
        <label>Perfil<select value={formulario.perfilId} onChange={e => alterar('perfilId', Number(e.target.value))} required>
          {perfis.map(p => <option key={p.id} value={p.id}>{p.nome}</option>)}
        </select></label>
        <div className="acoes"><button>Salvar</button>{editandoId && <button type="button" className="secundario" onClick={cancelar}>Cancelar</button>}</div>
      </form>
      <div className="tabela"><table><thead><tr><th>Nome</th><th>E-mail</th><th>Perfil</th><th>Status</th><th>Ações</th></tr></thead>
        <tbody>{usuarios.map(usuario => <tr key={usuario.id}>
          <td>{usuario.nome}</td><td>{usuario.email}</td><td>{usuario.perfil}</td><td>{usuario.ativo ? 'Ativo' : 'Excluído'}</td>
          <td className="acoes"><button className="secundario" onClick={() => iniciarEdicao(usuario)}>Editar</button><button className="perigo" disabled={!usuario.ativo} onClick={() => excluir(usuario)}>Excluir</button></td>
        </tr>)}</tbody>
      </table></div>
    </section>
  )
}
