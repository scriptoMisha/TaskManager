import { useState, type FormEvent } from 'react'
import type { TaskInput } from '../types'

interface TaskFormProps {
  initial?: TaskInput
  submitLabel: string
  onSubmit: (input: TaskInput) => Promise<void>
  onCancel?: () => void
}

export default function TaskForm({ initial, submitLabel, onSubmit, onCancel }: TaskFormProps) {
  const [title, setTitle] = useState(initial?.title ?? '')
  const [description, setDescription] = useState(initial?.description ?? '')
  const [busy, setBusy] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setBusy(true)

    try {
      await onSubmit({ title: title.trim(), description: description.trim() || null })

      if (!initial) {
        setTitle('')
        setDescription('')
      }
    } finally {
      setBusy(false)
    }
  }

  return (
    <form className="task-form" onSubmit={handleSubmit}>
      <input
        placeholder="What needs to be done?"
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        maxLength={200}
        required
      />
      <textarea
        placeholder="Description (optional)"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        maxLength={2000}
        rows={2}
      />
      <div className="row">
        <button type="submit" className="primary" disabled={busy || title.trim() === ''}>
          {submitLabel}
        </button>
        {onCancel && (
          <button type="button" onClick={onCancel} disabled={busy}>
            Cancel
          </button>
        )}
      </div>
    </form>
  )
}
