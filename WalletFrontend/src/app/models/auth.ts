export interface LoginDto {
    email: string;
    password: string;
}

export interface AuthResponseDto {
    token: string;
    expiresAt: string;   // ISO string from backend
}