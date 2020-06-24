using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoiX.UI
{
    /// <summary>
    /// State of UI element.
    /// Note that these states are mutually exclusive.
    /// 
    /// Focused and Hover are treated as orthogonal to the states.  Since
    /// disabled widgets don't react to any input this gives us a total
    /// of 7 possible combinations that we need to worry about:
    /// 
    /// Disabled
    /// 
    /// Normal
    /// NormalFocused
    /// NormalHover
    /// 
    /// Selected
    /// SelectedFocused
    /// SelectedHover
    /// 
    /// Focus is used by both gamepad and keyboard input.  The "in focus" widget is
    /// the one that will receive input.
    /// 
    /// Hover is only valid for mouse input.
    /// 
    /// Selected may not be valid for all widgets.  Primarily is is used for buttons.
    /// the Selected state is the same as Pressed.
    /// 
    /// </summary>
    [Flags]
    public enum UIState
    {
        None = 0x00,            // Should be invalid.

        // These next three are the state proper.  They are mutually exclusive.
        Inactive = 0x01,        // When elements are not being used.
        Disabled = 0x02,        // Greyed out.  Visible but doesn't react to input.
        Active = 0x04,          // Rendering and accepting input.


        // The following should never be part of teh state value.  They
        // are strictly to be used to generate unique value to use in
        // switch statements.
        Selected = 0x10,        // Includes 'pressed'.  For a press button this implies focused but for a radio button it doesn't.
        Focused = 0x20,         // Tab or GamePad focus.
        Hover = 0x40,           // Only valid for mouse UI.

        DisabledSelected = Disabled | Selected,

        ActiveSelected = Active | Selected,
        ActiveFocused = Active | Focused,
        ActiveHover = Active | Hover,
        ActiveFocusedHover = Active | Focused | Hover,
        ActiveSelectedFocused = Active | Selected | Focused,
        ActiveSelectedHover = Active | Selected | Hover,
        ActiveSelectedFocusedHover = Active | Selected | Focused | Hover,
    }

    /// <summary>
    /// Used to describe the packing of widgets inside a WidgetSet.
    /// </summary>
    public enum Justification
    {
        Top,
        Bottom,
        Left,
        Right,
        Center, // Packed into center.
        Full,   // Spread across full width (or height).
    }

    /// <summary>
    /// Used to describe the orientation of members of a WidgetSet.
    /// </summary>
    public enum Orientation
    {
        None,       // Use for freeform layout.
        Horizontal,
        Vertical,
    }

    /// <summary>
    /// Mostly just a namespace for UI stuff.
    /// </summary>
    public static class UIStuff
    {
    }   // end of class UIStuff
}   // end of namespace KoiX.UI
