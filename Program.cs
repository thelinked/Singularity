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
using System.IO;
using NDesk.Options;

namespace Singularity
{
    static class Program
    {
        private static string path;
        private static string saveTo;
        private static int padding = 2;

        private static int width = 0;
        private static int height = 0;

        static void Main(string[] args)
        {
            ParseCmdLine(args);

            var images = Directory.GetFiles(path)
                                  .Where(file => file.EndsWith("png") ||
                                                 file.EndsWith("jpg") ||
                                                 file.EndsWith("bmp"))
                                  .Select(file => new Sprite(file, padding));

            var spriteSheet = new SpritePacker(images);
            if (width != 0)
            {
                if (!spriteSheet.Pack(width, height))
                {
                    Console.WriteLine("Failed to fit sprites in the desired dimensions");
                    Environment.Exit(5);
                }
            }
            else
            {
                spriteSheet.Pack();
            }
            spriteSheet.Write(saveTo);
            Console.WriteLine("\nSaved sprite sheet to {0}",saveTo);
        }

        static void ParseCmdLine(IEnumerable<string> args)
        {
            var help = false;
            var options = new OptionSet
            {
                { "help=", "Get Help", h => help = h != null },
                { "f=", "The path to the folder of sprites you want to turn into a sprite sheet",p => path = p},
                { "s=", "The name of the resulting sprite sheet",s => saveTo = s },
                { "p=", "Set the padding between sprites in pixels (default:2)",p => padding = int.Parse(p) },
                { "w=", "Width of the spritesheet",s => width = int.Parse(s) },
                { "h=", "Height of the spritesheet",s => height = int.Parse(s) }
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
                Console.WriteLine("Missing or incorect args. Try using -help");
                Environment.Exit(2);
            }

            if (width != 0 && height == 0)
            {
                Console.WriteLine("If you're setting width you also have to set height");
                Environment.Exit(3);
            }

            if (width == 0 && height != 0)
            {
                Console.WriteLine("If you're setting height you also have to set width");
                Environment.Exit(4);
            }
        }
    }
}