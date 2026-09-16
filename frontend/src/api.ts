import type { Manual, Perfil, Sessao, Usuario } from './tipos'

const URL = 'http://localhost:5080/api'

export function obterSessao(): Sessao | null {
  const valor = sessionStorage.getItem('sessao')
  return valor ? JSON.parse(valor) : null
}

export function limparSessao() {
  sessionStorage.removeItem('sessao')
}

async function requisitar<T>(caminho: string, opcoes: RequestInit = {}): Promise<T> {
  const sessao = obterSessao()
  const headers = new Headers(opcoes.headers)
  if (sessao) headers.set('Authorization', `Bearer ${sessao.token}`)
  if (opcoes.body && !(opcoes.body instanceof FormData)) headers.set('Content-Type', 'application/json')

  const resposta = await fetch(`${URL}${caminho}`, { ...opcoes, headers })
  if (resposta.status === 401) limparSessao()
  if (!resposta.ok) {
    const erro = await resposta.json().catch(() => ({}))
    throw new Error(erro.mensagem || erro.title || 'Não foi possível concluir a operação.')
  }
  return resposta.status === 204 ? (undefined as T) : resposta.json()
}

export async function entrar(email: string, senha: string) {
  const sessao = await requisitar<Sessao>('/autenticacao/login', {
    method: 'POST', body: JSON.stringify({ email, senha })
  })
  sessionStorage.setItem('sessao', JSON.stringify(sessao))
  return sessao
}

export const listarUsuarios = () => requisitar<Usuario[]>('/usuarios')
export const listarPerfis = () => requisitar<Perfil[]>('/usuarios/perfis')
export const criarUsuario = (dados: object) => requisitar<Usuario>('/usuarios', { method: 'POST', body: JSON.stringify(dados) })
export const editarUsuario = (id: number, dados: object) => requisitar<Usuario>(`/usuarios/${id}`, { method: 'PUT', body: JSON.stringify(dados) })
export const excluirUsuario = (id: number) => requisitar<void>(`/usuarios/${id}`, { method: 'DELETE' })

export const listarManuais = () => requisitar<Manual[]>('/manuais')
export const criarManual = (dados: object) => requisitar<Manual>('/manuais', { method: 'POST', body: JSON.stringify(dados) })
export const editarManual = (id: number, dados: object) => requisitar<Manual>(`/manuais/${id}`, { method: 'PUT', body: JSON.stringify(dados) })
export const excluirManual = (id: number) => requisitar<void>(`/manuais/${id}`, { method: 'DELETE' })
export function enviarArquivo(id: number, arquivo: File, substituir: boolean) {
  const dados = new FormData()
  dados.append('arquivo', arquivo)
  return requisitar(`/manuais/${id}/arquivo?substituir=${substituir}`, { method: 'POST', body: dados })
}
