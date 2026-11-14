namespace KunigiArchive.Application.Abstractions.Exceptions;

public abstract class ConflictException(string message) : Exception(message);