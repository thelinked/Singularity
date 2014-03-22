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

using System.Linq;
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
        public readonly Image Image;
        public int Padding { get; private set; }
        public string Name { get; private set; }

        public int Width
        {
            get{ return Image.Width; }
        }
        public int Height
        {
            get { return Image.Height; }
        }

        public int PaddedWidth()
        {
            return Image.Width + Padding * 2;
        }
        public int PaddedHeight()
        {
            return Image.Height + Padding*2;
        }

        public int Area
        {
            get { return Image.Width * Image.Height; }
        }
        public int PaddedArea
        {
            get { return PaddedWidth() * PaddedHeight(); }
        }

        public Sprite(string _pathToImage, int _padding)
        {
            Padding = _padding;
            Name = _pathToImage.Remove(_pathToImage.Count() - 4).Split('\\').Last();
            Image = Image.FromFile(_pathToImage);
        }
    }
}
