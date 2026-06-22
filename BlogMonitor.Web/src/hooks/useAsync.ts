import { useEffect, useState, useCallback } from 'react'

export function useAsync<T>(asyncFn: () => Promise<T>, deps: unknown[] = []) {
  const [data, setData] = useState<T | null>(null)
  const [isLoading, setIsLoading] = useState<boolean>(true)
  const [error, setError] = useState<string | null>(null)

  const execute = useCallback(async () => {
    setIsLoading(true)
    setError(null)
    try {
      const res = await asyncFn()
      setData(res)
      return res
    } catch (err: any) {
      setError(err?.response?.data?.message || err?.message || String(err))
      setData(null)
      throw err
    } finally {
      setIsLoading(false)
    }
  }, deps) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    void execute()
  }, [execute])

  return { data, isLoading, error, refetch: execute }
}

export default useAsync
