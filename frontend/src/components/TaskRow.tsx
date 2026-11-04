import { useState } from 'react'
import { api } from '../api'
import type { TaskInput, TaskItem } from '../types'
import TaskForm from './TaskForm'

interface TaskRowProps {
  task: TaskItem
  onChanged: () => void
  onError: (error: unknown) => void
}

export default function TaskRow({ task, onChanged, onError }: TaskRowProps) {
  const [editing, setEditing] = useState(false)
  const [busy, setBusy] = useState(false)

  async function run(action: () => Promise<void>): Promise<boolean> {
    setBusy(true)

    try {
      await action()
      onChanged()
      return true
    } catch (err) {
      onError(err)
      return false
    } finally {
      setBusy(false)
    }
  }

  function toggleDone() {
    return run(() =>
      task.isDone
        ? api.updateTask(task.id, { title: task.title, description: task.description, isDone: false })
        : api.markDone(task.id),
    )
  }

  function remove() {
    if (window.confirm(`Delete "${task.title}"?`)) {
      return run(() => api.deleteTask(task.id))
    }
  }

  async function save(input: TaskInput) {
    if (await run(() => api.updateTask(task.id, { ...input, isDone: task.isDone }))) {
      setEditing(false)
    }
  }

  if (editing) {
    return (
      <li className="task editing">
        <TaskForm
          initial={{ title: task.title, description: task.description }}
          submitLabel="Save"
          onSubmit={save}
          onCancel={() => setEditing(false)}
        />
      </li>
    )
  }

  return (
    <li className={task.isDone ? 'task done' : 'task'}>
      <input type="checkbox" checked={task.isDone} onChange={toggleDone} disabled={busy} aria-label="Done" />
      <div className="task-body">
        <span className="task-title">{task.title}</span>
        {task.description && <span className="task-description">{task.description}</span>}
        <span className="task-date">{new Date(task.createdAt).toLocaleString()}</span>
      </div>
      <div className="row">
        <button type="button" onClick={() => setEditing(true)} disabled={busy}>
          Edit
        </button>
        <button type="button" className="danger" onClick={remove} disabled={busy}>
          Delete
        </button>
      </div>
    </li>
  )
}
