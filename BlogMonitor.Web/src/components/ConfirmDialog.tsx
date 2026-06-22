import React from 'react'

const ConfirmDialog: React.FC<{
  isOpen: boolean
  title: string
  message: string
  onConfirm: () => void
  onCancel: () => void
}> = ({ isOpen, title, message, onConfirm, onCancel }) => {
  if (!isOpen) return null
  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50" onClick={onCancel}>
      <div className="bg-white rounded shadow p-6 max-w-md w-full" onClick={(e) => e.stopPropagation()}>
        <h3 className="text-lg font-bold mb-2">{title}</h3>
        <p className="mb-4">{message}</p>
        <div className="flex justify-end gap-2">
          <button onClick={onCancel} className="px-4 py-2 rounded bg-gray-100">Cancel</button>
          <button onClick={onConfirm} className="px-4 py-2 rounded bg-red-600 text-white">Confirm</button>
        </div>
      </div>
    </div>
  )
}

export default ConfirmDialog
