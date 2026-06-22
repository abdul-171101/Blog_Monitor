export const truncate = (value: string, maxLength = 80) =>
  value.length > maxLength ? `${value.slice(0, maxLength - 1)}…` : value

export const formatDate = (iso?: string | null) => {
  if (!iso) return 'Unknown'
  const date = new Date(iso)
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}

export const formatDateTime = (iso?: string | null) => {
  if (!iso) return 'Unknown'
  const date = new Date(iso)
  return date.toLocaleString(undefined, { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

export const getDateRange = (start: string, end: string) => {
  const result: string[] = []
  const current = new Date(start)
  const finish = new Date(end)
  while (current <= finish) {
    result.push(current.toISOString().slice(0, 10))
    current.setDate(current.getDate() + 1)
  }
  return result
}
