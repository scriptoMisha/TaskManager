export type TaskStatusFilter = 'all' | 'active' | 'done'

export type TaskSort = 'newest' | 'oldest' | 'title'

export interface TaskItem {
  id: string
  title: string
  description: string | null
  isDone: boolean
  createdAt: string
}

export interface TaskInput {
  title: string
  description: string | null
}

export interface TaskQuery {
  status: TaskStatusFilter
  sort: TaskSort
  page: number
  pageSize: number
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface AuthResponse {
  accessToken: string
  expiresAt: string
}

export interface Session {
  token: string
  email: string
  expiresAt: string
}
