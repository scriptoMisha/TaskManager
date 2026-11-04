import { useEffect, useState } from 'react'
import { onUnauthorized } from './api'
import AuthPage from './components/AuthPage'
import TasksPage from './components/TasksPage'
import { clearSession, loadSession } from './session'
import type { Session } from './types'

export default function App() {
  const [session, setSession] = useState<Session | null>(() => loadSession())

  useEffect(() => onUnauthorized(() => setSession(null)), [])

  function logout() {
    clearSession()
    setSession(null)
  }

  return (
    <main className="app">
      <header className="header">
        <h1>Task Manager</h1>
        {session && (
          <div className="row">
            <span className="muted">{session.email}</span>
            <button type="button" onClick={logout}>
              Sign out
            </button>
          </div>
        )}
      </header>

      {session ? <TasksPage /> : <AuthPage onLogin={setSession} />}
    </main>
  )
}
