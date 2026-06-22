import { apiClient } from './apiClient';
import type { LoginRequest, LoginResponse, RegisterRequest } from '../types';

export const authService = {
  login: async (data: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/api/auth/login', data);
    return response.data;
  },

  register: async (data: RegisterRequest): Promise<void> => {
    await apiClient.post('/api/auth/register', data);
  },

  logout: async (): Promise<void> => {
    try {
      await apiClient.post('/api/auth/logout');
    } catch {
      // Ignore logout errors, just clear local state
    }
  },
};
