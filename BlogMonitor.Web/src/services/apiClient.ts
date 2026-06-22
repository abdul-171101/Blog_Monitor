import axios from 'axios';
import type { AxiosInstance } from 'axios';

// Module-level variable to store the access token
let accessToken: string | null = null;

// Create Axios instance
export const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true,
});

// Request interceptor - attach access token to every request
apiClient.interceptors.request.use(
  (config) => {
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor - handle 401 and attempt token refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    // If not a 401 or already a retry, reject
    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    // Don't refresh if this is the refresh or login endpoint
    if (
      originalRequest.url.includes('/api/auth/refresh') ||
      originalRequest.url.includes('/api/auth/login')
    ) {
      // Clear token on failed refresh/login
      accessToken = null;
      window.location.href = '/login';
      return Promise.reject(error);
    }

    try {
      // Mark as retry to prevent infinite loop
      originalRequest._retry = true;

      // Call refresh endpoint
      const refreshResponse = await apiClient.post('/api/auth/refresh');

      // Update access token
      const newAccessToken = refreshResponse.data.accessToken;
      setAccessToken(newAccessToken);

      // Retry original request with new token
      originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
      return apiClient(originalRequest);
    } catch (refreshError) {
      // Refresh failed - clear token and redirect to login
      accessToken = null;
      window.location.href = '/login';
      return Promise.reject(refreshError);
    }
  }
);

// Export functions to manage access token
export const setAccessToken = (token: string | null): void => {
  accessToken = token;
};

export const getAccessToken = (): string | null => {
  return accessToken;
};
