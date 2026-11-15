using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Games;

public sealed class GameYearAlreadyTakenException(short year)
    : ConflictException($"Υπάρχει ήδη παιχνίδι για το {year}.");