using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using BlogMonitor.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlogMonitor.Infrastructure.Services
{
    public class TfidfSummariserService : ITfidfSummariserService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<TfidfSummariserService> _logger;

        public TfidfSummariserService(IConfiguration config, ILogger<TfidfSummariserService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public string? Summarise(string markdownFilePath)
        {
            try
            {
                var text = File.ReadAllText(markdownFilePath);
                // strip header block lines starting with #, **, or --- at top
                var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                int idx = 0;
                while (idx < lines.Count && (lines[idx].StartsWith("#") || lines[idx].StartsWith("**") || lines[idx].Trim() == "---")) idx++;
                var bodyLines = lines.Skip(idx).ToList();
                var body = string.Join(" ", bodyLines);

                var sentences = Regex.Split(body, "(?<=[.?!])\\s+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                int totalWords = Regex.Split(body, "\\s+").Count(w => !string.IsNullOrWhiteSpace(w));
                int minWords = int.TryParse(_config["Summarizer:MinContentWords"], out var mw) ? mw : 50;
                if (totalWords < minWords) return null;

                var stopWords = new HashSet<string>(new[] { "a","an","the","is","are","was","were","be","been","being","have","has","had","do","does","did","will","would","shall","should","may","might","must","can","could","to","of","in","on","at","for","with","by","from","and","or","but","not","this","that","these","those","it","its","as","if","so","than","then","when","where","which","who","what","how" });

                var sentenceWords = new List<List<string>>();
                foreach (var s in sentences)
                {
                    var tokens = Regex.Split(s.ToLowerInvariant(), "[^a-z0-9]+").Where(t => t.Length >= 3 && !stopWords.Contains(t)).ToList();
                    sentenceWords.Add(tokens);
                }

                int S = sentenceWords.Count;
                var idf = new Dictionary<string, double>(StringComparer.Ordinal);
                var allWords = sentenceWords.SelectMany(w => w).Distinct();
                foreach (var w in allWords)
                {
                    int docsWith = sentenceWords.Count(sw => sw.Contains(w));
                    idf[w] = Math.Log((double)S / (1 + docsWith));
                }

                var scores = new double[S];
                for (int i = 0; i < S; i++)
                {
                    var sw = sentenceWords[i];
                    if (sw.Count == 0) { scores[i] = 0; continue; }
                    var tf = sw.GroupBy(x => x).ToDictionary(g => g.Key, g => (double)g.Count() / sw.Count);
                    double score = 0;
                    foreach (var kv in tf)
                    {
                        if (idf.TryGetValue(kv.Key, out var idfv)) score += kv.Value * idfv;
                    }
                    scores[i] = score;
                }

                var filtered = new List<int>();
                var banned = new[] { "click here", "subscribe", "buy now", "limited offer", "discount", "promo", "sign up", "advertisement", "cookie" };
                for (int i = 0; i < sentences.Count; i++)
                {
                    var low = sentences[i].ToLowerInvariant();
                    if (banned.Any(b => low.Contains(b))) continue;
                    filtered.Add(i);
                }

                var ranked = filtered.OrderByDescending(i => scores[i]).ToList();
                int minS = int.TryParse(_config["Summarizer:MinSentences"], out var mn) ? mn : 3;
                int maxS = int.TryParse(_config["Summarizer:MaxSentences"], out var mx) ? mx : 7;
                int takeN = Math.Clamp(minS, minS, maxS);
                takeN = Math.Min(takeN, ranked.Count);
                var top = ranked.Take(takeN).OrderBy(i => i).Select(i => sentences[i].Trim());
                return string.Join(" ", top);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TF-IDF summariser failed for {Path}", markdownFilePath);
                return null;
            }
        }
    }
}
