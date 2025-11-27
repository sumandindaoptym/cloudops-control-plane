using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudOps.Web.Pages.Dashboard;

[Authorize]
public class AgentDownloadsModel : PageModel
{
    public void OnGet()
    {
    }
}
