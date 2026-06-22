import React, { useEffect, useState } from 'react'
import PageHeader from '../components/PageHeader'
import DataTable from '../components/DataTable'
import LoadingSpinner from '../components/LoadingSpinner'
import { logService } from '../services/logService'
import { urlService } from '../services/urlService'
import type { MonitoringLogResponse, UrlSummaryResponse } from '../types'
import useAsync from '../hooks/useAsync'
import { useAuth } from '../store/AuthContext'

const truncate = (s: string, n = 120) => (s.length > n ? s.slice(0, n - 1) + '…' : s)

const levelClass = (lvl: string) => {
  switch (lvl) {
    case 'Error':
      return 'text-red-600'
    case 'Warning':
      return 'text-yellow-600'
    default:
      return 'text-gray-700'
  }
}

const LogViewer: React.FC = () => {
  const { authState } = useAuth()
  const [page, setPage] = useState(1)
  const [urlFilter, setUrlFilter] = useState<number | null>(null)
  const [levelFilter, setLevelFilter] = useState<string>('')
  const [fromDate, setFromDate] = useState<string>('')
  const [toDate, setToDate] = useState<string>('')

  const { data: urls, isLoading: urlsLoading } = useAsync(async () => await urlService.getAll(), [])
  const { data: logs, isLoading: logsLoading, error } = useAsync(
    async () =>
      await logService.getLogs({
        clientUrlId: urlFilter ?? undefined,
        level: levelFilter || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        page,
        pageSize: 50,
      }),
    [page, urlFilter, levelFilter, fromDate, toDate]
  )

  useEffect(() => {
    document.title = 'Monitoring Logs | Blog Monitor'
  }, [])

  if (authState.role !== 'Admin') return <div className="text-red-600">Access denied</div>
  if (urlsLoading || logsLoading) return <div className="flex justify-center py-20"><LoadingSpinner size="large" /></div>
  if (error) return <div className="text-red-600">Error: {error}</div>

  const rows: MonitoringLogResponse[] = logs ?? []

  const columns = [
    { header: 'Timestamp', accessor: (r: MonitoringLogResponse) => new Date(r.timestamp).toLocaleString() },
    { header: 'URL', accessor: (r: MonitoringLogResponse) => r.url ?? 'System' },
    { header: 'Event', accessor: (r: MonitoringLogResponse) => r.eventType },
    { header: 'Level', accessor: (r: MonitoringLogResponse) => <span className={levelClass(r.level)}>{r.level}</span> },
    { header: 'Message', accessor: (r: MonitoringLogResponse) => <span title={r.message}>{truncate(r.message)}</span> },
  ]

  return (
    <div>
      <PageHeader title="Monitoring Logs" />

      <div className="bg-white rounded shadow p-4 mb-4">
        <h3 className="font-semibold mb-3">Filters</h3>
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
          <div>
            <label className="block text-sm font-medium">URL</label>
            <select className="w-full border px-2 py-1 rounded text-sm" value={urlFilter ?? ''} onChange={(e) => { setUrlFilter(e.target.value ? Number(e.target.value) : null); setPage(1) }}>
              <option value="">All URLs</option>
              {(urls ?? []).map((u: UrlSummaryResponse) => (
                <option key={u.id} value={u.id}>{u.url.slice(0, 40)}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium">Level</label>
            <select className="w-full border px-2 py-1 rounded text-sm" value={levelFilter} onChange={(e) => { setLevelFilter(e.target.value); setPage(1) }}>
              <option value="">All Levels</option>
              <option>Info</option>
              <option>Warning</option>
              <option>Error</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium">From</label>
            <input type="date" className="w-full border px-2 py-1 rounded text-sm" value={fromDate} onChange={(e) => { setFromDate(e.target.value); setPage(1) }} />
          </div>
          <div>
            <label className="block text-sm font-medium">To</label>
            <input type="date" className="w-full border px-2 py-1 rounded text-sm" value={toDate} onChange={(e) => { setToDate(e.target.value); setPage(1) }} />
          </div>
          <div className="flex items-end">
            <button onClick={() => { setUrlFilter(null); setLevelFilter(''); setFromDate(''); setToDate(''); setPage(1) }} className="w-full bg-gray-100 px-2 py-1 rounded text-sm">Clear</button>
          </div>
        </div>
      </div>

      <DataTable columns={columns} rows={rows} emptyMessage="No logs found for selected filters" />

      <div className="flex justify-between items-center mt-4">
        <div className="text-sm text-gray-600">Page {page} (50 per page)</div>
        <div className="flex gap-2">
          <button disabled={page === 1} onClick={() => setPage(page - 1)} className="px-3 py-1 bg-gray-100 rounded disabled:opacity-50">← Previous</button>
          <button onClick={() => setPage(page + 1)} className="px-3 py-1 bg-gray-100 rounded">Next →</button>
        </div>
      </div>
    </div>
  )
}

export default LogViewer
