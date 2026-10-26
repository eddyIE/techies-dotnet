namespace Techies.Web.Domain;

// A business rule was violated. The message is Vietnamese and safe to show to the user.
public sealed class DomainException(string message) : Exception(message);
