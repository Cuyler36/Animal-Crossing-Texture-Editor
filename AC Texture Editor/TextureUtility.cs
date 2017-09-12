using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.IO;

namespace AC_Texture_Editor
{
    public static class TextureUtility
    {
        //TODO: Fix data being jumbled (probably a problem in Swap_Pattern)
        public static ushort[] DumpBitmap(Bitmap Texture, int Width)
        {
            using (MemoryStream Stream = new MemoryStream())
            {
                Texture.Save(Stream, ImageFormat.Bmp);
                byte[] Bitmap_Data = Stream.ToArray().Skip(0x36).ToArray(); // Skip 36 for the bitmap file header
                ushort[] RGB555_Data = new ushort[Bitmap_Data.Length / 2];

                // Convert data to ushorts and flip it vertically
                int Idx = 0;
                for (int i = RGB555_Data.Length - 1; i >= 0; i--)
                {
                    RGB555_Data[Idx] = (ushort)((Bitmap_Data[i * 2 + 1] << 8) + Bitmap_Data[i * 2]);
                    Idx++;
                }
                //System.Windows.Forms.MessageBox.Show(RGB555_Data.Length.ToString());
                // Flip data horizontally
                for (int i = 0; i < RGB555_Data.Length; i += Width)
                {
                    Array.Reverse(RGB555_Data, i, Width);
                }

                return RGB555_Data;
            }
        }

        public static byte ClosestPaletteColor(ushort Pixel, ushort[] Palette)
        {
            byte Closest = 0;
            double Closest_Distance = -1;

            // Convert Pixel to RGB
            uint r = (uint)((Pixel >> (10)) & 31);
            uint g = (uint)((Pixel >> 5) & 31);
            uint b = (uint)(Pixel & 31);
            uint R = r * 255 / 31;
            uint G = g * 255 / 31;
            uint B = b * 255 / 31;

            for (int i = 0; i < Palette.Length; i++)
            {
                // Convert Palette to RGB
                uint r2 = (uint)((Palette[i] >> (10)) & 31);
                uint g2 = (uint)((Palette[i] >> 5) & 31);
                uint b2 = (uint)(Palette[i] & 31);
                uint R2 = r2 * 255 / 31;
                uint G2 = g2 * 255 / 31;
                uint B2 = b2 * 255 / 31;

                // Using Distance, might switch to Chroma
                double Distance = Math.Sqrt(Math.Pow(R - R2, 2) + Math.Pow(G - G2, 2) + Math.Pow(B - B2, 2));
                if (Closest_Distance == -1 || (Distance < Closest_Distance && Distance >= 0))
                {
                    Closest_Distance = Distance;
                    Closest = (byte)i;
                }
            }

            return Closest;
        }

        public static byte[] ConvertRGB555(ushort[] RGB555_Data, ushort[] Palette, int Sections, int Blocks, int Width)
        {
            byte[] Data = new byte[RGB555_Data.Length / 2];
            bool Warning_Shown = false;

            for (int i = 0; i < RGB555_Data.Length; i += 2)
            {
                byte Condensed_Data = 0;
                bool Found = false;
                for (int x = 0; x < 16; x++)
                {
                    if (Palette[x] == RGB555_Data[i])
                    {
                        Condensed_Data = (byte)(x << 4);
                        Found = true;
                        break;
                    }
                }

                if (!Found)
                {
                    if (!Warning_Shown)
                        System.Windows.Forms.MessageBox.Show(
                            string.Format("No valid color found for pixel #{0} with RGB555 value of {1}. The closest palette color will be used. More occurances may occur.",
                            i, RGB555_Data[i].ToString("X4")));
                    Warning_Shown = true;
                    Condensed_Data = (byte)(ClosestPaletteColor(RGB555_Data[i], Palette) << 4);
                }

                Found = false;
                for (int x = 0; x < 16; x++)
                {
                    if (Palette[x] == RGB555_Data[i + 1])
                    {
                        Condensed_Data += (byte)x;
                        Found = true;
                        break;
                    }
                }

                if (!Found)
                {
                    if (!Warning_Shown)
                        System.Windows.Forms.MessageBox.Show(
                            string.Format("No valid color found for pixel #{0} with RGB555 value of {1}. The closest palette color will be used. More occurances may occur.",
                            i, RGB555_Data[i].ToString("X4")));
                    Warning_Shown = true;
                    Condensed_Data += ClosestPaletteColor(RGB555_Data[i + 1], Palette);
                }

                Data[i / 2] = Condensed_Data;
            }

            return Encode(Data); //Swap_Pattern(Data, Sections, Blocks, Width, true);
        }

        // Temporary until I fix Swap_Pattern with re-encoding (not sure why it's broken)
        public static byte[] Encode(byte[] Input)
        {
            byte[] Encoded_Data = new byte[Input.Length];
            if (MainWindow.File_Type == 1)
            {
                List<byte[]> Block_Data = new List<byte[]>();
                for (int i = 0; i < 16; i++)
                {
                    byte[] block = new byte[16];
                    for (int x = 0; x < 4; x++)
                    {
                        int pos = (i % 4) * 4 + (i / 4) * 64 + x * 16;
                        Buffer.BlockCopy(Input, pos, block, x * 4, 4);
                    }
                    Block_Data.Add(block);
                }
                List<byte[]> Sorted_Blocks = new List<byte[]>()
                {
                    Block_Data[0], Block_Data[4], Block_Data[1], Block_Data[5], //0, 1, 2, 3|
                    Block_Data[2], Block_Data[6], Block_Data[3], Block_Data[7], //4, 5, 6, 7|
                    Block_Data[8], Block_Data[12], Block_Data[9], Block_Data[13], //8, 9, 10, 11|
                    Block_Data[10], Block_Data[14], Block_Data[11], Block_Data[15], //12, 13, 14, 15|
                };
                for (int i = 0; i < 16; i++)
                    Sorted_Blocks[i].CopyTo(Encoded_Data, i * 16);
            }
            else if (MainWindow.File_Type == 2)
            {
                List<byte[]> Block_Data = new List<byte[]>();
                for (int i = 0; i < 32; i++)
                {
                    byte[] block = new byte[16];
                    for (int x = 0; x < 4; x++)
                    {
                        int pos = (i % 4) * 4 + (i / 4) * 64 + x * 16;
                        Buffer.BlockCopy(Input, pos, block, x * 4, 4);
                    }
                    Block_Data.Add(block);
                }
                List<byte[]> Sorted_Blocks = new List<byte[]>()
                {
                    Block_Data[0], Block_Data[4], Block_Data[1], Block_Data[5], //0, 1, 2, 3|
                    Block_Data[2], Block_Data[6], Block_Data[3], Block_Data[7], //4, 5, 6, 7|
                    Block_Data[8], Block_Data[12], Block_Data[9], Block_Data[13], //8, 9, 10, 11|
                    Block_Data[10], Block_Data[14], Block_Data[11], Block_Data[15], //12, 13, 14, 15|
                    Block_Data[16], Block_Data[20], Block_Data[17], Block_Data[21], //16, 17, 18, 19
                    Block_Data[18], Block_Data[22], Block_Data[19], Block_Data[23], //20, 21, 22, 23
                    Block_Data[24], Block_Data[28], Block_Data[25], Block_Data[29], //24, 25, 26, 27
                    Block_Data[26], Block_Data[30], Block_Data[27], Block_Data[31], //28, 29, 30, 31
                };
                for (int i = 0; i < 32; i++)
                    Sorted_Blocks[i].CopyTo(Encoded_Data, i * 16);
            }
            else if (MainWindow.File_Type == 3 || MainWindow.File_Type == 4)
            {
                List<byte[]> Blocks = new List<byte[]>();
                int Total_Blocks = Input.Length / 4; // 4 bytes per block (4x1)
                for (int i = 0; i < Total_Blocks; i++)
                {
                    Blocks.Add(Input.Skip(i * 4).Take(4).ToArray());
                }
                List<byte[]> Sorted_Blocks = new List<byte[]>();
                for (int i = 0; i < Blocks.Count; i++)
                {
                    Sorted_Blocks.Add(new byte[0]);
                }
                for (int Block = 0; Block < 8; Block++)
                {
                    int Block_Offset = Block * 64;
                    int Column = 0;
                    for (int i = 0; i < 64; i++)
                    {
                        if (i > 0 && i % 8 == 0)
                        {
                            Column++;
                        }
                        int Actual_Location = Block_Offset + (i % 8) * 8 + Column;
                        //Console.WriteLine(string.Format("Index: {0} Actual Position: {1} Column: {2}", Block_Offset + i, Actual_Location, Column));
                        Sorted_Blocks[Actual_Location] = Blocks[Block_Offset + i];
                    }
                }

                // Dump organized data into an array
                for (int i = 0; i < Sorted_Blocks.Count; i++)
                {
                    Sorted_Blocks[i].CopyTo(Encoded_Data, i * 4);
                }
            }
            else
            {
                return Input;
            }
            return Encoded_Data;
        }

        public static byte[] Swap_Pattern(byte[] Input, int Section_Count = 4, int Block_Count = 32, int Width = 4, bool Encode = false)
        {
            List<byte[]> Blocks = new List<byte[]>();
            int Total_Blocks = Input.Length / 4; // 4 bytes per block (4x1)
            for (int i = 0; i < Total_Blocks; i++)
            {
                Blocks.Add(Input.Skip(i * 4).Take(4).ToArray());
            }
            List<byte[]> Sorted_Blocks = new List<byte[]>();
            for (int i = 0; i < Blocks.Count; i++)
            {
                Sorted_Blocks.Add(new byte[0]);
            }
            for (int Block = 0; Block < Section_Count; Block++)
            {
                int Block_Offset = Block * Block_Count;
                int Column = 0;
                if (Encode)
                {
                    for (int i = 0; i < Block_Count; i++)
                    {
                        int Actual_Location = Block_Offset + (i / Width) + (i % Width) * 8;
                        Sorted_Blocks[Actual_Location] = Blocks[i];
                        //System.Windows.MessageBox.Show(string.Format("Index: {0} | New Location: {1}", i + Block_Offset, Actual_Location));
                    }
                }
                else
                {
                    for (int i = 0; i < Block_Count; i++)
                    {
                        if (i > 0 && i % 8 == 0)
                        {
                            Column++;
                        }
                        int Actual_Location = Block_Offset + (i % 8) * Width + Column;
                        Sorted_Blocks[Actual_Location] = Blocks[Block_Offset + i];
                    }
                }
            }

            // Dump organized data into an array
            byte[] Organized_Data = new byte[Input.Length];
            for (int i = 0; i < Sorted_Blocks.Count; i++)
            {
                Sorted_Blocks[i].CopyTo(Organized_Data, i * 4);
            }

            return Organized_Data;
        }

        public static byte[] Condense_Data(byte[] Data)
        {
            byte[] Condensed_Data = new byte[Data.Length / 2];
            for (int i = 0; i < Condensed_Data.Length; i++)
            {
                int Offset = i * 2;
                Condensed_Data[i] = (byte)(((Data[Offset] << 4) & 0xF0) + (Data[Offset + 1] & 0x0F));
            }
            return Condensed_Data;
        }

        public static byte[] Unpack_Data(byte[] Data)
        {
            byte[] Unpacked_Data = new byte[Data.Length * 2];
            for (int i = 0; i < Data.Length; i++)
            {
                int Offset = i * 2;
                Unpacked_Data[Offset] = (byte)((Data[i] >> 4) & 0x0F);
                Unpacked_Data[Offset + 1] = (byte)(Data[i] & 0x0F);
            }
            return Unpacked_Data;
        }

        public static ushort[] CondensePalette(byte[] paletteData)
        {
            ushort[] Palette = new ushort[16];
            for (int i = 0; i < 16; i++)
            {
                Palette[i] = (ushort)((paletteData[i * 2] << 8) + paletteData[i * 2 + 1]);
            }
            return Palette;
        }

        public static Bitmap GenerateBitmap(byte[] patternRawData, ushort[] Palette, int Sections, int Blocks, int Width, int Size_X, int Size_Y)
        {
            byte[] Organized_Data = Swap_Pattern(patternRawData, Sections, Blocks, Width);
            byte[] patternBitmapBuffer = new byte[Organized_Data.Length * 4];

            int pos = 0;
            for (int i = 0; i < patternBitmapBuffer.Length; i += 4)
            {
                byte LeftPixel = (byte)((Organized_Data[pos] >> 4) & 0x0F);
                byte RightPixel = (byte)(Organized_Data[pos] & 0x0F);
                Buffer.BlockCopy(BitConverter.GetBytes(Palette[LeftPixel]), 0, patternBitmapBuffer, i, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(Palette[RightPixel]), 0, patternBitmapBuffer, i + 2, 2);
                pos++;
            }

            Bitmap Pattern_Bitmap = new Bitmap(Size_X, Size_Y, PixelFormat.Format16bppRgb555);
            BitmapData bitmapData = Pattern_Bitmap.LockBits(new Rectangle(0, 0, Size_X, Size_Y), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb555);
            System.Runtime.InteropServices.Marshal.Copy(patternBitmapBuffer, 0, bitmapData.Scan0, patternBitmapBuffer.Length);
            Pattern_Bitmap.UnlockBits(bitmapData);
            return Pattern_Bitmap;
        }

        public static Bitmap DrawPalette(ushort[] Palette)
        {
            byte[] patternBitmapBuffer = new byte[8192];
            for (int i = 0; i < 16; i++)
            {
                for (int x = 0; x < 512; x += 2)
                {
                    patternBitmapBuffer[i * 512 + x + 1] = (byte)(Palette[i] >> 8);
                    patternBitmapBuffer[i * 512 + x] = (byte)(Palette[i]);
                }
            }
            Bitmap Pattern_Bitmap = new Bitmap(16, 256, PixelFormat.Format16bppRgb555);
            BitmapData bitmapData = Pattern_Bitmap.LockBits(new Rectangle(0, 0, 16, 256), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb555);
            System.Runtime.InteropServices.Marshal.Copy(patternBitmapBuffer, 0, bitmapData.Scan0, patternBitmapBuffer.Length);
            Pattern_Bitmap.UnlockBits(bitmapData);
            return Pattern_Bitmap;
        }
    }
}
