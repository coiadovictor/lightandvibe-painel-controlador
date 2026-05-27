import axios from 'axios';
import { getStoredToken } from '../contexts/AuthContext';

const baseURL = import.meta.env.VITE_API_BASE_URL ?? '/api';

export const api = axios.create({
  baseURL,
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use(config => {
  const token = getStoredToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('painel_token');
      localStorage.removeItem('painel_user');
      window.location.href = '/login';
    }
    return Promise.reject(err);
  },
);
