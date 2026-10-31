import { useState, type FormEvent } from 'react'
import { entrar } from '../api'
import type { Sessao } from '../tipos'

export function Login({ aoEntrar }: { aoEntrar: (sessao: Sessao) => void }) {
  const [email, setEmail] = useState('admin@manualia.com')
  const [senha, setSenha] = useState('Admin123!')
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    setErro('')
    setCarregando(true)

    try {
      aoEntrar(await entrar(email, senha))
    } catch (e) {
      setErro((e as Error).message)
    } finally {
      setCarregando(false)
    }
  }

  return (
    <main className="login">
      <div className="vagalumes" aria-hidden="true">
        {Array.from({ length: 28 }, (_, indice) => <span key={indice} />)}
      </div>
      <div className="login-card-container">
        <div className="ornamento-raizes" aria-hidden="true">
          <img src="/ornamento-login-botanico.png" alt="" />
        </div>
      <form className="cartao login-formulario" onSubmit={enviar}>
        <div className="login-marca">
          <span className="marca-coracao">♥</span>
          <div>
            <strong>Simpática</strong>
            <small>Manuais inteligentes</small>
          </div>
        </div>
        <h2>Que bom ter você aqui</h2>
        <p>Entre para acessar o sistema.</p>

        {erro && <div className="mensagem erro">{erro}</div>}

        <label>
          E-mail
          <input
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
            onInvalid={e =>
              e.currentTarget.setCustomValidity('Informe seu e-mail.')
            }
            onInput={e => e.currentTarget.setCustomValidity('')}
          />
        </label>

        <label>
          Senha
          <input
            type="password"
            value={senha}
            onChange={e => setSenha(e.target.value)}
            required
            onInvalid={e =>
              e.currentTarget.setCustomValidity('Informe sua senha.')
            }
            onInput={e => e.currentTarget.setCustomValidity('')}
          />
        </label>

        <button disabled={carregando}>
          {carregando ? 'Entrando...' : 'Entrar'}
        </button>
      </form>
      </div>
    </main>
  )
}
