using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Posts;

public sealed class PostNotFoundException(Guid id)
    : NotFoundException($"Δεν βρέθηκε η ανάρτηση '{id}'.");