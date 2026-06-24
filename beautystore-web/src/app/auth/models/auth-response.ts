export interface AuthResponse {
  accessToken:  string;
  refreshToken: string;
  email:        string;
  fullName:     string;
  roles:        string[];
}
