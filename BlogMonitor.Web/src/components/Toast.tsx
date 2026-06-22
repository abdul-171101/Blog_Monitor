import React from 'react'

export type ToastMessage = { id: number; message: string; type: 'success' | 'error' | 'info' }

const ToastItem: React.FC<{ t: ToastMessage; onClose: (id: number) => void }> = ({ t, onClose }) => {
  const border = t.type === 'success' ? 'border-green-500' : t.type === 'error' ? 'border-red-500' : 'border-blue-500'
  return (
    <div className={`bg-white shadow rounded p-3 mb-3 border-l-4 ${border} w-96`}>
      <div className="flex justify-between items-start">
        <div className="text-sm text-gray-800">{t.message}</div>
        <button onClick={() => onClose(t.id)} className="text-gray-400 hover:text-gray-600 ml-2">✕</button>
      </div>
    </div>
  )
}

const ToastContainer: React.FC<{ toasts: ToastMessage[]; onClose: (id: number) => void }> = ({ toasts, onClose }) => {
  return (
    <div className="fixed top-4 right-4 z-50 flex flex-col items-end">
      {toasts.map((t) => (
        <ToastItem key={t.id} t={t} onClose={onClose} />
      ))}
    </div>
  )
}

export default ToastContainer
