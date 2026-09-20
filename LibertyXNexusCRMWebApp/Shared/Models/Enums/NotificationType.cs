using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Models.Enums
{
    /// <summary>
    /// Category of notification, used to drive display and filtering
    /// </summary>
    public enum NotificationType
    {
        DocumentUploaded,
        QueryReceived,
        QueryResponded,
        MeetingBooked,
        MeetingConfirmed,
        MeetingCancelled,
        CaseUpdated,
        ClientRegistered,
        PendingAction
    }
}
