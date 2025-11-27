using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudOps.Web.Pages.Dashboard;

[Authorize]
public class HostedAgentsModel : PageModel
{
    public void OnGet()
    {
    }
}
