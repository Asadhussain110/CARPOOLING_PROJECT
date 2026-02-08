using System;

namespace CARPOOLING_PROJECT.Models
{
    /// <summary>
    /// Partial extension for the EF generated <see cref="User"/> entity.
    /// Keeps custom logic separate from the auto-generated file.
    /// </summary>
    public partial class User
    {
        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Name))
            {
                return Name;
            }

            if (!string.IsNullOrWhiteSpace(Email))
            {
                return Email;
            }

            return base.ToString();
        }
    }
}

