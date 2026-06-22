import React, { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import PageHeader from '../components/PageHeader'
import DataTable from '../components/DataTable'
import LoadingSpinner from '../components/LoadingSpinner'
import StatusBadge from '../components/StatusBadge'
import { reportService } from '../services/reportService'
import { urlService } from '../services/urlService'
import type { WeeklyReportListItem, DailySummaryResponse, UrlSummaryResponse } from '../types'
import useAsync from '../hooks/useAsync'
import { useToast } from '../store/ToastContext'
import { formatDate, formatDateTime, getDateRange, truncate } from '../utils/formatUtils'

interface ParsedReportJsonDay {
  Date: string
  Status: string | null
  PagesChanged: number
  PagesNoChange: number
  PagesFailed: number
  CompiledSummary: string
}

interface ParsedReportJson {
  ClientName: string
  ClientEmail: string
  MonitoredUrl: string
  WeekNumber: number
  CycleStartDate: string
  CycleEndDate: string
  GeneratedAt: string
  Days: ParsedReportJsonDay[]
}

const parseReportJson = (value: string | null): ParsedReportJson | null => {
  if (!value) return null
  try {
    return JSON.parse(value) as ParsedReportJson
  } catch {
    return null
  }
}

const ReportViewer: React.FC = () => {
  const [searchParams] = useSearchParams()
  const toast = useToast()
  const queryUrlId = searchParams.get('clientUrlId') ? Number(searchParams.get('clientUrlId')) : null

  const [selectedClientUrlId, setSelectedClientUrlId] = useState<number | null>(null)
  const [selectedReportId, setSelectedReportId] = useState<number | null>(null)
  const [activeTab, setActiveTab] = useState<'weekly' | 'daily'>('weekly')
  const [dailyDate, setDailyDate] = useState<string>(() => new Date().toISOString().slice(0, 10))
  const [dailySummary, setDailySummary] = useState<DailySummaryResponse | null>(null)
  const [dailyMissing, setDailyMissing] = useState(false)
  const [dailyLoading, setDailyLoading] = useState(false)
  const [downloadLoading, setDownloadLoading] = useState({ pdf: false, docx: false })
  const [resendLoading, setResendLoading] = useState(false)
  const [fallbackSummaries, setFallbackSummaries] = useState<DailySummaryResponse[]>([])

  const { data: urls, isLoading: urlsLoading, error: urlsError } = useAsync(async () => await urlService.getAll(), [])
  const { data: weeklyReports, isLoading: weeklyLoading, error: weeklyError } = useAsync(
    async () => {
      if (!selectedClientUrlId) return []
      return await reportService.listWeekly(selectedClientUrlId)
    },
    [selectedClientUrlId]
  )

  const { data: selectedReport, isLoading: selectedReportLoading, error: selectedReportError, refetch: fetchSelectedReport } = useAsync(
    async () => {
      if (!selectedReportId) return null
      return await reportService.getWeeklyDetail(selectedReportId)
    },
    [selectedReportId]
  )

  useEffect(() => {
    document.title = 'Report Viewer | Blog Monitor'
  }, [])

  useEffect(() => {
    if (!urls?.length) return
    if (selectedClientUrlId !== null) return

    if (queryUrlId && urls.some((item) => item.id === queryUrlId)) {
      setSelectedClientUrlId(queryUrlId)
    } else {
      setSelectedClientUrlId(urls[0].id)
    }
  }, [urls, queryUrlId, selectedClientUrlId])

  useEffect(() => {
    setSelectedReportId(null)
    setDailySummary(null)
    setDailyMissing(false)
    setActiveTab('weekly')
  }, [selectedClientUrlId])

  useEffect(() => {
    if (!selectedReport || selectedReport.reportJson || !selectedClientUrlId || !selectedReport.weekStartDate || !selectedReport.weekEndDate) {
      setFallbackSummaries([])
      return
    }

    let isCancelled = false
    const loadSummaries = async () => {
      const summaries: DailySummaryResponse[] = []
      const startDate = selectedReport.weekStartDate!
      const endDate = selectedReport.weekEndDate!
      const days = getDateRange(startDate, endDate)

      for (const date of days) {
        try {
          const summary = await reportService.getDailySummary(selectedClientUrlId, date)
          if (isCancelled) return
          summaries.push(summary)
        } catch {
          continue
        }
      }

      if (!isCancelled) {
        setFallbackSummaries(summaries)
      }
    }

    void loadSummaries()
    return () => {
      isCancelled = true
    }
  }, [selectedReport, selectedClientUrlId])

  const handleSelectClientUrl = (value: string) => {
    const id = Number(value)
    setSelectedClientUrlId(Number.isFinite(id) ? id : null)
  }

  const handleSelectReport = (value: string) => {
    const id = Number(value)
    setSelectedReportId(Number.isFinite(id) ? id : null)
  }

  const handleDownload = async (format: 'pdf' | 'docx') => {
    if (!selectedReport) return
    setDownloadLoading((prev) => ({ ...prev, [format]: true }))
    try {
      await reportService.downloadReport(selectedReport.id, format)
      toast.showToast(`Downloaded ${format.toUpperCase()}`, 'success')
    } catch (err: any) {
      toast.showToast(err?.response?.data?.message || 'Download failed', 'error')
    } finally {
      setDownloadLoading((prev) => ({ ...prev, [format]: false }))
    }
  }

  const handleResendEmail = async () => {
    if (!selectedReport) return
    setResendLoading(true)
    try {
      await reportService.resendEmail(selectedReport.id)
      toast.showToast('Email resent', 'success')
      await fetchSelectedReport()
    } catch (err: any) {
      toast.showToast(err?.response?.data?.message || 'Resend failed', 'error')
    } finally {
      setResendLoading(false)
    }
  }

  const handleLoadDailySummary = async () => {
    if (!selectedClientUrlId || !dailyDate) return
    setDailyLoading(true)
    setDailyMissing(false)
    setDailySummary(null)

    try {
      const summary = await reportService.getDailySummary(selectedClientUrlId, dailyDate)
      setDailySummary(summary)
    } catch (err: any) {
      if (err?.response?.status === 404) {
        setDailyMissing(true)
      } else {
        toast.showToast(err?.response?.data?.message || 'Unable to load daily summary', 'error')
      }
    } finally {
      setDailyLoading(false)
    }
  }

  const clientUrl = urls?.find((item) => item.id === selectedClientUrlId) || null
  const reports = weeklyReports ?? []
  const selectedReportJson = parseReportJson(selectedReport?.reportJson ?? null)
  const noUrlsFound = !urlsLoading && urls?.length === 0

  if (urlsLoading || weeklyLoading || selectedReportLoading) {
    return <div className="flex justify-center py-20"><LoadingSpinner size="large" /></div>
  }

  if (urlsError) return <div className="text-red-600">Error: {urlsError}</div>
  if (weeklyError) return <div className="text-red-600">Error: {weeklyError}</div>
  if (selectedReportError) return <div className="text-red-600">Error: {selectedReportError}</div>

  return (
    <div>
      <PageHeader title="Report Viewer" />

      <div className="grid gap-4 md:grid-cols-[1.5fr_2fr] mb-6">
        <div className="bg-white rounded shadow p-4">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h2 className="text-lg font-semibold">Select monitored URL</h2>
              <p className="text-sm text-gray-600">Review weekly and daily reports for tracked URLs.</p>
            </div>
            <div className="w-full sm:w-auto">
              <label className="block text-sm font-medium mb-1">Monitored URL</label>
              <select value={selectedClientUrlId ?? ''} onChange={(e) => handleSelectClientUrl(e.target.value)} className="w-full border rounded px-3 py-2 text-sm">
                <option value="">Select a URL</option>
                {(urls ?? []).map((item: UrlSummaryResponse) => (
                  <option key={item.id} value={item.id}>{truncate(item.url, 60)}</option>
                ))}
              </select>
            </div>
          </div>

          {clientUrl && (
            <div className="mt-4 space-y-2">
              <div className="text-sm text-gray-600">Client</div>
              <div className="font-medium">{clientUrl.clientName}</div>
              <div className="text-sm text-gray-600">URL</div>
              <div className="break-words">{clientUrl.url}</div>
            </div>
          )}

          {noUrlsFound && <div className="mt-4 text-sm text-gray-600">No monitored URLs are registered yet. Add a URL to begin receiving reports.</div>}
        </div>

        <div className="bg-white rounded shadow p-4">
          <div className="flex items-center gap-3 border-b pb-3 mb-4">
            <button onClick={() => setActiveTab('weekly')} className={`px-3 py-2 rounded ${activeTab === 'weekly' ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-700'}`}>
              Weekly Report
            </button>
            <button onClick={() => setActiveTab('daily')} className={`px-3 py-2 rounded ${activeTab === 'daily' ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-700'}`}>
              Daily Summary
            </button>
          </div>

          {activeTab === 'daily' ? (
            <div className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-1">Date</label>
                  <input type="date" className="w-full border rounded px-3 py-2 text-sm" value={dailyDate} onChange={(e) => setDailyDate(e.target.value)} />
                </div>
                <div className="flex items-end gap-3">
                  <button onClick={handleLoadDailySummary} className="rounded bg-blue-600 px-4 py-2 text-white" disabled={!selectedClientUrlId || dailyLoading}>
                    {dailyLoading ? 'Loading…' : 'Load Daily Summary'}
                  </button>
                </div>
              </div>

              {dailyMissing && !dailyLoading && (
                <div className="rounded border border-amber-300 bg-amber-50 p-4 text-sm text-amber-900">
                  Daily summary for {dailyDate} was not found.
                </div>
              )}

              {dailySummary && (
                <div className="rounded bg-gray-50 p-4">
                  <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4 mb-4">
                    <div>
                      <div className="text-sm text-gray-500">Status</div>
                      <StatusBadge status={dailySummary.status ?? 'UNKNOWN'} />
                    </div>
                    <div>
                      <div className="text-sm text-gray-500">Pages Changed</div>
                      <div className="mt-1 font-semibold">{dailySummary.pagesChanged}</div>
                    </div>
                    <div>
                      <div className="text-sm text-gray-500">Pages No Change</div>
                      <div className="mt-1 font-semibold">{dailySummary.pagesNoChange}</div>
                    </div>
                    <div>
                      <div className="text-sm text-gray-500">Pages Failed</div>
                      <div className="mt-1 font-semibold">{dailySummary.pagesFailed}</div>
                    </div>
                  </div>
                  <div>
                    <div className="text-sm text-gray-500">Compiled Summary</div>
                    <div className="mt-2 whitespace-pre-wrap text-sm text-gray-700">{dailySummary.compiledSummary || 'No summary available.'}</div>
                  </div>
                </div>
              )}
            </div>
          ) : (
            <div>
              <div className="mb-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                <div className="rounded bg-gray-50 p-4">
                  <div className="text-sm text-gray-500">Available Reports</div>
                  <div className="text-2xl font-semibold">{reports.length}</div>
                </div>
                <div className="rounded bg-gray-50 p-4">
                  <div className="text-sm text-gray-500">Selected Report</div>
                  <div className="text-2xl font-semibold">{selectedReport ? `Week ${selectedReport.weekNumber ?? '—'}` : 'None'}</div>
                </div>
                <div className="rounded bg-gray-50 p-4">
                  <div className="text-sm text-gray-500">Last Loaded</div>
                  <div className="text-2xl font-semibold">{selectedReport ? formatDateTime(selectedReport.emailSentAt ?? selectedReport.weekEndDate) : '—'}</div>
                </div>
                <div className="rounded bg-gray-50 p-4">
                  <div className="text-sm text-gray-500">Email Status</div>
                  <div className="mt-2">{selectedReport ? <StatusBadge status={selectedReport.emailStatus} /> : '—'}</div>
                </div>
              </div>

              <div className="grid gap-4 md:grid-cols-[1fr_2fr]">
                <div className="bg-white rounded shadow p-4">
                  <label className="block text-sm font-medium mb-2">Select a weekly report</label>
                  <select value={selectedReportId ?? ''} onChange={(e) => handleSelectReport(e.target.value)} className="w-full border rounded px-3 py-2 text-sm">
                    <option value="">Choose a week</option>
                    {reports.map((report: WeeklyReportListItem) => (
                      <option key={report.id} value={report.id}>
                        {report.weekNumber ? `Week ${report.weekNumber}` : 'Unnamed cycle'} • {formatDate(report.weekStartDate)} — {formatDate(report.weekEndDate)}
                      </option>
                    ))}
                  </select>

                  <div className="mt-4 text-sm text-gray-600">
                    Pick a report to load the weekly summary, export files, or resend the email.
                  </div>
                </div>

                <div className="bg-white rounded shadow p-4">
                  <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mb-4">
                    <div>
                      <div className="text-sm text-gray-500">Actions</div>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <button onClick={() => handleDownload('pdf')} disabled={!selectedReport || downloadLoading.pdf} className="rounded bg-blue-600 px-3 py-2 text-sm text-white disabled:opacity-50">
                        {downloadLoading.pdf ? 'Downloading PDF…' : 'Download PDF'}
                      </button>
                      <button onClick={() => handleDownload('docx')} disabled={!selectedReport || downloadLoading.docx} className="rounded bg-gray-100 px-3 py-2 text-sm text-gray-800 disabled:opacity-50">
                        {downloadLoading.docx ? 'Downloading DOCX…' : 'Download DOCX'}
                      </button>
                      <button onClick={handleResendEmail} disabled={!selectedReport || resendLoading} className="rounded bg-purple-600 px-3 py-2 text-sm text-white disabled:opacity-50">
                        {resendLoading ? 'Resending…' : 'Resend Email'}
                      </button>
                    </div>
                  </div>

                  {selectedReport ? (
                    <div className="space-y-3 text-sm text-gray-700">
                      <div>
                        <strong>Week:</strong> {selectedReport.weekNumber ? `Week ${selectedReport.weekNumber}` : 'Unknown'}
                      </div>
                      <div>
                        <strong>Cycle:</strong> {selectedReport.cycleLabel ?? 'Unknown'}
                      </div>
                      <div>
                        <strong>Period:</strong> {formatDate(selectedReport.weekStartDate)} — {formatDate(selectedReport.weekEndDate)}
                      </div>
                      <div>
                        <strong>Generated:</strong> {formatDateTime(selectedReport.emailSentAt ?? selectedReport.weekEndDate)}
                      </div>
                    </div>
                  ) : (
                    <div className="rounded border border-dashed border-gray-300 p-4 text-sm text-gray-600">Select a report to see details and export options.</div>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      <div className="bg-white rounded shadow p-4">
        <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-lg font-semibold">Report archive</h2>
            <p className="text-sm text-gray-500">Weekly report history for the selected URL.</p>
          </div>
          <div className="text-sm text-gray-600">{reports.length} report{reports.length === 1 ? '' : 's'}</div>
        </div>

        <DataTable columns={[
          { header: 'Week', accessor: (r: WeeklyReportListItem) => r.weekNumber ? `Week ${r.weekNumber}` : '—' },
          { header: 'Date Range', accessor: (r: WeeklyReportListItem) => `${formatDate(r.weekStartDate)} — ${formatDate(r.weekEndDate)}` },
          { header: 'Changed', accessor: (r: WeeklyReportListItem) => r.daysChanged },
          { header: 'No Change', accessor: (r: WeeklyReportListItem) => r.daysNoChange },
          { header: 'Pages', accessor: (r: WeeklyReportListItem) => r.totalPagesScraped },
          { header: 'Email', accessor: (r: WeeklyReportListItem) => <StatusBadge status={r.emailStatus} /> },
        ]} rows={reports} emptyMessage="No reports found for this URL" />
      </div>

      {selectedReport && (
        <div className="mt-6 bg-white rounded shadow p-4">
          <div className="mb-4 flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 className="text-lg font-semibold">Weekly report detail</h2>
              <p className="text-sm text-gray-500">Content extracted from the selected weekly report.</p>
            </div>
            <div className="text-sm text-gray-600">{selectedReportJson ? 'JSON authored report' : 'Markdown fallback view'}</div>
          </div>

          {selectedReportJson ? (
            <div className="space-y-4">
              <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
                <div className="rounded bg-gray-50 p-4">
                  <div className="text-sm text-gray-500">Monitored URL</div>
                  <div className="mt-2 font-semibold break-words">{selectedReportJson.MonitoredUrl}</div>
                </div>
                <div className="rounded bg-gray-50 p-4">
                  <div className="text-sm text-gray-500">Client</div>
                  <div className="mt-2 font-semibold">{selectedReportJson.ClientName}</div>
                </div>
                <div className="rounded bg-gray-50 p-4">
                  <div className="text-sm text-gray-500">Cycle range</div>
                  <div className="mt-2 font-semibold">{formatDate(selectedReportJson.CycleStartDate)} — {formatDate(selectedReportJson.CycleEndDate)}</div>
                </div>
                <div className="rounded bg-gray-50 p-4">
                  <div className="text-sm text-gray-500">Generated</div>
                  <div className="mt-2 font-semibold">{formatDateTime(selectedReportJson.GeneratedAt)}</div>
                </div>
              </div>

              <div className="space-y-3">
                {selectedReportJson.Days.map((day) => (
                  <div key={day.Date} className="rounded border border-gray-200 p-4">
                    <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                      <div>
                        <div className="text-sm text-gray-500">{formatDate(day.Date)}</div>
                        <div className="font-semibold">{day.Status ?? 'Status unknown'}</div>
                      </div>
                      <div className="flex flex-wrap gap-2 text-sm text-gray-700">
                        <span>Changed: {day.PagesChanged}</span>
                        <span>No change: {day.PagesNoChange}</span>
                        <span>Failed: {day.PagesFailed}</span>
                      </div>
                    </div>
                    <div className="mt-3 whitespace-pre-wrap text-sm text-gray-700">{day.CompiledSummary || 'No summary available.'}</div>
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <div className="rounded border border-dashed border-gray-300 p-4 text-sm text-gray-700">
              <p className="mb-3">The selected report has no structured JSON report available.</p>
              {fallbackSummaries.length > 0 ? (
                <div className="space-y-3">
                  {fallbackSummaries.map((summary) => (
                    <div key={summary.id} className="rounded bg-gray-50 p-3">
                      <div className="flex items-center justify-between text-sm text-gray-500 mb-2">
                        <span>{formatDate(summary.summaryDate)}</span>
                        <StatusBadge status={summary.status ?? 'UNKNOWN'} />
                      </div>
                      <div className="whitespace-pre-wrap text-sm text-gray-700">{summary.compiledSummary || 'No summary available.'}</div>
                    </div>
                  ))}
                </div>
              ) : (
                <p>No daily summaries are available for this report cycle.</p>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  )
}

export default ReportViewer
