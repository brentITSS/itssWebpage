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
  defaultLoginLandingPage?: string;
  roles: string[];
  workstreamAccess: WorkstreamAccessDto[];
  propertyGroupAccess: PropertyGroupAccessDto[];
  isGlobalAdmin: boolean;
  mustChangePassword: boolean;
}

export interface WorkstreamAccessDto {
  workstreamId: number;
  workstreamName: string;
  permissionTypeId: number;
  permissionTypeName: string;
}

export interface PropertyGroupAccessDto {
  propertyGroupId: number;
  propertyGroupName: string;
}

export interface ForgotPasswordResponse {
  message: string;
  resetToken?: string | null;
  resetPath?: string | null;
}

export interface ChangePasswordRequest {
  newPassword: string;
}

export const authService = {
  login: async (request: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify(request),
    });
    
    // Store token
    localStorage.setItem('token', response.token);
    localStorage.setItem('mustChangePassword', response.user.mustChangePassword ? 'true' : 'false');
    
    return response;
  },

  getCurrentUser: async (): Promise<UserDto> => {
    return await apiClient<UserDto>('/auth/me');
  },

  logout: (): void => {
    localStorage.removeItem('token');
    localStorage.removeItem('mustChangePassword');
    window.location.href = '/Login';
  },

  isAuthenticated: (): boolean => {
    return !!localStorage.getItem('token');
  },

  forgotPassword: async (email: string): Promise<ForgotPasswordResponse> => {
    return await apiClient<ForgotPasswordResponse>('/auth/forgot-password', {
      method: 'POST',
      body: JSON.stringify({ email }),
    });
  },

  completePasswordReset: async (token: string, newPassword: string): Promise<void> => {
    await apiClient<void>('/auth/complete-password-reset', {
      method: 'POST',
      body: JSON.stringify({ token, newPassword }),
    });
  },

  changePassword: async (request: ChangePasswordRequest): Promise<void> => {
    await apiClient<void>('/auth/change-password', {
      method: 'POST',
      body: JSON.stringify(request),
    });
    localStorage.setItem('mustChangePassword', 'false');
  },
};
