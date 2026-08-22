import ProtectedRoute from "@/components/ProtectedRoute";
import Sidebar from "@/components/Sidebar";
import Navbar from "@/components/Navbar";
import TrackingResumer from "@/components/TrackingResumer";

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  return (
    <ProtectedRoute>
      <TrackingResumer />
      <div className="min-h-screen flex bg-surface">
        <Sidebar />
        <div className="flex-1 flex flex-col min-w-0">
          <Navbar />
          <main className="min-w-0 flex-1 overflow-x-hidden overflow-y-auto p-4 sm:p-8">{children}</main>
        </div>
      </div>
    </ProtectedRoute>
  );
}
