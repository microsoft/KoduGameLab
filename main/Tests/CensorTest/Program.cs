// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace CensorTest
{
    using Boku.Common;

    class Program
    {
        static void Main(string[] args)
        {
            string filename = "Profanity.csv";

            CensorContent censor;
            {
                StreamReader reader = new StreamReader(filename);
                CensorContent.CensorContentFile file = CensorContent.ReadSourceRepresentation(reader, filename, BokuContentBuildConsoleLogger.Instance);

                censor = new CensorContent();
                censor.Compile(file, BokuContentBuildConsoleLogger.Instance);
            }

            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Submit an empty line to exit.");

                    while (true)
                    {
                        Console.WriteLine();
                        Console.Write("> ");
                        string input = Console.ReadLine();
                        if (input.Length == 0)
                            break;

                        ConsoleColor color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Green;

                        string output = null;
                        if (censor.Scrub(input, ref output))
                            Console.WriteLine("  " + input + " --> " + output);
                        else
                            Console.WriteLine("  " + input + " --> " + input);

                        Console.ForegroundColor = color;
                    }
                }
                else
                {
                    StreamReader reader = new StreamReader(args[0]);

                    while (!reader.EndOfStream)
                    {
                        string input = reader.ReadLine();

                        string output = null;
                        if (censor.Scrub(input, ref output))
                            Console.WriteLine("  " + input + " --> " + output);
                    }

                    reader.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
