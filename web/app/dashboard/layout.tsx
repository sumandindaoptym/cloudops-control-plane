import { redirect } from 'next/navigation';
import { auth } from '@/auth';
import DashboardNav from './DashboardNav';
import Sidebar from './components/Sidebar';

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
      <div className="flex">
        <Sidebar />
        <main className="flex-1 min-h-screen">{children}</main>
      </div>
    </div>
  );
}
