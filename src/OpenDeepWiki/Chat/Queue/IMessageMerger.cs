using OpenDeepWiki.Chat.Abstractions;

namespace OpenDeepWiki.Chat.Queue;

/// <summary>
/// Message merger interface
/// Merges multiple short messages into one
/// </summary>
public interface IMessageMerger
{
    /// <summary>
    /// Try to merge messages
    /// </summary>
    /// <param name="messages">List of messages to merge</param>
    /// <returns>Merge result; returns the original messages if they cannot be merged</returns>
    MergeResult TryMerge(IReadOnlyList<IChatMessage> messages);
    
    /// <summary>
    /// Check whether messages can be merged
    /// </summary>
    /// <param name="messages">List of messages to check</param>
    /// <returns>Whether the messages can be merged</returns>
    bool CanMerge(IReadOnlyList<IChatMessage> messages);
}

/// <summary>
/// Merge result
/// </summary>
/// <param name="WasMerged">Whether a merge was performed</param>
/// <param name="Messages">Resulting list of messages</param>
public record MergeResult(bool WasMerged, IReadOnlyList<IChatMessage> Messages);
