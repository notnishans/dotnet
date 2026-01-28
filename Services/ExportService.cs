using JournalApp.Models;
using JournalApp.Data;
using System.Text;

namespace JournalApp.Services
{
    /// <summary>
    /// Service for exporting journal entries to HTML/PDF
    /// Simple, student-friendly approach using HTML export
    /// </summary>
    /// <summary>
    /// Service for exporting journal entries.
    /// [VIVA INFO]: Demonstrates 'File I/O' and 'Information Presentation'.
    /// This service transforms raw database records into a professional HTML report.
    /// </summary>
    public class ExportService
    {
        private readonly JournalService _journalService;

        public ExportService(JournalService journalService)
        {
            _journalService = journalService;
        }

        /// <summary>
        /// Generates an HTML report for a range of entries.
        /// [VIVA INFO]: HTML is used because it's cross-platform and can be 
        /// converted to PDF by any modern browser or specialized library.
        /// </summary>
        public async Task<string> ExportToPdfAsync(DateTime startDate, DateTime endDate, string filePath, int userId)
        {
            try
            {
                // [LOGIC]: Delegate data fetching to the JournalService (Separation of Concerns).
                var entries = await _journalService.FilterByDateRangeAsync(startDate, endDate, userId);

                if (!entries.Any()) return "No entries found for the selected date range.";

                // [FILE SYSTEM]: Ensure the output directory exists on the user's device.
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                filePath = Path.ChangeExtension(filePath, ".html");

                // [STRING BUILDING]: Constructing the HTML structure with embedded CSS for styling.
                var html = new StringBuilder();
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html lang='en'>");
                html.AppendLine("<head>");
                html.AppendLine("    <meta charset='UTF-8'>");
                html.AppendLine("    <title>Journal Export</title>");
                html.AppendLine("    <style>");
                html.AppendLine("        :root { --ink: #2A2F33; --muted: #6F7C86; --line: #E7E1D8; --card: #FFFFFF; --bg: #FDFBF7; --accent: #FF69B4; --accent-soft: rgba(255, 105, 180, 0.12); }");
                html.AppendLine("        * { box-sizing: border-box; }");
                html.AppendLine("        body { font-family: 'Segoe UI', Arial, sans-serif; background: var(--bg); color: var(--ink); padding: 48px; }");
                html.AppendLine("        h1 { margin: 0 0 6px 0; font-size: 28px; letter-spacing: -0.02em; }");
                html.AppendLine("        .report-meta { color: var(--muted); font-size: 14px; margin-bottom: 28px; }");
                html.AppendLine("        .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; }");
                html.AppendLine("        .card { background: var(--card); border: 1px solid var(--line); border-radius: 16px; padding: 18px 20px; box-shadow: 0 8px 20px rgba(42, 47, 51, 0.06); display: flex; flex-direction: column; min-height: 220px; }");
                html.AppendLine("        .card-header { display: flex; flex-direction: column; gap: 6px; margin-bottom: 8px; }");
                html.AppendLine("        .date { font-weight: 700; color: var(--accent); font-size: 13px; text-transform: uppercase; letter-spacing: 0.08em; }");
                html.AppendLine("        .title { font-size: 18px; margin: 8px 0 10px 0; }");
                html.AppendLine("        .content { color: var(--ink); line-height: 1.7; font-size: 14px; }");
                html.AppendLine("        .meta { margin-top: 12px; display: flex; gap: 12px; flex-wrap: wrap; color: var(--muted); font-size: 12px; }");
                html.AppendLine("        .pill { background: var(--accent-soft); color: var(--accent); padding: 4px 10px; border-radius: 999px; font-weight: 600; }");
                html.AppendLine("        .tags { display: flex; gap: 8px; flex-wrap: wrap; }");
                html.AppendLine("        .tag { border: 1px dashed var(--line); padding: 4px 8px; border-radius: 8px; font-size: 12px; color: var(--muted); }");
                html.AppendLine("        .footer { margin-top: 28px; color: var(--muted); font-size: 12px; }");
                html.AppendLine("    </style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                html.AppendLine("    <h1>Journal Card Grid Report</h1>");
                html.AppendLine($"    <div class='report-meta'>Range: {startDate:MMM dd, yyyy} – {endDate:MMM dd, yyyy} • Entries: {entries.Count}</div>");
                html.AppendLine("    <div class='grid'>");
                
                foreach (var entry in entries.OrderBy(e => e.EntryDate))
                {
                    // [DATA TRANSFORMATION]: Converting database objects into HTML elements.
                    var safeTitle = EscapeHtml(entry.Title ?? "Untitled");
                    var safeContent = EscapeHtml(entry.Content ?? "").Replace("\n", "<br/>");
                    var tags = string.IsNullOrWhiteSpace(entry.Tags)
                        ? new List<string>()
                        : entry.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(EscapeHtml)
                            .ToList();
                    var tagsHtml = tags.Any()
                        ? $"<div class='tags'>{string.Join("", tags.Select(t => $"<span class='tag'>#{t}</span>"))}</div>"
                        : "";

                    html.AppendLine("        <div class='card'>");
                    html.AppendLine("            <div class='card-header'>");
                    html.AppendLine($"                <div class='date'>{entry.EntryDate:dddd, MMMM dd, yyyy}</div>");
                    html.AppendLine($"                <div class='title'>{safeTitle}</div>");
                    html.AppendLine("            </div>");
                    html.AppendLine($"            <div class='content'>{safeContent}</div>");
                    html.AppendLine("            <div class='meta'>");
                    html.AppendLine($"                    <span class='pill'>Words: {entry.WordCount}</span>");
                    if (!string.IsNullOrWhiteSpace(entry.PrimaryMood))
                    {
                        html.AppendLine($"                    <span class='pill'>Mood: {EscapeHtml(entry.PrimaryMood)}</span>");
                    }
                    html.AppendLine("            </div>");
                    if (!string.IsNullOrEmpty(tagsHtml))
                    {
                        html.AppendLine($"            {tagsHtml}");
                    }
                    html.AppendLine("        </div>");
                }

                html.AppendLine("    </div>");
                html.AppendLine("    <div class='footer'>Generated by JournalApp Export</div>");
                html.AppendLine("</body></html>");

                // [FILE I/O]: Writing the generated string to the local storage.
                await File.WriteAllTextAsync(filePath, html.ToString());

                return $"Successfully exported {entries.Count} entries";
            }
            catch (Exception ex)
            {
                return $"Error creating export: {ex.Message}";
            }
        }

        /// <summary>
        /// [SECURITY]: Prevents XSS (Cross-Site Scripting) by escaping special HTML characters.
        /// </summary>
        private string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
