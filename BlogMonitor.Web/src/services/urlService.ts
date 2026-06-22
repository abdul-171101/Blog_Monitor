import { apiClient } from './apiClient'
import type { UrlSummaryResponse, UrlDetailResponse, RegisterUrlRequest } from '../types'

export const urlService = {
  getAll: async (): Promise<UrlSummaryResponse[]> => {
    const res = await apiClient.get<UrlSummaryResponse[]>('/api/urls')
    return res.data
  },

  getById: async (id: number): Promise<UrlDetailResponse> => {
    const res = await apiClient.get<UrlDetailResponse>(`/api/urls/${id}`)
    return res.data
  },

  register: async (data: RegisterUrlRequest): Promise<UrlDetailResponse> => {
    const res = await apiClient.post<UrlDetailResponse>('/api/urls', data)
    return res.data
  },

  stop: async (id: number): Promise<void> => {
    await apiClient.post(`/api/urls/${id}/stop`)
  },

  runNow: async (id: number): Promise<void> => {
    await apiClient.post(`/api/urls/${id}/run`)
  },
}
