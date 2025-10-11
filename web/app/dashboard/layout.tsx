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
    <div className="min-h-screen bg-slate-900">
      <DashboardNav user={session.user} />
      <div className="flex">
        <Sidebar />
        <main className="flex-1 min-h-screen">{children}</main>
      </div>
    </div>
  );
}
