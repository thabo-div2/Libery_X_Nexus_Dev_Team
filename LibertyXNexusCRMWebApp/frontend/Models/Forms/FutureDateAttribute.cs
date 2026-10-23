using System.ComponentModel.DataAnnotations;

namespace frontend.Models.Forms
{
    /// <summary>
    /// Makes sure a date and time is later than right now (used for booking meetings).
    /// </summary>
    public sealed class FutureDateAttribute : ValidationAttribute
    {
        public FutureDateAttribute()
            : base("Pick a date and time in the future.")
        {
        }

        public override bool IsValid(object? value)
        {
            // Leave empty values to [Required].
            if (value is null)
            {
                return true;
            }

            return value is DateTime date && date > DateTime.Now;
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
