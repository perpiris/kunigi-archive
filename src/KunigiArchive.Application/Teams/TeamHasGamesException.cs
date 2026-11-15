using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Teams;

public sealed class TeamHasGamesException(string teamName)
    : ConflictException($"Η ομάδα '{teamName}' δεν μπορεί να διαγραφεί γιατί έχει διοργανώσει ή κερδίσει παιχνίδια.");
