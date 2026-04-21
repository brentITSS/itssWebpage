// API base URL - configured via REACT_APP_API_URL environment variable.
const configuredApiUrl = (process.env.REACT_APP_API_URL || '').trim();
const API_BASE_URL =
  configuredApiUrl || 'https://itsson-api.azurewebsites.net/api';

// Get JWT token from localStorage
const getToken = (): string | null => {
  return localStorage.getItem('token');
};

// API client helper
const apiClient = async <T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> => {
  const token = getToken();
  const isFormData = options.body instanceof FormData;
  
  const headers: Record<string, string> = {
    ...(options.headers as Record<string, string> || {}),
  };

  if (!isFormData && !headers['Content-Type']) {
    headers['Content-Type'] = 'application/json';
  }

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers,
  });

  if (!response.ok) {
    if (response.status === 401) {
      // Unauthorized - clear token and redirect to login
      localStorage.removeItem('token');
      window.location.href = '/Login';
    }
    const error = await response.json().catch(() => ({ message: response.statusText }));
    throw new Error(error.message || `HTTP error! status: ${response.status}`);
  }

  // Handle empty responses
  const text = await response.text();
  if (!text) {
    return {} as T;
  }

  return JSON.parse(text) as T;
};

export default apiClient;
