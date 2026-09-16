using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Models.Enums
{
    /// <summary>
    /// Status of a policy held by a client
    /// </summary>
    public enum PolicyStatus
    {
        Pending,
        Active,
        Lapsed,
        Cancelled,
        Matured
    }
}
