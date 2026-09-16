using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Models.Enums
{
    /// <summary>
    /// Lifecycle state of a client
    /// </summary>
    public enum ClientStatus
    {
        Invited,
        Registered,
        Active,
        Dormant,
        Offboarded
    }
}
