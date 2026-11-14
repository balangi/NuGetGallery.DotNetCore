namespace NuGetGallery;

/// <summary>
/// Thrown when valid user information is given to us, but the user information refers to a defunct account.
/// </summary>
public class CurrentUserDeletedException : ArgumentException
{
    public CurrentUserDeletedException()
        : base(message: null)
    { }

    public CurrentUserDeletedException(string message)
        : base(message)
    { }
}
