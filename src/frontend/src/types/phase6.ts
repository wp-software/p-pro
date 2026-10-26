// ─── Communication types ──────────────────────────────────────────────────────

export interface Communication {
  id: string;
  companyId: string;
  title: string;
  content: string;
  contentHtml?: string;
  summary?: string;
  commType: 'announcement' | 'update' | 'report' | 'alert' | 'invitation';
  visibility: 'all' | 'investors' | 'founders' | 'employees' | 'specific';
  targetRoles?: string[];
  sendEmail: boolean;
  isPinned: boolean;
  publishedAt?: string;
  expiresAt?: string;
  createdBy: string;
  createdAt: string;
  viewsCount: number;
  hasViewed?: boolean;
}

export interface CreateCommunicationRequest {
  title: string;
  content: string;
  contentHtml?: string;
  summary?: string;
  commType: string;
  visibility: string;
  targetRoles?: string[];
  sendEmail?: boolean;
  isPinned: boolean;
  expiresAt?: string;
}

// ─── Notification types ───────────────────────────────────────────────────────

export interface Notification {
  id: string;
  userId: string;
  companyId: string;
  notificationType: string;
  title: string;
  body: string;
  actionUrl?: string;
  referenceType?: string;
  referenceId?: string;
  isRead: boolean;
  readAt?: string;
  createdAt: string;
}

export interface NotificationPreference {
  id: string;
  userId: string;
  notificationType: string;
  channel: 'in_app' | 'email' | 'both' | 'none';
}

// ─── Workflow types ───────────────────────────────────────────────────────────

export interface WorkflowApproval {
  id: string;
  workflowStepId: string;
  userId: string;
  decision: 'approved' | 'rejected' | 'requested_changes';
  comments?: string;
  decidedAt: string;
}

export interface WorkflowStep {
  id: string;
  workflowId: string;
  stepOrder: number;
  name: string;
  description?: string;
  stepType: 'approval' | 'review' | 'notification' | 'automated';
  assignedRole?: string;
  assignedUserId?: string;
  status: 'pending' | 'in_progress' | 'completed' | 'skipped';
  isCurrent: boolean;
  startedAt?: string;
  dueDate?: string;
  completedAt?: string;
  notes?: string;
  approvals?: WorkflowApproval[];
}

export interface Workflow {
  id: string;
  companyId: string;
  workflowType: string;
  workflowTypeLabel: string;
  referenceType: string;
  referenceId: string;
  title: string;
  description?: string;
  status: 'pending' | 'in_progress' | 'approved' | 'rejected' | 'cancelled';
  priority: 'low' | 'medium' | 'high' | 'urgent';
  currentStep: number;
  totalSteps: number;
  requestedBy: string;
  requestedByName: string;
  requestedAt: string;
  dueDate?: string;
  completedAt?: string;
  cancelledAt?: string;
  cancellationReason?: string;
  steps?: WorkflowStep[];
}

export interface CreateWorkflowStepRequest {
  name: string;
  stepType: string;
  assignedRole?: string;
  assignedUserId?: string;
  dueDate?: string;
}

export interface CreateWorkflowRequest {
  title: string;
  description?: string;
  workflowType: string;
  referenceType: string;
  referenceId: string;
  priority: string;
  dueDate?: string;
  steps: CreateWorkflowStepRequest[];
}

// ─── DataRoom types ───────────────────────────────────────────────────────────

export interface DataRoomFolder {
  id: string;
  dataRoomId: string;
  parentId?: string;
  name: string;
  description?: string;
  displayOrder: number;
  visibility: 'internal' | 'investors' | 'public';
  createdAt: string;
  allowedRoles?: string[];
  subFolders?: DataRoomFolder[];
}

export interface DataRoomActivityLog {
  id: string;
  folderId?: string;
  folderName?: string;
  documentId?: string;
  documentName?: string;
  action: string;
  userId: string;
  userName?: string;
  details?: string;
  createdAt: string;
}

export interface FinancialDocument {
  documentId: string;
  folderId: string;
  metricKey?: string | null;
  name: string;
  fileName: string;
  fileSizeBytes: number;
  documentType: string;
  createdAt: string;
}

export interface DataRoom {
  id: string;
  companyId: string;
  name: string;
  description?: string;
  isActive: boolean;
  folders?: DataRoomFolder[];
}

// ─── Investor Portal types ────────────────────────────────────────────────────

export interface InvestorSummary {
  investorName: string;
  companyName: string;
  totalShares: number;
  ownershipPercentage: number;
  estimatedValue: number;
  currentValuation: number;
  lastVestingEvent?: string;
  documentsCount: number;
}
