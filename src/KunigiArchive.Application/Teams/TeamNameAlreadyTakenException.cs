using KunigiArchive.Application.Abstractions.Exceptions;

namespace KunigiArchive.Application.Teams;

public sealed class TeamNameAlreadyTakenException(string teamName)
    : ConflictException($"Υπάρχει ήδη ομάδα με το όνομα '{teamName}'");