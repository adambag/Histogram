using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.IO;

namespace Histogram
{
    public partial class MainWindow : Window
    {
        private Bitmap _bitmap;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Pliki obrazów (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png";
            if (openFileDialog.ShowDialog() == true)
            {
                _bitmap = new Bitmap(openFileDialog.FileName);
                LoadedImage.Source = BitmapToImageSource(_bitmap);
            }
        }

        private void NormalizeHistogram_Click(object sender, RoutedEventArgs e)
        {
            if (_bitmap != null)
            {
                _bitmap = HistogramNormalization(_bitmap);
                LoadedImage.Source = BitmapToImageSource(_bitmap);
            }
        }

        private void EqualizeHistogram_Click(object sender, RoutedEventArgs e)
        {
            if (_bitmap != null)
            {
                _bitmap = HistogramEqualization(_bitmap);
                LoadedImage.Source = BitmapToImageSource(_bitmap);
            }
        }

        private void BinaryThreshold_Click(object sender, RoutedEventArgs e)
        {
            if (_bitmap != null && int.TryParse(ThresholdInput.Text, out int threshold))
            {
                _bitmap = ApplyBinaryThreshold(_bitmap, threshold);
                LoadedImage.Source = BitmapToImageSource(_bitmap);
            }
        }

        private void PercentBlackSelection_Click(object sender, RoutedEventArgs e)
        {
            if (_bitmap != null && int.TryParse(PercentBlackInput.Text, out int percentBlack))
            {
                _bitmap = PercentBlackSelection(_bitmap, percentBlack);
                LoadedImage.Source = BitmapToImageSource(_bitmap);
            }
            else
            {
                MessageBox.Show("Wprowadź poprawną wartość procentową dla czarnych pikseli.");
            }
        }

        private void MeanIterativeSelection_Click(object sender, RoutedEventArgs e)
        {
            if (_bitmap != null)
            {
                _bitmap = MeanIterativeSelection(_bitmap);
                LoadedImage.Source = BitmapToImageSource(_bitmap);
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
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

        private Bitmap HistogramNormalization(Bitmap bitmap)
        {
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);
            int min = 255, max = 0;

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int pixel = bitmap.GetPixel(x, y).R;
                    if (pixel < min) min = pixel;
                    if (pixel > max) max = pixel;
                }
            }

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int pixel = bitmap.GetPixel(x, y).R;
                    int newPixel = (pixel - min) * 255 / (max - min);
                    result.SetPixel(x, y, Color.FromArgb(newPixel, newPixel, newPixel));
                }
            }

            return result;
        }
        private Bitmap HistogramEqualization(Bitmap bitmap)
        {
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);
            int[] histogram = new int[256];
            int[] cdf = new int[256];
            int totalPixels = bitmap.Width * bitmap.Height;

            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                    histogram[bitmap.GetPixel(x, y).R]++;

            cdf[0] = histogram[0];
            for (int i = 1; i < 256; i++)
                cdf[i] = cdf[i - 1] + histogram[i];

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int pixel = bitmap.GetPixel(x, y).R;
                    int newPixel = cdf[pixel] * 255 / totalPixels;
                    result.SetPixel(x, y, Color.FromArgb(newPixel, newPixel, newPixel));
                }
            }

            return result;
        }
        private Bitmap ApplyBinaryThreshold(Bitmap bitmap, int threshold)
        {
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int pixel = bitmap.GetPixel(x, y).R;
                    int newPixel = (pixel >= threshold) ? 255 : 0;
                    result.SetPixel(x, y, Color.FromArgb(newPixel, newPixel, newPixel));
                }
            }

            return result;
        }
        private Bitmap PercentBlackSelection(Bitmap bitmap, int percentBlack)
        {
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);
            int[] histogram = new int[256];
            int totalPixels = bitmap.Width * bitmap.Height;

            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                    histogram[bitmap.GetPixel(x, y).R]++;

            int targetBlackPixels = (totalPixels * percentBlack) / 100;
            int cumulativePixels = 0;
            int threshold = 0;

            for (int i = 0; i < 256; i++)
            {
                cumulativePixels += histogram[i];
                if (cumulativePixels >= targetBlackPixels)
                {
                    threshold = i;
                    break;
                }
            }

            return ApplyBinaryThreshold(bitmap, threshold);
        }
        private Bitmap MeanIterativeSelection(Bitmap bitmap)
        {
            Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);
            int threshold = 127; // Początkowa wartość progu
            int newThreshold;
            bool continueIteration;

            do
            {
                int lowSum = 0, highSum = 0, lowCount = 0, highCount = 0;

                for (int y = 0; y < bitmap.Height; y++)
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        int pixel = bitmap.GetPixel(x, y).R;
                        if (pixel < threshold)
                        {
                            lowSum += pixel;
                            lowCount++;
                        }
                        else
                        {
                            highSum += pixel;
                            highCount++;
                        }
                    }

                int meanLow = (lowCount > 0) ? (lowSum / lowCount) : 0;
                int meanHigh = (highCount > 0) ? (highSum / highCount) : 255;
                newThreshold = (meanLow + meanHigh) / 2;
                continueIteration = Math.Abs(newThreshold - threshold) > 1;
                threshold = newThreshold;

            } while (continueIteration);

            return ApplyBinaryThreshold(bitmap, threshold);
        }
    }
}
