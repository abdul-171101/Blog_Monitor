import React, { createContext, useContext, useState } from 'react'
import type { ReactNode } from 'react'
import ToastContainer from '../components/Toast'
import type { ToastMessage } from '../components/Toast'

type ToastContextType = { showToast: (message: string, type?: ToastMessage['type']) => void }

const ToastContext = createContext<ToastContextType | undefined>(undefined)

export const ToastProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [toasts, setToasts] = useState<ToastMessage[]>([])

  const showToast = (message: string, type: ToastMessage['type'] = 'info') => {
    const id = Date.now()
    const t: ToastMessage = { id, message, type }
    setToasts((s) => [t, ...s])
    setTimeout(() => {
      setToasts((s) => s.filter((x) => x.id !== id))
    }, 3000)
  }

  const onClose = (id: number) => setToasts((s) => s.filter((t) => t.id !== id))

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <ToastContainer toasts={toasts} onClose={onClose} />
    </ToastContext.Provider>
  )
}

export const useToast = () => {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast must be used within ToastProvider')
  return ctx
}
