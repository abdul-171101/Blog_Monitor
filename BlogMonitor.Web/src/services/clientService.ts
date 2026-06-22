import { apiClient } from './apiClient';
import type { ClientResponse, CreateClientRequest, UpdateClientRequest } from '../types';

export const clientService = {
  getAll: async (): Promise<ClientResponse[]> => {
    const response = await apiClient.get<ClientResponse[]>('/api/clients');
    return response.data;
  },

  getById: async (id: number): Promise<ClientResponse> => {
    const response = await apiClient.get<ClientResponse>(`/api/clients/${id}`);
    return response.data;
  },

  create: async (data: CreateClientRequest): Promise<ClientResponse> => {
    const response = await apiClient.post<ClientResponse>('/api/clients', data);
    return response.data;
  },

  update: async (id: number, data: UpdateClientRequest): Promise<ClientResponse> => {
    const response = await apiClient.put<ClientResponse>(`/api/clients/${id}`, data);
    return response.data;
  },

  softDelete: async (id: number): Promise<void> => {
    await apiClient.delete(`/api/clients/${id}`);
  },
};
