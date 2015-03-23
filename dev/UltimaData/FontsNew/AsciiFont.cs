﻿/***************************************************************************
 *   ASCIIFont.cs
 *   Based on code from UltimaSDK: http://ultimasdk.codeplex.com/
 *   
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 3 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/
#region usings
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UltimaXNA.Core.SpacePacking;
using UltimaXNA.Diagnostics;
#endregion

namespace UltimaXNA.UltimaData.FontsNew
{
    public class ASCIIFont
    {
        private int m_Height;
        private ASCIICharacter[] m_Characters;

        public Texture2D Texture;

        public int Height { get { return m_Height; } set { m_Height = value; } }

        public ASCIIFont(GraphicsDevice device, byte[] buffer, ref int pos)
        {
            Height = 0;
            m_Characters = new ASCIICharacter[224];

            CygonRectanglePacker packer = new CygonRectanglePacker(512, 512);
            uint[] textureData = new uint[512 * 512];

            byte header = buffer[pos++];

            // get the sprite data and get a place for each character in the texture, then write the character data to an array.
            for (int k = 0; k < 224; k++)
            {
                ASCIICharacter ch = new ASCIICharacter(buffer, ref pos);
                if (k < 96 && ch.Height > Height)
                {
                    Height = ch.Height;
                }

                Point uv;
                if (packer.TryPack(ch.Width + 2, ch.Height + 2, out uv)) // allow a one-pixel buffer on each side of the character
                {
                    ch.TextureBounds = new Rectangle(uv.X + 1, uv.Y + 1, ch.Width, ch.Height);
                    ch.WriteTextureData(textureData);
                }
                else
                {
                    Logger.Fatal("Could not pack font.mul texture with character '{0}'.", (char)(k + 32));
                }

                m_Characters[k] = ch;
            }

            // write the completed array of character data to the texture.
            Texture2D m_Texture = new Texture2D(device, 512, 512);
            m_Texture.SetData<uint>(textureData);
        }

        public ASCIICharacter GetCharacter(char character)
        {
            int index = (((int)character) & 0x000000ff) - 0x20;
            return m_Characters[index];
        }

        public void GetTextDimensions(ref string text, ref int width, ref int height, int wrapwidth)
        {
            width = 0;
            height = Height;
            int biggestwidth = 0;
            List<char> word = new List<char>();

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (((int)c) > 32)
                {
                    word.Add(c);
                }

                if (c == ' ' || i == text.Length - 1 || c == '\n')
                {
                    // Size the word, character by character.
                    int wordwidth = 0;

                    if (word.Count > 0)
                    {
                        for (int j = 0; j < word.Count; j++)
                        {
                            int charwidth = GetCharacter(word[j]).Width;
                            wordwidth += charwidth;
                        }
                    }

                    // Now make sure this line can fit the word.
                    if (width + wordwidth <= wrapwidth)
                    {
                        width += wordwidth;
                        word.Clear();
                        // if this word is followed by a space, does it fit? If not, drop it entirely and insert \n after the word.
                        if (c == ' ')
                        {
                            int charwidth = GetCharacter(c).Width;
                            if (width + charwidth <= wrapwidth)
                            {
                                // we can fit an extra space here.
                                width += charwidth;
                            }
                            else
                            {
                                // can't fit an extra space on the end of the line. replace the space with a \n.
                                text = text.Substring(0, i) + '\n' + text.Substring(i + 1, text.Length - i - 1);
                                i--;
                            }
                        }
                    }
                    else
                    {
                        // The word is too big for the line. SOME words are longer than the entire line, so we have to break them up manually.
                        if (wordwidth > wrapwidth)
                        {
                            int splitwidth = 0;
                            for (int j = 0; j < word.Count; j++)
                            {
                                splitwidth += GetCharacter(word[j]).Width + 1;
                                if (splitwidth > wrapwidth)
                                {
                                    text = text.Insert(i - word.Count + j - 1, "\n");
                                    word.Insert(j - 1, '\n');
                                    j--;
                                    splitwidth = 0;
                                }
                            }
                            i = i - word.Count - 1;
                            word.Clear();
                        }
                        else
                        {
                            // this word is too big, so we insert a \n character before the word... and try again.
                            text = text.Insert(i - word.Count, "\n");
                            i = i - word.Count - 1;
                            word.Clear();
                        }
                    }
                }

                if (c == '\n')
                {
                    if (width > biggestwidth)
                        biggestwidth = width;
                    height += Height;
                    width = 0;
                }
            }

            if (biggestwidth > width)
                width = biggestwidth;
        }
    }
}
