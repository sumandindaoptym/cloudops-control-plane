using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudOps.Web.Pages.Dashboard;

[Authorize]
public class HostedAgentsModel : PageModel
{
    public IActionResult OnGet()
    {
        return Redirect("/Dashboard/Agents/Pools");
    }
}
