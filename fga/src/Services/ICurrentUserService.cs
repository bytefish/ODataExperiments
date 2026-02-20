namespace ODataFga.Services;

/// <summary>
/// Used to get the current user and their required permission mask from the request context. This is used 
/// by the authorization handler to check if the user has the required permissions for a given document.
/// </summary>
public interface ICurrentUserService 
{
    /// <summary>
    /// User ID extracted from the request context. This should be in the format "user:{id}" to 
    /// match the user format used in OpenFGA. If the user is not authenticated, this 
    /// will return null.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// The Required Permission Mask is an integer that represents the permissions required for the current request.
    /// </summary>
    int RequiredMask { get; } 
}
