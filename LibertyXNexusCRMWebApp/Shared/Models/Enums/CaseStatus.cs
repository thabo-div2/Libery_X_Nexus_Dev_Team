using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Models.Enums
{
    /// <summary>
    /// Case/workflow status for a policy, per the case management requirement
    /// </summary>
    public enum CaseStatus
    {
        InProgress,
        AwaitingApproval,
        Completed,
        OnHold
    }
}
