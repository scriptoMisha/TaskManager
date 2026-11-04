import { clearSession, loadSession } from './session'
import type { AuthResponse, PagedResponse, TaskInput, TaskItem, TaskQuery } from './types'

interface ProblemDetails {
  title?: string
  errors?: Record<string, string[]>
}

interface RequestOptions {
  method?: string
  body?: unknown
  auth?: boolean
}

export class ApiError extends Error {
  readonly status: number
  readonly errors: Record<string, string[]>

  constructor(status: number, message: string, errors: Record<string, string[]> = {}) {
    super(message)
    this.status = status
    this.errors = errors
  }
}

const unauthorizedListeners = new Set<() => void>()

export function onUnauthorized(listener: () => void): () => void {
  unauthorizedListeners.add(listener)
  return () => unauthorizedListeners.delete(listener)
}

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const details = Object.values(error.errors).flat()
    return details.length > 0 ? details.join(' ') : error.message
  }

  return error instanceof Error ? error.message : 'Something went wrong.'
}

async function request<T>(path: string, { method = 'GET', body, auth = true }: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = {}

  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  const session = auth ? loadSession() : null
  if (session) {
    headers.Authorization = `Bearer ${session.token}`
  }

  const response = await fetch(`/api${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (response.status === 401 && auth) {
    clearSession()
    unauthorizedListeners.forEach((listener) => listener())
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as ProblemDetails | null
    throw new ApiError(response.status, problem?.title ?? `Request failed with status ${response.status}.`, problem?.errors)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export const api = {
  register: (email: string, password: string) =>
    request<unknown>('/auth/register', { method: 'POST', body: { email, password }, auth: false }),

  login: (email: string, password: string) =>
    request<AuthResponse>('/auth/login', { method: 'POST', body: { email, password }, auth: false }),

  getTasks: (query: TaskQuery) => {
    const params = new URLSearchParams({
      sort: query.sort,
      page: String(query.page),
      pageSize: String(query.pageSize),
    })

    if (query.status !== 'all') {
      params.set('status', query.status)
    }

    return request<PagedResponse<TaskItem>>(`/tasks?${params}`)
  },

  createTask: (input: TaskInput) => request<TaskItem>('/tasks', { method: 'POST', body: input }),

  updateTask: (id: string, input: TaskInput & { isDone: boolean }) =>
    request<void>(`/tasks/${id}`, { method: 'PUT', body: input }),

  markDone: (id: string) => request<void>(`/tasks/${id}/done`, { method: 'PATCH' }),

  deleteTask: (id: string) => request<void>(`/tasks/${id}`, { method: 'DELETE' }),
}
