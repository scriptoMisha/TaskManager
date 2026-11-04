import { useEffect, useState } from 'react'
import { api, errorMessage } from '../api'
import type { PagedResponse, TaskInput, TaskItem, TaskQuery, TaskSort, TaskStatusFilter } from '../types'
import TaskForm from './TaskForm'
import TaskRow from './TaskRow'

const STATUS_OPTIONS: { value: TaskStatusFilter; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'active', label: 'Active' },
  { value: 'done', label: 'Done' },
]

const SORT_OPTIONS: { value: TaskSort; label: string }[] = [
  { value: 'newest', label: 'Newest first' },
  { value: 'oldest', label: 'Oldest first' },
  { value: 'title', label: 'By title' },
]

export default function TasksPage() {
  const [query, setQuery] = useState<TaskQuery>({ status: 'all', sort: 'newest', page: 1, pageSize: 10 })
  const [data, setData] = useState<PagedResponse<TaskItem> | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let ignore = false

    api
      .getTasks(query)
      .then((result) => {
        if (ignore) {
          return
        }

        if (result.items.length === 0 && query.page > 1) {
          setQuery((q) => ({ ...q, page: q.page - 1 }))
          return
        }

        setData(result)
        setError(null)
      })
      .catch((err) => {
        if (!ignore) {
          setError(errorMessage(err))
        }
      })

    return () => {
      ignore = true
    }
  }, [query, reloadKey])

  const reload = () => setReloadKey((key) => key + 1)
  const showError = (err: unknown) => setError(errorMessage(err))
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1

  async function createTask(input: TaskInput) {
    try {
      await api.createTask(input)
      reload()
    } catch (err) {
      showError(err)
      throw err
    }
  }

  return (
    <section className="stack">
      <div className="card">
        <TaskForm submitLabel="Add task" onSubmit={createTask} />
      </div>

      <div className="toolbar">
        <div className="tabs">
          {STATUS_OPTIONS.map((option) => (
            <button
              key={option.value}
              type="button"
              className={query.status === option.value ? 'tab active' : 'tab'}
              onClick={() => setQuery((q) => ({ ...q, status: option.value, page: 1 }))}
            >
              {option.label}
            </button>
          ))}
        </div>

        <select
          value={query.sort}
          onChange={(e) => setQuery((q) => ({ ...q, sort: e.target.value as TaskSort, page: 1 }))}
          aria-label="Sort"
        >
          {SORT_OPTIONS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </div>

      {error && <p className="error">{error}</p>}

      {data && data.items.length === 0 && <p className="muted">No tasks here yet.</p>}

      {data && data.items.length > 0 && (
        <ul className="task-list">
          {data.items.map((task) => (
            <TaskRow key={task.id} task={task} onChanged={reload} onError={showError} />
          ))}
        </ul>
      )}

      {data && data.totalCount > data.pageSize && (
        <div className="pager">
          <button type="button" disabled={query.page <= 1} onClick={() => setQuery((q) => ({ ...q, page: q.page - 1 }))}>
            Previous
          </button>
          <span>
            Page {query.page} of {totalPages}
          </span>
          <button
            type="button"
            disabled={query.page >= totalPages}
            onClick={() => setQuery((q) => ({ ...q, page: q.page + 1 }))}
          >
            Next
          </button>
        </div>
      )}
    </section>
  )
}
