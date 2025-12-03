using CloudOps.Web.Models;

namespace CloudOps.Web.Services;

public interface IServiceBusRuntimeService
{
    Task<DlqCountResponse> GetDlqCountAsync(DlqCountRequest request, string accessToken);
    Task<PurgeResult> PurgeDlqAsync(PurgeRequest request, string accessToken, Action<PurgeProgress>? onProgress = null);
    Task<PurgeResult> PurgeDlqWithProgressAsync(PurgeRequest request, string accessToken, Func<string, int, Task> onProgress, CancellationToken cancellationToken = default);
    Task<PurgeResult> PurgeDlqFastAsync(PurgeRequest request, string accessToken, Func<string, int, int, Task> onProgress, CancellationToken cancellationToken = default);
}
