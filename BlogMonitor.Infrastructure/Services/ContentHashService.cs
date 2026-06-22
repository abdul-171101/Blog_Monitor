using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using BlogMonitor.Infrastructure.Data;
using BlogMonitor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

using System;

namespace BlogMonitor.Infrastructure.Services
{
    public class ContentHashService : IContentHashService
    {
        private readonly ApplicationDbContext _db;

        public ContentHashService(ApplicationDbContext db)
        {
            _db = db;
        }

        public string ComputeHash(string cleanText)
        {
            var bytes = Encoding.UTF8.GetBytes(cleanText ?? string.Empty);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public async Task<HashComparisonResult> CompareAndGetResultAsync(int clientUrlId, string pageUrl, string newHash)
        {
            var rec = await _db.ContentHashes.FirstOrDefaultAsync(c => c.ClientUrlId == clientUrlId && c.PageUrl == pageUrl);
            if (rec == null)
            {
                return new HashComparisonResult { PageUrl = pageUrl, StoredHash = null, NewHash = newHash, IsChanged = true };
            }
            var equal = string.Equals(rec.ContentHashValue, newHash, StringComparison.OrdinalIgnoreCase);
            return new HashComparisonResult { PageUrl = pageUrl, StoredHash = rec.ContentHashValue, NewHash = newHash, IsChanged = !equal };
        }

        public async Task UpsertHashAsync(int clientUrlId, string pageUrl, string newHash)
        {
            var rec = await _db.ContentHashes.FirstOrDefaultAsync(c => c.ClientUrlId == clientUrlId && c.PageUrl == pageUrl);
            if (rec != null)
            {
                rec.ContentHashValue = newHash;
                rec.LastHashDate = DateOnly.FromDateTime(DateTime.UtcNow);
            }
            else
            {
                rec = new ContentHash
                {
                    ClientUrlId = clientUrlId,
                    PageUrl = pageUrl,
                    ContentHashValue = newHash,
                    LastHashDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };
                _db.ContentHashes.Add(rec);
            }
            await _db.SaveChangesAsync();
        }
    }
}
