namespace BlogMonitor.Infrastructure.DuckDb
{
    public static class DuckDbSchema
    {
        public const string CreatePageSummariesTable =
            @"CREATE TABLE IF NOT EXISTS page_summaries (
                id BIGINT,
                client_url_id INTEGER NOT NULL,
                page_url VARCHAR NOT NULL,
                page_title VARCHAR NULL,
                summary_date DATE NOT NULL,
                week_number INTEGER NULL,
                cycle_day_number INTEGER NULL,
                md_file_path VARCHAR NULL,
                summary_text VARCHAR NULL,
                word_count INTEGER NULL,
                summary_status VARCHAR NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );";

        public const string CreateDailySummariesTable =
            @"CREATE TABLE IF NOT EXISTS daily_summaries (
                id BIGINT,
                client_url_id INTEGER NOT NULL,
                summary_date DATE NOT NULL,
                day_in_cycle INTEGER NULL,
                status VARCHAR NULL,
                pages_changed INTEGER DEFAULT 0,
                pages_no_change INTEGER DEFAULT 0,
                pages_failed INTEGER DEFAULT 0,
                compiled_summary VARCHAR NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );";

        public const string CreateWeeklyReportsTable =
            @"CREATE TABLE IF NOT EXISTS weekly_reports (
                id BIGINT,
                client_url_id INTEGER NOT NULL,
                client_id INTEGER NOT NULL,
                week_start_date DATE NULL,
                week_end_date DATE NULL,
                week_number INTEGER NULL,
                cycle_label VARCHAR NULL,
                report_json VARCHAR NULL,
                md_file_path VARCHAR NULL,
                pdf_file_path VARCHAR NULL,
                docx_file_path VARCHAR NULL,
                total_days INTEGER DEFAULT 0,
                days_changed INTEGER DEFAULT 0,
                days_no_change INTEGER DEFAULT 0,
                total_pages_scraped INTEGER DEFAULT 0,
                email_status VARCHAR DEFAULT 'PENDING',
                email_sent_at TIMESTAMP NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );";
    }
}
