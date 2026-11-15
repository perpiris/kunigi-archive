using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Games;

public sealed class GameOrderAlreadyTakenException(string orderTitle)
    : ConflictException($"Υπάρχει ήδη το {orderTitle}.");