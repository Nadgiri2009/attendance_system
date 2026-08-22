"use client";

import { createContext, useContext, useEffect, useState, ReactNode } from "react";
import { useRouter } from "next/navigation";
import { api, storeAuthTokens, clearAuthTokens } from "./api";
import { locationTracker } from "./locationTracker";
import { AuthResult } from "./types";

interface AuthUser {
  userId: string;
  userName: string;
  email: string;
  employeeId?: string | null;
  roles: string[];
}

interface AuthContextValue {
  user: AuthUser | null;
  isLoading: boolean;
  login: (userNameOrEmail: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function getEmployeeIdFromAccessToken(): string | null {
  const token = window.localStorage.getItem("ewms_access_token");
  if (!token) return null;

  try {
    const payload = JSON.parse(atob(token.split(".")[1])) as { employeeId?: string };
    return payload.employeeId ?? null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    try {
      if (typeof window === "undefined") {
        setIsLoading(false);
        return;
      }
      const stored = window.localStorage.getItem("ewms_user");
      if (!stored) {
        setUser(null);
        return;
      }

      const parsed = JSON.parse(stored) as Partial<AuthUser>;
      const employeeId = parsed.employeeId ?? getEmployeeIdFromAccessToken();
      if (parsed?.userId && parsed?.userName) {
        const hydratedUser = { ...parsed, employeeId } as AuthUser;
        setUser(hydratedUser);
        window.localStorage.setItem("ewms_user", JSON.stringify(hydratedUser));
      } else {
        window.localStorage.removeItem("ewms_user");
        setUser(null);
      }
    } catch {
      if (typeof window !== "undefined") {
        window.localStorage.removeItem("ewms_user");
      }
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  async function login(userNameOrEmail: string, password: string) {
    const { data } = await api.post<{ success: boolean; data: AuthResult; message?: string }>(
      "/auth/login",
      { userNameOrEmail, password }
    );

    const result = data.data;
    storeAuthTokens(result.accessToken, result.refreshToken);

    const authUser: AuthUser = {
      userId: result.userId,
      userName: result.userName,
      email: result.email,
      employeeId: result.employeeId,
      roles: result.roles
    };
    if (typeof window !== "undefined") {
      window.localStorage.setItem("ewms_user", JSON.stringify(authUser));
    }
    setUser(authUser);
    router.push("/dashboard");
  }

  async function logout() {
    await locationTracker.stop();
    clearAuthTokens();
    setUser(null);
    router.push("/login");
  }

  return (
    <AuthContext.Provider value={{ user, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within AuthProvider");
  return context;
}
