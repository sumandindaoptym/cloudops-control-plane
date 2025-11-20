using CloudOps.Api.Models;

namespace CloudOps.Api.Services;

public interface IServiceBusRuntimeService
{
    Task<DlqCountResponse> GetDlqCountAsync(DlqCountRequest request, string accessToken);
}
