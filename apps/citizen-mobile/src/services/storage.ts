import AsyncStorage from '@react-native-async-storage/async-storage';
import type { CitizenUser } from './api';

const ACCESS_KEY = 'citizen_access_token';
const USER_KEY = 'citizen_user';

export type StoredSession = { token: string; user: CitizenUser } | null;

export async function loadSession(): Promise<StoredSession> {
  try {
    const [token, rawUser] = await Promise.all([
      AsyncStorage.getItem(ACCESS_KEY),
      AsyncStorage.getItem(USER_KEY)
    ]);
    if (!token || !rawUser) return null;
    return { token, user: JSON.parse(rawUser) as CitizenUser };
  } catch {
    return null;
  }
}

export async function saveSession(token: string, user: CitizenUser): Promise<void> {
  await AsyncStorage.multiSet([[ACCESS_KEY, token], [USER_KEY, JSON.stringify(user)]]);
}

export async function clearSession(): Promise<void> {
  await AsyncStorage.multiRemove([ACCESS_KEY, USER_KEY]);
}
