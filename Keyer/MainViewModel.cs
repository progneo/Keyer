using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Color = System.Drawing.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MediaColor = System.Windows.Media.Color;

namespace Keyer;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public DelegateCommand OpenFileCommand { get; private set; }
    public DelegateCommand ProcessImageCommand { get; private set; }
    public DelegateCommand SaveImageCommand { get; private set; }

    private string _imageUri = string.Empty;
    public string ImageUri
    {
        get => _imageUri;
        set {}
    }

    public BitmapSource ResultImage { get; private set; } = new BitmapImage();
    public MediaColor SelectedColor { get; set; }

    public bool IsFileSelected => _imageUri.Length > 0;
    public bool IsImageProcessed { get; private set; }

    public MainViewModel()
    {
        SelectedColor = (MediaColor)ColorConverter.ConvertFromString("#FF00cd18");
        IsImageProcessed = false;
        OpenFileCommand = new DelegateCommand(OpenImage);
        ProcessImageCommand = new DelegateCommand(ProcessImage);
        SaveImageCommand = new DelegateCommand(SaveImage);
    }

    private void OpenImage()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            FileName = "Изображение",
            DefaultExt = ".png",
            Filter = "Image Files|*.png;"
        };

        var result = dialog.ShowDialog();

        if (result != true) return;

        Console.WriteLine(dialog.FileName);
        _imageUri = dialog.FileName;

        OnPropertyChanged(nameof(ImageUri));
        OnPropertyChanged(nameof(IsFileSelected));
    }

    private void SaveImage()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = "Изображение",
            DefaultExt = ".png",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;"
        };

        var result = dialog.ShowDialog();

        if (result != true) return;

        using var fileStream = new FileStream(dialog.FileName, FileMode.Create);

        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(ResultImage));
        encoder.Save(fileStream);
    }

    private async void ProcessImage()
    {
        Console.WriteLine($"Image processing {SelectedColor}");
        var bmp = new Bitmap(_imageUri);

        bmp.MakeTransparent();
        await Task.Run(() =>
        {
            for (var x = 0; x < bmp.Width; x++)
            {
                for (var y = 0; y < bmp.Height; y++)
                {
                    var pixelColor = bmp.GetPixel(x, y);

                    if (IsDominantColor(pixelColor, SelectedColor))
                    {
                        bmp.SetPixel(x, y, Color.Transparent);
                    }
                }
            }
        });

        ResultImage = Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        IsImageProcessed = true;

        OnPropertyChanged(nameof(ResultImage));
        OnPropertyChanged(nameof(IsImageProcessed));
    }

    private static bool IsDominantColor(Color pixelColor, MediaColor colorToRemove)
    {
        return Math.Abs(pixelColor.R - colorToRemove.R) < 100 &&
               Math.Abs(pixelColor.G - colorToRemove.G) < 100 &&
               Math.Abs(pixelColor.B - colorToRemove.B) < 100;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}