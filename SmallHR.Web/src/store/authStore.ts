import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { User, RolePermission } from '../types/api';
import axios from 'axios';
import { authAPI } from '../services/api';

interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  permissions: RolePermission[];
  permissionsLoaded: boolean;
  login: (user: User) => Promise<void>;
  logout: () => Promise<void>;
  updateUser: (user: User) => void;
  setPermissions: (permissions: RolePermission[]) => void;
  fetchPermissions: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      isAuthenticated: false,
      permissions: [],
      permissionsLoaded: false,
      
      login: async (user) => {
        // Tokens are stored in httpOnly cookies by backend
        // We only store user info in the auth state
        set({
          user,
          isAuthenticated: true,
        });
        
        // Fetch permissions after login
        await get().fetchPermissions();
      },
      
      logout: async () => {
        // Call logout endpoint to clear httpOnly cookies
        try {
          await authAPI.logout();
        } catch (error) {
          console.error('Logout error:', error);
        }
        
        set({
          user: null,
          isAuthenticated: false,
          permissions: [],
          permissionsLoaded: false,
        });
      },
      
      updateUser: (user) => {
        set({ user });
      },
      
      setPermissions: (permissions) => {
        set({ permissions, permissionsLoaded: true });
      },
      
      fetchPermissions: async () => {
        const { user } = get();
        
        if (!user?.roles?.[0]) {
          console.warn('[Session] Cannot fetch permissions: no role');
          set({ permissionsLoaded: true });
          return;
        }

        try {
          console.log('[Session] Fetching permissions for user role:', user.roles[0]);
          // Token is sent via httpOnly cookie automatically
          const response = await axios.get(
            'http://localhost:5192/api/RolePermissions/my-permissions',
            { withCredentials: true }
          );
          
          console.log(`[Session] Loaded ${response.data.length} permissions`, response.data);
          set({ 
            permissions: response.data, 
            permissionsLoaded: true 
          });
        } catch (error) {
          console.error('[Session] Failed to fetch permissions:', error);
          // Set loaded to true even on error to prevent infinite loading
          set({ 
            permissions: [], 
            permissionsLoaded: true 
          });
        }
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
        permissions: state.permissions,
        permissionsLoaded: state.permissionsLoaded,
      }),
    }
  )
);

