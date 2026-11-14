using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Users;

public sealed class UserIsNotManagerException()
    : ConflictException("Ο χρήστης δεν υπάρχει ή δεν έχει τον ρόλο διαχειριστή ομάδας.");