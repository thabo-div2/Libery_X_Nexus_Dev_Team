using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Models.Enums
{
    /// <summary>
    /// Distinguishes which role performed an audited action, since both
    /// clients and the advisor can trigger auditable changes
    /// </summary>
    public enum UserRole
    {
        ProspectiveClient,
        RegisteredClient,
        FinancialAdviser
    }
}
