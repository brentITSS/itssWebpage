import apiClient from './api';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: UserDto;
}

export interface UserDto {
  userId: number;
  email: string;
  firstName?: string;
  lastName?: string;
  isActive: boolean;
  roles: string[];
  workstreamAccess: WorkstreamAccessDto[];
  isGlobalAdmin: boolean;
}

export interface WorkstreamAccessDto {
  workstreamId: number;
  workstreamName: string;
  permissionTypeId: number;
  permissionTypeName: string;
}

export const authService = {
  login: async (request: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(request),
    });
    
    // Store token
    localStorage.setItem('token', response.token);
    
    return response;
  },

  getCurrentUser: async (): Promise<UserDto> => {
    return await apiClient<UserDto>('/auth/me');
  },

  logout: (): void => {
    localStorage.removeItem('token');
    window.location.href = '/Login';
  },

  isAuthenticated: (): boolean => {
    return !!localStorage.getItem('token');
  },
};
