namespace KunigiArchive.Infrastructure.Email;

// An email waiting in EmailQueue. Kind names it in the logs; the address and the body (it can hold a temporary
// password) are never logged. Attempt counts the failed sends so far.
internal sealed record QueuedEmail(Guid Id, string Kind, string To, string Subject, string Body, int Attempt = 0);
