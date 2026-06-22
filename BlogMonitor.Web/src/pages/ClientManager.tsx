import React, { useEffect, useState } from 'react'
import PageHeader from '../components/PageHeader'
import DataTable from '../components/DataTable'
import StatusBadge from '../components/StatusBadge'
import LoadingSpinner from '../components/LoadingSpinner'
import ConfirmDialog from '../components/ConfirmDialog'
import { clientService } from '../services/clientService'
import type { ClientResponse, CreateClientRequest, UpdateClientRequest } from '../types'
import { useAuth } from '../store/AuthContext'
import useAsync from '../hooks/useAsync'
import { useToast } from '../store/ToastContext'

const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const ClientManager: React.FC = () => {
  const { authState } = useAuth()
  const { data, isLoading, error, refetch } = useAsync(async () => await clientService.getAll(), [])
  const [isModalOpen, setModalOpen] = useState(false)
  const [editing, setEditing] = useState<ClientResponse | null>(null)
  const [formErrors, setFormErrors] = useState<Record<string, string>>({})
  const [form, setForm] = useState<CreateClientRequest & Partial<UpdateClientRequest>>({ fullName: '', email: '', companyName: null, phone: null })
  const [confirm, setConfirm] = useState<{ isOpen: boolean; client?: ClientResponse }>({ isOpen: false })
  const toast = useToast()

  useEffect(() => {
    document.title = 'Client Manager | Blog Monitor'
  }, [])

  const clients: ClientResponse[] = data ?? []

  const openAdd = () => {
    setEditing(null)
    setForm({ fullName: '', email: '', companyName: null, phone: null })
    setFormErrors({})
    setModalOpen(true)
  }

  const openEdit = (c: ClientResponse) => {
    setEditing(c)
    setForm({ fullName: c.fullName, email: c.email, companyName: c.companyName, phone: c.phone, status: c.status })
    setFormErrors({})
    setModalOpen(true)
  }

  const validate = () => {
    const e: Record<string, string> = {}
    if (!form.fullName) e.fullName = 'Full name is required.'
    if (!form.email) e.email = 'Email is required.'
    else if (!emailRegex.test(form.email)) e.email = 'Invalid email address.'
    setFormErrors(e)
    return Object.keys(e).length === 0
  }

  const handleSave = async () => {
    if (!validate()) return
    try {
      if (editing) {
        await clientService.update(editing.id, form as UpdateClientRequest)
        toast.showToast('Client updated', 'success')
      } else {
        await clientService.create(form as CreateClientRequest)
        toast.showToast('Client created', 'success')
      }
      setModalOpen(false)
      await refetch()
    } catch (err: any) {
      setFormErrors({ form: err?.response?.data?.message || 'Server error' })
    }
  }

  const handleDelete = async (c: ClientResponse) => {
    try {
      await clientService.softDelete(c.id)
      toast.showToast('Client deactivated', 'success')
      setConfirm({ isOpen: false })
      await refetch()
    } catch (err: any) {
      toast.showToast(err?.response?.data?.message || 'Delete failed', 'error')
    }
  }

  if (isLoading) return <div className="flex justify-center py-20"><LoadingSpinner size="large" /></div>
  if (error) return <div className="text-red-600">Error: {error}</div>

  if (authState.role !== 'Admin') return <div className="text-red-600">Access denied</div>

  const columns = [
    { header: 'Name', accessor: (r: ClientResponse) => r.fullName },
    { header: 'Email', accessor: (r: ClientResponse) => r.email },
    { header: 'Company', accessor: (r: ClientResponse) => r.companyName ?? '—' },
    { header: 'Phone', accessor: (r: ClientResponse) => r.phone ?? '—' },
    { header: 'Status', accessor: (r: ClientResponse) => <StatusBadge status={r.status} /> },
    { header: 'Active URLs', accessor: (r: ClientResponse) => r.activeUrlCount },
    { header: 'Created', accessor: (r: ClientResponse) => new Date(r.createdAt).toLocaleDateString() },
    {
      header: 'Actions',
      accessor: (r: ClientResponse) => (
        <div className="flex gap-2">
          <button onClick={() => openEdit(r)} className="px-2 py-1 bg-gray-100 rounded">✎</button>
          <button onClick={() => setConfirm({ isOpen: true, client: r })} className="px-2 py-1 bg-red-100 rounded">🗑</button>
        </div>
      ),
    },
  ]

  return (
    <div>
      <PageHeader title="Client Manager">
        <div>
          <button onClick={openAdd} className="bg-blue-600 text-white px-3 py-2 rounded">Add Client</button>
        </div>
      </PageHeader>
      <DataTable columns={columns} rows={clients} emptyMessage="No clients available" />

      <ConfirmDialog isOpen={confirm.isOpen} title="Delete Client" message={`Are you sure you want to deactivate ${confirm.client?.fullName}? This will not delete their data.`} onCancel={() => setConfirm({ isOpen: false })} onConfirm={() => handleDelete(confirm.client!)} />

      {isModalOpen && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50" onClick={() => setModalOpen(false)}>
          <div className="bg-white rounded p-6 w-[480px]" onClick={(e) => e.stopPropagation()}>
            <h3 className="text-lg font-bold mb-2">{editing ? 'Edit Client' : 'Add Client'}</h3>
            <div className="space-y-3">
              <div>
                <label className="block text-sm">Full Name</label>
                <input className="w-full border px-3 py-2 rounded" value={form.fullName ?? ''} onChange={(e) => setForm({ ...form, fullName: e.target.value })} />
                {formErrors.fullName && <div className="text-sm text-red-600">{formErrors.fullName}</div>}
              </div>
              <div>
                <label className="block text-sm">Email</label>
                <input className="w-full border px-3 py-2 rounded" value={form.email ?? ''} onChange={(e) => setForm({ ...form, email: e.target.value })} />
                {formErrors.email && <div className="text-sm text-red-600">{formErrors.email}</div>}
              </div>
              <div>
                <label className="block text-sm">Company</label>
                <input className="w-full border px-3 py-2 rounded" value={form.companyName ?? ''} onChange={(e) => setForm({ ...form, companyName: e.target.value })} />
              </div>
              <div>
                <label className="block text-sm">Phone</label>
                <input className="w-full border px-3 py-2 rounded" value={form.phone ?? ''} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
              </div>
              {editing && (
                <div>
                  <label className="block text-sm">Status</label>
                  <select className="w-full border px-3 py-2 rounded" value={form.status ?? 'ACTIVE'} onChange={(e) => setForm({ ...form, status: e.target.value })}>
                    <option>ACTIVE</option>
                    <option>INACTIVE</option>
                  </select>
                </div>
              )}

              {formErrors.form && <div className="text-sm text-red-600">{formErrors.form}</div>}

              <div className="flex justify-end gap-2 mt-4">
                <button onClick={() => setModalOpen(false)} className="px-4 py-2 rounded bg-gray-100">Cancel</button>
                <button onClick={handleSave} className="px-4 py-2 rounded bg-blue-600 text-white">Save</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

export default ClientManager
