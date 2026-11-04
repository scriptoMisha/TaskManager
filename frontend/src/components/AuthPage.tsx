import { useState, type FormEvent } from 'react'
import { api, errorMessage } from '../api'
import { saveSession } from '../session'
import type { Session } from '../types'

type Mode = 'login' | 'register'

interface AuthPageProps {
  onLogin: (session: Session) => void
}

export default function AuthPage({ onLogin }: AuthPageProps) {
  const [mode, setMode] = useState<Mode>('login')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)
    setError(null)

    try {
      if (mode === 'register') {
        await api.register(email, password)
      }

      const auth = await api.login(email, password)
      const session: Session = { token: auth.accessToken, email: email.trim().toLowerCase(), expiresAt: auth.expiresAt }

      saveSession(session)
      onLogin(session)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  function switchMode(next: Mode) {
    setMode(next)
    setError(null)
  }

  return (
    <section className="card auth">
      <div className="tabs">
        <button type="button" className={mode === 'login' ? 'tab active' : 'tab'} onClick={() => switchMode('login')}>
          Sign in
        </button>
        <button type="button" className={mode === 'register' ? 'tab active' : 'tab'} onClick={() => switchMode('register')}>
          Register
        </button>
      </div>

      <form className="stack" onSubmit={handleSubmit}>
        <label className="field">
          <span>Email</span>
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" required />
        </label>

        <label className="field">
          <span>Password</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
            minLength={mode === 'register' ? 8 : undefined}
            required
          />
        </label>

        {error && <p className="error">{error}</p>}

        <button type="submit" className="primary" disabled={busy}>
          {mode === 'login' ? 'Sign in' : 'Create account'}
        </button>
      </form>
    </section>
  )
}
