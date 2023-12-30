using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Instafade_Generator_WPF
{
    public partial class MainWindow : Window
    {
        private string SkinFolder = "";
        private static string quality = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        public static List<string> GrabPngs(string skinPath, string prefix)
        {
            Directory.SetCurrentDirectory(skinPath);

            string[] sdNums = Directory.GetFiles(".", $"{prefix}-?.png");
            string[] sdCircles = Directory.GetFiles(".", $"hitcircle*.png");
            string[] hdNums = Directory.GetFiles(".", $"{prefix}-?@2x.png");
            string[] hdCircles = Directory.GetFiles(".", $"hitcircle*" + "@2x.png");

            List<string> sdFiles = sdNums.Concat(sdCircles).ToList();
            List<string> hdFiles = hdNums.Concat(hdCircles).ToList();

            if (hdFiles.Count == 12)
            {
                quality = "@2x";
                return hdFiles;
            }

            else {
                quality = "";
                return sdFiles;
            }

            
        }

        public static string GrabDefaultPrefix(string path)
        {
            using (StreamReader sr = new StreamReader(path + "/skin.ini"))
            {
                string data = sr.ReadToEnd().ToLower();
                Regex prefixRegex = new Regex(@"hitcircleprefix: [a-z0-9]+");
                string prefix = prefixRegex.Match(data).Value.Split()[1];
                return prefix;
            }
        }

        public static void BackupFiles(List<string> files)
        {
            Directory.CreateDirectory("Backup");

            for (int i = 0; i < files.Count; i++)
            {
                files[i] = files[i].Substring(2);
            }

            files.Add("skin.ini");

            if (File.Exists($"default-x{quality}.png"))
            {
                files.Remove($"default-x{quality}.png");
            }

            foreach (string png in files)
            {
                if (!File.Exists("Backup/" + png) & File.Exists(png))
                {
                    File.Copy(png, "Backup/" + png);
                }
            }
        }

        public static List<Tuple<int, int, int>> GrabComboColours(string path)
        {
            using (StreamReader ini = new StreamReader(System.IO.Path.Combine(path, "skin.ini")))
            {
                string data = ini.ReadToEnd().ToLower();
                List<Tuple<int, int, int>> colours = new List<Tuple<int, int, int>>();
                Regex regex = new Regex(@"\ncombo\d: ([0-9]+, *[0-9]+, *[0-9]+)");
                MatchCollection matches = regex.Matches(data);
                foreach (Match match in matches)
                {
                    string colour = match.Groups[1].Value;
                    string[] components = colour.Split(',');
                    int r = int.Parse(components[0]);
                    int g = int.Parse(components[1]);
                    int b = int.Parse(components[2]);
                    colours.Add(new Tuple<int, int, int>(r, g, b));
                }
                return colours;
            }
        }

        public void Undo(string path, List<string> png_list)
        {
            List<string> filenames = new List<string>();
            foreach (var item in png_list)
            {
                filenames.Add(item.Substring(2));
            }
            filenames.Add("skin.ini");
            string backup_path = Path.Combine(path, "Backup");
            Directory.SetCurrentDirectory(backup_path);
            string[] files = Directory.GetFiles(backup_path);
            foreach (var file in files)
            {
                if (filenames.Contains(Path.GetFileName(file)))
                {
                    string destination = Path.Combine(path, Path.GetFileName(file));
                    if (File.Exists(destination))
                    {
                        File.Delete(destination);
                    }
                    File.Move(file, destination);
                }
            }
            update();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (SkinFolder != "")
            {
                string prefix = GrabDefaultPrefix(SkinFolder);
                Undo(SkinFolder, GrabPngs(SkinFolder, prefix));
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new()
            { IsFolderPicker = true };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SkinFolder = dialog.FileName;
                string prefix = GrabDefaultPrefix(SkinFolder);
                List<string> dummy = GrabPngs(SkinFolder, prefix);
                update();
            }
        }

        private void update()
        {

            Directory.SetCurrentDirectory(SkinFolder);
            string prefix = GrabDefaultPrefix(SkinFolder);
            var comboColours = GrabComboColours(SkinFolder);
            ColourSelectBox.Items.Clear();
            foreach (var colour in comboColours.ToArray())
            {
                ColourSelectBox.Items.Add(colour.ToString());
            }

            ColourSelectBox.SelectedIndex = 0;
            int comboindex = ColourSelectBox.SelectedIndex;
            Color combo_colour = Color.FromArgb(255, 255, 255);
            try
            {
                combo_colour = Color.FromArgb(comboColours[comboindex].Item1, comboColours[comboindex].Item2, comboColours[comboindex].Item3);
            }
            catch (Exception) { }
           
            GeneratePreview(combo_colour, prefix);
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (SkinFolder != "")
            {
                string prefix = GrabDefaultPrefix(SkinFolder);
                var comboColours = GrabComboColours(SkinFolder);

                List<string> pnglist = GrabPngs(SkinFolder, prefix);
                BackupFiles(pnglist);

                List<string> defaults = new List<string>();
                for (int i = 0; i < 10; i++)
                {
                    defaults.Add($"{prefix}-{i}{quality}.png");
                }

                Color combo_colour = Color.White;
                int comboindex = ColourSelectBox.SelectedIndex;
                try
                {
                    combo_colour = Color.FromArgb(comboColours[comboindex].Item1, comboColours[comboindex].Item2, comboColours[comboindex].Item3);
                }
                catch (Exception) { }

                OverlayImages(defaults, combo_colour);

                foreach (var name in defaults)
                {
                    File.Delete(name);
                    File.Move("a" + name, name);
                }
                GenerateEmpty();
                update();
                MessageBox.Show("Done");
            }
        }

        private void GenerateEmpty()
        {
            using (Bitmap bmp = new Bitmap(1, 1))
            {
                bmp.SetPixel(0, 0, Color.Transparent);
                bmp.Save($"hitcircle{quality}.png", System.Drawing.Imaging.ImageFormat.Png);
            }
            File.Delete($"hitcircleoverlay{quality}.png");
            File.Copy($"hitcircle{quality}.png", $"hitcircleoverlay{quality}.png");

        }

        private void AdjustINI(string path, int example)
        {
            List<string> newline = new List<string>();
            using (StreamReader sr = new StreamReader(Path.Combine(path, "skin.ini")))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.ToLower().StartsWith("hitcircleoverlap"))
                    {
                        newline.Add($"HitCircleOverlap: {example}\n");
                    }
                    else
                    {
                        newline.Add(line);
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter(Path.Combine(path, "skin.ini")))
            {
                foreach (string line in newline)
                {
                    sw.WriteLine(line);
                }
            }
        }

        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        private void ColourSelectBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SkinFolder != "")
            {
                string prefix = GrabDefaultPrefix(SkinFolder);
                var comboColours = GrabComboColours(SkinFolder);
                int comboindex = ColourSelectBox.SelectedIndex;
                try
                {
                    Color combo_colour = Color.FromArgb(comboColours[comboindex].Item1, comboColours[comboindex].Item2, comboColours[comboindex].Item3);
                    GeneratePreview(combo_colour, prefix);
                }
                catch (Exception)
                {
                    Color combo_colour = Color.White;
                    GeneratePreview(combo_colour, prefix);
                }
            }
        }

        private void MultiplyColour(Bitmap image, Color Colour)
        {
            for (int w = 0; w < image.Width; w++)
            {
                for (int h = 0; h < image.Height; h++)
                {
                    Color pixel = image.GetPixel(w, h);
                    int resultR = (pixel.R * Colour.R) / 255;
                    int resultG = (pixel.G * Colour.G) / 255;
                    int resultB = (pixel.B * Colour.B) / 255;
                    int resultA = pixel.A;

                    Color newColor = Color.FromArgb(resultA, resultR, resultG, resultB);
                    image.SetPixel(w, h, newColor);
                }
            }
        }

        public void GeneratePreview(Color ComboColour, string prefix)
        {
            Bitmap circ = new Bitmap($"hitcircle{quality}.png");
            Bitmap overlay = new Bitmap($"hitcircleoverlay{quality}.png");

            MultiplyColour(circ, ComboColour);

            Bitmap image1 = new Bitmap(circ, (int)Math.Round(circ.Width * 1.25), (int)Math.Round(circ.Height * 1.25));
            circ.Dispose();

            Bitmap image3 = new Bitmap(overlay, (int)Math.Round(overlay.Width * 1.25), (int)Math.Round(overlay.Height * 1.25));

            Image blah = Image.FromFile($"{prefix}-0{quality}.png");
            int defsize = Math.Max(blah.Height, blah.Width);

            Bitmap image2 = ExpandImageBounds(blah, defsize, defsize);
            blah.Dispose();

            // Create a bitmap to hold the combined image.
            int maxWidth = Math.Max(image1.Width, image3.Width);
            int maxHeight = Math.Max(image1.Height, image3.Height);
            Bitmap finalImage = new Bitmap(maxWidth, maxHeight);

            // Get a graphics object from the final image.
            Graphics g = Graphics.FromImage(finalImage);

            // Draw the images onto the final image.
            g.DrawImage(image1, (finalImage.Width - image1.Width), (finalImage.Height - image1.Height) / 2);
            g.DrawImage(image2, ((finalImage.Width - image2.Width) / 2), ((finalImage.Height - image2.Height) / 2));
            g.DrawImage(image3, (finalImage.Width - image3.Width), (finalImage.Height - image3.Height) / 2);

            // Save the final image.
            PreviewImage.Source = BitmapToImageSource(finalImage);

            // Free Memory
            g.Dispose();
            finalImage.Dispose();
            image2.Dispose();
            overlay.Dispose();
            image1.Dispose();
            image3.Dispose();
        }

        public static Bitmap ExpandImageBounds(Image originalImage, int newWidth, int newHeight)
        {
            Bitmap expandedImage = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(expandedImage))
            {
                g.Clear(Color.Transparent);
                int xOffset = (newWidth - originalImage.Width) / 2;
                int yOffset = (newHeight - originalImage.Height) / 2;
                g.DrawImage(originalImage, xOffset, yOffset, originalImage.Width, originalImage.Height);
            }
            return expandedImage;
        }

        public void OverlayImages(List<string> png_list, Color ComboColour)
        {
            Bitmap circ = new Bitmap($"hitcircle{quality}.png");
            Bitmap overlay = new Bitmap($"hitcircleoverlay{quality}.png");

            MultiplyColour(circ, ComboColour);

            Bitmap image1 = new Bitmap(circ, (int)Math.Round(circ.Width * 1.25), (int)Math.Round(circ.Height * 1.25));
            circ.Dispose();
            Bitmap image3 = new Bitmap(overlay, (int)Math.Round(overlay.Width * 1.25), (int)Math.Round(overlay.Height * 1.25));

            foreach (string file in png_list)
            {
                var temppath = "a" + file;

                Image blah = Image.FromFile(file);
                int defsize = Math.Max(blah.Height, blah.Width);

                Bitmap image2 = ExpandImageBounds(blah, defsize, defsize);
                blah.Dispose();

                // Create a bitmap to hold the combined image.
                int maxWidth = Math.Max(image1.Width, image3.Width);
                int maxHeight = Math.Max(image1.Height, image3.Height);
                Bitmap finalImage = new Bitmap(maxWidth, maxHeight);

                // Get a graphics object from the final image.
                Graphics g = Graphics.FromImage(finalImage);

                // Draw the images onto the final image.
                g.DrawImage(image1, (maxWidth - image1.Width) / 2, (maxHeight - image1.Height) / 2);
                g.DrawImage(image2, (maxWidth - image2.Width) / 2, (maxHeight - image2.Height) / 2);
                g.DrawImage(image3, (maxWidth - image3.Width) / 2, (maxHeight - image3.Height) / 2);

                // Dispose the Graphics object.
                g.Dispose();

                AdjustINI(SkinFolder, finalImage.Width);

                // Save the final image.
                finalImage.Save(temppath, System.Drawing.Imaging.ImageFormat.Png);

                // Dispose the final image.
                finalImage.Dispose();
                image2.Dispose();
            }
            overlay.Dispose();
            image1.Dispose();
            image3.Dispose();
        }
    }
}