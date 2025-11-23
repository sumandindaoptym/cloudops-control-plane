using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudOps.Web.Pages;

[AllowAnonymous]
public class GoodByeModel : PageModel
{
    public void OnGet()
    {
    }
}
