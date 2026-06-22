import React from 'react'

const mapColor = (status: string) => {
  const s = status?.toUpperCase() ?? ''
  if (['ACTIVE', 'SUCCESS', 'SENT', 'CHANGED'].includes(s)) return 'bg-green-100 text-green-800'
  if (['STOPPED', 'FAILED', 'SUMMARY_FAILED'].includes(s)) return 'bg-red-100 text-red-800'
  if (['PAUSED', 'PENDING', 'PARTIAL'].includes(s)) return 'bg-amber-100 text-amber-800'
  if (['NO_CHANGE', 'INITIAL_RUN'].includes(s)) return 'bg-gray-100 text-gray-800'
  if (s === 'HASH_BASED') return 'bg-blue-100 text-blue-800'
  if (s === 'SITEMAP') return 'bg-teal-100 text-teal-800'
  return 'bg-gray-100 text-gray-800'
}

const StatusBadge: React.FC<{ status: string }> = ({ status }) => (
  <span className={`px-2 py-1 rounded-full text-xs font-semibold ${mapColor(status)}`}>{status}</span>
)

export default StatusBadge
