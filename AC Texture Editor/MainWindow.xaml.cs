using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Interop;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AC_Texture_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static byte File_Type;

        private byte[] Data_Buffer;
        private byte[] Pallet_Buffer;
        private string Working_Location = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        private string File_Location;
        private string Pallet_Location;
        private System.Windows.Forms.OpenFileDialog File_Select = new System.Windows.Forms.OpenFileDialog();
        private System.Windows.Forms.OpenFileDialog Import_Select = new System.Windows.Forms.OpenFileDialog();
        private System.Windows.Forms.SaveFileDialog File_Save = new System.Windows.Forms.SaveFileDialog();
        private int SelectedColor;
        private TextureEntry[] TextureEntries;
        private TextureEntry SelectedEntry;
        private DrawingBrush Shirt_Brush = new DrawingBrush();
        private DrawingBrush FloorCarpet_Brush = new DrawingBrush();
        private DrawingBrush Face_Brush = new DrawingBrush();
        private bool Setting_Color = false;
        private bool Mouse_Down = false;
        private string RGBBox_LastText = "";
        private int Last_X = -1;
        private int Last_Y = -1;

        public MainWindow()
        {
            InitializeComponent();

            Shirt_Brush.TileMode = TileMode.Tile;
            FloorCarpet_Brush.TileMode = TileMode.Tile;
            Face_Brush.TileMode = TileMode.Tile;

            Shirt_Brush.Viewport = new Rect(0, 0, 16, 16);
            FloorCarpet_Brush.Viewport = new Rect(0, 0, 8, 8);
            Face_Brush.Viewport = new Rect(0, 0, 16, 16);
            Shirt_Brush.ViewportUnits = BrushMappingMode.Absolute;
            FloorCarpet_Brush.ViewportUnits = BrushMappingMode.Absolute;
            Face_Brush.ViewportUnits = BrushMappingMode.Absolute;

            GeometryDrawing Shirt_Face = new GeometryDrawing();
            Shirt_Face.Geometry = new RectangleGeometry { Rect = new Rect(0, 0, 16, 16) };
            Shirt_Face.Pen = new System.Windows.Media.Pen { Brush = System.Windows.Media.Brushes.Gray, Thickness = 0.5};

            GeometryDrawing Carpet_Floor = new GeometryDrawing();
            Carpet_Floor.Geometry = new RectangleGeometry { Rect = new Rect(0, 0, 8, 8) };
            Carpet_Floor.Pen = new System.Windows.Media.Pen { Brush = System.Windows.Media.Brushes.Gray, Thickness = 0.5 };

            Shirt_Brush.Drawing = Shirt_Face;
            Face_Brush.Drawing = Shirt_Face;
            FloorCarpet_Brush.Drawing = Carpet_Floor;
        }

        private void PopulateTreeView(TextureEntry[] Entries, TreeViewItem Parent = null)
        {
            // Only clear it on the first run
            if (Parent == null)
            {
                EntryTreeView.Items.Clear();
            }

            for (int i = 0; i < Entries.Length; i++)
            {
                TextureEntry Entry = Entries[i];
                TreeViewItem Entry_Item = new TreeViewItem();

                StackPanel Panel = new StackPanel();
                Panel.Orientation = Orientation.Horizontal;

                Label Name_Label = new Label();
                Name_Label.Content = Entry.Texture_Name ?? i.ToString();

                Panel.Children.Add(Name_Label);
                Entry_Item.Header = Panel;

                if (Entry.IsContainer)
                {
                    PopulateTreeView(Entry.Subentries, Entry_Item);
                }
                else
                {
                    System.Windows.Controls.Image Preview = new System.Windows.Controls.Image();
                    Preview.Source = BitmapSourceFromBitmap(Entry.Texture);
                    Preview.Width = 16;
                    Preview.Height = 16;
                    Panel.Children.Add(Preview);
                    Entry_Item.Selected += new RoutedEventHandler((object s, RoutedEventArgs e) => Entry_Item_Selected(s, e, Preview.Source, Entry));
                }

                if (Parent == null)
                {
                    EntryTreeView.Items.Add(Entry_Item);
                }
                else
                {
                    Parent.Items.Add(Entry_Item);
                }
            }
        }

        private void Entry_Item_Selected(object sender, RoutedEventArgs e, ImageSource Bitmap, TextureEntry Entry)
        {
            SelectedImage.Source = Bitmap;
            Set_Palette_Colors(Entry.Palette);
            SelectedEntry = Entry;
            SelectedLabel.Content = string.Format("{0} - {1}", Entry.Parent.Texture_Name, Entry.Entry_Index);
            SetPaletteColor(SelectedColor);
        }

        private void Set_Palette_Colors(ushort[] Palette)
        {
            uint[] Hex_Palette = new uint[Palette.Length];
            for (int i = 0; i < Palette.Length; i++)
            {
                Hex_Palette[i] = 0xFF000000 + RGB555_to_Hex(Palette[i]);
            }

            Palette0.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[0] >> 16), (byte)(Hex_Palette[0] >> 8), (byte)(Hex_Palette[0])));
            Palette1.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[1] >> 16), (byte)(Hex_Palette[1] >> 8), (byte)(Hex_Palette[1])));
            Palette2.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[2] >> 16), (byte)(Hex_Palette[2] >> 8), (byte)(Hex_Palette[2])));
            Palette3.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[3] >> 16), (byte)(Hex_Palette[3] >> 8), (byte)(Hex_Palette[3])));
            Palette4.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[4] >> 16), (byte)(Hex_Palette[4] >> 8), (byte)(Hex_Palette[4])));
            Palette5.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[5] >> 16), (byte)(Hex_Palette[5] >> 8), (byte)(Hex_Palette[5])));
            Palette6.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[6] >> 16), (byte)(Hex_Palette[6] >> 8), (byte)(Hex_Palette[6])));
            Palette7.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[7] >> 16), (byte)(Hex_Palette[7] >> 8), (byte)(Hex_Palette[7])));
            Palette8.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[8] >> 16), (byte)(Hex_Palette[8] >> 8), (byte)(Hex_Palette[8])));
            Palette9.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[9] >> 16), (byte)(Hex_Palette[9] >> 8), (byte)(Hex_Palette[9])));
            Palette10.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[10] >> 16), (byte)(Hex_Palette[10] >> 8), (byte)(Hex_Palette[10])));
            Palette11.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[11] >> 16), (byte)(Hex_Palette[11] >> 8), (byte)(Hex_Palette[11])));
            Palette12.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[12] >> 16), (byte)(Hex_Palette[12] >> 8), (byte)(Hex_Palette[12])));
            Palette13.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[13] >> 16), (byte)(Hex_Palette[13] >> 8), (byte)(Hex_Palette[13])));
            Palette14.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[14] >> 16), (byte)(Hex_Palette[14] >> 8), (byte)(Hex_Palette[14])));
            Palette15.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(Hex_Palette[15] >> 16), (byte)(Hex_Palette[15] >> 8), (byte)(Hex_Palette[15])));
        }

        private ushort Hex_to_RGB555(uint Hex)
        {
            uint R = (Hex >> 16) & 0xFF;
            uint G = (Hex >> 8) & 0xFF;
            uint B = Hex & 0xFF;
            uint r = R * 31 / 255;
            uint g = G * 31 / 255;
            uint b = B * 31 / 255;

            return (ushort)(r << (10) | (g << 5) | b);
        }
        
        private uint RGB555_to_Hex(ushort RGB)
        {
            uint r = (uint)((RGB >> (10)) & 31);
            uint g = (uint)((RGB >> 5) & 31);
            uint b = (uint)(RGB & 31);
            uint R = r * 255 / 31;
            uint G = g * 255 / 31;
            uint B = b * 255 / 31;

            return (R << 16) | (G << 8) | B;
        }
        
        private void RGB555_to_RGB888(ushort RGB, out byte Red, out byte Green, out byte Blue)
        {
            uint r = (uint)((RGB >> (10)) & 31);
            uint g = (uint)((RGB >> 5) & 31);
            uint b = (uint)(RGB & 31);
            uint R = r * 255 / 31;
            uint G = g * 255 / 31;
            uint B = b * 255 / 31;

            Red = (byte)R;
            Green = (byte)G;
            Blue = (byte)B;
        }

        private ushort RGB888_to_RGB555(byte R, byte G, byte B)
        {
            R = (byte)(R * 31 / 255);
            G = (byte)(G * 31 / 255);
            B = (byte)(B * 31 / 255);

            return (ushort)(R << 10 | G << 5 | B);
        }

        //Trying RGB655
        private void RGB565_to_RGB888(ushort RGB, out byte Red, out byte Green, out byte Blue)
        {
            byte R = (byte)((RGB >> 10) & 0x3F);
            byte G = (byte)((RGB >> 5) & 0x1F);
            byte B = (byte)(RGB & 0x1F);

            Red = (byte)((R << 2) | (R >> 4));
            Green = (byte)((G << 3) | (G >> 2));
            Blue = (byte)((B << 3) | (B >> 2));
        }

        private void MousePosition_to_Coordinates(MouseEventArgs e, out int X, out int Y)
        {
            if (File_Type == 1 || File_Type == 2)
            {
                X = (int)e.GetPosition(SelectedImage).X / 16;
                Y = (int)e.GetPosition(SelectedImage).Y / 16;
            }
            else if (File_Type == 3 || File_Type == 4)
            {
                X = (int)e.GetPosition(SelectedImage).X / 8;
                Y = (int)e.GetPosition(SelectedImage).Y / 8;
            }
            else
            {
                X = 0;
                Y = 0;
            }
        }

        private void Paint(int X, int Y)
        {
            int Data_Index = X / 2 + Y * (SelectedEntry.Image_Width / 2);

            // Is it a left or right pixel?
            if (X % 2 == 0)
            {
                SelectedEntry.Organized_Data[Data_Index] = (byte)((SelectedColor << 4) + (SelectedEntry.Organized_Data[Data_Index] & 0x0F));
            }
            else
            {
                SelectedEntry.Organized_Data[Data_Index] = (byte)((SelectedEntry.Organized_Data[Data_Index] & 0xF0) + SelectedColor);
            }

            SelectedEntry.Raw_Data = TextureUtility.Encode(SelectedEntry.Organized_Data);

            // Redraw bitmap
            SelectedEntry.Texture = TextureUtility.GenerateBitmap(SelectedEntry.Raw_Data, SelectedEntry.Palette, SelectedEntry.Sections, SelectedEntry.Blocks,
                SelectedEntry.Width, SelectedEntry.Image_Width, SelectedEntry.Image_Height);

            SelectedImage.Source = BitmapSourceFromBitmap(SelectedEntry.Texture);
            PopulateTreeView(TextureEntries);
        }

        private void CanvasGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (SelectedEntry != null)
            {
                MousePosition_to_Coordinates(e, out int X, out int Y);
                PositionLabel.Content = string.Format("X: {0} Y: {1}", X, Y);
                if (Mouse_Down && (Last_X != X || Last_Y != Y))
                {
                    Paint(X, Y);
                    Last_X = X;
                    Last_Y = Y;
                }
            }
        }

        private void CanvasGrid_MouseDown(object sender, MouseEventArgs e)
        {
            if (SelectedEntry != null)
            {
                Mouse_Down = true;
                MousePosition_to_Coordinates(e, out int X, out int Y);
                Last_X = X;
                Last_Y = Y;
                Paint(X, Y);
            }
        }

        private void CanvasGrid_MouseUp(object sender, MouseEventArgs e)
        {
            Mouse_Down = false;
        }

        private void CanvasMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SelectedEntry != null)
            {
                string Idx = (sender as Canvas).Name.Substring(7);
                if (int.TryParse(Idx, out int Palette_Index))
                {
                    SetPaletteColor(Palette_Index);
                }
            }
        }

        private void SetPaletteColor(int Index)
        {
            SelectedColor = Index;
            RGB555_to_RGB888(SelectedEntry.Palette[Index], out byte Red, out byte Green, out byte Blue);

            redBox.Text = Red.ToString();
            redSlider.Value = Red;
            greenBox.Text = Green.ToString();
            greenSlider.Value = Green;
            blueBox.Text = Blue.ToString();
            blueSlider.Value = Blue;

            rgbBox.Text = SelectedEntry.Palette[Index].ToString("X4");

            ColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(Red, Green, Blue));
        }

        private void SetPaletteColorRGB(byte R, byte G, byte B)
        {
            ColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(R, G, B));
        }

        private static bool IsTextAllowed(string text)
        {
            return !new Regex("[^0-9.-]+").IsMatch(text);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Setting_Color)
                return;

            if (SelectedEntry != null)
            {

                Setting_Color = true;
                Slider slider = sender as Slider;
                byte Value = (byte)e.NewValue;
                if (slider == redSlider)
                {
                    redBox.Text = Value.ToString();
                }
                else if (slider == greenSlider)
                {
                    greenBox.Text = Value.ToString();

                }
                else if (slider == blueSlider)
                {
                    blueBox.Text = Value.ToString();
                }
                ushort Color = RGB888_to_RGB555(byte.Parse(redBox.Text), byte.Parse(greenBox.Text), byte.Parse(blueBox.Text));
                rgbBox.Text = Color.ToString("X4");
                SetPaletteColorRGB(byte.Parse(redBox.Text), byte.Parse(greenBox.Text), byte.Parse(blueBox.Text));
                Setting_Color = false;
            }
        }

        private void sliderBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Setting_Color)
                return;

            if (SelectedEntry != null)
            {
                TextBox Box = sender as TextBox;
                if (!IsTextAllowed(Box.Text))
                {
                    e.Handled = false;
                }
                else
                {
                    if (byte.TryParse(Box.Text, out byte Value))
                    {
                        Setting_Color = true;
                        ushort Color = RGB888_to_RGB555(byte.Parse(redBox.Text), byte.Parse(greenBox.Text), byte.Parse(blueBox.Text));
                        if (Box == redBox)
                        {
                            redSlider.Value = Value;
                        }
                        else if (Box == greenBox)
                        {
                            greenSlider.Value = Value;
                        }
                        else if (Box == blueBox)
                        {
                            blueSlider.Value = Value;
                        }
                        rgbBox.Text = Color.ToString("X4");
                        SetPaletteColorRGB(byte.Parse(redBox.Text), byte.Parse(greenBox.Text), byte.Parse(blueBox.Text));
                        Setting_Color = false;
                    }
                }
            }
        }

        private void rgbBox_PreviewTextInput(object sender, TextChangedEventArgs e)
        {
            if (Setting_Color)
                return;

            rgbBox.Text = rgbBox.Text.ToUpper();
            if (SelectedEntry != null && ushort.TryParse(rgbBox.Text, NumberStyles.AllowHexSpecifier, null, out ushort Color))
            {
                Setting_Color = true;
                RGB555_to_RGB888(Color, out byte R, out byte G, out byte B);
                SetPaletteColorRGB(R, G, B);
                RGBBox_LastText = rgbBox.Text;
                redBox.Text = R.ToString();
                redSlider.Value = R;
                greenBox.Text = G.ToString();
                greenSlider.Value = G;
                blueBox.Text = B.ToString();
                blueSlider.Value = B;
                Setting_Color = false;
            }
            else if (!string.IsNullOrEmpty(rgbBox.Text))
            {
                rgbBox.Text = RGBBox_LastText;
            }
            else
            {
                RGBBox_LastText = "";
            }
            rgbBox.SelectionStart = rgbBox.Text.Length;
            rgbBox.SelectionLength = 0;
        }

        private void SetColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEntry != null && ushort.TryParse(rgbBox.Text, NumberStyles.AllowHexSpecifier, null, out ushort Color))
            {
                for (int i = 0; i < SelectedEntry.Parent.Subentries.Length; i++)
                {
                    TextureEntry Current_Entry = SelectedEntry.Parent.Subentries[i];
                    Current_Entry.Palette[SelectedColor] = Color;
                    Current_Entry.Texture = TextureUtility.GenerateBitmap(Current_Entry.Raw_Data, Current_Entry.Palette, Current_Entry.Sections,
                        Current_Entry.Blocks, Current_Entry.Width, Current_Entry.Image_Width, Current_Entry.Image_Height);
                }
                // Redraw TreeView
                PopulateTreeView(TextureEntries);

                // Reload Current Bitmap & Palette
                SelectedImage.Source = BitmapSourceFromBitmap(SelectedEntry.Texture);
                Set_Palette_Colors(SelectedEntry.Palette);
            }
        }

        private void ImportOverSelected_Click(object sender, RoutedEventArgs e)
        {
            if (File_Location != null && SelectedEntry != null)
            {
                File_Select.FileName = "";
                File_Select.Filter = "Bitmap File|*.bmp";
                if (File_Select.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    byte[] Bitmap_Data = File.ReadAllBytes(File_Select.FileName);
                    if (Bitmap_Data[0] == 0x42 && Bitmap_Data[1] == 0x4D) // Check first two bytes are hex of ASCII "BM"
                    {
                        if (Bitmap_Data[0x1C] == 16) // Check bits per pixel is set to 16
                        {
                            if (BitConverter.ToInt32(Bitmap_Data.Skip(0x12).Take(4).ToArray(), 0) == SelectedEntry.Image_Width
                                && BitConverter.ToInt32(Bitmap_Data.Skip(0x16).Take(4).ToArray(), 0) == SelectedEntry.Image_Height) // Check for equal sizing
                            {
                                try
                                {
                                    // Strip Bitmap File Header (0x36 bytes)
                                    byte[] Stripped_Data = Bitmap_Data.Skip(0x36).ToArray();

                                    // Convert Stripped Data to ushorts
                                    ushort[] RGB555_Data = new ushort[Stripped_Data.Length / 2];

                                    // Flip it vertically
                                    int idx = 0;
                                    for (int i = Stripped_Data.Length - 2; i >= 0; i -= 2)
                                    {
                                        RGB555_Data[idx] = (ushort)((Stripped_Data[i + 1] << 8) + Stripped_Data[i]);
                                        idx++;
                                    }

                                    //Flip it horizontally
                                    for (int i = 0; i < RGB555_Data.Length; i += SelectedEntry.Image_Width)
                                    {
                                        Array.Reverse(RGB555_Data, i, SelectedEntry.Image_Width);
                                    }

                                    // Convert Sripped Data to AC format
                                    byte[] Converted_Data = TextureUtility.ConvertRGB555(RGB555_Data, SelectedEntry.Palette, SelectedEntry.Sections,
                                        SelectedEntry.Blocks, SelectedEntry.Width);
                                    SelectedEntry.Raw_Data = TextureUtility.Encode(Converted_Data);

                                    // Generate new Bitmap and redraw TreeView
                                    SelectedEntry.Texture = TextureUtility.GenerateBitmap(Converted_Data, SelectedEntry.Palette, SelectedEntry.Sections, SelectedEntry.Blocks,
                                        SelectedEntry.Width, SelectedEntry.Image_Width, SelectedEntry.Image_Height);

                                    PopulateTreeView(TextureEntries);
                                    Entry_Item_Selected(null, null, BitmapSourceFromBitmap(SelectedEntry.Texture), SelectedEntry);
                                }
                                catch { MessageBox.Show("An error occured while importing the image! The image cannot be processed."); }
                            }
                            else
                            {
                                MessageBox.Show("Unable to import the image because the width and height are not the same as the texture being replaced!");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Unable to import the image because the bits per pixel are wrong. Images need to be 16 bits per pixel (RGB555)");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Unable to import the image because it does not seem to be a valid bitmap file!");
                    }
                }
            }
        }

        private void DumpSelected_Click(object sender, RoutedEventArgs e)
        {
            if (File_Location != null && SelectedEntry != null)
            {
                string Output_Dir = Path.GetDirectoryName(File_Location) + @"\" + Path.GetFileNameWithoutExtension(File_Location);
                if (!Directory.Exists(Output_Dir))
                {
                    Directory.CreateDirectory(Output_Dir);
                }

                string Sub_Dir = Output_Dir + string.Format(@"\[{0}] - ", Array.IndexOf(TextureEntries, SelectedEntry.Parent)) + SelectedEntry.Parent.Texture_Name;
                if (!Directory.Exists(Sub_Dir))
                {
                    Directory.CreateDirectory(Sub_Dir);
                }
                string File_Name = Sub_Dir + @"\" + SelectedEntry.Entry_Index + ".bmp";
                string Palette_File_Name = Sub_Dir + @"\Palette.bmp";
                using (FileStream Texture_Stream = new FileStream(File_Name, FileMode.OpenOrCreate))
                {
                    SelectedEntry.Texture.Save(Texture_Stream, ImageFormat.Bmp);
                    Texture_Stream.Flush();
                }
                using (FileStream Palette_Stream = new FileStream(Palette_File_Name, FileMode.OpenOrCreate))
                {
                    TextureUtility.DrawPalette(SelectedEntry.Palette).Save(Palette_Stream, ImageFormat.Bmp);
                    Palette_Stream.Flush();
                }
            }
        }

        private void DumpAll_Click(object sender, RoutedEventArgs e)
        {
            if (File_Location != null)
            {
                string Output_Dir = Path.GetDirectoryName(File_Location) + @"\" + Path.GetFileNameWithoutExtension(File_Location);
                if (!Directory.Exists(Output_Dir))
                {
                    Directory.CreateDirectory(Output_Dir);
                }

                for (int i = 0; i < TextureEntries.Length; i++)
                {
                    string Sub_Dir = Output_Dir + string.Format(@"\[{0}] - ", i) + TextureEntries[i].Texture_Name;
                    if (!Directory.Exists(Sub_Dir))
                    {
                        Directory.CreateDirectory(Sub_Dir);
                    }
                    for (int Texture = 0; Texture < TextureEntries[i].Subentries.Length; Texture++)
                    {
                        string File_Name = Sub_Dir + @"\" + Texture + ".bmp";
                        using (FileStream Texture_Stream = new FileStream(File_Name, FileMode.OpenOrCreate))
                        {
                            TextureEntries[i].Subentries[Texture].Texture.Save(Texture_Stream, ImageFormat.Bmp);
                            Texture_Stream.Flush();
                        }
                    }

                    // Only save Palette once
                    if (TextureEntries[i].Subentries.Length > 0)
                    {
                        string Palette_File_Name = Sub_Dir + @"\Palette.bmp";
                        using (FileStream Palette_Stream = new FileStream(Palette_File_Name, FileMode.OpenOrCreate))
                        {
                            TextureUtility.DrawPalette(TextureEntries[i].Subentries[0].Palette).Save(Palette_Stream, ImageFormat.Bmp);
                            Palette_Stream.Flush();
                        }
                    }
                }
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            File_Select.FileName = "";
            File_Select.Filter = "Binary File|*.bin|All Files|*.*";

            if (File_Select.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Data_Buffer = File.ReadAllBytes(File_Select.FileName);
                File_Location = File_Select.FileName;

                switch (Path.GetFileNameWithoutExtension(File_Location))
                {
                    case "face_boy":
                        File_Type = 1;
                        ImageGridCanvas.Height = 256;
                        SelectedImage.Height = 256;
                        ImageGridCanvas.Background = Face_Brush;
                        TextureEntries = new TextureEntry[64];
                        for (int Face = 0; Face < 64; Face++)
                        {
                            int Face_Offset = 0xE20 * Face;
                            TextureEntry[] Subentries = new TextureEntry[14];
                            for (int Expression = 0; Expression < 14; Expression++)
                            {
                                int Offset = Face_Offset + 0x100 * Expression;
                                int Palette_Offset = Face_Offset + 0xE00;
                                Subentries[Expression] = new TextureEntry(Offset, Face_Offset + 0xE00, 2, 32, 4, 32, 16, Data_Buffer.Skip(Offset).Take(0x100).ToArray(),
                                    Data_Buffer.Skip(Palette_Offset).Take(0x20).ToArray());
                            }
                            TextureEntries[Face] = new TextureEntry(Subentries);
                            TextureEntries[Face].Texture_Name = ObjectDatabase.Faces[Face];
                        }
                        break;
                    case "tex_boy":
                    case "pallet_boy":
                        string File_Directory = Path.GetDirectoryName(File_Location);
                        bool Is_Tex = Path.GetFileNameWithoutExtension(File_Location).Equals("tex_boy");
                        if (File.Exists(File_Directory + (Is_Tex ? @"\pallet_boy.bin" : @"\tex_boy.bin")))
                        {
                            ImageGridCanvas.Height = 512;
                            SelectedImage.Height = 512;
                            ImageGridCanvas.Background = Shirt_Brush;
                            File_Type = 2;
                            Pallet_Buffer = File.ReadAllBytes(Is_Tex ? File_Directory + @"\pallet_boy.bin" : File_Location);
                            if (!Is_Tex)
                            {
                                Data_Buffer = File.ReadAllBytes(File_Directory + @"\tex_boy.bin");
                                Pallet_Location = File_Location;
                                File_Location = File_Directory + @"\tex_boy.bin";
                            }
                            else
                            {
                                Pallet_Location = File_Directory + @"\pallet_boy.bin";
                            }
                            TextureEntries = new TextureEntry[256];
                            for (int Shirt = 0; Shirt < 256; Shirt++)
                            {
                                int Shirt_Offset = 0x200 * Shirt;
                                TextureEntry[] Subentries = new TextureEntry[1];
                                int Palette_Offset = Shirt * 0x20;
                                Subentries[0] = new TextureEntry(Shirt_Offset, Palette_Offset, 4, 32, 4, 32, 32, Data_Buffer.Skip(Shirt_Offset).Take(0x200).ToArray(),
                                    Pallet_Buffer.Skip(Palette_Offset).Take(0x20).ToArray());
                                TextureEntries[Shirt] = new TextureEntry(Subentries);
                                TextureEntries[Shirt].Texture_Name = ObjectDatabase.Shirts[Shirt];
                            }
                        }
                        break;
                    case "player_room_floor":
                        File_Type = 3;
                        ImageGridCanvas.Height = 512;
                        SelectedImage.Height = 512;
                        ImageGridCanvas.Background = FloorCarpet_Brush;
                        TextureEntries = new TextureEntry[Data_Buffer.Length / 0x2020];
                        for (int Floor = 0; Floor < TextureEntries.Length; Floor++)
                        {
                            int Floor_Offset = 0x2020 * Floor;
                            TextureEntry[] Subentries = new TextureEntry[4];
                            for (int Pattern = 0; Pattern < 4; Pattern++)
                            {
                                int Offset = Floor_Offset + 0x20 + 0x800 * Pattern;
                                int Palette_Offset = Floor_Offset;
                                Subentries[Pattern] = new TextureEntry(Offset, Floor_Offset, 8, 64, 8, 64, 64, Data_Buffer.Skip(Offset).Take(0x800).ToArray(),
                                    Data_Buffer.Skip(Palette_Offset).Take(0x20).ToArray());
                            }
                            TextureEntries[Floor] = new TextureEntry(Subentries);
                            TextureEntries[Floor].Texture_Name = ObjectDatabase.Carpets[Floor];
                        }
                        break;
                    case "player_room_wall":
                        File_Type = 4;
                        ImageGridCanvas.Height = 512;
                        SelectedImage.Height = 512;
                        ImageGridCanvas.Background = FloorCarpet_Brush;
                        TextureEntries = new TextureEntry[Data_Buffer.Length / 0x1020];
                        for (int Wall = 0; Wall < TextureEntries.Length; Wall++)
                        {
                            int Wall_Offset = 0x1020 * Wall;
                            TextureEntry[] Subentries = new TextureEntry[2];
                            for (int Pattern = 0; Pattern < 2; Pattern++)
                            {
                                int Offset = Wall_Offset + 0x20 + 0x800 * Pattern;
                                int Palette_Offset = Wall_Offset;
                                Subentries[Pattern] = new TextureEntry(Offset, Wall_Offset, 8, 64, 8, 64, 64, Data_Buffer.Skip(Offset).Take(0x800).ToArray(),
                                    Data_Buffer.Skip(Palette_Offset).Take(0x20).ToArray());
                            }
                            TextureEntries[Wall] = new TextureEntry(Subentries);
                            TextureEntries[Wall].Texture_Name = ObjectDatabase.Wallpaper[Wall];
                        }
                        break;
                    default:
                        File_Type = 0;
                        break;
                }

                if (TextureEntries.Length > 0 && TextureEntries[0] != null)
                {
                    if (TextureEntries[0].Subentries.Length > 0 && TextureEntries[0].Subentries[0] != null)
                    {
                        SelectedImage.Source = BitmapSourceFromBitmap(TextureEntries[0].Subentries[0].Texture);
                        Set_Palette_Colors(TextureEntries[0].Subentries[0].Palette);
                        SelectedEntry = TextureEntries[0].Subentries[0];
                        SelectedLabel.Content = string.Format("{0} - {1}", SelectedEntry.Parent.Texture_Name, SelectedEntry.Entry_Index);
                        SetPaletteColor(0);

                        // Enable Controls
                        Import.IsEnabled = true;
                        Dump.IsEnabled = true;
                        DumpAll.IsEnabled = true;
                        redSlider.IsEnabled = true;
                        greenSlider.IsEnabled = true;
                        blueSlider.IsEnabled = true;
                        redBox.IsEnabled = true;
                        greenBox.IsEnabled = true;
                        blueBox.IsEnabled = true;
                        rgbBox.IsEnabled = true;
                        SetColorButton.IsEnabled = true;
                    }
                    else
                    {
                        SelectedImage.Source = null;
                    }
                }
                else
                {
                    SelectedImage.Source = null;
                }

                PopulateTreeView(TextureEntries);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (File_Location != null)
            {
                using (BinaryWriter Texture_Writer = new BinaryWriter(new FileStream(File_Location, FileMode.OpenOrCreate)))
                {
                    BinaryWriter Palette_Writer = File_Type == 2 ? new BinaryWriter(new FileStream(Pallet_Location, FileMode.OpenOrCreate)) : null;
                    for (int i = 0; i < TextureEntries.Length; i++)
                    {
                        if (TextureEntries[i].IsContainer)
                        {
                            for (int x = 0; x < TextureEntries[i].Subentries.Length; x++)
                            {
                                TextureEntries[i].Subentries[x].Write(Texture_Writer, Palette_Writer);
                            }
                        }
                        else
                        {
                            TextureEntries[i].Write(Texture_Writer, Palette_Writer);
                        }
                    }
                    Texture_Writer.Flush();
                    Texture_Writer.Close();
                    if (Palette_Writer != null)
                    {
                        Palette_Writer.Flush();
                        Palette_Writer.Close();
                    }
                }
            }
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (File_Location != null)
            {
                File_Save.FileName = Path.GetFileName(File_Location);
                File_Save.Filter = "Binary File|*.bin";
                if (File_Save.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    using (BinaryWriter Texture_Writer = new BinaryWriter(new FileStream(File_Save.FileName, FileMode.OpenOrCreate)))
                    {
                        BinaryWriter Palette_Writer = File_Type == 2 ? new BinaryWriter(new FileStream(Path.GetDirectoryName(File_Save.FileName)
                            + @"\pallet_boy.bin", FileMode.OpenOrCreate)) : null;
                        for (int i = 0; i < TextureEntries.Length; i++)
                        {
                            if (TextureEntries[i].IsContainer)
                            {
                                for (int x = 0; x < TextureEntries[i].Subentries.Length; x++)
                                {
                                    TextureEntries[i].Subentries[x].Write(Texture_Writer, Palette_Writer);
                                }
                            }
                            else
                            {
                                TextureEntries[i].Write(Texture_Writer, Palette_Writer);
                            }
                        }
                        Texture_Writer.Flush();
                        Texture_Writer.Close();
                        if (Palette_Writer != null)
                        {
                            Palette_Writer.Flush();
                            Palette_Writer.Close();
                        }
                    }
                }
            }
        }

        // From: https://stackoverflow.com/questions/26260654/wpf-converting-bitmap-to-imagesource
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public BitmapSource BitmapSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
    }
}
