export interface UserDto {
  userId: number;
  name: string;
  email: string;
}

export interface UpdateUserRequest {
  name?: string | null;
  email?: string | null;
  currentPassword?: string | null;
  newPassword?: string | null;
}
