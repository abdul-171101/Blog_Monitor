import React, { useEffect, useState } from 'react'
import PageHeader from '../components/PageHeader'
import DataTable from '../components/DataTable'
import StatusBadge from '../components/StatusBadge'
import LoadingSpinner from '../components/LoadingSpinner'
import ConfirmDialog from '../components/ConfirmDialog'
import { urlService } from '../services/urlService'
import { clientService } from '../services/clientService'
import type { UrlSummaryResponse, RegisterUrlRequest } from '../types'
import useAsync from '../hooks/useAsync'
import { useToast } from '../store/ToastContext'
import { useAuth } from '../store/AuthContext'
import { useNavigate } from 'react-router-dom'
import { truncate } from '../utils/formatUtils'

const UrlManager: React.FC = () => {
  const { data, isLoading, error, refetch } = useAsync(async () => {
    const [urls, clients] = await Promise.all([urlService.getAll(), clientService.getAll()])
    return { urls, clients }
  }, [])
  const toast = useToast()
  const { authState } = useAuth()
  const navigate = useNavigate()

  useEffect(() => {
    document.title = 'URL Manager | Blog Monitor'
  }, [])

  if (isLoading) return <div className="flex justify-center py-20"><LoadingSpinner size="large" /></div>
  if (error) return <div className="text-red-600">Error: {error}</div>

  const urls: UrlSummaryResponse[] = data?.urls ?? []
  const clients = data?.clients ?? []

  const [isRegisterOpen, setRegisterOpen] = useState(false)
  const [registerForm, setRegisterForm] = useState<RegisterUrlRequest>({ clientId: 0, url: '' })
  const [perRowLoading, setPerRowLoading] = useState<Record<string, boolean>>({})
  const [confirm, setConfirm] = useState<{ isOpen: boolean; url?: UrlSummaryResponse }>({ isOpen: false })

  const handleRunNow = async (r: UrlSummaryResponse) => {
    setPerRowLoading((s) => ({ ...s, [r.id]: true }))
    try {
      await urlService.runNow(r.id)
      toast.showToast('Run started', 'success')
    } catch (err: any) {
      toast.showToast(err?.response?.data?.message || 'Run failed', 'error')
    } finally {
      setPerRowLoading((s) => ({ ...s, [r.id]: false }))
    }
  }

  const handleStop = async (r: UrlSummaryResponse) => {
    try {
      await urlService.stop(r.id)
      toast.showToast('Stopped', 'success')
      await refetch()
    } catch (err: any) {
      toast.showToast(err?.response?.data?.message || 'Stop failed', 'error')
    }
  }

  const handleRegister = async () => {
    try {
      await urlService.register(registerForm)
      toast.showToast('URL registered', 'success')
      setRegisterOpen(false)
      await refetch()
    } catch (err: any) {
      toast.showToast(err?.response?.data?.message || 'Register failed', 'error')
    }
  }

  const columns = [
    { header: 'Client', accessor: (r: UrlSummaryResponse) => r.clientName },
    { header: 'URL', accessor: (r: UrlSummaryResponse) => <span title={r.url}>{truncate(r.url, 60)}</span> },
    { header: 'Track', accessor: (r: UrlSummaryResponse) => <StatusBadge status={r.trackType} /> },
    { header: 'Status', accessor: (r: UrlSummaryResponse) => <StatusBadge status={r.status} /> },
    { header: 'Last Run', accessor: (r: UrlSummaryResponse) => (r.lastRunAt ? new Date(r.lastRunAt).toLocaleString() : 'Never') },
    {
      header: 'Actions',
      accessor: (r: UrlSummaryResponse) => (
        <div className="flex gap-2 flex-wrap">
          <button onClick={() => navigate(`/reports?clientUrlId=${r.id}`)} className="px-2 py-1 bg-gray-100 rounded">View</button>
          <button disabled={perRowLoading[r.id]} onClick={() => handleRunNow(r)} className="px-2 py-1 bg-blue-100 rounded">{perRowLoading[r.id] ? 'Running…' : 'Run Now'}</button>
          {authState.role === 'Admin' && r.status === 'ACTIVE' && <button onClick={() => setConfirm({ isOpen: true, url: r })} className="px-2 py-1 bg-red-100 rounded">Stop</button>}
        </div>
      ),
    },
  ]

  return (
    <div>
      <PageHeader title="URL Manager">
        <div>
          {authState.role === 'Admin' && <button onClick={() => setRegisterOpen(true)} className="bg-blue-600 text-white px-3 py-2 rounded">Register URL</button>}
        </div>
      </PageHeader>

      <DataTable columns={columns} rows={urls} emptyMessage="No monitored URLs yet" />

      <ConfirmDialog isOpen={confirm.isOpen} title="Stop URL" message={`Stop monitoring ${confirm.url?.url}?`} onCancel={() => setConfirm({ isOpen: false })} onConfirm={() => handleStop(confirm.url!)} />

      {isRegisterOpen && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50" onClick={() => setRegisterOpen(false)}>
          <div className="bg-white rounded p-6 w-[520px]" onClick={(e) => e.stopPropagation()}>
            <h3 className="text-lg font-bold mb-2">Register URL</h3>
            <div className="space-y-3">
              <div>
                <label className="block text-sm">Client</label>
                <select className="w-full border px-3 py-2 rounded" value={registerForm.clientId || ''} onChange={(e) => setRegisterForm({ ...registerForm, clientId: Number(e.target.value) })}>
                  <option value="">Select client</option>
                  {clients.map((c: any) => (
                    <option key={c.id} value={c.id}>{c.fullName}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm">URL</label>
                <input className="w-full border px-3 py-2 rounded" value={registerForm.url} onChange={(e) => setRegisterForm({ ...registerForm, url: e.target.value })} />
              </div>
              <div className="flex justify-end gap-2 mt-4">
                <button onClick={() => setRegisterOpen(false)} className="px-4 py-2 rounded bg-gray-100">Cancel</button>
                <button onClick={handleRegister} className="px-4 py-2 rounded bg-blue-600 text-white">Register</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default UrlManager
