// Types para autenticação - espelham os DTOs do backend

export interface LoginRequest {
  email: string;
  password: string;
  companyId?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserInfo;
}

export interface UserInfo {
  id: string;
  email: string;
  name: string;
  avatarUrl?: string;
  companyId: string;
  companyName: string;
  shareholderId?: string | null;
  roles: string[];
  language: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ActivateAccountRequest {
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}
