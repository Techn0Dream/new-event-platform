import axios from 'axios';

const api = axios.create({
    baseURL: 'http://localhost:5283/api/v1',
    withCredentials: true // Extremely important for sending/receiving HttpOnly cookies
});

// Interceptor to attach Access Token from memory if it exists
let accessToken = null;

export const setAccessToken = (token) => {
    accessToken = token;
};

api.interceptors.request.use(
    (config) => {
        if (accessToken) {
            config.headers.Authorization = `Bearer ${accessToken}`;
        }
        return config;
    },
    (error) => Promise.reject(error)
);

// Interceptor to handle 401s and automatically try refreshing the token
api.interceptors.response.use(
    (response) => response,
    async (error) => {
        const originalRequest = error.config;

        // If error is 401 and we haven't retried yet
        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;

            try {
                // Call refresh endpoint. The HttpOnly cookie will be sent automatically
                const { data } = await axios.post('http://localhost:5283/api/v1/Auth/refresh', {}, { withCredentials: true });
                
                if (data.success && data.data?.accessToken) {
                    setAccessToken(data.data.accessToken);
                    originalRequest.headers.Authorization = `Bearer ${accessToken}`;
                    return api(originalRequest);
                }
            } catch (refreshError) {
                // Refresh failed (e.g. cookie expired)
                setAccessToken(null);
                // Optionally redirect to login or clear state
                window.dispatchEvent(new CustomEvent('unauthorized'));
                return Promise.reject(refreshError);
            }
        }
        return Promise.reject(error);
    }
);

export default api;
