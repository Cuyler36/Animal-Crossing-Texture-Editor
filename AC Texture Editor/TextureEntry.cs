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
        public int[] RGBA8_Palette;
        public byte[] Raw_Data; // Data is in AC format
        public byte[] Organized_Data;
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
            RGBA8_Palette = TextureUtility.RGB555A3_to_RGBA8_Palette(Palette);
            Raw_Data = Texture_Data;
            Organized_Data = TextureUtility.Swap_Pattern(Raw_Data, Sections, Blocks, Width);
            Texture = TextureUtility.GenerateBitmap(Raw_Data, RGBA8_Palette, Sections, Blocks, Width, Image_Width, Image_Height);
            Palette_Bitmap = TextureUtility.DrawPalette(RGBA8_Palette);
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
            Texture_Writer.Write(Raw_Data);

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
