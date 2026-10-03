using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Models.Enums
{
    /// <summary>
    /// Action recorded in the audit log
    /// </summary>
    public enum AuditActionType
    {
        Create,
        Update,
        Delete,
        Login,
        Logout,
        DocumentUpload,
        DocumentDownload,
        PermissionChange
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
