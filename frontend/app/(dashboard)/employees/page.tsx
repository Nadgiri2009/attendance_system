"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { EmployeeDto, PaginatedList } from "@/lib/types";
import DataTable, { Column } from "@/components/DataTable";
import Pagination from "@/components/Pagination";

const PAGE_SIZE = 10;

export default function EmployeesPage() {
  const { user } = useAuth();
  const [result, setResult] = useState<PaginatedList<EmployeeDto> | null>(null);
  const [search, setSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [sortBy, setSortBy] = useState("firstname");
  const [sortDescending, setSortDescending] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  // Reset to page 1 whenever the search term changes, so a filtered result
  // set doesn't leave the user stranded on a page that no longer exists.
  useEffect(() => {
    setPageNumber(1);
  }, [search]);

  useEffect(() => {
    const timeout = setTimeout(() => {
      setIsLoading(true);
      api
        .get<{ data: PaginatedList<EmployeeDto> }>("/employees", {
          params: { search, sortBy, sortDescending, pageNumber, pageSize: PAGE_SIZE }
        })
        .then((res) => setResult(res.data.data))
        .catch((err) => {
          console.warn("[Employees] Failed to fetch employees:", err);
        })
        .finally(() => setIsLoading(false));
    }, 300);
    return () => clearTimeout(timeout);
  }, [search, sortBy, sortDescending, pageNumber]);

  function handleSortChange(key: string) {
    if (key === sortBy) {
      setSortDescending((prev) => !prev);
    } else {
      setSortBy(key);
      setSortDescending(false);
    }
  }

  const columns: Column<EmployeeDto>[] = [
    {
      header: "Employee",
      sortKey: "firstname",
      render: (e) => (
        <div>
          <div className="font-medium text-ink">{e.firstName} {e.lastName}</div>
          <div className="text-xs text-slate-400">{e.employeeCode}</div>
        </div>
      )
    },
    { header: "Department", sortKey: "department", render: (e) => e.departmentName },
    { header: "Designation", sortKey: "designation", render: (e) => e.designationTitle },
    { header: "Email", sortKey: "email", render: (e) => e.email },
    {
      header: "Status",
      render: (e) => (
        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
          e.isActive ? "bg-success/10 text-success" : "bg-slate-100 text-slate-500"
        }`}>
          {e.isActive ? "Active" : "Inactive"}
        </span>
      )
    },
    {
      header: "",
      render: (e) => (
        <Link href={`/employees/${e.id}`} className="text-primary-600 hover:text-primary-700 text-sm font-medium">
          View →
        </Link>
      )
    }
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="font-display text-2xl text-ink">Employees</h1>
          <p className="text-sm text-slate-500 mt-1">Organization-wide employee directory.</p>
        </div>
      </div>

      <input
        type="text"
        placeholder="Search by name, code, or email…"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        className="w-full max-w-md rounded-md border border-border bg-white px-3 py-2 text-sm focus-ring focus:border-primary-400"
      />

      {isLoading || !result ? (
        <div className="text-sm text-slate-400 py-10 text-center">Loading employees…</div>
      ) : (
        <>
          <DataTable
            columns={columns}
            rows={result.items}
            emptyMessage="No employees match your search."
            sortState={{ sortBy, sortDescending }}
            onSortChange={handleSortChange}
          />
          <Pagination
            pageNumber={result.pageNumber}
            totalPages={result.totalPages}
            totalCount={result.totalCount}
            hasPreviousPage={result.hasPreviousPage}
            hasNextPage={result.hasNextPage}
            onPageChange={setPageNumber}
          />
        </>
      )}
    </div>
  );
}
