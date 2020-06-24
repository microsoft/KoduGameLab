using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoiX
{
    /// <summary>
    /// Interface for things with a dirty flag.
    /// Yes this seems kind of dumb but I think it's the only way to get
    /// the Twitchable class to work right.
    /// </summary>
    public interface IDirty
    {
        bool Dirty
        {
            get;
            set;
        }
    }
}
