using Microsoft.AspNetCore.SignalR;

namespace CloudOps.Web.Hubs;

public class PurgeHub : Hub
{
    public async Task JoinPurgeGroup(string purgeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, purgeId);
    }

    public async Task LeavePurgeGroup(string purgeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, purgeId);
    }
}
