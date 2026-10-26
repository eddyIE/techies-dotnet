namespace Techies.Web.Infrastructure;

public static class Emails
{
    // Emails are stored lower-cased so uniqueness and login are case-insensitive.
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
