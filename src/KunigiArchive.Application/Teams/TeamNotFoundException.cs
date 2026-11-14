using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Teams;

public sealed class TeamNotFoundException(string slug)
    : NotFoundException($"Δεν βρέθηκε ομάδα '{slug}'.");