namespace EventPrime.Services;

/// <summary>
/// Temporary mock authentication service for admin access.
/// Replace with a proper API-backed implementation once the login endpoint is available.
/// </summary>
public class AdminAuthService
{
    // TODO: Replace with API-backed authentication when the login endpoint is ready.
    // Use .NET user-secrets or environment variables for credentials in the interim.
    private const string MockEmail = "admin@eventprime.com";
    private const string MockPassword = "admin123";

    public bool IsAuthenticated { get; private set; }

    public bool Login(string email, string password)
    {
        if (string.Equals(email, MockEmail, StringComparison.OrdinalIgnoreCase)
            && password == MockPassword)
        {
            IsAuthenticated = true;
            return true;
        }
        return false;
    }

    public void Logout()
    {
        IsAuthenticated = false;
    }
}
