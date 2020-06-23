using System;
using System.Collections.Generic;
using System.Text;

using Boku.Base;
using Boku.Input;

namespace Boku.UI
{
    public delegate void ControlEvent( object sender );

    public interface IControl
    {
        void AddCommands(CommandMap map);
        void RemoveCommands(CommandMap map);
        bool Hot
        {
            get;
            set;
        }
        bool Disabled
        {
            get;
            set;
        }
    }
}
