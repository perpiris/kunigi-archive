using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Games;

public sealed class GameNotFoundException(string game)
    : NotFoundException($"Δεν βρέθηκε το παιχνίδι '{game}'.");