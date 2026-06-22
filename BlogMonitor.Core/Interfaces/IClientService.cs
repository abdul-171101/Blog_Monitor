using System.Collections.Generic;
using System.Threading.Tasks;
using BlogMonitor.Core.DTOs;

namespace BlogMonitor.Core.Interfaces
{
    public interface IClientService
    {
        Task<List<ClientResponse>> GetAllClientsAsync();
        Task<ClientResponse?> GetClientByIdAsync(int id);
        Task<ClientResponse> CreateClientAsync(CreateClientRequest request);
        Task<ClientResponse?> UpdateClientAsync(int id, UpdateClientRequest request);
        Task<bool> SoftDeleteClientAsync(int id);
    }
}
