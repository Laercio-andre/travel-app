export type UserRole = 'Traveler' | 'Admin';

export interface AuthUser {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest extends LoginRequest {
  firstName: string;
  lastName: string;
}

export interface ForgotPasswordResponse {
  message: string;
  resetToken?: string | null;
  resetUrl?: string | null;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}
