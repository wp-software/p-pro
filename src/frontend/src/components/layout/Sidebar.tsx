import { useState, useEffect } from 'react';
import { NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  LayoutDashboard,
  PieChart,
  Users,
  UserCheck,
  CheckSquare,
  FileText,
  Target,
  TrendingUp,
  DollarSign,
  Briefcase,
  Settings,
  LogOut,
  Globe,
  Building2,
  List,
  Layers,
  FlaskConical,
  ChevronDown,
  LucideIcon,
  MessageSquare,
  Bell,
  FolderOpen,
} from 'lucide-react';
import { useAuthStore } from '@/stores/authStore';
import { cn } from '@/utils/cn';
import i18n from '@/i18n';

// ─── Types ────────────────────────────────────────────────────────────────────

interface NavItem {
  nameKey: string;
  href: string;
  icon: LucideIcon;
  requiresRole?: string[];
}

interface NavGroup {
  key: string;
  labelKey: string;
  icon: LucideIcon;
  items: NavItem[];
}

// ─── Navigation structure ─────────────────────────────────────────────────────

const navGroups: NavGroup[] = [
  {
    key: 'dashboards',
    labelKey: 'nav.dashboards',
    icon: LayoutDashboard,
    items: [
      { nameKey: 'nav.dashboard', href: '/dashboard', icon: LayoutDashboard },
      { nameKey: 'nav.valuationDashboard', href: '/valuations/dashboard', icon: TrendingUp },
      { nameKey: 'nav.financeiroDashboard', href: '/financial/dashboard', icon: DollarSign },
      { nameKey: 'nav.myVesting', href: '/my-vesting', icon: Target },
      { nameKey: 'nav.investorPortal', href: '/investor', icon: Briefcase },
    ],
  },
  {
    key: 'captable',
    labelKey: 'nav.capTable',
    icon: PieChart,
    items: [
      { nameKey: 'nav.companies', href: '/companies', icon: Building2 },
      { nameKey: 'nav.partners', href: '/shareholders', icon: Users },
      { nameKey: 'nav.capTableNav', href: '/cap-table', icon: PieChart },
      { nameKey: 'nav.vestingPlans', href: '/vesting', icon: Target },
      { nameKey: 'nav.vestingGrants', href: '/vesting/grants', icon: UserCheck },
    ],
  },
  {
    key: 'valuation',
    labelKey: 'nav.valuation',
    icon: TrendingUp,
    items: [
      { nameKey: 'nav.valuationNav', href: '/valuations', icon: TrendingUp },
      { nameKey: 'nav.financial', href: '/financial', icon: DollarSign },
      { nameKey: 'nav.documents', href: '/documents', icon: FileText },
      { nameKey: 'nav.dataRoom', href: '/dataroom', icon: FolderOpen },
    ],
  },
  {
    key: 'contratos',
    labelKey: 'nav.contracts',
    icon: FileText,
    items: [
      { nameKey: 'nav.contractsNav', href: '/contracts', icon: FileText },
      { nameKey: 'nav.contractTemplates', href: '/contracts/templates', icon: FileText, requiresRole: ['Admin', 'Legal'] },
    ],
  },
  {
    key: 'comunicacoes',
    labelKey: 'nav.communications',
    icon: MessageSquare,
    items: [
      { nameKey: 'nav.communicationsNav', href: '/communications', icon: MessageSquare },
      { nameKey: 'nav.notifications', href: '/notifications', icon: Bell },
    ],
  },
  {
    key: 'aprovacoes',
    labelKey: 'nav.approvals',
    icon: CheckSquare,
    items: [
      { nameKey: 'nav.flows', href: '/approvals/flows', icon: List },
      { nameKey: 'nav.approvers', href: '/approvals/approvers', icon: UserCheck },
      { nameKey: 'nav.approvalsNav', href: '/approvals', icon: CheckSquare },
    ],
  },
  {
    key: 'admin',
    labelKey: 'nav.admin',
    icon: Settings,
    items: [
      { nameKey: 'nav.users', href: '/settings/users', icon: Users },
      { nameKey: 'nav.roles', href: '/settings/roles', icon: UserCheck },
      {
        nameKey: 'nav.coreClients',
        href: '/admin/clients',
        icon: Building2,
        requiresRole: ['SuperAdmin'],
      },
    ],
  },
  {
    key: 'acessorios',
    labelKey: 'nav.accessories',
    icon: Layers,
    items: [
      { nameKey: 'nav.customFormulas', href: '/valuations/custom-formulas', icon: FlaskConical },
      { nameKey: 'nav.shareClasses', href: '/share-classes', icon: PieChart },
      { nameKey: 'nav.clauses', href: '/contracts/clauses', icon: List, requiresRole: ['Admin', 'Legal'] },
      { nameKey: 'nav.milestoneTemplates', href: '/vesting/milestone-templates', icon: Layers, requiresRole: ['Admin'] },
    ],
  },
];

// ─── Helpers ──────────────────────────────────────────────────────────────────

const STORAGE_KEY = 'sidebar_open_groups';

/** Returns the group key whose item best (most specifically) matches the given path. */
function getGroupForPath(pathname: string): string | null {
  let bestKey: string | null = null;
  let bestLen = -1;

  for (const group of navGroups) {
    for (const item of group.items) {
      if (pathname === item.href || pathname.startsWith(item.href + '/')) {
        if (item.href.length > bestLen) {
          bestLen = item.href.length;
          bestKey = group.key;
        }
      }
    }
  }

  return bestKey;
}

function loadSavedGroups(): string[] | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) return JSON.parse(raw) as string[];
  } catch {
    // ignore parse errors
  }
  return null;
}

// ─── Languages ────────────────────────────────────────────────────────────────

const languages = [
  { code: 'pt', name: 'Português', flag: '🇧🇷' },
  { code: 'en', name: 'English', flag: '🇺🇸' },
  { code: 'es', name: 'Español', flag: '🇪🇸' },
];

// ─── Component ────────────────────────────────────────────────────────────────

export default function Sidebar() {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuthStore();
  const { t } = useTranslation();

  const [openGroups, setOpenGroups] = useState<string[]>(() => {
    const saved = loadSavedGroups();
    if (saved !== null) return saved;
    const active = getGroupForPath(location.pathname);
    return active ? [active] : [];
  });

  // When navigating to a new route, ensure that group is expanded
  useEffect(() => {
    const active = getGroupForPath(location.pathname);
    if (active && !openGroups.includes(active)) {
      setOpenGroups((prev) => [...prev, active]);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname]);

  // Persist open groups to localStorage whenever they change
  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(openGroups));
  }, [openGroups]);

  const toggleGroup = (key: string) => {
    setOpenGroups((prev) =>
      prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key]
    );
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const handleLanguageChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const lang = e.target.value;
    i18n.changeLanguage(lang);
    localStorage.setItem('pm-language', lang);
  };

  return (
    <aside className="sidebar">
      {/* Logo */}
      <div className="sidebar-logo flex justify-center">
        <img
          src="/logo.png"
          alt="WP Manager"
          className="h-20 w-auto object-contain"
        />
      </div>

      {/* Navigation */}
      <nav className="sidebar-nav">
        <ul className="space-y-0.5">
          {navGroups.map((group) => {
            const isOpen = openGroups.includes(group.key);
            return (
              <li key={group.key}>
                {/* Group header (collapsible trigger) */}
                <button
                  onClick={() => toggleGroup(group.key)}
                  className={cn(
                    'w-full flex items-center gap-3 px-4 py-2 rounded-lg',
                    'text-white/50 hover:bg-white/10 hover:text-white/80',
                    'transition-all duration-200'
                  )}
                >
                  <group.icon className="w-3.5 h-3.5 shrink-0" />
                  <span className="flex-1 text-left text-[10px] font-bold uppercase tracking-widest">
                    {t(group.labelKey)}
                  </span>
                  <ChevronDown
                    className={cn(
                      'w-3 h-3 shrink-0 transition-transform duration-300',
                      isOpen ? 'rotate-0' : '-rotate-90'
                    )}
                  />
                </button>

                {/* Animated items container — CSS grid row trick */}
                <div
                  className={cn(
                    'grid transition-all duration-300 ease-in-out',
                    isOpen ? 'grid-rows-[1fr]' : 'grid-rows-[0fr]'
                  )}
                >
                  <div className="overflow-hidden">
                    <ul className="pt-0.5 pb-2 space-y-0.5">
                      {group.items
                        .filter((item) => {
                          if (!item.requiresRole) return true;
                          return item.requiresRole.some((role) =>
                            user?.roles?.includes(role)
                          );
                        })
                        .map((item) => (
                          <li key={item.href}>
                            <NavLink
                              to={item.href}
                              end
                              className={({ isActive }) =>
                                cn(
                                  'flex items-center gap-3 pl-9 pr-4 py-2 rounded-lg text-sm',
                                  'text-white/60 hover:bg-white/10 hover:text-white transition-all duration-200',
                                  isActive && 'bg-white/10 text-white font-medium'
                                )
                              }
                            >
                              <item.icon className="w-4 h-4 shrink-0" />
                              <span>{t(item.nameKey)}</span>
                            </NavLink>
                          </li>
                        ))}
                    </ul>
                  </div>
                </div>
              </li>
            );
          })}
        </ul>
      </nav>

      {/* Footer */}
      <div className="sidebar-footer space-y-3">
        {/* Language Selector */}
        <div className="flex items-center gap-2 px-4 py-2 text-white/60">
          <Globe className="w-4 h-4" />
          <select
            className="bg-transparent text-sm cursor-pointer focus:outline-none"
            value={i18n.language?.split('-')[0] || 'pt'}
            onChange={handleLanguageChange}
          >
            {languages.map((lang) => (
              <option key={lang.code} value={lang.code} className="text-primary">
                {lang.flag} {lang.name}
              </option>
            ))}
          </select>
        </div>

        {/* Actions */}
        <div className="flex gap-2">
          <button className="sidebar-item flex-1 justify-center">
            <Settings className="w-4 h-4" />
          </button>
          <button
            onClick={handleLogout}
            className="sidebar-item flex-1 justify-center hover:bg-error/20 hover:text-error-300"
          >
            <LogOut className="w-4 h-4" />
          </button>
        </div>
      </div>
    </aside>
  );
}
