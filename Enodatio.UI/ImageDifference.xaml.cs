using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Enodatio.Logic.Processors;
using Enodatio.UI.Extensions;
using Enodatio.UI.Services;
using Enodatio.UI.ViewModels;
using Microsoft.Win32;

namespace Enodatio.UI;

public partial class ImageDifference : Window
{
    private const string FailFilePath = "./www/images/Fail.png";

    private static readonly BitmapImage FailImage =
        new BitmapImage(new Uri(Path.Combine(Directory.GetCurrentDirectory(), FailFilePath)));

    private static readonly Regex NumbersOnly = new Regex("[^0-9.-]+");

    private readonly WindowFactory _windowFactory;
    private readonly DifferenceProcessor _differenceProcessor;

    public ImageDifference(DifferenceProcessor differenceProcessor, WindowFactory windowFactory)
    {
        InitializeComponent();

        _differenceProcessor = differenceProcessor;
        _windowFactory = windowFactory;

        DataContext = new ImageDifferenceViewModel();
    }

    private async void UploadFirstImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        var viewModel = (ImageDifferenceViewModel) DataContext;
        var openFileDialog = new OpenFileDialog()
        {
            Filter = "Images only. |*.png;*.jpeg;*.jpg;*.bmp;",
            FilterIndex = 1,
            Multiselect = false
        };

        if (openFileDialog.ShowDialog().GetValueOrDefault())
        {
            var fileUri = new Uri(openFileDialog.FileName);
            viewModel.First = new BitmapImage(fileUri);

            if (viewModel.Second is not null)
            {
                await UpdateDifferenceImageAsync(viewModel);
            }
        }
    }

    private async void UploadSecondImageButton_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var viewModel = (ImageDifferenceViewModel) DataContext;

        var openFileDialog = new OpenFileDialog()
        {
            Filter = "Images only. |*.png;*.jpeg;*.jpg;*.bmp",
            FilterIndex = 1,
            Multiselect = false
        };

        if (openFileDialog.ShowDialog().GetValueOrDefault())
        {
            var fileUri = new Uri(openFileDialog.FileName);
            viewModel.Second = new BitmapImage(fileUri);

            if (viewModel.First is not null)
            {
                await UpdateDifferenceImageAsync(viewModel);
            }
        }
    }

    private async Task UpdateDifferenceImageAsync(ImageDifferenceViewModel viewModel)
    {
        try
        {
            var differenceImage = await GetDifferenceAsync(viewModel);
            viewModel.IsDifferenceImageSet = true;
            viewModel.Difference = differenceImage;
        }
        catch (Exception)
        {
            viewModel.Reset();
            viewModel.Difference = FailImage;
        }
    }

    private async Task<BitmapImage> GetDifferenceAsync(ImageDifferenceViewModel viewModel)
    {
        return await Task.Run(() =>
        {
            using var firstOutStream = new MemoryStream();
            using var secondOutStream = new MemoryStream();
            
            var bitmapEncoder = new BmpBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(viewModel.First!));
            bitmapEncoder.Save(firstOutStream);
            var first = new Bitmap(firstOutStream);

            bitmapEncoder = new BmpBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(viewModel.Second!));
            bitmapEncoder.Save(secondOutStream);
            var second = new Bitmap(secondOutStream);

            var difference = _differenceProcessor.GetDifference(first, second, viewModel.Threshold);
            
            return difference.ToBitmapImage();
        });
    }

    private void Reset_OnClick(object sender, RoutedEventArgs e)
    {
        var viewModel = (ImageDifferenceViewModel) DataContext;
        viewModel.Reset();
    }

    private void ThresholdText_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = NumbersOnly.IsMatch(e.Text);
    }

    private async void SubmitThreshold_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var viewModel = (ImageDifferenceViewModel) DataContext;

        viewModel.Threshold = byte.Parse(ThresholdText.Text);
        await UpdateDifferenceImageAsync(viewModel);
    }

    private async void SaveDifference_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var viewModel = (ImageDifferenceViewModel) DataContext;
        
        var saveFileDialog = new SaveFileDialog()
        {
            Filter = "PNG or JPG |*.png;*.jpg",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };

        if (saveFileDialog.ShowDialog().GetValueOrDefault())
        {
            using (var memoryStream = new MemoryStream())
            {
                var bitmapEncoder = new BmpBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(viewModel.Difference!));
                bitmapEncoder.Save(memoryStream);

                await File.WriteAllBytesAsync(saveFileDialog.FileName, memoryStream.ToArray());
            }
        }
    }
    
    private void ModeItem_OnClick(object sender, RoutedEventArgs e)
    {
        var clickedButton = (MenuItem) sender;
        switch (clickedButton.Name)
        {
            case "ImageDifferenceMode":
                _windowFactory.Create<ImageDifference>().Show();
                break;

            case "ImageAnalysisMode":
                _windowFactory.Create<ImageAnalysis>().Show();
                break;
        }
    }
}