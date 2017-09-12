using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;

namespace AC_Texture_Editor
{
    class TextureEntry
    {
        public Bitmap Texture;
        public Bitmap Palette_Bitmap;
        public ushort[] Palette;
        public byte[] Raw_Data; // Data is in AC format
        public int Texture_Offset;
        public int Palette_Offset;
        public int Sections;
        public int Blocks;
        public int Width;
        public string Texture_Name;
        public int Image_Width;
        public int Image_Height;
        public TextureEntry Parent;
        public int Entry_Index = 0;

        // Container Portion
        public bool IsContainer = false;
        public TextureEntry[] Subentries;

        // This exists to allow for "subentries"
        public TextureEntry(TextureEntry[] Entries)
        {
            IsContainer = true;
            Subentries = Entries;
            for (int i = 0; i < Entries.Length; i++)
            {
                Entries[i].Parent = this;
                Entries[i].Entry_Index = i;
            }
        }

        public TextureEntry(int Texture_Offset, int Palette_Offset, int Sections, int Blocks, int Width, int Size_X, int Size_Y, byte[] Texture_Data, byte[] Palette_Data)
        {
            this.Texture_Offset = Texture_Offset;
            this.Palette_Offset = Palette_Offset;
            this.Sections = Sections;
            this.Blocks = Blocks;
            this.Width = Width;
            Image_Width = Size_X;
            Image_Height = Size_Y;
            Palette = TextureUtility.CondensePalette(Palette_Data);
            Raw_Data = Texture_Data;
            Texture = TextureUtility.GenerateBitmap(Raw_Data, Palette, Sections, Blocks, Width, Image_Width, Image_Height);
            Palette_Bitmap = TextureUtility.DrawPalette(Palette);

            // Test
            /*if (Texture_Offset == 0)
            {
                int Original_Total = 0;
                for (int i = 0; i < Texture_Data.Length; i++)
                {
                    Original_Total += Texture_Data[i];
                }
                int Decoded_Total = 0;
                byte[] Decoded_Data = TextureUtility.Swap_Pattern(Texture_Data, Sections, Blocks, Width);
                byte[] Encoded_Data = TextureUtility.Encode(Decoded_Data); //TextureUtility.Swap_Pattern(Decoded_Data, Sections, Blocks, Width, true);
                for (int i = 0; i < Decoded_Data.Length; i++)
                {
                    Decoded_Total += Decoded_Data[i];
                }
                System.Windows.Forms.MessageBox.Show("Original Total = Re-encoded Total: " + (Original_Total == Decoded_Total).ToString());
                using (TextWriter Writer = File.CreateText(@"C:\Users\olsen\Texture_Output.txt"))
                {
                    for (int i = 0; i < Encoded_Data.Length; i++)
                    {
                        if (Encoded_Data[i] != Texture_Data[i])
                        {
                            Writer.WriteLine(string.Format("Re-encoded data failed to matchup! Index: {0} | Block: {1} | Expected: {2} Got: {3}",
                                i, i / 4, Texture_Data[i].ToString("X2"), Encoded_Data[i].ToString("X2")));
                        }
                    }
                }
            }*/
        }

        public void Dispose()
        {
            Texture.Dispose();
            Palette_Bitmap.Dispose();
        }

        // Two BinaryWriters are specified here for the "tex_boy.bin" case, where the palette data is stored in pallet_boy.bin
        public void Write(BinaryWriter Texture_Writer, BinaryWriter PaletteWriter = null)
        {
            // Check for null PaletteWriter
            PaletteWriter = PaletteWriter ?? Texture_Writer;

            // Write Texture
            Texture_Writer.Seek(Texture_Offset, SeekOrigin.Begin);
            Texture_Writer.Write(TextureUtility.ConvertRGB555(TextureUtility.DumpBitmap(Texture, Image_Width), Palette, Sections, Blocks, Width));

            // Write Palette
            byte[] Palette_Data = new byte[Palette.Length * 2];
            for (int i = 0; i < Palette.Length; i++)
            {
                Palette_Data[i * 2] = (byte)(Palette[i] >> 8);
                Palette_Data[i * 2 + 1] = (byte)(Palette[i]);
            }

            PaletteWriter.Seek(Palette_Offset, SeekOrigin.Begin);
            PaletteWriter.Write(Palette_Data);
        }
    }
}
