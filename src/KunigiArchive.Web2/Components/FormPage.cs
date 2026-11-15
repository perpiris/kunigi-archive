using Microsoft.AspNetCore.Components;

namespace KunigiArchive.Web2.Components;

// Base for pages with forms. A successful submit leaves a one-time alert (Flash) and redirects, like the old
// app: to the parent page after a form, or back to the same page after an action on it (uploads, deletes,
// managers). The redirect also means a refresh never submits the form again. Errors stay on the page.
public abstract class FormPage : ComponentBase
{
    [CascadingParameter]
    protected HttpContext HttpContext { get; set; } = null!;

    [Inject]
    protected NavigationManager Navigation { get; set; } = null!;

    protected void Done(string message, string url)
    {
        Flash.Success(HttpContext, message);
        Navigation.NavigateTo(url);
    }

    protected void Reload(string message)
    {
        Done(message, Navigation.Uri);
    }
}
