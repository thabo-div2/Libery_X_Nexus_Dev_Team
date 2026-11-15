namespace API.Services.Interfaces
{
    /// <summary>
    /// Creates follow-up tasks for advisors automatically when something happens that needs their attention.
    /// Each event creates at most one task, and a task the advisor already completed is never reopened.
    /// </summary>
    public interface ITaskAutomationService
    {
        /// <summary>A document was marked as needing replacement: follow up with the client in three days.</summary>
        Task DocumentNeedsReplacementAsync(int documentId);

        /// <summary>
        /// Keeps the "prepare for meeting" task in step with the meeting: created when it is confirmed,
        /// moved when it is rescheduled, removed when it is cancelled.
        /// </summary>
        Task SyncMeetingTaskAsync(int meetingId);

        /// <summary>Creates a "review renewal" task for every policy that ends within the next 30 days.</summary>
        Task<int> EnsureRenewalTasksAsync(CancellationToken cancellationToken = default);
    }
}
