import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { authService, type UserDto } from '../services/authService';
import {
  hasPropertyHubWorkstreamAccess,
  isPropertyHubWorkstreamAdmin,
  isPropertyHubWorkstreamUserOnly,
} from '../utils/access';

type AuthAccessContextValue = {
  user: UserDto | null;
  loading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
  hasPropertyHubAccess: boolean;
  isPropertyHubAdmin: boolean;
  isPropertyHubUserOnly: boolean;
  isGlobalAdmin: boolean;
};

const AuthAccessContext = createContext<AuthAccessContextValue | null>(null);

export const AuthAccessProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    const token = localStorage.getItem('token');
    if (!token) {
      setUser(null);
      setLoading(false);
      setError(null);
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const u = await authService.getCurrentUser();
      setUser(u);
    } catch (e: unknown) {
      setUser(null);
      setError(e instanceof Error ? e.message : 'Failed to load user');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    const onAuthChanged = () => {
      void load();
    };
    window.addEventListener('itss-auth-changed', onAuthChanged);
    return () => window.removeEventListener('itss-auth-changed', onAuthChanged);
  }, [load]);

  const value = useMemo<AuthAccessContextValue>(() => {
    const hasPropertyHubAccess = hasPropertyHubWorkstreamAccess(user);
    const isPropertyHubAdmin = isPropertyHubWorkstreamAdmin(user);
    const isPropertyHubUserOnlyFlag = isPropertyHubWorkstreamUserOnly(user);
    return {
      user,
      loading,
      error,
      refresh: load,
      hasPropertyHubAccess,
      isPropertyHubAdmin,
      isPropertyHubUserOnly: isPropertyHubUserOnlyFlag,
      isGlobalAdmin: Boolean(user?.isGlobalAdmin),
    };
  }, [user, loading, error, load]);

  return <AuthAccessContext.Provider value={value}>{children}</AuthAccessContext.Provider>;
};

export function useAuthAccess(): AuthAccessContextValue {
  const ctx = useContext(AuthAccessContext);
  if (!ctx) {
    throw new Error('useAuthAccess must be used within AuthAccessProvider');
  }
  return ctx;
}
