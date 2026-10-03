using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Models.Enums
{
    /// <summary>
    /// State of booked meeting between a client and the advisor
    /// </summary>
    public enum MeetingStatus
    {
        Requested,
        Confirmed,
        Completed,
        Cancelled,
        Rescheduled
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
