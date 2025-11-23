using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudOps.Web.Areas.MicrosoftIdentity.Pages.Account;

[AllowAnonymous]
public class SignedOutModel : PageModel
{
    public void OnGet()
    {
    }
}
