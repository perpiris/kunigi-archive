namespace KunigiArchive.Application.Abstractions;

public interface ICurrentUser
{
    Guid? Id { get; }
    bool IsInRole(string role);
}