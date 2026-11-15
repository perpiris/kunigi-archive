using KunigiArchive.Application.Abstractions.Exceptions;
using KunigiArchive.Domain.Common;

namespace KunigiArchive.Application.Games;

public sealed class GameSlugAlreadyTakenException(Slug slug)
    : ConflictException($"Η διεύθυνση '{slug}' χρησιμοποιείται ήδη από άλλο παιχνίδι.");