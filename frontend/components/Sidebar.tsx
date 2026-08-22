"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import clsx from "clsx";
import { useAuth } from "@/lib/auth-context";
import { ChevronDown, Database, FileCog, MapPin, Menu, Network, UserPlus, UsersRound, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";

const NAV_ITEMS = [
  { href: "/dashboard", label: "Dashboard", icon: "▦" }
];

const ADMIN_NAV_ITEMS = [
  { href: "/attendance", label: "Attendance", icon: "◫" },
  { href: "/attendance-reports", label: "Audit Reports", icon: "▤" }
];

const TRACKING_NAV_ITEM = { href: "/tracking", label: "Live Tracking", icon: "◉" };
const TRACKING_ROLES = ["Admin", "HR", "Manager"];

export default function Sidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const { user, logout } = useAuth();
  const [isMasterDataOpen, setIsMasterDataOpen] = useState(false);
  const [isMobileOpen, setIsMobileOpen] = useState(false);
  const masterDataRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function closeOnOutsideClick(event: MouseEvent) {
      if (!masterDataRef.current?.contains(event.target as Node)) setIsMasterDataOpen(false);
    }
    document.addEventListener("mousedown", closeOnOutsideClick);
    return () => document.removeEventListener("mousedown", closeOnOutsideClick);
  }, []);

  function openMasterDataSection(section: string) {
    setIsMasterDataOpen(false);
    setIsMobileOpen(false);
    if (section === "employees" || section === "employees-new") {
      router.push(section === "employees-new" ? "/employees/new" : "/employees");
      return;
    }
    if (pathname.startsWith("/master-data")) window.location.hash = section;
    else router.push(`/master-data#${section}`);
  }

  const navItems =
    user && user.roles.includes("Admin")
      ? [...NAV_ITEMS, ...ADMIN_NAV_ITEMS, TRACKING_NAV_ITEM]
      : user && TRACKING_ROLES.some((r) => user.roles.includes(r))
        ? [...NAV_ITEMS, ...ADMIN_NAV_ITEMS.filter((item) => item.href === "/attendance"), TRACKING_NAV_ITEM]
        : NAV_ITEMS;

  return (
    <>
      <button type="button" title="Open navigation" onClick={() => setIsMobileOpen(true)} className="fixed left-4 top-4 z-50 rounded-md border border-border bg-white p-2 text-primary-700 shadow-card lg:hidden">
        <Menu size={20} aria-hidden="true" />
      </button>
      {isMobileOpen && <button type="button" aria-label="Close navigation overlay" onClick={() => setIsMobileOpen(false)} className="fixed inset-0 z-40 bg-slate-900/30 lg:hidden" />}
    <aside className={clsx("fixed inset-y-0 left-0 z-50 flex w-60 shrink-0 flex-col border-r border-border bg-white transition-transform duration-300 ease-in-out lg:static lg:z-auto lg:translate-x-0", isMobileOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0")}>
      <div className="h-16 flex items-center px-6 border-b border-border">
        <span className="font-mono text-xs tracking-[0.3em] text-primary-600 uppercase">EWMS</span>
        <button type="button" title="Close navigation" onClick={() => setIsMobileOpen(false)} className="ml-auto rounded-md p-2 text-slate-500 hover:bg-surface lg:hidden">
          <X size={18} aria-hidden="true" />
        </button>
      </div>
      <nav className="flex-1 py-4">
        {navItems.map((item) => {
          const isActive = pathname.startsWith(item.href);
          return (
            <Link
              key={item.href}
              href={item.href}
              className={clsx(
                "flex items-center gap-3 px-6 py-2.5 text-sm transition-colors",
                isActive
                  ? "bg-primary-50 text-primary-700 font-medium border-r-2 border-primary-600"
                  : "text-slate-600 hover:bg-surface hover:text-ink"
              )}
              onClick={() => setIsMobileOpen(false)}
            >
              <span className="text-base leading-none opacity-70">{item.icon}</span>
              {item.label}
            </Link>
          );
        })}
        {user?.roles.includes("Admin") && (
          <div ref={masterDataRef} className="relative mt-2 border-t border-border pt-2">
            <button type="button" onClick={() => setIsMasterDataOpen((open) => !open)} className="flex w-full items-center gap-3 px-6 py-2.5 text-left text-sm text-slate-600 hover:bg-surface hover:text-ink">
              <Database size={16} aria-hidden="true" />
              <span className="flex-1">Master Data</span>
              <ChevronDown size={15} className={clsx("transition-transform", isMasterDataOpen && "rotate-180")} aria-hidden="true" />
            </button>
            {isMasterDataOpen && <div className="space-y-1 bg-slate-50 py-2">
              {[{ section: "employees", label: "Employees", Icon: UsersRound }, { section: "employees-new", label: "Create Employee", Icon: UserPlus }, { section: "designations", label: "Designations", Icon: FileCog }, { section: "departments", label: "Departments", Icon: Network }, { section: "sub-departments", label: "Sub-department", Icon: UsersRound }, { section: "biometric-devices", label: "Device Locations", Icon: MapPin }].map(({ section, label, Icon }) => <button key={section} type="button" onClick={() => openMasterDataSection(section)} className="flex w-full items-center gap-3 px-10 py-2 text-left text-xs text-slate-600 hover:text-primary-700"><Icon size={14} aria-hidden="true" />{label}</button>)}
            </div>}
          </div>
        )}
      </nav>
      <div className="border-t border-border p-4 lg:hidden">
        <button type="button" onClick={logout} className="w-full rounded-md px-3 py-2 text-left text-sm text-slate-500 transition-colors hover:bg-surface hover:text-danger focus-ring">
          Sign out
        </button>
      </div>
    </aside>
    </>
  );
}
