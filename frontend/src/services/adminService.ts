import apiClient from './api';

// User DTOs
export interface UserResponseDto {
  userId: number;
  email: string;
  firstName?: string;
  lastName?: string;
  isActive: boolean;
  createdDate: string;
  roles: RoleDto[];
  workstreamAccess: WorkstreamAccessDto[];
}

export interface CreateUserRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  roleIds: number[];
}

export interface UpdateUserRequest {
  firstName?: string;
  lastName?: string;
  isActive?: boolean;
  roleIds?: number[];
}

export interface ResetPasswordRequest {
  newPassword: string;
}

// Role DTOs
export interface RoleResponseDto {
  roleId: number;
  roleName: string;
  roleTypeId: number;
  roleTypeName: string;
  createdDate: string;
}

export interface RoleTypeDto {
  roleTypeId: number;
  roleTypeName: string;
}

export interface CreateRoleRequest {
  roleName: string;
  roleTypeId: number;
}

export interface UpdateRoleRequest {
  roleName?: string;
  roleTypeId?: number;
}

// Workstream DTOs
export interface WorkstreamResponseDto {
  workstreamId: number;
  workstreamName: string;
  description?: string;
  isActive: boolean;
  createdDate: string;
}

export interface CreateWorkstreamRequest {
  workstreamName: string;
  description?: string;
}

export interface UpdateWorkstreamRequest {
  workstreamName?: string;
  description?: string;
  isActive?: boolean;
}

// Permission Type DTOs
export interface PermissionTypeDto {
  permissionTypeId: number;
  permissionTypeName: string;
  description?: string;
}

// Workstream User Assignment
export interface AssignWorkstreamUserRequest {
  userId: number;
  permissionTypeId: number;
}

export interface RoleDto {
  roleId: number;
  roleName: string;
  roleTypeName: string;
}

export interface WorkstreamAccessDto {
  workstreamId: number;
  workstreamName: string;
  permissionTypeId: number;
  permissionTypeName: string;
}

// Tenant DTOs
export interface CreateTenantRequest {
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
}

export interface UpdateTenantRequest {
  firstName?: string;
  lastName?: string;
  email?: string;
  phone?: string;
}

export interface TenantResponseDto {
  tenantId: number;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  createdDate: string;
}

// Tenancy DTOs
export interface CreateTenancyRequest {
  propertyId: number;
  tenantId: number;
  startDate: string;
  endDate?: string;
  monthlyRent?: number;
}

export interface UpdateTenancyRequest {
  propertyId?: number;
  tenantId?: number;
  startDate?: string;
  endDate?: string;
  monthlyRent?: number;
}

export interface TenancyResponseDto {
  tenancyId: number;
  propertyId: number;
  propertyName: string;
  tenantId: number;
  tenantName: string;
  startDate: string;
  endDate?: string;
  monthlyRent?: number;
  createdDate: string;
}

export const adminService = {
  // Users
  getUsers: async (): Promise<UserResponseDto[]> => {
    return await apiClient<UserResponseDto[]>('/users');
  },

  getUser: async (id: number): Promise<UserResponseDto> => {
    return await apiClient<UserResponseDto>(`/users/${id}`);
  },

  createUser: async (request: CreateUserRequest): Promise<UserResponseDto> => {
    return await apiClient<UserResponseDto>('/users', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateUser: async (id: number, request: UpdateUserRequest): Promise<UserResponseDto> => {
    return await apiClient<UserResponseDto>(`/users/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  deleteUser: async (id: number): Promise<void> => {
    await apiClient<void>(`/users/${id}`, {
      method: 'DELETE',
    });
  },

  resetPassword: async (id: number, request: ResetPasswordRequest): Promise<void> => {
    await apiClient<void>(`/users/${id}/reset-password`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  // Roles
  getRoles: async (): Promise<RoleResponseDto[]> => {
    return await apiClient<RoleResponseDto[]>('/roles');
  },

  getRoleTypes: async (): Promise<RoleTypeDto[]> => {
    return await apiClient<RoleTypeDto[]>('/roles/types');
  },

  getRole: async (id: number): Promise<RoleResponseDto> => {
    return await apiClient<RoleResponseDto>(`/roles/${id}`);
  },

  createRole: async (request: CreateRoleRequest): Promise<RoleResponseDto> => {
    return await apiClient<RoleResponseDto>('/roles', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateRole: async (id: number, request: UpdateRoleRequest): Promise<RoleResponseDto> => {
    return await apiClient<RoleResponseDto>(`/roles/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  // Workstreams
  getWorkstreams: async (): Promise<WorkstreamResponseDto[]> => {
    return await apiClient<WorkstreamResponseDto[]>('/workstreams');
  },

  getWorkstream: async (id: number): Promise<WorkstreamResponseDto> => {
    return await apiClient<WorkstreamResponseDto>(`/workstreams/${id}`);
  },

  createWorkstream: async (request: CreateWorkstreamRequest): Promise<WorkstreamResponseDto> => {
    return await apiClient<WorkstreamResponseDto>('/workstreams', {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  updateWorkstream: async (id: number, request: UpdateWorkstreamRequest): Promise<WorkstreamResponseDto> => {
    return await apiClient<WorkstreamResponseDto>(`/workstreams/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    });
  },

  getPermissionTypes: async (): Promise<PermissionTypeDto[]> => {
    return await apiClient<PermissionTypeDto[]>('/workstreams/permission-types');
  },

  // Workstream Users
  getWorkstreamUsers: async (workstreamId: number): Promise<UserResponseDto[]> => {
    return await apiClient<UserResponseDto[]>(`/workstreams/${workstreamId}/users`);
  },

  assignUserToWorkstream: async (workstreamId: number, request: AssignWorkstreamUserRequest): Promise<void> => {
    await apiClient<void>(`/workstreams/${workstreamId}/users`, {
      method: 'POST',
      body: JSON.stringify(request),
    });
  },

  removeUserFromWorkstream: async (workstreamId: number, userId: number): Promise<void> => {
    await apiClient<void>(`/workstreams/${workstreamId}/users/${userId}`, {
      method: 'DELETE',
    });
  },
};
