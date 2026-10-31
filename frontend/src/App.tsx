import { useState } from 'react'
import { limparSessao, obterSessao } from './api'
import type { Sessao } from './tipos'
import { Login } from './paginas/Login'
import { Usuarios } from './paginas/Usuarios'
import { Manuais } from './paginas/Manuais'
import { ConsultaManuais } from './paginas/ConsultaManuais'

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
        <div className="marca">
          <img src="/simpatica-avatar.png" alt="" />
          <div>
            <strong>Simpática</strong>
            <small>Manuais inteligentes</small>
          </div>
        </div>
        <span className="usuario-logado">
          {sessao.usuario.nome} — {sessao.usuario.perfil}
        </span>
        <button className="secundario" onClick={sair}>
          Sair
        </button>
      </header>

      {administrador ? (
        <main className="area-administrativa">
          <nav>
            <button
              className={pagina === 'manuais' ? 'ativo' : ''}
              onClick={() => setPagina('manuais')}
            >
              Manuais
            </button>
            <button
              className={pagina === 'usuarios' ? 'ativo' : ''}
              onClick={() => setPagina('usuarios')}
            >
              Usuários
            </button>
          </nav>

          {pagina === 'manuais' ? (
            <Manuais />
          ) : (
            <Usuarios usuarioLogadoId={sessao.usuario.id} />
          )}
        </main>
      ) : (
        <main>
          <ConsultaManuais />
        </main>
      )}
    </>
  )
}

export default App
