using CloudOps.Web.Models;

namespace CloudOps.Web.Services;

public interface IServiceBusRuntimeService
{
    Task<DlqCountResponse> GetDlqCountAsync(DlqCountRequest request, string accessToken);
    Task<PurgeResult> PurgeDlqAsync(PurgeRequest request, string accessToken, Action<PurgeProgress>? onProgress = null);
}
