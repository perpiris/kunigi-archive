using KunigiArchive.Application.Users.CreateUser;
using KunigiArchive.Web.Forms.Users;

namespace KunigiArchive.Web.Mappers;

public static class UserFormMapper
{
    public static CreateUserCommand ToCommand(this CreateUserForm form)
    {
        return new CreateUserCommand(form.Email, form.Role);
    }
}
