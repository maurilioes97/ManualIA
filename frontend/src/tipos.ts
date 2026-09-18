export type Usuario = {
  id: number
  nome: string
  email: string
  perfilId: number
  perfil: string
  ativo: boolean
}

export type Perfil = {
  id: number
  nome: string
}

export type Sessao = {
  token: string
  usuario: Usuario
}

export type ArquivoManual = {
  nome: string
  tipo: string
  tamanho: number
  enviadoEm: string
  quantidadeChunks: number
}

export type Manual = {
  id: number
  titulo: string
  descricao: string
  criadoEm: string
  atualizadoEm: string
  arquivo: ArquivoManual | null
}
