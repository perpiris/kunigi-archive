using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Domain.Common;

namespace KunigiArchive.Application.Teams;

public sealed class SlugAlreadyTakenException(Slug slug)
    : ConflictException($"Η διεύθυνση '{slug}' χρησιμοποιείται ήδη από άλλη ομάδα.");