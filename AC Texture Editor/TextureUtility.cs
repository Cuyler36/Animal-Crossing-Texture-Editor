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
        /// <summary>
        /// Converts RGB5A3 to RGBA8. If highest bit is set then the pixel has no alpha channel.
        /// </summary>
        /// <param name="pixel">RGB5A3 ushort</param>
        /// <param name="A">Returned Alpha value</param>
        /// <param name="R">Returned Red value</param>
        /// <param name="G">Returned Green value</param>
        /// <param name="B">Returned Blue valuer</param>
        public static void RGB5A3_to_RGBA8(ushort pixel, out byte A, out byte R, out byte G, out byte B)
        {
            if ((pixel & 0x8000) == 0x8000)
            {
                // No Alpha Channel
                A = 0xFF;

                // Separate RGB from bits
                R = (byte)((pixel & 0x7C00) >> 10);
                G = (byte)((pixel & 0x03E0) >> 5);
                B = (byte)(pixel & 0x001F);

                // Convert to RGB8 values
                R = (byte)((R << (8 - 5)) | (R >> (10 - 8)));
                G = (byte)((G << (8 - 5)) | (G >> (10 - 8)));
                B = (byte)((B << (8 - 5)) | (B >> (10 - 8)));
            }
            else
            {
                // An Alpha Channel Exists, 3 bits for Alpha Channel and 4 bits each for RGB
                A = (byte)((pixel & 0x7000) >> 12);
                R = (byte)((pixel & 0x0F00) >> 8);
                G = (byte)((pixel & 0x00F0) >> 4);
                B = (byte)(pixel & 0x000F);

                A = (byte)((A << (8 - 3)) | (A << (8 - 6)) | (A >> (9 - 8)));
                R = (byte)((R << (8 - 4)) | R);
                G = (byte)((G << (8 - 4)) | G);
                B = (byte)((B << (8 - 4)) | B);
            }
        }

        public static ushort RGBA8_to_RGB5A3(byte A, byte R, byte G, byte B)
        {
            if (A >= 0xE0)
            {
                return (ushort)(0x8000 | (((R & 0xF8) << 7) | ((G & 0xF8) << 2) | (B >> 3)));
            }
            else
            {
                return (ushort)(((A & 0xE0) << 7) | ((R & 0xF0) << 4) | (G & 0xF0) | ((B & 0xF0) >> 4));
            }
        }

        public static byte ClosestPaletteColor(ushort Pixel, ushort[] Palette, bool Include_Alpha = false)
        {
            byte Closest = 0;
            double Closest_Distance = 0;

            // Convert Pixel to RGB
            RGB5A3_to_RGBA8(Pixel, out byte A, out byte R, out byte G, out byte B);

            for (int i = 0; i < Palette.Length; i++)
            {
                // Convert Palette to RGB
                RGB5A3_to_RGBA8(Palette[i], out byte A2, out byte R2, out byte G2, out byte B2);

                // Using Distance, might switch to Chroma
                double Distance = Math.Sqrt((Include_Alpha ? Math.Pow(A - A2, 2) : 0) + Math.Pow(R - R2, 2) + Math.Pow(G - G2, 2) + Math.Pow(B - B2, 2));
                if (i == 0 || (Distance < Closest_Distance && Distance >= 0))
                {
                    Closest_Distance = Distance;
                    Closest = (byte)i;
                    if (Closest_Distance == 0)
                        return Closest;
                }
            }

            return Closest;
        }

        public static byte[] ConvertRGB555(ushort[] RGB555_Data, ushort[] Palette, int Sections, int Blocks, int Width, bool Encode_Data = true)
        {
            byte[] Data = new byte[RGB555_Data.Length / 2];
            bool Warning_Shown = false;
            bool Include_Alpha = false;

            for (int i = 0; i < RGB555_Data.Length; i += 2)
            {
                byte Condensed_Data = 0;
                bool Found = false;
                for (int x = 0; x < 16; x++)
                {
                    if (Palette[x] == RGB555_Data[i])
                    {
                        //System.Windows.Forms.MessageBox.Show(Palette[x].ToString("X4") + " | " + RGB555_Data[i].ToString("X4"));
                        Condensed_Data = (byte)(x << 4);
                        Found = true;
                        break;
                    }
                }

                if (!Found)
                {
                    if (!Warning_Shown)
                        if (System.Windows.Forms.MessageBox.Show(
                            string.Format("No valid color found for pixel #{0} with RGB5A3 value of {1}. The closest palette color will be used. More occurances may occur. Would you like to include Transparency when determining the closest color?",
                            i, RGB555_Data[i].ToString("X4")), "Import", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                        {
                            Include_Alpha = true;
                        }
                    Warning_Shown = true;
                    Condensed_Data = (byte)(ClosestPaletteColor(RGB555_Data[i], Palette, Include_Alpha) << 4);
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
                        if (System.Windows.Forms.MessageBox.Show(
                            string.Format("No valid color found for pixel #{0} with RGB5A3 value of {1}. The closest palette color will be used. More occurances may occur. Would you like to include Transparency when determining the closest color?",
                            i, RGB555_Data[i].ToString("X4")), "Import", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                        {
                            Include_Alpha = true;
                        }
                    Warning_Shown = true;
                    Condensed_Data += ClosestPaletteColor(RGB555_Data[i + 1], Palette, Include_Alpha);
                }

                Data[i / 2] = Condensed_Data;
            }

            return Encode_Data ? Encode(Data) : Data; //Swap_Pattern(Data, Sections, Blocks, Width, true);
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

        public static int[] RGB555A3_to_RGBA8_Palette(ushort[] RGB555A3)
        {
            int[] RGBA8 = new int[16];
            for (int i = 0; i < 16; i++)
            {
                RGB5A3_to_RGBA8(RGB555A3[i], out byte A, out byte R, out byte G, out byte B);
                RGBA8[i] = (A << 24) | (R << 16) | (G << 8) | B;
            }
            return RGBA8;
        }

        public static Bitmap GenerateBitmap(byte[] patternRawData, int[] Palette, int Sections, int Blocks, int Width, int Size_X, int Size_Y, bool Swap = true)
        {
            byte[] Organized_Data = Swap ? Swap_Pattern(patternRawData, Sections, Blocks, Width) : patternRawData;
            byte[] patternBitmapBuffer = new byte[Organized_Data.Length * 8];

            int pos = 0;
            for (int i = 0; i < patternBitmapBuffer.Length; i += 8)
            {
                byte LeftPixel = (byte)((Organized_Data[pos] >> 4) & 0x0F);
                byte RightPixel = (byte)(Organized_Data[pos] & 0x0F);
                Buffer.BlockCopy(BitConverter.GetBytes(Palette[LeftPixel]), 0, patternBitmapBuffer, i, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(Palette[RightPixel]), 0, patternBitmapBuffer, i + 4, 4);
                pos++;
            }

            Bitmap Pattern_Bitmap = new Bitmap(Size_X, Size_Y, PixelFormat.Format32bppArgb);
            BitmapData bitmapData = Pattern_Bitmap.LockBits(new Rectangle(0, 0, Size_X, Size_Y), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            System.Runtime.InteropServices.Marshal.Copy(patternBitmapBuffer, 0, bitmapData.Scan0, patternBitmapBuffer.Length);
            Pattern_Bitmap.UnlockBits(bitmapData);
            return Pattern_Bitmap;
        }

        public static Bitmap DrawPalette(int[] Palette)
        {
            byte[] patternBitmapBuffer = new byte[16384];
            for (int i = 0; i < 16; i++)
            {
                for (int x = 0; x < 1024; x += 4)
                {
                    patternBitmapBuffer[i * 1024 + x + 3] = (byte)(Palette[i] >> 24);
                    patternBitmapBuffer[i * 1024 + x + 2] = (byte)(Palette[i] >> 16);
                    patternBitmapBuffer[i * 1024 + x + 1] = (byte)(Palette[i] >> 8);
                    patternBitmapBuffer[i * 1024 + x] = (byte)(Palette[i]);
                }
            }
            Bitmap Pattern_Bitmap = new Bitmap(16, 256, PixelFormat.Format32bppArgb);
            BitmapData bitmapData = Pattern_Bitmap.LockBits(new Rectangle(0, 0, 16, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            System.Runtime.InteropServices.Marshal.Copy(patternBitmapBuffer, 0, bitmapData.Scan0, patternBitmapBuffer.Length);
            Pattern_Bitmap.UnlockBits(bitmapData);
            return Pattern_Bitmap;
        }
    }
}
