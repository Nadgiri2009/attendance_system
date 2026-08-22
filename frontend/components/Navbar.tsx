"use client";

import { useAuth } from "@/lib/auth-context";

export default function Navbar() {
  const { user, logout } = useAuth();

  return (
    <header className="flex min-h-16 flex-wrap items-center justify-between gap-3 border-b border-border bg-white px-4 pl-16 sm:px-6 lg:pl-6">
      <div className="text-sm text-slate-500">
        {new Date().toLocaleDateString(undefined, { weekday: "long", year: "numeric", month: "long", day: "numeric" })}
      </div>
      <div className="flex items-center gap-4">
        <div className="text-right">
          <div className="text-sm font-medium text-ink">{user?.userName}</div>
          <div className="text-xs text-slate-400">{user?.roles.join(", ")}</div>
        </div>
        <div className="h-9 w-9 rounded-full bg-primary-600 text-white flex items-center justify-center text-sm font-medium">
          {user?.userName?.slice(0, 2).toUpperCase()}
        </div>
        <button
          onClick={logout}
          className="hidden rounded px-2 py-1 text-sm text-slate-500 transition-colors hover:text-danger focus-ring lg:inline-flex"
        >
          Sign out
        </button>
      </div>
    </header>
  );
}
