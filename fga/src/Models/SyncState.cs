namespace ODataFga.Models;

/// <summary>
/// The SyncState class represents the synchronization state of a particular entity or process in the system. It 
/// contains a key that identifies the specific entity or process being synchronized, and a timestamp indicating 
/// the last time the synchronization occurred.
/// </summary>
public class SyncState
{
    /// <summary>
    /// Gets or sets the unique identifier associated with the item.
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// Gets or sets the date and time when the last synchronization occurred.
    /// </summary>
    public DateTime? LastSyncTime { get; set; }
}