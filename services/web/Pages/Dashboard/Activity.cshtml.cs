using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudOps.Web.Pages.Dashboard;

[Authorize]
public class ActivityModel : PageModel
{
    public void OnGet()
    {
    }
}
