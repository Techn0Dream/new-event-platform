import api, { setAccessToken } from './api';

export const AuthService = {
    async login(username, password) {
        try {
            const response = await api.post('/Auth/login', { username, password });
            
            if (!response.data.success) {
                throw new Error(response.data.message || 'Login failed');
            }

            const { accessToken, user, team } = response.data.data;
            
            // Store token in memory
            setAccessToken(accessToken);

            return { user, team };
        } catch (error) {
            throw new Error(error.response?.data?.message || error.message || 'Login failed');
        }
    },

    async logout() {
        try {
            await api.post('/Auth/logout');
        } catch (e) {
            console.error('Logout request failed', e);
        } finally {
            setAccessToken(null);
        }
    },

    // In a real app with HttpOnly cookies, we can't get the session from localStorage.
    // Instead, we hit a /me endpoint on mount to retrieve the profile if the cookie is valid.
    // However, since we haven't built a /me endpoint that returns team state easily without a token,
    // we will rely on the interceptor's auto-refresh. For the initial load, we'll try to refresh.
    async restoreSession() {
        try {
            const response = await api.post('/Auth/refresh');
            if (response.data.success && response.data.data) {
                const { accessToken, user, team } = response.data.data;
                setAccessToken(accessToken);
                return { user, team };
            }
        } catch (error) {
            // No valid session
            return null;
        }
        return null;
    }
};
