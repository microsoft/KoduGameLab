// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cab;

namespace CabTest
{
    class Program
    {
        static void Main(string[] args)
        {
            {
                Cab.Compressor comp = new Cab.Compressor(new FileHelper());
                comp.Create(@"C:\bin\", "test.cab");
                comp.AddFile(null, @"C:\bin\trend.xml", null, true);
                comp.AddFile(null, @"C:\bin\procexp.chm", null, true);
                comp.Destroy();
            }

            {
                Cab.Decompressor decomp = new Cab.Decompressor();
                decomp.Create();
                decomp.Expand(null, @"C:\bin\test.cab");
                decomp.Destroy();
            }
        }
    }
}
