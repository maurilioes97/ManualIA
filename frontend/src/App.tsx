import { useState } from 'react'
import { limparSessao, obterSessao } from './api'
import type { Sessao } from './tipos'
import { Login } from './paginas/Login'
import { Usuarios } from './paginas/Usuarios'
import { Manuais } from './paginas/Manuais'

function App() {
  const [sessao, setSessao] = useState<Sessao | null>(obterSessao())
  const [pagina, setPagina] = useState<'usuarios' | 'manuais'>('manuais')

  if (!sessao) return <Login aoEntrar={setSessao} />

  function sair() {
    limparSessao()
    setSessao(null)
  }

  const administrador = sessao.usuario.perfil === 'Administrador'

  return (
    <>
      <header>
        <strong>Manual IA</strong>
        <span>{sessao.usuario.nome} — {sessao.usuario.perfil}</span>
        <button className="secundario" onClick={sair}>Sair</button>
      </header>
      {administrador ? (
        <main>
          <nav>
            <button className={pagina === 'manuais' ? 'ativo' : ''} onClick={() => setPagina('manuais')}>Manuais</button>
            <button className={pagina === 'usuarios' ? 'ativo' : ''} onClick={() => setPagina('usuarios')}>Usuários</button>
          </nav>
          {pagina === 'manuais' ? <Manuais /> : <Usuarios />}
        </main>
      ) : (
        <main className="cartao"><h1>Bem-vindo</h1><p>Seu acesso de funcionário está ativo.</p></main>
      )}
    </>
  )
}

export default App
