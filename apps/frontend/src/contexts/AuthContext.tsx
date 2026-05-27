import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';

interface AuthUser {
  username: string;
  token: string;
  expiresAt: string;
}

interface AuthContextValue {
  user: AuthUser | null;
  login: (username: string, token: string, expiresAt: string) => void;
  logout: () => void;
  isAuthenticated: boolean;
}

const TOKEN_KEY = 'painel_token';
const USER_KEY = 'painel_user';

function loadStoredUser(): AuthUser | null {
  try {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    const user: AuthUser = JSON.parse(raw);
    if (new Date(user.expiresAt) <= new Date()) {
      localStorage.removeItem(USER_KEY);
      localStorage.removeItem(TOKEN_KEY);
      return null;
    }
    return user;
  } catch {
    return null;
  }
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(loadStoredUser);

  const login = useCallback((username: string, token: string, expiresAt: string) => {
    const authUser: AuthUser = { username, token, expiresAt };
    localStorage.setItem(USER_KEY, JSON.stringify(authUser));
    localStorage.setItem(TOKEN_KEY, token);
    setUser(authUser);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(TOKEN_KEY);
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, login, logout, isAuthenticated: user !== null }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}

export function getStoredToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}
