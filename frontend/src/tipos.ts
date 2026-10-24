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

export type SessaoChat = {
  id: number
  manualId: number
  manualTitulo: string
  criadaEm: string
}

export type TrechoEncontrado = {
  chunkId: number
  ordem: number
  pagina: number | null
  texto: string
}

export type PerguntaProcessada = {
  sessaoId: number
  mensagemId: number
  pergunta: string
  enviadaEm: string
  trechos: TrechoEncontrado[]
  respostaId: number
  resposta: string
  geradaPorIA: boolean
  motivo: string
}
