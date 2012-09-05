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

namespace Singularity
{
    class Rect
    {
        public int x, y, w, h;
        public Rect(int _x, int _y, int _w, int _h)
        {
            x = _x;
            y = _y;
            w = _w;
            h = _h;
        }
        public int Area
        {
            get { return w*h; }
        }
    }

    class Sprite
    {
        public Image image;
        public Rect rect;
        public int Padding { get; private set; }
        public string Path { get; private set; }
        public string Name { get; private set; }

        public int Width
        {
            get{ return image.Width; }
        }
        public int Height
        {
            get { return image.Height; }
        }

        public int PaddedWidth()
        {
            return image.Width + Padding * 2;
        }
        public int PaddedHeight()
        {
            return image.Height + Padding*2;
        }

        public int Area
        {
            get { return image.Width * image.Height; }
        }
        public int PaddedArea
        {
            get { return PaddedWidth() * PaddedHeight(); }
        }

        public Sprite(string _pathToImage)
        {
            Padding = 2;
            Path = _pathToImage;
            Name = Path.Remove(Path.Count() - 4).Split('\\').Last();
            image = Image.FromFile(_pathToImage);
            rect = new Rect(0, 0, image.Width, image.Height);
        }
    }
}
