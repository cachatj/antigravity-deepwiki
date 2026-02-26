"use client";

import React, { createContext, useContext } from "react";
import {
  UserInfo,
  LoginRequest,
  RegisterRequest,
} from "@/lib/auth-api";

interface AuthContextType {
  user: UserInfo | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (request: LoginRequest) => Promise<void>;
  register: (request: RegisterRequest) => Promise<void>;
  logout: () => void;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

// AUTH BYPASS: Always authenticated as local admin.
// No external auth servers are contacted.
// To re-enable auth (e.g. for SSO), restore the original AuthProvider
// from git history and configure the SSO provider.
const LOCAL_USER: UserInfo = {
  id: "local-admin",
  email: "admin@localhost",
  name: "Local Admin",
  roles: ["Admin"],
};

export function AuthProvider({ children }: { children: React.ReactNode }) {
  return (
    <AuthContext.Provider
      value={{
        user: LOCAL_USER,
        isLoading: false,
        isAuthenticated: true,
        login: async () => {},
        register: async () => {},
        logout: () => {},
        refreshUser: async () => {},
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
