"use client";

import { FormEvent, useEffect, useState } from "react";
import { api, getErrorMessage } from "@/lib/api";
import { useAuth } from "@/lib/auth-context";
import { DepartmentDto, DesignationDto } from "@/lib/types";
import { ChevronDown, FileCog, MapPin, Network, Pencil, Plus, Trash2, UsersRound } from "lucide-react";

type Device = { id: string; deviceId: string; provider: string; displayName?: string | null; apiUrl?: string | null; isActive: boolean };
type Department = DepartmentDto & { parentDepartmentId?: string | null };
type Designation = DesignationDto & { departmentName?: string };

const inputClass = "min-w-0 w-full rounded-md border border-border bg-white px-3 py-2 text-sm text-ink";

export default function MasterDataPage() {
  const { user } = useAuth();
  const [departments, setDepartments] = useState<Department[]>([]);
  const [designations, setDesignations] = useState<Designation[]>([]);
  const [devices, setDevices] = useState<Device[]>([]);
  const [editing, setEditing] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [departmentForm, setDepartmentForm] = useState({ name: "", code: "", parentDepartmentId: "" });
  const [designationForm, setDesignationForm] = useState({ title: "", departmentId: "", level: "1" });
  const [deviceForm, setDeviceForm] = useState({ deviceId: "", provider: "", displayName: "", apiUrl: "", isActive: true });
  const [activeSection, setActiveSection] = useState("designations");

  async function refresh() {
    const [departmentResponse, designationResponse, deviceResponse] = await Promise.all([
      api.get<{ data: Department[] }>("/masterdata/departments"),
      api.get<{ data: Designation[] }>("/masterdata/designations"),
      api.get<{ data: Device[] }>("/masterdata/biometric-devices")
    ]);
    setDepartments(departmentResponse.data.data); setDesignations(designationResponse.data.data); setDevices(deviceResponse.data.data);
  }

  useEffect(() => { if (user?.roles.includes("Admin")) refresh().catch((err) => setError(getErrorMessage(err, "Could not load master data."))); }, [user]);

  useEffect(() => {
    function selectFromHash() {
      const section = window.location.hash.slice(1);
      if (["designations", "departments", "sub-departments", "biometric-devices"].includes(section)) setActiveSection(section);
    }
    selectFromHash();
    window.addEventListener("hashchange", selectFromHash);
    return () => window.removeEventListener("hashchange", selectFromHash);
  }, []);

  if (!user?.roles.includes("Admin")) return <div className="rounded-lg border border-border bg-white p-8 text-center text-sm text-slate-500">Admin access is required.</div>;

  async function save(event: FormEvent, type: "department" | "designation" | "device") {
    event.preventDefault(); setError(null);
    try {
      if (type === "department") {
        const body = { ...departmentForm, parentDepartmentId: departmentForm.parentDepartmentId || null };
        await api[editing ? "put" : "post"](editing ? `/masterdata/departments/${editing}` : "/masterdata/departments", body);
        setDepartmentForm({ name: "", code: "", parentDepartmentId: "" });
      } else if (type === "designation") {
        await api[editing ? "put" : "post"](editing ? `/masterdata/designations/${editing}` : "/masterdata/designations", { ...designationForm, level: Number(designationForm.level) });
        setDesignationForm({ title: "", departmentId: "", level: "1" });
      } else {
        await api[editing ? "put" : "post"](editing ? `/masterdata/biometric-devices/${editing}` : "/masterdata/biometric-devices", deviceForm);
        setDeviceForm({ deviceId: "", provider: "", displayName: "", apiUrl: "", isActive: true });
      }
      setEditing(null); await refresh();
    } catch (err) { setError(getErrorMessage(err, "Could not save master data.")); }
  }

  async function remove(type: "departments" | "designations" | "biometric-devices", id: string) {
    if (!window.confirm("Delete this master data record?")) return;
    try { await api.delete(`/masterdata/${type}/${id}`); await refresh(); } catch (err) { setError(getErrorMessage(err, "Could not delete this record.")); }
  }

  function editDepartment(item: Department) { setEditing(item.id); setDepartmentForm({ name: item.name, code: item.code, parentDepartmentId: item.parentDepartmentId ?? "" }); }
  function editDesignation(item: Designation) { setEditing(item.id); setDesignationForm({ title: item.title, departmentId: item.departmentId, level: String(item.level) }); }
  function editDevice(item: Device) { setEditing(item.id); setDeviceForm({ deviceId: item.deviceId, provider: item.provider, displayName: item.displayName ?? "", apiUrl: item.apiUrl ?? "", isActive: item.isActive }); }
  function cancelEdit() { setEditing(null); setDepartmentForm({ name: "", code: "", parentDepartmentId: "" }); setDesignationForm({ title: "", departmentId: "", level: "1" }); setDeviceForm({ deviceId: "", provider: "", displayName: "", apiUrl: "", isActive: true }); }

  const parentDepartments = departments.filter((item) => !item.parentDepartmentId);
  const subDepartments = departments.filter((item) => item.parentDepartmentId);
  const actions = (onEdit: () => void, onDelete: () => void) => <div className="flex justify-end gap-2"><button type="button" title="Edit" onClick={onEdit} className="rounded p-1.5 text-primary-600 hover:bg-primary-50"><Pencil size={16} /></button><button type="button" title="Delete" onClick={onDelete} className="rounded p-1.5 text-danger hover:bg-red-50"><Trash2 size={16} /></button></div>;
  const section = (id: string, title: string, Icon: typeof FileCog, children: React.ReactNode) => activeSection === id ? <section id={id} className="min-w-0 scroll-mt-5 overflow-hidden rounded-lg border border-border bg-white shadow-card"><div className="flex items-center gap-3 p-4 sm:p-5"><Icon size={20} className="shrink-0 text-primary-600" /><h2 className="min-w-0 flex-1 text-lg font-medium text-ink">{title}</h2><ChevronDown size={18} className="shrink-0 rotate-180 text-primary-600" /></div><div className="border-t border-border p-4 sm:p-5">{children}</div></section> : null;

  return <div className="space-y-6">
    <div><h1 className="font-display text-2xl text-ink">Master Data</h1><p className="mt-1 text-sm text-slate-500">Manage the reference data used across attendance and employee records.</p></div>
    {error && <div className="rounded-md border border-danger/20 bg-danger/5 px-3 py-2 text-sm text-danger">{error}</div>}
    {section("designations", "Designations", FileCog, <><form onSubmit={(event) => save(event, "designation")} className="mb-4 grid gap-2 md:grid-cols-[1fr_1fr_120px_auto]"> <input required placeholder="Designation title" value={designationForm.title} onChange={(e) => setDesignationForm({ ...designationForm, title: e.target.value })} className={inputClass} /><select required value={designationForm.departmentId} onChange={(e) => setDesignationForm({ ...designationForm, departmentId: e.target.value })} className={inputClass}><option value="">Department</option>{parentDepartments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select><input required type="number" min="1" placeholder="Level" value={designationForm.level} onChange={(e) => setDesignationForm({ ...designationForm, level: e.target.value })} className={inputClass} /><button title={editing ? "Update designation" : "Add designation"} className="rounded-md bg-primary-600 px-3 py-2 text-white hover:bg-primary-700"><Plus size={18} /></button></form><div className="divide-y divide-border">{designations.map((item) => <div key={item.id} className="grid grid-cols-1 items-start gap-2 py-3 text-sm sm:grid-cols-[1fr_1fr_80px_80px] sm:items-center sm:gap-3"><span className="font-medium text-ink">{item.title}</span><span className="text-slate-500">{item.departmentName ?? departments.find((department) => department.id === item.departmentId)?.name ?? "-"}</span><span className="text-slate-500">Level {item.level}</span>{actions(() => editDesignation(item), () => remove("designations", item.id))}</div>)}</div></>)}
    {section("departments", "Departments", Network, <><form onSubmit={(event) => save(event, "department")} className="mb-4 grid gap-2 md:grid-cols-[1fr_160px_1fr_auto]"> <input required placeholder="Department name" value={departmentForm.name} onChange={(e) => setDepartmentForm({ ...departmentForm, name: e.target.value })} className={inputClass} /><input required placeholder="Code" value={departmentForm.code} onChange={(e) => setDepartmentForm({ ...departmentForm, code: e.target.value })} className={inputClass} /><select value={departmentForm.parentDepartmentId} onChange={(e) => setDepartmentForm({ ...departmentForm, parentDepartmentId: e.target.value })} className={inputClass}><option value="">No parent department</option>{parentDepartments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select><button title={editing ? "Update department" : "Add department"} className="rounded-md bg-primary-600 px-3 py-2 text-white hover:bg-primary-700"><Plus size={18} /></button></form><div className="divide-y divide-border">{parentDepartments.map((item) => <div key={item.id} className="grid min-w-0 grid-cols-1 items-start gap-2 py-3 text-sm sm:grid-cols-[minmax(0,1fr)_160px_minmax(0,1fr)_80px] sm:items-center sm:gap-3"><span className="min-w-0 break-words font-medium text-ink">{item.name}</span><span className="min-w-0 break-words text-slate-500">{item.code}</span><span className="min-w-0 break-words text-slate-400">Department</span>{actions(() => editDepartment(item), () => remove("departments", item.id))}</div>)}</div></>)}
    {section("sub-departments", "Sub-department", UsersRound, <><form onSubmit={(event) => save(event, "department")} className="mb-4 grid gap-2 md:grid-cols-[1fr_160px_1fr_auto]"><input required placeholder="Sub-department name" value={departmentForm.name} onChange={(e) => setDepartmentForm({ ...departmentForm, name: e.target.value })} className={inputClass} /><input required placeholder="Code" value={departmentForm.code} onChange={(e) => setDepartmentForm({ ...departmentForm, code: e.target.value })} className={inputClass} /><select required value={departmentForm.parentDepartmentId} onChange={(e) => setDepartmentForm({ ...departmentForm, parentDepartmentId: e.target.value })} className={inputClass}><option value="">Parent department</option>{parentDepartments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select><button title={editing ? "Update sub-department" : "Add sub-department"} className="rounded-md bg-primary-600 px-3 py-2 text-white hover:bg-primary-700"><Plus size={18} /></button></form><div className="divide-y divide-border">{subDepartments.map((item) => <div key={item.id} className="grid min-w-0 grid-cols-1 items-start gap-2 py-3 text-sm sm:grid-cols-[minmax(0,1fr)_160px_minmax(0,1fr)_80px] sm:items-center sm:gap-3"><span className="min-w-0 break-words font-medium text-ink">{item.name}</span><span className="min-w-0 break-words text-slate-500">{item.code}</span><span className="min-w-0 break-words text-slate-500">{departments.find((parent) => parent.id === item.parentDepartmentId)?.name ?? "-"}</span>{actions(() => editDepartment(item), () => remove("departments", item.id))}</div>)}</div></>)}
    {section("biometric-devices", "Bio-Metric Device Locations", MapPin, <><form onSubmit={(event) => save(event, "device")} className="mb-4 grid gap-2 md:grid-cols-[1fr_1fr_1fr_1fr_auto]">{(["deviceId", "provider", "displayName", "apiUrl"] as const).map((key) => <input key={key} required={key === "deviceId" || key === "provider"} placeholder={key === "deviceId" ? "Device ID" : key === "displayName" ? "Location name" : key === "apiUrl" ? "API URL" : "Provider"} value={deviceForm[key]} onChange={(e) => setDeviceForm({ ...deviceForm, [key]: e.target.value })} className={inputClass} />)}<button title={editing ? "Update device" : "Add device"} className="rounded-md bg-primary-600 px-3 py-2 text-white hover:bg-primary-700"><Plus size={18} /></button></form><div className="divide-y divide-border">{devices.map((item) => <div key={item.id} className="grid min-w-0 grid-cols-1 items-start gap-2 py-3 text-sm sm:grid-cols-[minmax(0,1fr)_minmax(0,1fr)_minmax(0,1fr)_100px_80px] sm:items-center sm:gap-3"><span className="min-w-0 break-words font-medium text-ink">{item.displayName || item.deviceId}</span><span className="min-w-0 break-words text-slate-500">{item.deviceId}</span><span className="min-w-0 break-words text-slate-500">{item.provider}</span><span className={item.isActive ? "text-success" : "text-slate-400"}>{item.isActive ? "Active" : "Inactive"}</span>{actions(() => editDevice(item), () => remove("biometric-devices", item.id))}</div>)}</div></>)}
    {editing && <button type="button" onClick={cancelEdit} className="fixed bottom-5 right-5 rounded-full border border-border bg-white px-4 py-2 text-sm text-slate-600 shadow-card">Cancel edit</button>}
  </div>;
}