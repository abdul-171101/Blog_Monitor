import { apiClient } from './apiClient'
import type { MonitoringLogResponse } from '../types'

interface GetLogsParams {
  clientUrlId?: number
  level?: string
  fromDate?: string
  toDate?: string
  page?: number
  pageSize?: number
}

export const logService = {
  getLogs: async (params: GetLogsParams): Promise<MonitoringLogResponse[]> => {
    const res = await apiClient.get<MonitoringLogResponse[]>('/api/logs', { params })
    return res.data
  },
}
