'use client';

import { useRouter } from 'next/navigation';
import { useTransition } from 'react';

export function useLoader() {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();

  const startLoader = () => {
    if (typeof window !== 'undefined') {
      const event = new Event('startNavigationProgress');
      window.dispatchEvent(event);
    }
  };

  const stopLoader = () => {
    if (typeof window !== 'undefined') {
      const event = new Event('stopNavigationProgress');
      window.dispatchEvent(event);
    }
  };

  const withLoader = async (action: () => Promise<void> | void) => {
    startLoader();
    try {
      await action();
    } finally {
      stopLoader();
    }
  };

  return {
    startLoader,
    stopLoader,
    withLoader,
    isPending,
    startTransition,
  };
}
