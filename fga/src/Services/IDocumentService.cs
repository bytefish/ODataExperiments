using ODataFga.Dtos;
using ODataFga.Models;

namespace ODataFga.Services;

/// <summary>
/// Defines methods for creating, moving, and deleting documents asynchronously within a document management system.
/// </summary>
/// <remarks>Implementations of this interface should ensure thread safety for concurrent operations. All methods
/// perform asynchronous operations and return tasks that complete when the requested action is finished. Callers should
/// validate input parameters to avoid errors related to invalid or missing document identifiers or request
/// objects.</remarks>
public interface IDocumentService
{
    /// <summary>
    /// Asynchronously creates a new document based on the specified request parameters.
    /// </summary>
    /// <param name="request">The details of the document to create, including content and metadata. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created document.</returns>
    Task<Document> CreateDocumentAsync(CreateDocumentRequest request);

    /// <summary>
    /// Moves the specified document to a new location or collection asynchronously.
    /// </summary>
    /// <remarks>If the document does not exist or the move operation fails due to invalid parameters, the
    /// task may complete with an error. Ensure that the target location specified in the request is valid and
    /// accessible.</remarks>
    /// <param name="key">The unique identifier of the document to move. Cannot be null or empty.</param>
    /// <param name="request">An object containing the details of the move operation, including the target location and any additional
    /// options. Cannot be null.</param>
    /// <returns>A task that represents the asynchronous move operation.</returns>
    Task MoveDocumentAsync(string key, MoveDocumentRequest request);

    /// <summary>
    /// Asynchronously deletes the document identified by the specified key.
    /// </summary>
    /// <param name="key">The unique identifier of the document to delete. Cannot be null or empty.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteDocumentAsync(string key);
}