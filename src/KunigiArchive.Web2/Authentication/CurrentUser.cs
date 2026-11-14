using System.Security.Claims;
using KunigiArchive.Application.Abstractions;

namespace KunigiArchive.Web2.Authentication;

// Pages render with static SSR, so there is always an HttpContext with the signed-in user.
internal sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public Guid? Id => Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : null;

    public bool IsInRole(string role)
    {
        return Principal?.IsInRole(role) ?? false;
    }
}