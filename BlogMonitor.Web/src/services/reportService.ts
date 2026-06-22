import { apiClient } from './apiClient'
import type {
  WeeklyReportListItem,
  WeeklyReportDetailResponse,
  DailySummaryResponse,
} from '../types'

export const reportService = {
  listWeekly: async (clientUrlId: number): Promise<WeeklyReportListItem[]> => {
    const res = await apiClient.get<WeeklyReportListItem[]>(`/api/reports/weekly?clientUrlId=${clientUrlId}`)
    return res.data
  },

  getWeeklyDetail: async (id: number): Promise<WeeklyReportDetailResponse> => {
    const res = await apiClient.get<WeeklyReportDetailResponse>(`/api/reports/weekly/${id}`)
    return res.data
  },

  downloadReport: async (id: number, format: string): Promise<void> => {
    const res = await apiClient.get(`/api/reports/weekly/${id}/download?format=${format}`, { responseType: 'blob' })
    const url = window.URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = `report-${id}.${format}`
    document.body.appendChild(a)
    a.click()
    a.remove()
    window.URL.revokeObjectURL(url)
  },

  resendEmail: async (id: number): Promise<void> => {
    await apiClient.post(`/api/reports/weekly/${id}/resend`)
  },

  getDailySummary: async (clientUrlId: number, date: string): Promise<DailySummaryResponse> => {
    const res = await apiClient.get<DailySummaryResponse>(`/api/reports/daily?clientUrlId=${clientUrlId}&date=${encodeURIComponent(date)}`)
    return res.data
  },
}
