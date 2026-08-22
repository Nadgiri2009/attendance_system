import { ReactNode } from "react";

export interface Column<T> {
  header: string;
  render: (row: T) => ReactNode;
  className?: string;
  // Optional: when set, the column header becomes clickable and calls
  // onSortChange(sortKey) — used by the Employees and Attendance lists to
  // satisfy the "Sorting" requirement without duplicating a table component.
  sortKey?: string;
}

export interface SortState {
  sortBy: string;
  sortDescending: boolean;
}

export default function DataTable<T extends { id: string }>({
  columns,
  rows,
  emptyMessage = "No records found.",
  sortState,
  onSortChange
}: {
  columns: Column<T>[];
  rows: T[];
  emptyMessage?: string;
  sortState?: SortState;
  onSortChange?: (sortKey: string) => void;
}) {
  return (
    <div className="overflow-x-auto rounded-lg border border-border bg-white shadow-card">
      <table className="min-w-full divide-y divide-border text-sm">
        <thead className="bg-surface">
          <tr>
            {columns.map((col) => {
              const isSortable = !!col.sortKey && !!onSortChange;
              const isActive = sortState?.sortBy === col.sortKey;
              return (
                <th
                  key={col.header}
                  onClick={isSortable ? () => onSortChange!(col.sortKey!) : undefined}
                  className={`px-4 py-3 text-left text-xs font-medium uppercase tracking-wide text-slate-500 ${
                    isSortable ? "cursor-pointer select-none hover:text-ink" : ""
                  }`}
                >
                  {col.header}
                  {isActive && <span className="ml-1 text-primary-600">{sortState?.sortDescending ? "↓" : "↑"}</span>}
                </th>
              );
            })}
          </tr>
        </thead>
        <tbody className="divide-y divide-border">
          {rows.length === 0 ? (
            <tr>
              <td colSpan={columns.length} className="px-4 py-10 text-center text-slate-400">
                {emptyMessage}
              </td>
            </tr>
          ) : (
            rows.map((row) => (
              <tr key={row.id} className="hover:bg-surface/60 transition-colors">
                {columns.map((col) => (
                  <td key={col.header} className={col.className ?? "px-4 py-3 text-ink"}>
                    {col.render(row)}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}
