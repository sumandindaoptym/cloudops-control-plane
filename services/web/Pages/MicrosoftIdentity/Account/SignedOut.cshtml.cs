using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudOps.Web.Pages.MicrosoftIdentity.Account;

[AllowAnonymous]
public class SignedOutModel : PageModel
{
    public IActionResult OnGet()
    {
        return Redirect("/GoodBye");
    }
}
