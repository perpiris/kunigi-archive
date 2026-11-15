using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Users;

public sealed class UserNotFoundException(Guid userId)
    : NotFoundException($"Δεν βρέθηκε χρήστης '{userId}'.");
