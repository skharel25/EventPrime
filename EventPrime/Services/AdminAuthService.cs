namespace EventPrime.Services;

public class AdminAuthService
{
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
