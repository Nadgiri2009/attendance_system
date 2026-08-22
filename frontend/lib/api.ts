import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";

export function getApiBaseUrl() {
  if (typeof window !== "undefined" && ["192.0.0.2", "192.0.0.4"].includes(window.location.hostname)) {
    return `http://${window.location.hostname}:5000/api/v1`;
  }
  return process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://192.168.31.152:5000/api/v1";
}

const API_BASE_URL = getApiBaseUrl();

console.log("[API] Initialized with base URL:", API_BASE_URL);

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: { "Content-Type": "application/json" },
  timeout: 30000 // 30s timeout for requests
});

// BUG FIX: every form's catch block was reading err.response.data.errors[0],
// which only matches our own hand-rolled { success:false, errors:[...] }
// shape. When a request fails ASP.NET Core's *automatic* [ApiController]
// model validation (e.g. a bad enum/GUID before our code ever runs), the
// response is instead a standard ValidationProblemDetails object:
//   { errors: { FieldName: ["message", ...], ... }, title, status, ... }
// `errors` there is a dictionary, not an array, so `errors[0]` was always
// undefined and every form silently fell back to a generic "failed" message
// — which is exactly why Employee/Attendance creation looked like it
// "did nothing" instead of showing the real validation reason.
// This helper understands both shapes so forms show the actual message.
export function getErrorMessage(err: unknown, fallback = "Something went wrong. Please try again."): string {
  if (axios.isAxiosError(err)) {
    // Handle network errors (no response from server)
    if (!err.response) {
      console.error("[API] Network error details:", {
        message: err.message,
        code: err.code,
        isNetworkError: !err.response
      });
      
      if (err.code === 'ECONNABORTED' || err.message?.includes('timeout')) {
        return "Request timeout. Please check your network connection.";
      }
      if (err.code === 'ERR_NETWORK' || err.message?.includes('Network Error')) {
        return "Network error. Please check your internet connection and ensure the server is running at: " + (process.env.NEXT_PUBLIC_API_BASE_URL || "http://192.168.31.152:5000/api/v1");
      }
      return fallback;
    }

    const data = err.response?.data as
      | { success?: boolean; message?: string; errors?: string[] | Record<string, string[]> }
      | undefined;

    if (!data) return fallback;

    if (typeof data.message === "string" && data.message !== "Validation failed.") {
      return data.message;
    }

    if (Array.isArray(data.errors) && data.errors.length > 0) {
      return data.errors[0];
    }

    if (data.errors && typeof data.errors === "object") {
      const firstField = Object.values(data.errors)[0];
      if (Array.isArray(firstField) && firstField.length > 0) return firstField[0];
      if (typeof firstField === "string") return firstField;
    }

    const extra = data as Record<string, unknown>;
    if (typeof extra.title === "string") return extra.title;
    if (typeof extra.detail === "string") return extra.detail;
    if (typeof data.message === "string") return data.message;
  }

  if (err instanceof Error && err.message) return err.message;

  return fallback;
}

function getStoredToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem("ewms_access_token");
}

function getStoredRefreshToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem("ewms_refresh_token");
}

export function storeAuthTokens(accessToken: string, refreshToken: string) {
  window.localStorage.setItem("ewms_access_token", accessToken);
  window.localStorage.setItem("ewms_refresh_token", refreshToken);
}

export function clearAuthTokens() {
  window.localStorage.removeItem("ewms_access_token");
  window.localStorage.removeItem("ewms_refresh_token");
  window.localStorage.removeItem("ewms_user");
}

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getStoredToken();
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }

  if (typeof FormData !== "undefined" && config.data instanceof FormData) {
    delete config.headers?.["Content-Type"];
  }
  
  // Log outgoing requests (especially for tracking)
  if (config.url?.includes('tracking')) {
    console.log("[API Request]", config.method?.toUpperCase(), config.url, {
      hasAuth: !!token,
      baseURL: config.baseURL
    });
  }
  
  return config;
});

let isRefreshing = false;
let pendingQueue: Array<() => void> = [];

api.interceptors.response.use(
  (response) => {
    // Log successful tracking responses only
    if (response.config.url?.includes('tracking')) {
      console.log("[API Response]", response.status, response.config.url);
    }
    return response;
  },
  async (error: AxiosError) => {
    const originalRequest = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;
    
    // Only log errors for non-404 and non-401 cases to reduce noise
    // Don't log 404s (endpoint not found) or 401s (auth refresh) as they're handled
    if (error.response?.status !== 404 && error.response?.status !== 401) {
      const errorDetails = `${error.message}${error.response?.status ? ` (${error.response.status})` : ''}`;
      console.warn(`[API Error] ${originalRequest?.method?.toUpperCase()} ${originalRequest?.url}: ${errorDetails}`);
    }

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      const refreshToken = getStoredRefreshToken();
      if (!refreshToken) {
        clearAuthTokens();
        if (typeof window !== "undefined") window.location.href = "/login";
        return Promise.reject(error);
      }

      originalRequest._retry = true;

      if (isRefreshing) {
        return new Promise((resolve) => {
          pendingQueue.push(() => resolve(api(originalRequest)));
        });
      }

      isRefreshing = true;
      try {
        const { data } = await axios.post(`${API_BASE_URL}/auth/refresh-token`, { refreshToken });
        storeAuthTokens(data.data.accessToken, data.data.refreshToken);
        pendingQueue.forEach((cb) => cb());
        pendingQueue = [];
        return api(originalRequest);
      } catch (refreshError) {
        clearAuthTokens();
        if (typeof window !== "undefined") window.location.href = "/login";
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
