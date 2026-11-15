using KunigiArchive.Application.Users.CreateUser;
using KunigiArchive.Web2.Forms.Users;

namespace KunigiArchive.Web2.Mappers;

public static class UserFormMapper
{
    public static CreateUserCommand ToCommand(this CreateUserForm form)
    {
        return new CreateUserCommand(form.Email, form.Role);
    }
}
