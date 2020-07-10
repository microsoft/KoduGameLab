// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
//using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// We want to get display resolutions without relying on XNA to report them, so we're
// going "old school" by calling into the native User32 API "EnumDisplaySettings"
// Here's the struct used by User32 to report display modes
[StructLayout(LayoutKind.Sequential)]
public struct DEVMODE1
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string dmDeviceName;
    public short dmSpecVersion;
    public short dmDriverVersion;
    public short dmSize;
    public short dmDriverExtra;
    public int dmFields;

    public short dmOrientation;
    public short dmPaperSize;
    public short dmPaperLength;
    public short dmPaperWidth;

    public short dmScale;
    public short dmCopies;
    public short dmDefaultSource;
    public short dmPrintQuality;
    public short dmColor;
    public short dmDuplex;
    public short dmYResolution;
    public short dmTTOption;
    public short dmCollate;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string dmFormName;
    public short dmLogPixels;
    public short dmBitsPerPel;
    public int dmPelsWidth;
    public int dmPelsHeight;

    public int dmDisplayFlags;
    public int dmDisplayFrequency;

    public int dmICMMethod;
    public int dmICMIntent;
    public int dmMediaType;
    public int dmDitherType;
    public int dmReserved1;
    public int dmReserved2;

    public int dmPanningWidth;
    public int dmPanningHeight;
};



class User_32
{
    [DllImport("user32.dll")]
    public static extern int EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE1 devMode);

    // special value to query for the current display settings.
    public const int ENUM_CURRENT_SETTINGS = -1;
}


namespace BokuPreBoot
{
    // A simple class to encapsulate a display resolution. Currently only holds X & Y because
    // that's all we care about; we could reference frequency, etc.
    // Currently we don't care about anything but size since we use XNA to actually change
    // display settings. This is really just for surfacing what the current monitor can do
    // and letting the user pick.
    // It's a class rather than a struct so that it's nullable, simplifying readability.
    public class DisplayResolution
    {
        public int X;
        public int Y;
        public bool widescreen = false;

        public DisplayResolution(int x, int y)
        {
            X = x;
            Y = y;

            widescreen = ((float)X / (float)Y) > 1.34f;
        }

        public override string ToString()
        {
            string str = X.ToString() + " X " + Y.ToString();
            if (widescreen)
            {
                str += " (widescreen)";
            }
            return str;
        }
    }
    
    class DisplayInspector
    {
        // Return the resolution of the current display device.
        // Returns null if the device was for some reason unreadable.
        static public DisplayResolution CurrentResolution()
        {
            DEVMODE1 dm = new DEVMODE1();
            dm.dmDeviceName = new String(new char[32]);
            dm.dmFormName = new String(new char[32]);
            dm.dmSize = (short)Marshal.SizeOf(dm);

            DisplayResolution result = null;

            if (0 != User_32.EnumDisplaySettings(null, User_32.ENUM_CURRENT_SETTINGS, ref dm))
            {
                result = new DisplayResolution(dm.dmPelsWidth, dm.dmPelsHeight);
            }

            return result;
        }

        // Return a list of all 32-bit display resolutions supported by the CURRENT display.
        static public List<DisplayResolution> Get32BitModes()
        {
            List<DisplayResolution> result = new List<DisplayResolution>();

            DEVMODE1 dm = new DEVMODE1();
            dm.dmDeviceName = new String(new char[32]);
            dm.dmFormName = new String(new char[32]);
            dm.dmSize = (short)Marshal.SizeOf(dm);

            // let's look at all the available modes of the given device, starting at zero
            int iModeNum = 0;
            // we're stripping out redundant modes (i.e. 640 x 480 60hz, 640 x 480 72hz)
            int lastX = 0, lastY = 0;

            while (0 != User_32.EnumDisplaySettings(null, iModeNum++, ref dm))
            {
                int curX = dm.dmPelsWidth;
                int curY = dm.dmPelsHeight;
                // modes are sorted first by pixel depth. within those groups they're sorted by resolution.
                if (dm.dmBitsPerPel == 32)
                {
                    if (curX != lastX || curY != lastY)
                    {
                        result.Add(new DisplayResolution(curX, curY));

                        lastX = curX;
                        lastY = curY;
                    }
                }  // if
            }  // while

            return result;
        } // Get32BitModes

        static public void SortByAspectRatio(List<DisplayResolution> list)
        {
            // Sort to match aspect ratio of current screen resolution.
            DisplayResolution res = CurrentResolution();

            if (res.widescreen)
            {
                // Bubble non-widescreen options to top of list.
                for (int i = 0; i < list.Count - 1; i++)
                {
                    for (int j = 0; j < list.Count - 1; j++)
                    {
                        if (!list[j].widescreen && list[j + 1].widescreen)
                        {
                            // Swap
                            DisplayResolution tmp = list[j];
                            list[j] = list[j + 1];
                            list[j + 1] = tmp;
                        }
                    }
                }
            }
            else
            {
                // Bubble widescreen options to top of list.
                for (int i = 0; i < list.Count - 1; i++)
                {
                    for (int j = i; j < list.Count - 1; j++)
                    {
                        if (list[j].widescreen && !list[j + 1].widescreen)
                        {
                            // Swap
                            DisplayResolution tmp = list[j];
                            list[j] = list[j + 1];
                            list[j + 1] = tmp;
                        }
                    }
                }
            }
        }   // end of SortByAspectRatio()

    }
}
