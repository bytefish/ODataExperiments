using System.ComponentModel.DataAnnotations;

namespace ODataFga.Dtos
{
    /// <summary>
    /// Represents a request to share a document with a specified user, defining the user's access level.
    /// </summary>
    /// <remarks>The TargetUserId property must be provided to specify the user with whom the document is
    /// being shared. The Relation property determines the level of access granted to the user, defaulting to 'viewer'
    /// if not specified.</remarks>
    public class ShareDocumentRequest 
    { 
        /// <summary>
        /// Gets or sets the unique identifier for the target user.
        /// </summary>
        /// <remarks>This property is required and must not be null or empty. It is used to specify the
        /// user to whom an action or message is directed.</remarks>
        [Required] public string TargetUserId { get; set; } = "";

        /// <summary>
        /// Gets or sets the relationship type that defines the level of access or interaction the target 
        /// user has with the document.
        /// </summary>
        [Required] public string Relation { get; set; } = "viewer"; }
}
