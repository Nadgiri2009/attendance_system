export default function Pagination({
  pageNumber,
  totalPages,
  totalCount,
  hasPreviousPage,
  hasNextPage,
  onPageChange
}: {
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  onPageChange: (page: number) => void;
}) {
  if (totalCount === 0) return null;

  return (
    <div className="flex items-center justify-between text-sm text-slate-500 pt-1">
      <span>
        Page {pageNumber} of {Math.max(totalPages, 1)} · {totalCount} total
      </span>
      <div className="flex gap-2">
        <button
          type="button"
          disabled={!hasPreviousPage}
          onClick={() => onPageChange(pageNumber - 1)}
          className="rounded-md border border-border px-3 py-1.5 text-sm hover:bg-surface disabled:opacity-40 disabled:cursor-not-allowed"
        >
          Previous
        </button>
        <button
          type="button"
          disabled={!hasNextPage}
          onClick={() => onPageChange(pageNumber + 1)}
          className="rounded-md border border-border px-3 py-1.5 text-sm hover:bg-surface disabled:opacity-40 disabled:cursor-not-allowed"
        >
          Next
        </button>
      </div>
    </div>
  );
}
