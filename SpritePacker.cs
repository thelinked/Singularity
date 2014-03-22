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

using System.Collections.Generic;
using System.Linq;
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
        private State spaceState;
        private readonly Rect size;
        private readonly Sprite sprite;
        private readonly List<Space> subSpaces;

        
        //A space to be divided futher
        public Space(int _topLeftX, int _topLeftY, int _width, int _height)
        {
            spaceState = State.Empty;
            sprite = null;
            size = new Rect(_topLeftX, _topLeftY, _width, _height);
            subSpaces = new List<Space>();
        }
        //A space that is being used for a sprite.
        private Space(int _topLeftX, int _topLeftY, int _width, int _height, Sprite _sprite)
        {
            spaceState = State.Sprite;
            sprite = _sprite;
            size = new Rect(_topLeftX, _topLeftY, _width, _height);
            subSpaces = new List<Space>();
        }

        //Add a sprite to the space. 
        //If the space isn't split then it is split and a space is made for the sprite.
        //If the space is split then we check if the sprite will fit into one of the child spaces
        public bool Add( Sprite _sprite )
        {
            switch (spaceState)
            {
                //if the space is not already split then try to split it up.
                case State.Empty:
                    return Split(_sprite);

                //If the space is already split then see if they is any room in the children spaces
                case State.Split:
                    return subSpaces.OrderByDescending(space => space.size.Area)
                                    .Any(s => s.Add(_sprite));

                default:
                    return false;
            }
        }

        private bool Split(Sprite _sprite)
        {
            //Can the sprite fit in the remaining space?
            if (size.w >= _sprite.PaddedWidth() && size.h >= _sprite.PaddedHeight())
            {
                //Make a space for the sprite and insert it.
                subSpaces.Add(new Space(size.x,
                                        size.y,
                                        _sprite.PaddedWidth(),
                                        _sprite.PaddedHeight(),
                                        _sprite));

                //Carve up the remaining space so they can be used.
                if ((size.w - _sprite.Width) > 0)
                {
                    subSpaces.Add(new Space(size.x + _sprite.PaddedWidth(),
                                            size.y ,
                                            size.w - _sprite.PaddedWidth(),
                                            size.h));
                }
                if ((size.h - _sprite.Height) > 0)
                {
                    subSpaces.Add(new Space(size.x ,
                                            size.y + _sprite.PaddedHeight(),
                                            _sprite.PaddedWidth(),
                                            size.h - _sprite.PaddedHeight()));
                }

                //We have successfully fitted the sprite and the space is split
                spaceState = State.Split;
                return true;
            }
            //Space was too small to be split.
            spaceState = State.Empty;
            return false;
        }

        public void GenerateImage(string path)
        {
            var spriteSheet = new Bitmap(size.w, size.h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var canvas = Graphics.FromImage(spriteSheet))
            {
                WriteToImage(canvas);
            }
            spriteSheet.Save(path + ".png");
        }
        private void WriteToImage(Graphics canvas)
        {
            switch (spaceState)
            {
                case State.Empty:
                    break;

                case State.Split:
                    foreach (var space in subSpaces)
                    {
                        space.WriteToImage(canvas);
                    }
                    break;

                case State.Sprite:
                    canvas.DrawImage(sprite.Image,
                        new Rectangle(size.x + sprite.Padding, 
                            size.y + sprite.Padding, 
                            size.w - sprite.Padding * 2,
                            size.h - sprite.Padding * 2));
                    break;
            }
        }

        public void GenerateLuaFile(string path, float _Width, float _Height)
        {
            var metaData = GetMetaData(_Width, _Height).Select(e => e + ",") .ToList();
            metaData[metaData.Count-1] = metaData.Last().TrimEnd(',');

            var luaFile = new StreamWriter(path + ".lua");

            var Name = path.Split( '\\' ).Last();
            luaFile.WriteLine("{");

            foreach( var entry in metaData )
            {
                luaFile.WriteLine(entry);
            }
            luaFile.WriteLine("}");
            luaFile.Close();

        }
        private IEnumerable<string> GetMetaData(float _Width, float _Height)
        {
            switch (spaceState)
            {
                case State.Empty:
                    yield break;

                case State.Split:
                    var children = subSpaces.SelectMany(space => space.GetMetaData(_Width, _Height));
                    foreach (var line in children)
                    {
                        yield return line;
                    }
                    yield break;

                case State.Sprite:
                    string chunk = "\t" + sprite.Name + " =\n\t{\n";

                    int x = size.x + sprite.Padding;
                    int y = size.y + sprite.Padding;
                    int w = size.w;
                    int h = size.h;

                    chunk += string.Format("\t\tnormX = {0},\n", x / _Width);
                    chunk += string.Format("\t\tnormY = {0},\n", y / _Height);
                    chunk += string.Format("\t\tnormW = {0},\n", w / _Width);
                    chunk += string.Format("\t\tnormH = {0}\n", h / _Height);
                    chunk += "\t}";

                    yield return chunk;
                    yield break;
            }
        }
    }
    
    class SpritePacker
    {
        private readonly List<Sprite> sprites;
        private Space tree;
        private int width;
        private int height;

        public SpritePacker(IEnumerable<Sprite> sprites)
        {
            GetSpriteSheetSize(sprites);
            tree = new Space(0, 0, width, height);
            this.sprites = sprites.OrderByDescending(sprite => sprite.Area).ToList();
        }

        private void GetSpriteSheetSize(IEnumerable<Sprite> sprites)
        {
            bool GrowingWidth = true;
            int WidthGuess = 32, HeightGuess = 32;
            int area = sprites.Select(sprite => sprite.PaddedArea).Sum();
            
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

            width = WidthGuess;
            height = HeightGuess;
        }

        public bool Pack()
        {
            while (!TryPack())
            {
                if (height == width)
                {
                    width *= 2;
                }
                else
                {
                    height *= 2;
                }
                tree = new Space(0, 0, width, height);
            }
            return true;
        }

        public bool Pack(int width, int height)
        {
            tree = new Space(0, 0, width, height);
            return TryPack();
        }

        private bool TryPack()
        {
            return sprites.All(sprite => tree.Add(sprite));
        }

        public void Write(string path)
        {
            tree.GenerateImage(path);
            tree.GenerateLuaFile(path, width, height);
        }
    }
}