/*
Singularity

Copyright 2012 Daniel Hartnett

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

 http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.IO;
using NDesk.Options;

namespace Singularity
{
    class Program
    {
        private static string path;
        private static string saveTo;

        static void Main(string[] args)
        {
            ParseCmdLine(args);

            var images = from file in Directory.GetFiles(path)
                         where file.EndsWith("png") ||
                               file.EndsWith("jpg") ||
                               file.EndsWith("bmp")
                         select new Sprite(file);

            var SpriteSheet = new SpritePacker(images);
            SpriteSheet.Pack();
            SpriteSheet.Write(saveTo);
            Console.WriteLine("\nSaved sprite sheet to {0}",saveTo);
        }

        static void ParseCmdLine(string[] args)
        {
            bool help = false;
            OptionSet options = new OptionSet()
            {
                { "h", h => help = h != null },
                { "p", p => path = p},
                { "s", s => saveTo = s }
            };

            try
            {
                options.Parse(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to parse input arguments:{0}", ex);
                Environment.Exit(1);
            }

            if (help)
            {
                options.WriteOptionDescriptions(Console.Out);
                Environment.Exit(0);
            }

            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(saveTo))
            {
                Console.WriteLine("");
                Environment.Exit(2);
            }
        }
    }
}