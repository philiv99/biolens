import { createContext, useContext, useEffect, useState, useCallback, type ReactNode } from 'react';
import type { User } from '../types';
import {
  login as apiLogin,
  register as apiRegister,
  validateSession,
  getStoredUserId,
  clearAuthStorage,
} from './authApi';

interface AuthContextValue {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (username: string, password: string) => Promise<void>;
  register: (fields: {
    username: string;
    email: string;
    displayName: string;
    password: string;
    confirmPassword: string;
  }) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // On mount, validate stored session
  useEffect(() => {
    const init = async () => {
      const storedUserId = getStoredUserId();
      if (storedUserId) {
        const validUser = await validateSession(storedUserId);
        if (validUser) {
          setUser(validUser);
        } else {
          clearAuthStorage();
        }
      }
      setIsLoading(false);
    };
    init();
  }, []);

  const login = useCallback(async (username: string, password: string) => {
    const response = await apiLogin(username, password);
    if (response.success && response.user) {
      setUser(response.user);
    } else {
      throw new Error(response.message || 'Login failed');
    }
  }, []);

  const register = useCallback(
    async (fields: {
      username: string;
      email: string;
      displayName: string;
      password: string;
      confirmPassword: string;
    }) => {
      const response = await apiRegister(fields);
      if (response.success && response.user) {
        setUser(response.user);
      } else {
        throw new Error(response.message || 'Registration failed');
      }
    },
    [],
  );

  const logout = useCallback(() => {
    clearAuthStorage();
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within <AuthProvider>');
  return ctx;
}
