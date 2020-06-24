using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

#if !NETFX_CORE
using System.Management;
using System.IO.Ports;
using Microsoft.Win32.SafeHandles;
#endif

using KoiX;
using KoiX.Input;

using Boku.Common;
using Boku.Common.Xml;
using Boku.Programming;

#if !NETFX_CORE
using MicrobitNeedDriverDlg;
#endif

#if !NETFX_CORE
namespace Boku.Input
{
    /// <summary>
    /// Brief descriptor of a connected microbit device.
    /// </summary>
    public class MicrobitDesc
    {
        public string COM;
        public string Drive;
        public string PNPDeviceId;  // Short version.  Just the number at the end.
    }

    /// <summary>
    /// Singleton responsible for enumerating attached microbits.
    /// </summary>
    public static class MicrobitManager
    {
        public static ConcurrentDictionary<int, Microbit> Microbits = new ConcurrentDictionary<int, Microbit>();
        public static bool DriverInstalled = true;

        static MicrobitManager()
        {
        }

        /// <summary>
        /// Close and destroy all attached microbits.
        /// </summary>
        public static void ReleaseDevices()
        {
                // Dispose the current set of Microbits
            foreach (var microbit in Microbits.Values)
            {
                try
                {
                    microbit.Dispose();
                }
                catch { }
            }
            // Clear the array of Microbit objects.
            Microbits.Clear();
        }


        /// <summary>
        /// Returns a list of brief structures representing an attached microbit. 
        /// Struct contains information such as COM port and drive letter.
        /// </summary>
        /// <param name="microcontrollerId"></param>
        /// <param name="isFauxbit"></param>
        /// <returns></returns>
        private static List<MicrobitDesc> GetDeviceDesc()
        {
            List<MicrobitDesc> descs = new List<MicrobitDesc>();

            //
            // Find the COM ports.
            //
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(ManagementPath.DefaultPath.ToString(), "SELECT * FROM Win32_PnPEntity");

            // Note:  We're also using this opportuning to test for any Xbox controllers
            // on the system.  This way the system can display the correct icons depending
            // on whether the user has an Xbox 360 controller or an Xbox One controller.
            GamePadInput.Xbox360ControllerFound = false;
            GamePadInput.XboxOneControllerFound = false;

            foreach (ManagementObject queryObj in searcher.Get())
            {
                string caption = (string)queryObj["Caption"];

                if (string.Equals(caption, "Xbox 360 Controller for Windows", StringComparison.InvariantCultureIgnoreCase))
                {
                    GamePadInput.Xbox360ControllerFound = true;
                }

                if (string.Equals(caption, "Xbox Gaming Device", StringComparison.InvariantCultureIgnoreCase))
                {
                    GamePadInput.XboxOneControllerFound = true;
                }

                if (caption != null && caption.StartsWith("mbed Serial Port") && caption.Contains("(COM"))
                {
                    int start = caption.IndexOf("(COM");
                    int end = caption.IndexOf(")");
                    if (start < end)
                    {
                        string PNPDeviceId = (string)queryObj["PNPDeviceId"];

                        // Create a new desc.
                        MicrobitDesc desc = new MicrobitDesc();
                        descs.Add(desc);

                        desc.COM = caption.Substring(start + 1, end - start - 1);
                        int numIndex = PNPDeviceId.LastIndexOf("\\");
                        if (numIndex > 1)
                        {
                            desc.PNPDeviceId = PNPDeviceId.Substring(numIndex + 1);
                        }
                    }
                }
            }

            //
            // Find the drive letters that match up with COM ports we've already found.
            //
            ManagementScope scope = new ManagementScope(ManagementPath.DefaultPath);
            scope.Connect();

            string qstr = String.Empty;
            try
            {
                qstr = String.Format(@"Select * From Win32_PnPEntity where ClassGuid = '{{4d36e967-e325-11ce-bfc1-08002be10318}}'");
                using (var diskDeviceSearcher = new ManagementObjectSearcher(ManagementPath.DefaultPath.ToString(), qstr))
                using (var diskDeviceCollection = diskDeviceSearcher.Get())
                {
                    foreach (var diskDevice in diskDeviceCollection)
                    {
                        qstr = String.Format(@"Select * from Win32_DiskDrive");
                        using (var diskDriveSearcher = new ManagementObjectSearcher(ManagementPath.DefaultPath.ToString(), qstr))
                        using (var diskDriveCollection = diskDriveSearcher.Get())
                        {
                            foreach (var diskDrive in diskDriveCollection)
                            {
                                string Caption = (string)diskDrive["Caption"];

                                // If not an MDBED device, just skip.
                                if (!Caption.StartsWith("MBED"))
                                    continue;

                                string PNPDeviceID = (string)diskDrive["PNPDeviceID"];

                                string diskDrive_DeviceID = (string)diskDrive.GetPropertyValue("DeviceID");
                                UInt32 diskDrive_Index = (UInt32)diskDrive.GetPropertyValue("Index");
                                qstr = String.Format(@"Select * from Win32_DiskPartition where DeviceID like 'Disk #{0}%' and PrimaryPartition = True", diskDrive_Index);
                                using (var primaryPartitionSearcher = new ManagementObjectSearcher(ManagementPath.DefaultPath.ToString(), qstr))
                                using (var primaryPartitionCollection = primaryPartitionSearcher.Get())
                                {
                                    foreach (var primaryPartition in primaryPartitionCollection)
                                    {
                                        string primaryPartition_DeviceID = (string)primaryPartition.GetPropertyValue("DeviceID");
                                        qstr = String.Format(@"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{0}'}} WHERE AssocClass = Win32_LogicalDiskToPartition", primaryPartition_DeviceID);
                                        var query = new RelatedObjectQuery(qstr);
                                        using (var logicalDiskSearcher = new ManagementObjectSearcher(scope, query))
                                        using (var logicalDiskCollection = logicalDiskSearcher.Get())
                                        {
                                            foreach (var logicalDisk in logicalDiskCollection)
                                            {
                                                string logicalDisk_DeviceID = logicalDisk["DeviceID"].ToString();

                                                // Figure out which device this drive letter works with.  If we don't
                                                // find a matching desc that means we have a drive without a corresponding
                                                // port which means we need to install the mbed driver, so set the flag.
                                                bool found = false;
                                                foreach (MicrobitDesc desc in descs)
                                                {
                                                    if (PNPDeviceID.Contains(desc.PNPDeviceId))
                                                    {
                                                        desc.Drive = logicalDisk_DeviceID;
                                                        found = true;
                                                    }
                                                }
                                                if (!found)
                                                {
                                                    DriverInstalled = false;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                while (e != null)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    e = e.InnerException;
                }
                // Don't crash if WMI throws an exception.
                return null;
            }

            return descs;

        }   // end of GetDeviceDesc()

        /// <summary>
        /// Detect attached microbits.
        /// </summary>
        /// <param name="createDevices">Whether or not to open interfaces to attached microbits.</param>
        /// <returns>The number of attached microbits.</returns>
        public static int RefreshDevices(bool createDevices = true)
        {
            // If specified on the command line, do not scan for microbits.
            if (Program2.CmdLine.Exists("NoMicrobit"))
            {
                return 0;
            }

            int prevDeviceCount = Microbits.Count;

            ReleaseDevices();

#if false
            // Print the Caption and DeviceID of all connected PNP devices. This
            // is helpful when you need to discover what the exact device id is
            // for a particular device you want to be able to detect.
            {
                using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity"))
                using (var collection = searcher.Get())
                {
                    foreach (var device in collection)
                    {
                        string Caption = (string)device.GetPropertyValue("Caption");
                        string DeviceID = (string)device.GetPropertyValue("DeviceID");
                        System.Diagnostics.Debug.WriteLine(Caption + " ---- " + DeviceID);
                    }
                }
            }
#endif

            List<MicrobitDesc> microbitDescs = new List<MicrobitDesc>();

            // Detect microbits.
            microbitDescs = GetDeviceDesc();

            // Open interfaces to devices.
            if (createDevices)
            {
                if (DriverInstalled)
                {
                    if (microbitDescs != null && microbitDescs.Count > 0)
                    {
                        int microbitIndex = (int)GamePadSensor.PlayerId.One;
                        foreach (MicrobitDesc desc in microbitDescs)
                        {
                            Microbit microbit = Microbit.Create(desc);
                            if (microbit != null)
                            {
                                Microbits.TryAdd(microbitIndex++, microbit);
                            }
                        }
                    }
                }
                else
                {
                    // Do nothing here.  The main thread loop will notice that DirverInstalled is false
                    // and should put up the needed dialog there.  We can't do it here since the dialog
                    // can't be shown on a background thread.
                }
            }

            // If none were found, check if user tried the command line.
            if (microbitDescs.Count == 0 && Program2.MicrobitCmdLine != null)
            {
                if (Program2.MicrobitCmdLine.Length == 7)
                {
                    MicrobitDesc desc = new MicrobitDesc();
                    desc.COM = Program2.MicrobitCmdLine.Substring(0, 4);
                    desc.Drive = Program2.MicrobitCmdLine.Substring(5);
                    microbitDescs.Add(desc);
                }
            }

            int deviceCount = createDevices ? Microbits.Count : microbitDescs.Count;

            // If any microbits were detected, then permanently enable visibility of the microbit programming tiles.
            if (!XmlOptionsData.ShowMicrobitTiles && deviceCount > 0)
            {
                XmlOptionsData.ShowMicrobitTiles = true;
                Instrumentation.RecordEvent(Instrumentation.EventId.MicrobitTilesEnabled, "");
            }

            // Track the number of microbits attached at one time.
            if (prevDeviceCount < deviceCount)
            {
                Instrumentation.SetCounter(Instrumentation.CounterId.MicrobitCount, deviceCount);
            }

            return deviceCount;
        }

        /// <summary>
        /// A wrapper around RefreshDevices to allow it to be pushed off on a background thread.
        /// </summary>
        public static void RefreshWorker()
        {
            RefreshDevices();
        }

        public static void ShowDriverDialog()
        {
            DriverInstalled = true;

            var form = new MicrobitNeedDriverDlgForm();
            form.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            System.Windows.Forms.DialogResult dr = form.ShowModal(
                Strings.Localize("microbitNeedsDriverDlg.title"),
                Strings.Localize("microbitNeedsDriverDlg.message"),
                Strings.Localize("microbitNeedsDriverDlg.linkLabel"),
                Strings.Localize("microbitNeedsDriverDlg.cancelLabel"),
                Strings.Localize("microbitNeedsDriverDlg.installLabel"),
                MainForm.Instance);
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                string filename = Path.Combine(Storage4.TitleLocation, @"Content", @"Microbit", @"mbedWinSerial_16466.exe");
                Process proc = Process.Start(filename);

                // Busy loop while driver is loading.
                while (!proc.HasExited)
                {
                    System.Threading.Thread.Sleep(10);
                }

                // Refresh the list of attached microbits.
                {
                    System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(MicrobitManager.RefreshWorker));
                    t.Start();
                }
            }
        }   // end of ShowDriverDialog()

        /// <summary>
        /// Update each attached microbit.
        /// </summary>
        public static void Update()
        {
            foreach (var bit in Microbits.Values)
            {
                try
                {
                    bit.Update();
                }
                catch
                {
                    // Don't crash if a microbit experiences an exception while updating.
                }
            }
        }
    }
}
#endif


