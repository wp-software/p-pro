import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from './stores/authStore';

// Layouts
import MainLayout from './components/layout/MainLayout';
import AuthLayout from './components/layout/AuthLayout';

// Pages
import Login from './pages/Login';
import ForgotPassword from './pages/ForgotPassword';
import ResetPassword from './pages/ResetPassword';
import ActivateAccount from './pages/ActivateAccount';
import Dashboard from './pages/Dashboard';
import { CapTablePage, TransactionsPage } from './pages/captable';
import Companies from './pages/Companies';
import Users from './pages/settings/Users';
import { Plans, ClientsSubscriptions, BillingDashboard, Invoices } from './pages/billing';
import { ShareholdersListPage, ShareholderDetailPage } from './pages/shareholders';
import ContractsListPage from './pages/contracts';
import ContractTemplatesPage from './pages/contracts/templates';
import ClausesPage from './pages/contracts/clauses';
import ContractBuilderPage from './pages/contracts/builder';
import ContractDetailsPage from './pages/contracts/[id]';
import { VestingPage, VestingGrantDetailPage, MyVestingPage, VestingPlanDetailPage, VestingGrantsPage, GrantMilestonesPage, MilestoneTemplatesPage } from './pages/vesting';
import { ValuationsPage, ValuationDetailPage, ValuationNewPage, ValuationDashboardPage } from './pages/valuation';
import { FinancialPage, FinancialPeriodPage, FinancialDashboardPage } from './pages/financial';
import DocumentsPage from './pages/documents/DocumentsPage';
import CustomFormulasPage from './pages/custom-formulas/CustomFormulasPage';
import CustomFormulaNewPage from './pages/custom-formulas/CustomFormulaNewPage';
import InvestorPortalPage from './pages/investor/InvestorPortalPage';
import ApprovalsPage from './pages/approvals/ApprovalsPage';
import WorkflowDetailPage from './pages/approvals/WorkflowDetailPage';
import FlowsPage from './pages/approvals/FlowsPage';
import ApproversPage from './pages/approvals/ApproversPage';
import RolesPage from './pages/settings/RolesPage';
import CommunicationsPage from './pages/communications';
import CommunicationDetailPage from './pages/communications/CommunicationDetailPage';
import NotificationsPage from './pages/notifications';
import DataRoomPage from './pages/dataroom';
import CoreClientsPage from './pages/admin/CoreClientsPage';
import ClientDetailPage from './pages/admin/ClientDetailPage';

function App() {
  const { isAuthenticated, user } = useAuthStore();

  return (
    <Routes>
      {/* Auth Routes */}
      <Route element={<AuthLayout />}>
        <Route path="/login" element={<Login />} />
        <Route path="/forgot-password" element={<ForgotPassword />} />
        <Route path="/reset-password" element={<ResetPassword />} />
        <Route path="/activate-account" element={<ActivateAccount />} />
      </Route>

      {/* Protected Routes */}
      <Route
        element={
          isAuthenticated ? <MainLayout /> : <Navigate to="/login" replace />
        }
      >
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/cap-table" element={<CapTablePage />} />
        <Route path="/cap-table/transactions" element={<TransactionsPage />} />
        <Route path="/companies" element={<Companies />} />
        <Route path="/partners" element={<Navigate to="/shareholders" replace />} />

        {/* Shareholders Routes */}
        <Route path="/shareholders" element={<ShareholdersListPage />} />
        <Route path="/shareholders/:id" element={<ShareholderDetailPage />} />

        {/* Contracts Routes */}
        <Route path="/contracts" element={<ContractsListPage />} />
        <Route path="/contracts/:id" element={<ContractDetailsPage />} />
        <Route path="/contracts/templates" element={<ContractTemplatesPage />} />
        <Route path="/contracts/clauses" element={<ClausesPage />} />
        <Route path="/contracts/builder" element={<ContractBuilderPage />} />

        {/* Vesting Routes */}
        <Route path="/vesting" element={<VestingPage />} />
        <Route path="/vesting/grants" element={<VestingGrantsPage />} />
        <Route path="/vesting/grant/:id" element={<VestingGrantDetailPage />} />
        <Route path="/vesting/grant/:grantId/milestones" element={<GrantMilestonesPage />} />
        <Route path="/vesting/milestone-templates" element={<MilestoneTemplatesPage />} />
        <Route path="/vesting/plans/:id" element={<VestingPlanDetailPage />} />
        <Route path="/my-vesting" element={<MyVestingPage />} />

        {/* Valuation Routes */}
        <Route path="/valuations" element={<ValuationsPage />} />
        <Route path="/valuations/new" element={<ValuationNewPage />} />
        <Route path="/valuations/dashboard" element={<ValuationDashboardPage />} />
        <Route path="/valuations/custom-formulas" element={<CustomFormulasPage />} />
        <Route path="/valuations/custom-formulas/new" element={<CustomFormulaNewPage />} />
        <Route path="/valuations/:id" element={<ValuationDetailPage />} />

        {/* Financial Routes */}
        <Route path="/financial" element={<FinancialPage />} />
        <Route path="/financial/dashboard" element={<FinancialDashboardPage />} />
        <Route path="/financial/:year/:month" element={<FinancialPeriodPage />} />

        {/* Documents Routes */}
        <Route path="/documents" element={<DocumentsPage />} />

        {/* Investor Portal */}
        <Route path="/investor" element={<InvestorPortalPage />} />

        {/* Approvals Routes */}
        <Route path="/approvals" element={<ApprovalsPage />} />
        <Route path="/approvals/:id" element={<WorkflowDetailPage />} />
        <Route path="/approvals/flows" element={<FlowsPage />} />
        <Route path="/approvals/approvers" element={<ApproversPage />} />

        {/* Communications Routes */}
        <Route path="/communications" element={<CommunicationsPage />} />
        <Route path="/communications/:id" element={<CommunicationDetailPage />} />

        {/* Notifications */}
        <Route path="/notifications" element={<NotificationsPage />} />

        {/* Data Room */}
        <Route path="/dataroom" element={<DataRoomPage />} />

        {/* Settings Routes */}
        <Route path="/settings/users" element={<Users />} />
        <Route path="/settings/roles" element={<RolesPage />} />
        <Route
          path="/admin/clients"
          element={
            user?.roles.includes('SuperAdmin') ? (
              <CoreClientsPage />
            ) : (
              <Navigate to="/" replace />
            )
          }
        />
        <Route
          path="/admin/clients/:id"
          element={
            user?.roles.includes('SuperAdmin') ? (
              <ClientDetailPage />
            ) : (
              <Navigate to="/" replace />
            )
          }
        />

        {/* Billing Routes */}
        <Route path="/billing" element={<BillingDashboard />} />
        <Route path="/billing/plans" element={<Plans />} />
        <Route path="/billing/clients" element={<ClientsSubscriptions />} />
        <Route path="/billing/invoices" element={<Invoices />} />
      </Route>

      {/* 404 */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;
