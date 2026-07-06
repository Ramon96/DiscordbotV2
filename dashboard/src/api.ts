export interface CurrentUser {
  username: string
}

export class ApiError extends Error {
  constructor(public status: number) {
    super(`Request failed with status ${status}`)
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })

  if (!response.ok) {
    throw new ApiError(response.status)
  }

  return (response.status === 204 ? undefined : await response.json()) as T
}

export const authApi = {
  me: () => request<CurrentUser>('/api/auth/me'),
  login: (username: string, password: string) =>
    request<CurrentUser>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),
  logout: () => request<void>('/api/auth/logout', { method: 'POST' }),
}
