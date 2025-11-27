using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudOps.Web.Pages.Dashboard.Agents;

[Authorize]
public class PoolsModel : PageModel
{
    public void OnGet()
    {
    }
}
