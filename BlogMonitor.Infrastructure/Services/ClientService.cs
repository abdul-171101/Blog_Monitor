using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;
using BlogMonitor.Core.Interfaces;
using BlogMonitor.Infrastructure.Data;
using BlogMonitor.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogMonitor.Infrastructure.Services
{
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext _db;

        public ClientService(ApplicationDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public async Task<List<ClientResponse>> GetAllClientsAsync()
        {
            var clients = await _db.Clients
                .Where(c => c.Status != "INACTIVE")
                .Include(c => c.ClientUrls)
                .OrderBy(c => c.FullName)
                .ToListAsync();

            return clients.Select(c => new ClientResponse(
                c.Id,
                c.FullName,
                c.Email,
                c.CompanyName,
                c.Phone,
                c.Status,
                c.CreatedAt,
                c.ClientUrls.Count(u => u.Status == "ACTIVE")
            )).ToList();
        }

        public async Task<ClientResponse?> GetClientByIdAsync(int id)
        {
            var c = await _db.Clients
                .Include(x => x.ClientUrls)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null || c.Status == "INACTIVE") return null;

            return new ClientResponse(
                c.Id,
                c.FullName,
                c.Email,
                c.CompanyName,
                c.Phone,
                c.Status,
                c.CreatedAt,
                c.ClientUrls.Count(u => u.Status == "ACTIVE")
            );
        }

        public async Task<ClientResponse> CreateClientAsync(CreateClientRequest request)
        {
            var exists = await _db.Clients.AnyAsync(c => c.Email == request.Email);
            if (exists) throw new InvalidOperationException("A client with this email already exists.");

            var client = new Client
            {
                FullName = request.FullName,
                Email = request.Email,
                CompanyName = request.CompanyName,
                Phone = request.Phone,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            _db.Clients.Add(client);
            await _db.SaveChangesAsync();

            return new ClientResponse(client.Id, client.FullName, client.Email, client.CompanyName, client.Phone, client.Status, client.CreatedAt, 0);
        }

        public async Task<ClientResponse?> UpdateClientAsync(int id, UpdateClientRequest request)
        {
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id);
            if (client == null) return null;

            client.FullName = request.FullName;
            client.Email = request.Email;
            client.CompanyName = request.CompanyName;
            client.Phone = request.Phone;
            client.Status = request.Status;
            client.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var count = await _db.ClientUrls.CountAsync(u => u.ClientId == client.Id && u.Status == "ACTIVE");

            return new ClientResponse(client.Id, client.FullName, client.Email, client.CompanyName, client.Phone, client.Status, client.CreatedAt, count);
        }

        public async Task<bool> SoftDeleteClientAsync(int id)
        {
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == id);
            if (client == null) return false;

            client.Status = "INACTIVE";
            client.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return true;
        }
    }
}
