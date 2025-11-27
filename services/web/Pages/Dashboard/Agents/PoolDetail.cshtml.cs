using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudOps.Web.Pages.Dashboard.Agents;

[Authorize]
public class PoolDetailModel : PageModel
{
    public void OnGet()
    {
    }
}
