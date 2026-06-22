// Auth
export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresInMinutes: number;
  role: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  role: string;
}

// Client
export interface ClientResponse {
  id: number;
  fullName: string;
  email: string;
  companyName: string | null;
  phone: string | null;
  status: string;
  createdAt: string;
  activeUrlCount: number;
}

export interface CreateClientRequest {
  fullName: string;
  email: string;
  companyName: string | null;
  phone: string | null;
}

export interface UpdateClientRequest {
  fullName: string;
  email: string;
  companyName: string | null;
  phone: string | null;
  status: string;
}

// URL
export interface UrlSummaryResponse {
  id: number;
  clientId: number;
  clientName: string;
  url: string;
  trackType: string;
  status: string;
  registeredAt: string;
  lastRunAt: string | null;
  cycleWeekNumber: number | null;
  cycleDayNumber: number | null;
}

export interface UrlDetailResponse {
  id: number;
  clientId: number;
  clientName: string;
  url: string;
  sitemapUrl: string | null;
  trackType: string;
  status: string;
  registeredAt: string;
  lastRunAt: string | null;
  cycleStartDate: string | null;
  cycleWeekNumber: number | null;
}

export interface RegisterUrlRequest {
  clientId: number;
  url: string;
}

// Report
export interface WeeklyReportListItem {
  id: number;
  clientUrlId: number;
  url: string;
  weekStartDate: string | null;
  weekEndDate: string | null;
  weekNumber: number | null;
  cycleLabel: string | null;
  totalDays: number;
  daysChanged: number;
  daysNoChange: number;
  totalPagesScraped: number;
  emailStatus: string;
  createdAt: string;
}

export interface WeeklyReportDetailResponse {
  id: number;
  clientUrlId: number;
  url: string;
  weekStartDate: string | null;
  weekEndDate: string | null;
  weekNumber: number | null;
  cycleLabel: string | null;
  reportJson: string | null;
  mdFilePath: string | null;
  totalDays: number;
  daysChanged: number;
  daysNoChange: number;
  totalPagesScraped: number;
  emailStatus: string;
  emailSentAt: string | null;
}

export interface DailySummaryResponse {
  id: number;
  clientUrlId: number;
  summaryDate: string;
  dayInCycle: number | null;
  status: string | null;
  pagesChanged: number;
  pagesNoChange: number;
  pagesFailed: number;
  compiledSummary: string | null;
  createdAt: string;
}

// Logs
export interface MonitoringLogResponse {
  id: number;
  clientUrlId: number | null;
  url: string | null;
  eventType: string;
  message: string;
  level: string;
  timestamp: string;
}
