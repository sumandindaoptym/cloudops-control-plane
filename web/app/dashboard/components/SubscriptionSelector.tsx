'use client';

import { useState, useEffect } from 'react';

interface Subscription {
  id: string;
  name: string;
  subscriptionId: string;
  tenantId: string;
}

export default function SubscriptionSelector() {
  const [subscriptions, setSubscriptions] = useState<Subscription[]>([]);
  const [selectedSubscription, setSelectedSubscription] = useState<string>('');
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const fetchSubscriptions = async () => {
      try {
        // Get the user's session to retrieve access token
        const sessionResponse = await fetch('/api/auth/session');
        const session = await sessionResponse.json();
        
        if (!session?.accessToken) {
          throw new Error('No access token available');
        }
        
        // Fetch subscriptions with user's access token
        const response = await fetch('/api/azure/subscriptions', {
          headers: {
            'Authorization': `Bearer ${session.accessToken}`
          }
        });
        
        if (!response.ok) {
          throw new Error('Failed to fetch subscriptions');
        }
        
        const data: Subscription[] = await response.json();
        setSubscriptions(data);
        
        // Check if there's a saved subscription in localStorage
        const saved = localStorage.getItem('selectedSubscription');
        if (saved) {
          const savedSub = JSON.parse(saved);
          const exists = data.find(s => s.subscriptionId === savedSub.subscriptionId);
          if (exists) {
            setSelectedSubscription(exists.id);
            setIsLoading(false);
            return;
          }
        }
        
        // Otherwise, set first subscription as default
        if (data.length > 0) {
          setSelectedSubscription(data[0].id);
        }
      } catch (error) {
        console.error('Error fetching Azure subscriptions:', error);
        // Fall back to showing error state or empty
      } finally {
        setIsLoading(false);
      }
    };

    fetchSubscriptions();
  }, []);

  const handleSubscriptionChange = (subscriptionId: string) => {
    setSelectedSubscription(subscriptionId);
    const subscription = subscriptions.find(sub => sub.id === subscriptionId);
    
    if (subscription) {
      // TODO: Make API call to set Azure context
      console.log('Setting Azure context to:', subscription);
      
      // Store in localStorage for persistence
      localStorage.setItem('selectedSubscription', JSON.stringify(subscription));
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center gap-3">
        <svg className="w-5 h-5 animate-spin" style={{ color: 'var(--primary)' }} viewBox="0 0 24 24" fill="none">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <span className="text-sm" style={{ color: 'var(--muted-foreground)' }}>Loading subscriptions...</span>
      </div>
    );
  }

  return (
    <div className="flex items-center gap-3">
      <svg className="w-5 h-5" style={{ color: 'var(--primary)' }} fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 15a4 4 0 004 4h9a5 5 0 10-.1-9.999 5.002 5.002 0 10-9.78 2.096A4.001 4.001 0 003 15z" />
      </svg>
      
      <div className="flex items-center gap-2">
        <label htmlFor="subscription" className="text-sm font-medium" style={{ color: 'var(--foreground)' }}>
          Azure Subscription:
        </label>
        <select
          id="subscription"
          value={selectedSubscription}
          onChange={(e) => handleSubscriptionChange(e.target.value)}
          className="px-3 py-1.5 rounded-lg text-sm font-medium transition-colors cursor-pointer"
          style={{
            backgroundColor: 'var(--card)',
            color: 'var(--foreground)',
            border: '1px solid var(--border)'
          }}
        >
          {subscriptions.map((sub) => (
            <option key={sub.id} value={sub.id}>
              {sub.name}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
}
