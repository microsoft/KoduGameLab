// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace BokuShared
{
    /// <summary>
    /// Parses an array of strings into name/value pairs and provides an API for querying them.
    /// Example Syntax: /Width 1024 /Height 768 /Effects /Logon /NoCrash /Deadlock
    /// </summary>
    public class CmdLine
    {
        #region Private

        string[] args;
        string argline = null;
        Dictionary<string, string> pairs = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

        #endregion

        #region Public Accessors

        /// <summary>
        /// Return the set of arguments as a space delimited string.
        /// </summary>
        public string[] MultiLine
        {
            get { return args; }
        }

        /// <summary>
        /// Return the set of arguments passed to the contructor.
        /// </summary>
        public string SingleLine
        {
            get
            {
                if (argline == null)
                {
                    argline = String.Empty;
                    for (int i = 0; i < args.Length; ++i)
                    {
                        argline += " " + args[i];
                    }
                }
                return argline;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Create a CmdLine and parse the provided arguments.
        /// </summary>
        /// <param name="args"></param>
        public CmdLine(string[] args)
        {
#if NETFX_CORE
            if (args == null)
            {
                args = new string[0];
            }
#endif
            this.args = (string[])args.Clone();
            Parse(args);
        }

        /// <summary>
        /// Test whether the argument exists.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Exists(string name)
        {
            return pairs.ContainsKey(name);
        }

        /// <summary>
        /// Return the value of the argument as a string, or the default value if the argument doesn't exist.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string GetString(string name, string defaultValue)
        {
            if (pairs.ContainsKey(name))
            {
                return pairs[name];
            }
            return defaultValue;
        }

        /// <summary>
        /// Return the value of the argument as a boolean value, or the default value if the argument doesn't exist.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public bool GetBool(string name, bool defaultValue)
        {
            if (pairs.ContainsKey(name))
            {
                string value = pairs[name];

                if (String.IsNullOrEmpty(value))
                    return true;
                if (value == "0")
                    return false;
                if (value == "1")
                    return true;
                if (value.Equals("false", StringComparison.CurrentCultureIgnoreCase))
                    return false;
                if (value.Equals("true", StringComparison.CurrentCultureIgnoreCase))
                    return true;
                try
                {
                    return Boolean.Parse(value);
                }
                catch { }

                return true;
            }
            return defaultValue;
        }

        /// <summary>
        /// Return the value of the argument as an integer value, or the default value if the argument doesn't exist.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public int GetInt(string name, int defaultValue)
        {
            int result;

            if (pairs.ContainsKey(name))
            {
                try
                {
                    result = int.Parse(pairs[name], System.Globalization.NumberStyles.Integer);
                }
                catch
                {
                    result = defaultValue;
                }
            }
            else
            {
                result = defaultValue;
            }

            return result;
        }

        /// <summary>
        /// Add a name/value pair to the set of arguments.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void Add(string name, string value)
        {
            pairs.Add(name, value);
        }

        #endregion

        #region Private Methods

        enum ParseState
        {
            Name,
            MaybeValue,
        }

        void Parse(string[] args)
        {
            ParseState state = ParseState.Name;

            string name = null;

            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];

                switch (state)
                {
                    case ParseState.Name:
                        if (arg.StartsWith("/") || arg.StartsWith("-"))
                        {
                            name = arg.Remove(0, 1);
                            state = ParseState.MaybeValue;
                            pairs.Add(name, String.Empty);
                        }
                        break;

                    case ParseState.MaybeValue:
                        if (arg.StartsWith("/") || arg.StartsWith("-"))
                        {
                            name = arg.Remove(0, 1);
                            pairs.Add(name, String.Empty);
                        }
                        else
                        {
                            pairs[name] = arg;
                            state = ParseState.Name;
                        }
                        break;
                }
            }
        }

        #endregion
    }
}
