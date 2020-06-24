
using System;
using System.Collections.Generic;
using System.Diagnostics;

#if NETFX_CORE
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Security.Cryptography;
    using Windows.Security.Cryptography.Core;
    using Windows.Storage.Streams;
#else
    using System.Security.Cryptography;
#endif

namespace BokuShared
{
    /// <summary>
    /// Staitc functions for helping with Kodu's light-weight auth.
    /// </summary>
    public static class Auth
    {
        private static string creatorName;
        private static string pin;
        private static string idHash;

        private static string guestHash;

        public static string DefaultCreatorName = "Guest";
        public static string DefaultCreatorPin = "0000";
        public static string DefaultCreatorHash = CreateIdHash(DefaultCreatorName, DefaultCreatorPin);

        /// <summary>
        /// Returns the currently signed-in creator name.
        /// </summary>
        public static string CreatorName
        {
            get { return creatorName; }
        }

        /// <summary>
        /// Returns the idHash of the currently signed in creator.
        /// </summary>
        public static string IdHash
        {
            get { return idHash; }
        }

        /// <summary>
        /// Returns the pin of the currently signed in creator.
        /// </summary>
        public static string Pin
        {
            get { return pin; }
        }
        /// <summary>
        /// Set the current creator along with thier idHash
        /// </summary>
        /// <param name="creatorName"></param>
        /// <param name="idHash"></param>
        public static void SetCreator(string creatorName, string idHash)
        {
            Auth.creatorName = creatorName;
            Auth.idHash = idHash;
            Auth.pin = ExtractPin(idHash, creatorName);
        }

        /// <summary>
        /// Do we have someone other than guest signed in?
        /// </summary>
        public static bool IsSignedIn
        {
            get { return idHash != guestHash; }
        }

        /// <summary>
        /// Calculates MD5 hash
        /// </summary>
        /// <param name="data">Input array of bytes</param>
        /// <param name="numBytes">Number of bytes in array to use</param>
        /// <returns>byte[32] MD5 hash</returns>
        public static byte[] MD5(byte[] data, int numBytes)
        {
            Debug.Assert(numBytes <= data.Length);

            byte[] result;

#if NETFX_CORE
            HashAlgorithmProvider alg = Windows.Security.Cryptography.Core.HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            CryptographicHash hash = alg.CreateHash();
            IBuffer buffer = CryptographicBuffer.CreateFromByteArray(data);
            hash.Append(buffer);
            IBuffer hashBuff = hash.GetValueAndReset();
            result = hashBuff.ToArray();
#else
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            result = md5.ComputeHash(data);
#endif       

            return result;
        }   // end of MD5()

        public static void Init()
        {
            creatorName = "Guest";    // Do NOT localize...
            guestHash = CreateIdHash("Guest", "0000");
            idHash = guestHash;
        }

        /// <summary>
        /// Given a pin, determines if it is valid.  "Valid" in 
        /// this case means that it's not trivially simple.
        /// </summary>
        /// <param name="pin"></param>
        /// <returns></returns>
        public static bool IsPinValid(string pin)
        {
            // Must be the right length.
            if (pin == null || pin.Length != 4)
            {
                return false;
            }

            // Must be 4 digits.
            for (int i = 0; i < 4; i++)
            {
                if (!char.IsDigit(pin[i]))
                {
                    return false;
                }
            }

            // Calc deltas.
            int d0 = (int)pin[0] - (int)pin[1];
            int d1 = (int)pin[1] - (int)pin[2];
            int d2 = (int)pin[2] - (int)pin[3];

            if (d0 == d1 && d1 == d2)
            {
                return false;
            }

            return true;
        }   // end of IsPinValid()

        /// <summary>
        /// Given a checksum and a time stamp, determines if the
        /// checksum is valid for the current creator.  Used to
        /// filter on My Worlds and to verify whether a user is
        /// allowed to delete a community world.
        /// </summary>
        /// <param name="checksum"></param>
        /// <returns></returns>
        public static bool IsValidCreatorChecksum(string checksum, DateTime dateTime)
        {
            string hash = CreateChecksumHash(dateTime);
            if (hash == checksum)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Simple function to just get the bytes from a string.  Note that
        /// this assume no interpretation of the data and will even work
        /// if there are invalid characters in the string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Creates the user's idHash from creatorName and pin.
        /// idHash is returned as a string of 16 characters representing
        /// 8 bytes in hex.
        /// </summary>
        /// <param name="creatorName"></param>
        /// <param name="pin">Assumes pin is valid.  Need to check before here!</param>
        /// <returns></returns>
        public static string CreateIdHash(string creatorName, string pin)
        {
            string result = "";

            // First part.
            string s = creatorName + pin;
            s = s.ToLowerInvariant();
            byte[] data = GetBytes(s);
            byte[] hash = MD5(data, data.Length);

            for (int i = 0; i < hash.Length; i++)
            {
                result += hash[i].ToString("x2");
            }

            return result;
        }   // end of CreateIdHash()

        /// <summary>
        /// Creates a checksum value for use when saving levels.  This is
        /// like the idHash except the dateTime string is also included.
        /// </summary>
        /// <param name="creatorName"></param>
        /// <param name="pin"></param>
        /// <param name="dateTime">Assumes pin is valid.  Need to check before here!</param>
        /// <returns></returns>
        public static string CreateChecksumHash(string creatorName, string pin, DateTime dateTime)
        {
            string result = "";

            // Force UTC in case it isn't already.
            string dateString = dateTime.ToUniversalTime().ToString();

            string s = creatorName + pin + dateString;
            s = s.ToLowerInvariant();
            byte[] data = GetBytes(s);
            byte[] hash = MD5(data, data.Length);

            for (int i = 0; i < hash.Length; i++)
            {
                result += hash[i].ToString("x2");
            }
            return result;
        }   // end of CreateChecksumHash()


        /// <summary>
        /// Creates checksum hash based on dateTime using the 
        /// current creator and pin.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string CreateChecksumHash(DateTime dateTime)
        {
            string result = CreateChecksumHash(creatorName, pin, dateTime);

            return result;
        }   // end of CreateChecksumHash()

        /// <summary>
        /// Extracts the pin from the given idHash and creator name.
        /// </summary>
        /// <param name="idHash"></param>
        /// <param name="creatorName"></param>
        /// <returns>pin if found, null otherwise</returns>
        public static string ExtractPin(string idHash, string creatorName)
        {
            string pin = null;
            for (int i = 0; i < 10000; i++)
            {
                string pinString = i.ToString("d4");
                string hash = CreateIdHash(creatorName, pinString);

                if (hash == idHash)
                {
                    pin = pinString;
                    break;
                }
            }

            return pin;
        }   // end of ExtractPin()

        /// <summary>
        /// Extracts pin from given 
        /// </summary>
        /// <param name="fileChecksum"></param>
        /// <param name="creatorName"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ExtractPin(string fileChecksum, string creatorName, DateTime dateTime)
        {
            string pin = null;
            for (int i = 0; i < 10000; i++)
            {
                string pinString = i.ToString("d4");
                string checksum = CreateChecksumHash(creatorName, pinString, dateTime);

                if (checksum == fileChecksum)
                {
                    pin = pinString;
                    break;
                }
            }

            return pin;
        }   // end of ExtractPin()

    }   // end of Auth
}   // end of namespace BokuShared
