export interface EmployeeDto {
  id: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  aadhaarNumber: string;
  gender: string;
  dateOfBirth: string;
  dateOfJoining: string;
  photoUrl?: string | null;
  departmentId: string;
  departmentName: string;
  designationId: string;
  designationTitle: string;
  reportingManagerId?: string | null;
  reportingManagerName?: string | null;
  isActive: boolean;
}

export interface DepartmentDto {
  id: string;
  name: string;
  code: string;
  parentDepartmentId?: string | null;
}

export interface DesignationDto {
  id: string;
  title: string;
  departmentId: string;
  level: number;
}

export interface AttendanceDto {
  id: string;
  employeeId: string;
  employeeName: string;
  attendanceDate: string;
  checkInAtUtc?: string | null;
  checkInLatitude?: number | null;
  checkInLongitude?: number | null;
  checkOutAtUtc?: string | null;
  checkOutLatitude?: number | null;
  checkOutLongitude?: number | null;
  status: string;
  totalHours?: number | null;
  isMockLocationSuspected: boolean;
  remarks?: string | null;
}

export interface AttendanceReportRow {
  attendanceId: string;
  employeeId: string;
  employeePhoto: string;
  employeeName: string;
  employeeCode: string;
  employeeDepartment: string;
  subDepartment?: string | null;
  employeeDesignation: string;
  attendanceDate: string;
  inTimeUtc?: string | null;
  inDepartment?: string | null;
  inLocation?: string | null;
  inBiometricDevice?: string | null;
  outTimeUtc?: string | null;
  outDepartment?: string | null;
  outLocation?: string | null;
  outBiometricDevice?: string | null;
  totalWorkingHours?: number | null;
  attendanceStatus: string;
}

export interface AttendanceReportResult {
  items: AttendanceReportRow[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  summary: {
    workingDays: number;
    totalEmployees: number;
    present: number;
    absent: number;
    leave: number;
    halfDay: number;
    late: number;
    totalHours: number;
    attendancePercentage: number;
  };
}

export interface AttendanceDashboardSummary {
  totalEmployees: number;
  present: number;
  absent: number;
  onLeave: number;
  halfDay: number;
  late: number;
  checkedIn: number;
  checkedOut: number;
  attendancePercentage: number;
  currentlyWorking: number;
}

export interface AttendanceAuditRow {
  id: string;
  employeeId: string;
  employeeName: string;
  dateTimeUtc: string;
  transactionType: string;
  deviceId?: string | null;
  deviceName?: string | null;
  deviceLocation?: string | null;
  departmentAtDevice?: string | null;
  verificationStatus: string;
  createdAtUtc: string;
}

export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface AuditLogDto {
  id: string;
  action: string;
  entityType: string;
  entityId?: string | null;
  userName?: string | null;
  status: string;
  details?: string | null;
  errorMessage?: string | null;
  ipAddress?: string | null;
  eventAtUtc: string;
}

export const ATTENDANCE_STATUSES = [
  "Present",
  "Absent",
  "HalfDay",
  "OnLeave",
  "MissedCheckOut",
  "PendingApproval"
] as const;

export interface TrackingSessionDto {
  id: string;
  employeeId: string;
  employeeName: string;
  attendanceRecordId: string;
  startedAtUtc: string;
  startLatitude: number;
  startLongitude: number;
  deviceInfo?: string | null;
  endedAtUtc?: string | null;
  endLatitude?: number | null;
  endLongitude?: number | null;
  totalDistanceMeters?: number | null;
  totalDurationSeconds?: number | null;
  totalPointsCaptured: number;
  status: "Active" | "Stopped";
}

export interface LocationPointDto {
  id: string;
  latitude: number;
  longitude: number;
  accuracyMeters?: number | null;
  speedKmh?: number | null;
  heading?: number | null;
  batteryPercent?: number | null;
  isMockLocation: boolean;
  recordedAtUtc: string;
}

export interface TrackingHistoryDto {
  session: TrackingSessionDto;
  points: LocationPointDto[];
}

export interface LiveLocationDto {
  isActive: boolean;
  trackingSessionId?: string | null;
  employeeId: string;
  employeeName?: string | null;
  startedAtUtc?: string | null;
  lastLatitude?: number | null;
  lastLongitude?: number | null;
  lastSpeedKmh?: number | null;
  lastBatteryPercent?: number | null;
  lastRecordedAtUtc?: string | null;
  pointsCapturedSoFar: number;
}

export interface AuthResult {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  userId: string;
  userName: string;
  email: string;
  employeeId?: string | null;
  roles: string[];
}
