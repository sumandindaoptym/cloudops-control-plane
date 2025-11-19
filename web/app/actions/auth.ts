'use server';

import { signIn } from '@/auth';

export async function handleSignIn() {
  await signIn('azure-ad', { redirectTo: '/dashboard' });
}
