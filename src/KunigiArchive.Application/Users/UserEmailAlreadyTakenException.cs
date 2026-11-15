using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Users;

public sealed class UserEmailAlreadyTakenException(string email)
    : ConflictException($"Υπάρχει ήδη χρήστης με το email '{email}'.");
