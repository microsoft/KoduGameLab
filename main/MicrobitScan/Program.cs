using System;
using System.Collections.Generic;
using System.Management;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.IO;

namespace MicrobitScan
{
    class Program
    {
        public class MicrobitDesc
        {
            public string COM;
            public string Drive;
        }

        static StringBuilder results = new StringBuilder();
        static ManagementScope scope;

        static void Main(string[] args)
        {
            try
            {
                results.AppendLine("Connecting to default management scope.");
                scope = new ManagementScope(ManagementPath.DefaultPath);
                scope.Connect();
                results.AppendLine("Management scope: " + scope.Path.Path);
            }
            catch (Exception e)
            {
                while (e != null)
                {
                    results.AppendLine(e.Message);
                    e = e.InnerException;
                }
            }

            // Print the Caption and DeviceID of all connected PNP devices. This
            // is helpful when you need to discover what the exact device id is
            // for a particular device you want to be able to detect.
            try
            {
                string qstr = @"Select * From Win32_PnPEntity";
                SelectQuery query = new SelectQuery(qstr);
                results.AppendLine("Executing query: " + query.QueryString);
                results.AppendLine("Query language is: " + query.QueryLanguage);
                using (var searcher = new ManagementObjectSearcher(scope, query))
                using (var collection = searcher.Get())
                {
                    foreach (var device in collection)
                    {
                        string Caption = (string)device.GetPropertyValue("Caption");
                        string DeviceID = (string)device.GetPropertyValue("DeviceID");
                        string Class = (string)device.GetPropertyValue("PNPClass");
                        results.AppendLine(Class + ": " + Caption + " ---- " + DeviceID);
                    }
                }
            }
            catch (Exception e)
            {
                while (e != null)
                {
                    results.AppendLine(e.Message);
                    e = e.InnerException;
                }
            }

            // Detect real microbits.
            try
            {
                List<string> microcontrollerIds = ScanForDevices("VEN_MBED", new string[] {"PROD_DAPLINK_VFS", "PROD_VFS"}, "MICROBIT", '#');
                results.AppendLine(String.Format("ScanForDevices: Found {0} microbits. Attempting to determine COM Port and Drive Letter for each...", microcontrollerIds.Count));
                foreach (string microcontrollerId in microcontrollerIds)
                {
                    MicrobitDesc desc = GetDeviceDesc(microcontrollerId);
                    if (desc != null)
                    {
                        results.AppendLine(String.Format("Found microbit at {0}, {1}", desc.COM, desc.Drive));
                    }
                }
                if (microcontrollerIds.Count == 0)
                {
                    results.AppendLine("Failed to determine COM Port and Drive Letter for the connected microbits.");
                }
            }
            catch (Exception e)
            {
                while (e != null)
                {
                    results.AppendLine(e.Message);
                    e = e.InnerException;
                }
            }

            string body = results.ToString();
            try
            {
                StreamWriter writer = File.CreateText("microbitscan.txt");
                writer.Write(body);
                writer.Close();
                System.Console.Out.WriteLine("Wrote results to file: microbitscan.txt");
            }
            catch
            {
                System.Console.Out.WriteLine("Failed to write results to file. Dumping to console instead...");
                System.Console.Out.Write(body);
            }
        }

        private static List<string> ScanForDevices(string vendorName, string[] productNames, string captionLike, char idSeparator)
        {
            List<string> microcontrollerIds = new List<string>();

            results.AppendLine(String.Format("Scanning for {0},{1},{2}", vendorName, string.Join(", ", productNames), captionLike));

            try
            {
                // Query for all the connected MBED microcontrollers.
                string qstr = @"Select * From Win32_PnPEntity where Caption like '" + captionLike + "'";
                SelectQuery query = new SelectQuery(qstr);
                results.AppendLine("Executing query: " + query.QueryString);
                using (var searcher = new ManagementObjectSearcher(scope, query))
                using (var collection = searcher.Get())
                {
                    results.AppendLine(String.Format("Found {0} PNP entities where Caption like '{1}'", collection.Count, captionLike));
                    foreach (ManagementObject device in collection)
                    {
                        string DeviceID = (string)device.GetPropertyValue("DeviceID");
                        //-----------------------------------------------------------
                        // DeviceID of an MBED microprocessor - placeholder development board until we get a Microbit to work with.
                        //  USBSTOR\\DISK&VEN_MBED&PROD_MICROCONTROLLER&REV_1.0\\1100021850323120363937383030343237313033ADD5DFD8&0
                        //  USBSTOR\\DISK
                        //  VEN_MBED
                        //  PROD_MICROCONTROLLER
                        //  REV_1.0\\1100021850323120363937383030343237313033ADD5DFD8
                        //  0
                        //-----------------------------------------------------------
                        // DeviceID of a BBC micro:bit
                        // Caption:
                        //  MICROBIT
                        // DeviceID:
                        //  SWD\WPDBUSENUM\_??_USBSTOR#DISK
                        //  VEN_MBED
                        //  PROD_DAPLINK_VFS
                        //  REV_0.1#022631864E4500281013000000440000000033A04E45
                        //  0#{53F56307-B6BF-11D0-94F2-00A0C91EFB8B}

                        // Another one:
                        //  WPDBUSENUMROOT\UMB\2
                        //  37C186B&0&STORAGE#VOLUME#_??_USBSTOR#DISK
                        //  VEN_MBED
                        //  PROD_DAPLINK_VFS
                        //  REV_0.1#022831864E45003F1013000000400000000033AE4E45
                        //  0#

                        // Another:
                        //  SWD\WPDBUSENUM\_??_USBSTOR#DISK
                        //  VEN_MBED
                        //  PROD_VFS
                        //  REV_0.1#9900000037024E45004820050000000E0000000097969901
                        //  0#{53F56307-B6BF-11D0-94F2-00A0C91EFB8B}


                        bool gotVendorName = false;
                        bool gotProductName = false;
                        string microcontrollerId = String.Empty;
                        string[] andParts = DeviceID.Split('&');
                        foreach (var andPart in andParts)
                        {
                            if (!gotVendorName && andPart == vendorName)
                            {
                                gotVendorName = true;
                            }
                            else if (!gotProductName && Array.Exists(productNames, value => value == andPart))
                            {
                                gotProductName = true;
                            }
                            else if (andPart.StartsWith("REV_"))
                            {
                                string[] slashParts = andPart.Split(idSeparator);
                                if (slashParts.Length >= 2)
                                {
                                    microcontrollerId = slashParts[1];
                                }
                            }
                            if (gotVendorName && gotProductName && !String.IsNullOrEmpty(microcontrollerId))
                            {
                                microcontrollerIds.Add(microcontrollerId);
                                results.AppendLine(String.Format("Found a microbit with id {0}", microcontrollerId));
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                while (e != null)
                {
                    results.AppendLine(e.Message);
                    e = e.InnerException;
                }
            }

            return microcontrollerIds;
        }

        private static MicrobitDesc GetDeviceDesc(string microcontrollerId)
        {
            string Microbit_COM = null;
            string Microbit_Drive = null;


            string qstr = String.Empty;
            WqlObjectQuery query = new SelectQuery();
            try
            {
                qstr = String.Format(@"Select * From Win32_PnPEntity where DeviceID like '%{0}%' and ClassGuid = '{{4d36e978-e325-11ce-bfc1-08002be10318}}'", microcontrollerId);
                query = new SelectQuery(qstr);
                results.AppendLine("Executing query: " + query.QueryString);
                using (var serialDeviceSearcher = new ManagementObjectSearcher(scope, query))
                using (var serialDeviceCollection = serialDeviceSearcher.Get())
                {
                    foreach (var serialDevice in serialDeviceCollection)
                    {
                        results.AppendLine("Processing: " + serialDevice.ToString());
                        string serialDevice_Caption = (string)serialDevice.GetPropertyValue("Caption");
                        int start = serialDevice_Caption.IndexOf("(COM");
                        int end = serialDevice_Caption.IndexOf(")");
                        if (start < end)
                        {
                            Microbit_COM = serialDevice_Caption.Substring(start + 1, end - start - 1);
                        }
                        // Only process the first serial device.
                        break;
                    }
                }

                qstr = String.Format(@"Select * From Win32_PnPEntity where DeviceID like '%{0}%' and ClassGuid = '{{4d36e967-e325-11ce-bfc1-08002be10318}}'", microcontrollerId);
                query = new SelectQuery(qstr);
                results.AppendLine("Executing query: " + query.QueryString);
                using (var diskDeviceSearcher = new ManagementObjectSearcher(scope, query))
                using (var diskDeviceCollection = diskDeviceSearcher.Get())
                {
                    foreach (var diskDevice in diskDeviceCollection)
                    {
                        results.AppendLine("Processing: " + diskDevice.ToString());
                        qstr = String.Format(@"Select * from Win32_DiskDrive where PNPDeviceID like '%{0}%'", microcontrollerId);
                        query = new SelectQuery(qstr);
                        results.AppendLine("Executing query: " + query.QueryString);
                        using (var diskDriveSearcher = new ManagementObjectSearcher(scope, query))
                        using (var diskDriveCollection = diskDriveSearcher.Get())
                        {
                            foreach (var diskDrive in diskDriveCollection)
                            {
                                results.AppendLine("Processing: " + diskDrive.ToString());
                                string diskDrive_DeviceID = (string)diskDrive.GetPropertyValue("DeviceID");
                                UInt32 diskDrive_Index = (UInt32)diskDrive.GetPropertyValue("Index");
                                qstr = String.Format(@"Select * from Win32_DiskPartition where DeviceID like 'Disk #{0}%' and PrimaryPartition = True", diskDrive_Index);
                                query = new SelectQuery(qstr);
                                results.AppendLine("Executing query: " + query.QueryString);
                                using (var primaryPartitionSearcher = new ManagementObjectSearcher(scope, query))
                                using (var primaryPartitionCollection = primaryPartitionSearcher.Get())
                                {
                                    foreach (var primaryPartition in primaryPartitionCollection)
                                    {
                                        results.AppendLine("Processing: " + primaryPartition.ToString());
                                        string primaryPartition_DeviceID = (string)primaryPartition.GetPropertyValue("DeviceID");
                                        qstr = String.Format(@"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{0}'}} WHERE AssocClass = Win32_LogicalDiskToPartition", primaryPartition_DeviceID);
                                        query = new RelatedObjectQuery(qstr);
                                        results.AppendLine("Executing query: " + query.QueryString);
                                        using (var logicalDiskSearcher = new ManagementObjectSearcher(scope, query))
                                        using (var logicalDiskCollection = logicalDiskSearcher.Get())
                                        {
                                            foreach (var logicalDisk in logicalDiskCollection)
                                            {
                                                results.AppendLine("Processing: " + logicalDisk.ToString());
                                                string logicalDisk_DeviceID = logicalDisk["DeviceID"].ToString();
                                                Microbit_Drive = logicalDisk_DeviceID;
                                                // Only process the first logical disk.
                                                break;
                                            }
                                        }
                                        // Only process the first primary partition.
                                        break;
                                    }
                                }
                                // Only process the first disk drive.
                                break;
                            }
                        }
                        // Only process the first disk device.
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                results.AppendLine("Error executing query: " + qstr);
                while (e != null)
                {
                    results.AppendLine(e.Message);
                    e = e.InnerException;
                }
            }

            if (!String.IsNullOrEmpty(Microbit_Drive))
            {
                results.AppendLine(String.Format("Microbit storage device found at {0}", Microbit_Drive));
            }
            else
            {
                results.AppendLine("Failed to find microbit storage device.");
            }
            if (!String.IsNullOrEmpty(Microbit_COM))
            {
                results.AppendLine(String.Format("Microbit serial port found at {0}", Microbit_COM));
            }
            else
            {
                results.AppendLine("Failed to find microbit serial port.");
            }

            if (!String.IsNullOrEmpty(Microbit_COM) && !String.IsNullOrEmpty(Microbit_Drive))
            {
                MicrobitDesc desc = new MicrobitDesc();
                desc.COM = Microbit_COM;
                desc.Drive = Microbit_Drive;
                return desc;
            }

            return null;
        }

    }
}
