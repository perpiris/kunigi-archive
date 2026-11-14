namespace KunigiArchive.Application.Abstractions.Exceptions;

// Input the use case rejects before it reaches the domain, e.g. an unsupported file type.
public sealed class InvalidRequestException(string message) : Exception(message);