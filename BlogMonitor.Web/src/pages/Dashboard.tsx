import React, { useEffect } from 'react'
import PageHeader from '../components/PageHeader'
import DataTable from '../components/DataTable'
import StatusBadge from '../components/StatusBadge'
import LoadingSpinner from '../components/LoadingSpinner'
import { urlService } from '../services/urlService'
import { clientService } from '../services/clientService'
import useAsync from '../hooks/useAsync'

const formatDate = (s?: string | null) => {
  if (!s) return 'Never'
  const d = new Date(s)
  return d.toLocaleString(undefined, { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

const truncate = (t: string, n = 60) => (t.length > n ? t.slice(0, n - 1) + '…' : t)

const Dashboard: React.FC = () => {
  const { data, isLoading, error } = useAsync(async () => {
    const [urls, clients] = await Promise.all([urlService.getAll(), clientService.getAll()])
    return { urls, clients }
  }, [])

  useEffect(() => {
    document.title = 'Dashboard | Blog Monitor'
  }, [])

  if (isLoading) return <div className="flex justify-center py-20"><LoadingSpinner size="large" /></div>
  if (error) return <div className="text-red-600">Error: {error}</div>

  const urls = data?.urls ?? []
  const clients = data?.clients ?? []

  const totalClients = clients.length
  const totalUrls = urls.length
  const activeUrls = urls.filter((u: any) => u.status === 'ACTIVE').length
  const today = new Date().toISOString().slice(0, 10)
  const runningToday = urls.filter((u: any) => u.lastRunAt && u.lastRunAt.slice(0, 10) === today).length

  const urlColumns = [
    { header: 'Client', accessor: (r: any) => r.clientName },
    { header: 'URL', accessor: (r: any) => <span title={r.url}>{truncate(r.url, 60)}</span> },
    { header: 'Track Type', accessor: (r: any) => <StatusBadge status={r.trackType} /> },
    { header: 'Status', accessor: (r: any) => <StatusBadge status={r.status} /> },
    { header: 'Last Run', accessor: (r: any) => formatDate(r.lastRunAt) },
    { header: 'Week', accessor: (r: any) => (r.cycleWeekNumber ? `Week ${r.cycleWeekNumber}` : '—') },
  ]

  const clientColumns = [
    { header: 'Name', accessor: (c: any) => c.fullName },
    { header: 'Email', accessor: (c: any) => c.email },
    { header: 'Company', accessor: (c: any) => c.companyName ?? '—' },
    { header: 'Active URLs', accessor: (c: any) => c.activeUrlCount },
    { header: 'Status', accessor: (c: any) => <StatusBadge status={c.status} /> },
  ]

  return (
    <div>
      <PageHeader title="Dashboard" />

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <div className="bg-gray-50 rounded p-4">
          <div className="text-sm text-gray-500">Total Clients</div>
          <div className="text-2xl font-bold">{totalClients}</div>
        </div>
        <div className="bg-gray-50 rounded p-4">
          <div className="text-sm text-gray-500">Active URLs</div>
          <div className="text-2xl font-bold">{activeUrls}</div>
        </div>
        <div className="bg-gray-50 rounded p-4">
          <div className="text-sm text-gray-500">Total URLs</div>
          <div className="text-2xl font-bold">{totalUrls}</div>
        </div>
        <div className="bg-gray-50 rounded p-4">
          <div className="text-sm text-gray-500">URLs Running Today</div>
          <div className="text-2xl font-bold">{runningToday}</div>
        </div>
      </div>

      <h2 className="text-lg font-semibold mb-2">Registered URLs</h2>
      <DataTable columns={urlColumns} rows={urls} emptyMessage="No monitored URLs yet" />

      <h2 className="text-lg font-semibold mt-6 mb-2">Clients Overview</h2>
      <DataTable columns={clientColumns} rows={clients} emptyMessage="No clients found" />
    </div>
  )
}

export default Dashboard
