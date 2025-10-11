import { redirect } from 'next/navigation';
import { auth } from '@/auth';
import DashboardNav from './DashboardNav';

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session?.user) {
    redirect('/');
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-blue-950 to-slate-950">
      <DashboardNav user={session.user} />
      <main>{children}</main>
    </div>
  );
}
