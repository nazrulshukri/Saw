using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Acms.Web.Pages;

[IgnoreAntiforgeryToken]
public class StatusCodeModel : PageModel
{
    public string Title { get; private set; } = "Something went wrong";
    public string Explanation { get; private set; } = string.Empty;

    public void OnGet(int? code) => Describe(code);

    public void OnPost(int? code) => Describe(code);

    private void Describe(int? code)
    {
        (Title, Explanation) = code switch
        {
            401 or 403 => ("Access denied",
                "Your Windows account is not in an ACMS Active Directory group, or that group does not allow this action. "
                + "Ask your ACMS administrator for the Viewer, Engineer or Administrator role."),
            404 => ("Not found", "The page or item you asked for does not exist."),
            _ => ("Something went wrong", $"The request failed with status {code}."),
        };
    }
}
