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
using System.Drawing;
using System.IO;

namespace Singularity
{
    class Space
    {
        private enum State
        {
            Empty,
            Split,
            Sprite
        }
        private State SpaceState;
        private Rect Size;
        private Sprite sprite;
        private List<Space> subSpaces;

        
        //A space to be devided futher
        public Space(int _topLeftX, int _topLeftY, int _width, int _height)
        {
            SpaceState = State.Empty;
            sprite = null;
            Size = new Rect(_topLeftX, _topLeftY, _width, _height);
            subSpaces = new List<Space>();
        }
        //A space that is being used for a sprite.
        public Space(int _topLeftX, int _topLeftY, int _width, int _height, Sprite _sprite)
        {
            SpaceState = State.Sprite;
            sprite = _sprite;
            Size = new Rect(_topLeftX, _topLeftY, _width, _height);
            subSpaces = new List<Space>();
        }

        //Add a sprite to the space. 
        //If the space isn't split then it is split and a space is made for the sprite.
        //If the space is split then we check if the sprite will fit into one of the child spaces
        public bool Add( Sprite _sprite )
        {
            switch (SpaceState)
            {
                //if the space is not already split then try to split it up.
                case State.Empty:
                    return Split(_sprite);

                //If the space is already split then see if they is any room in the children spaces
                case State.Split:
                    var list = from space in subSpaces
                               orderby space.Size.Area descending
                               select space;

                    foreach (var s in list )
                    {
                        //Try to fit sprite into each child space
                        //If it fits in one then return.
                        if (s.Add(_sprite))
                        {
                            return true;
                        }
                    }
                    return false;

                case State.Sprite:
                    return false;

                default:
                    return false;
            }
        }
        public bool Split(Sprite _sprite)
        {
            //Can the sprite fit in the remaining space?
            if (Size.w >= _sprite.PaddedWidth() && Size.h >= _sprite.PaddedHeight())
            {
                //Make a space for the sprite and insert it.
                subSpaces.Add(new Space(Size.x,
                                        Size.y,
                                        _sprite.PaddedWidth(),
                                        _sprite.PaddedHeight(),
                                        _sprite));

                //Carve up the remaining space so they can be used.
                if ((Size.w - _sprite.Width) > 0)
                {
                    subSpaces.Add(new Space(Size.x + _sprite.PaddedWidth(),
                                            Size.y ,
                                            Size.w - _sprite.PaddedWidth(),
                                            Size.h));
                }
                if ((Size.h - _sprite.Height) > 0)
                {
                    subSpaces.Add(new Space(Size.x ,
                                            Size.y + _sprite.PaddedHeight(),
                                            _sprite.PaddedWidth(),
                                            Size.h - _sprite.PaddedHeight()));
                }

                //We have successfully fitted the sprite and the space is split
                SpaceState = State.Split;
                return true;
            }
            //Space was too small to be split.
            SpaceState = State.Empty;
            return false;
        }

        public void GenerateImage(string _path)
        {
            var spriteSheet = new Bitmap(Size.w, Size.h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics canvas = Graphics.FromImage(spriteSheet))
            {
                WriteToImage(canvas, spriteSheet);
            }
            spriteSheet.Save(_path + ".png");
        }
        private void WriteToImage(Graphics canvas, Image spriteSheet)
        {
            switch (SpaceState)
            {
                case State.Empty:
                    break;

                case State.Split:
                    foreach (var space in subSpaces)
                    {
                        space.WriteToImage(canvas, spriteSheet);
                    }
                    break;

                case State.Sprite:
                    canvas.DrawImage(sprite.image,
                        new Rectangle(Size.x + sprite.Padding, 
                            Size.y + sprite.Padding, 
                            Size.w - sprite.Padding * 2,
                            Size.h - sprite.Padding * 2));
                    break;
            }
        }

        public void GenerateLuaFile(string path, float _Width, float _Height)
        {
            var MetaData = new List<string>();
            GetMetaData(_Width, _Height, MetaData);
            MetaData = (from entry in MetaData select entry + ",").ToList();
            MetaData[MetaData.Count-1] = MetaData.Last().TrimEnd(',');

            var LuaFile = new StreamWriter(path + ".lua");

            var Name = path.Split( '\\' ).Last();
            LuaFile.WriteLine("{");

            foreach( var entry in MetaData )
            {
                LuaFile.WriteLine(entry);
            }
            LuaFile.WriteLine("}");
            LuaFile.Close();

        }
        private void GetMetaData( float _Width, float _Height, List<string> _MetaData )
        {
            switch (SpaceState)
            {
                case State.Empty:
                    break;

                case State.Split:
                    foreach (var space in subSpaces)
                    {
                        space.GetMetaData(_Width, _Height, _MetaData);
                    }
                    break;

                case State.Sprite:
                    string chunk = "\t" + sprite.Name + " =\n\t{\n";

                    int x = Size.x + sprite.Padding;
                    int y = Size.y + sprite.Padding;
                    int w = Size.w;
                    int h = Size.h;

                    chunk += string.Format("\t\tX = {0},\n", x);
                    chunk += string.Format("\t\tY = {0},\n", y);
                    chunk += string.Format("\t\tW = {0},\n", w);
                    chunk += string.Format("\t\tH = {0},\n", h);
                    chunk += string.Format("\t\tnormX = {0},\n", x / _Width);
                    chunk += string.Format("\t\tnormY = {0},\n", y / _Height);
                    chunk += string.Format("\t\tnormW = {0},\n", w / _Width);
                    chunk += string.Format("\t\tnormH = {0}\n", h / _Height);
                    chunk += "\t}";

                    _MetaData.Add(chunk);
                    break;
            }
        }
    }
    
    class SpritePacker
    {
        private List<Sprite> Sprites;
        public Space tree;
        int Width; 
        int Height;

        public SpritePacker(IEnumerable<Sprite> _Sprites)
        {
            GetSpriteSheetSize(_Sprites);
            tree = new Space(0, 0, Width, Height);
            //Sort Sprites from biggest to last
            Sprites = (from sprite in _Sprites
                       orderby sprite.Area descending
                       select sprite).ToList();
        }

        private void GetSpriteSheetSize(IEnumerable<Sprite> _Sprites)
        {
            bool GrowingWidth = true;
            int WidthGuess = 32, HeightGuess = 32;
            int area = (from sprite in _Sprites select sprite.PaddedArea).Sum();

            while (area*1.1 > WidthGuess * HeightGuess)
            {
                if(GrowingWidth)
                {
                    WidthGuess *= 2;
                }
                else
                {
                    HeightGuess *= 2;
                }
                GrowingWidth = !GrowingWidth;
            }

            Width = WidthGuess;
            Height = HeightGuess;
        }

        //Pack sprites into the spritesheet.
        public void Pack()
        {
            foreach (var sprite in Sprites)
            {
                if (!tree.Add(sprite))
                {
                    Console.WriteLine("ERROR");
                }
            }
        }

        public void Write(string _path)
        {
            tree.GenerateImage(_path);
            tree.GenerateLuaFile(_path, Width, Height);
        }
    }
}