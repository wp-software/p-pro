import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface User {
  id: string;
  clientId?: string;
  email: string;
  name: string;
  avatarUrl?: string;
  companyId: string;
  companyName: string;
  roles: string[];
  language: string;
}

interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  
  // Actions
  setAuth: (user: User, accessToken: string, refreshToken: string) => void;
  logout: () => void;
  updateUser: (user: Partial<User>) => void;
  setLoading: (loading: boolean) => void;
}

type PersistedAuthState = Pick<
  AuthState,
  'user' | 'accessToken' | 'refreshToken' | 'isAuthenticated'
>;

const REMEMBER_KEY = 'pm-remember-me';

export const setRememberMe = (remember: boolean) => {
  if (remember) localStorage.setItem(REMEMBER_KEY, 'true');
  else localStorage.removeItem(REMEMBER_KEY);
};

export const useAuthStore = create<AuthState>()(
  persist<AuthState, [], [], PersistedAuthState>(
    (set) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      isLoading: false,

      setAuth: (user, accessToken, refreshToken) =>
        set({
          user,
          accessToken,
          refreshToken,
          isAuthenticated: true,
          isLoading: false,
        }),

      logout: () =>
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false,
          isLoading: false,
        }),

      updateUser: (userData) =>
        set((state) => ({
          user: state.user ? { ...state.user, ...userData } : null,
        })),

      setLoading: (loading) => set({ isLoading: loading }),
    }),
    {
      name: 'pm-auth-storage',
      storage: {
        getItem: (key) => {
          const value = localStorage.getItem(key) ?? sessionStorage.getItem(key);
          return value ? JSON.parse(value) : null;
        },
        setItem: (key, value) => {
          const remember = localStorage.getItem(REMEMBER_KEY) === 'true';
          const target = remember ? localStorage : sessionStorage;
          const other = remember ? sessionStorage : localStorage;
          other.removeItem(key);
          target.setItem(key, JSON.stringify(value));
        },
        removeItem: (key) => {
          localStorage.removeItem(key);
          sessionStorage.removeItem(key);
        },
      },
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);
