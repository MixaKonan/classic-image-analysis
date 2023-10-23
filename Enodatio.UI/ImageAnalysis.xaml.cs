using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Enodatio.Logic.Models;
using Enodatio.Logic.Processors;
using Enodatio.UI.Extensions;
using Enodatio.UI.Services;
using Enodatio.UI.ViewModels;
using Microsoft.Win32;
using ScottPlot;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using Orientation = System.Windows.Controls.Orientation;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace Enodatio.UI;

public partial class ImageAnalysis : Window
{
    private readonly ImageAnalyser _imageAnalyser;
    private readonly WindowFactory _windowFactory;

    private Dictionary<string, List<Image>> BeamImages { get; } = new();

    public ImageAnalysis(ImageAnalyser imageAnalyser, WindowFactory windowFactory)
    {
        InitializeComponent();

        DataContext = new ImageAnalysisViewModel();

        _imageAnalyser = imageAnalyser;
        _windowFactory = windowFactory;
    }

    private void Reset_OnClickAsync(object sender, RoutedEventArgs e)
    {
        ((ImageAnalysisViewModel) DataContext).Reset();
    }

    private async void UploadImageButton_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var viewModel = (ImageAnalysisViewModel) DataContext;

        var openFileDialog = new OpenFileDialog()
        {
            Filter = "Images only. |*.png;*.jpeg;*.jpg;*.bmp",
            FilterIndex = 1,
            Multiselect = false
        };

        if (openFileDialog.ShowDialog().GetValueOrDefault())
        {
            var fileUri = new Uri(openFileDialog.FileName);
            var bitmapImage = new BitmapImage(fileUri);
            viewModel.Image = bitmapImage;
            viewModel.OriginalImage = bitmapImage;

            await UpdatePixelCountPlotsAsync(viewModel);
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

    private async void DrawBeams_OnClickAsync(object sender, RoutedEventArgs e)
    {
        BeamImages.Clear();
        BeamSelectComboBox.Items.Clear();
        await UpdateBeamInfoAsync();
    }
    
    private async void ApplyConfigurations_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var viewModel = (ImageAnalysisViewModel) DataContext;
        
        var value = byte.Parse(IntensitySpan.Text);

        if (value == 1)
        {
            value = 1;
        }

        viewModel.IntensitySpan = value;
        
        var updatePlotsTask = Task.WhenAll(UpdatePixelCountPlotsAsync(viewModel), UpdateBeamInfoAsync());
        
        BeamGraphsPanel.Children.Clear();
        BeamSelectComboBox.Items.Clear();
        BeamImages.Clear();

        await updatePlotsTask;
    }

    private void IntensitySpan_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = Common.Regexes.NumbersOnly.IsMatch(e.Text);
    }

    private async Task UpdatePixelCountPlotsAsync(ImageAnalysisViewModel viewModel)
    {
        var pixelCounts = await Task.Run(
            () => _imageAnalyser.GetPixelCounts(
                viewModel.Image!.ToBitmap(),
                viewModel.IntensitySpan));

        var pixelCountFigure = GetPixelCountGraph(pixelCounts);
        var cumulativePixelCountFigure = GetCumulativePixelCountGraph(pixelCounts);

        viewModel.PixelCountFigure = pixelCountFigure;
        viewModel.CumulativePixelCountFigure = cumulativePixelCountFigure;

        PixelCountGraphsBorder.Visibility = Visibility.Visible;
    }

    private async Task UpdateBeamInfoAsync()
    {
        var viewModel = (ImageAnalysisViewModel) DataContext;
        var (newImage, beams) = await DrawBeamsAsync(viewModel.OriginalImage!, viewModel.Degrees);

        for (var index = 0; index < beams.Count; index++)
        {
            var beam = beams[index];
            var label = $"Beam {index + 1}";

            var beamPixelIntensities = _imageAnalyser.GetPixelCounts(beam.Pixels, viewModel.IntensitySpan);
            var xLabels = beamPixelIntensities.Keys.Select(value => value.ToString()).ToArray();
            var values = beamPixelIntensities.Values.Select(value => (double) value).ToArray();

            var beamPixelCountGraph = new Image()
            {
                Source = GetBeamPixelCountGraph(xLabels, values)
            };

            var cumulativeBeamPixelCountFigure = new Image()
            {
                Source = GetCumulativeBeamPixelCountGraph(xLabels, values)
            };

            var resultingImage = new Image()
            {
                Source = GetCumulativePixelCountImage(600, 300, beamPixelIntensities)
            };

            BeamImages.Add(label, new List<Image>() {beamPixelCountGraph, cumulativeBeamPixelCountFigure, resultingImage});
            BeamSelectComboBox.Items.Add(label);
        }

        viewModel.Image = newImage;
        BeamSelectComboBox.SelectedItem = BeamSelectComboBox.Items[0];
        BeamGraphsBorder.Visibility = Visibility.Visible;
    }

    private async Task<(BitmapImage, List<Beam>)> DrawBeamsAsync(BitmapImage originalImage, int degrees)
    {
        return await Task.Run(() =>
        {
            var original = originalImage.ToBitmap();

            var clone = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppPArgb);

            using var gr = Graphics.FromImage(clone);

            gr.DrawImage(original, new Rectangle(0, 0, clone.Width, clone.Height));

            var beams = _imageAnalyser.DrawBeamsFromCenter(clone, degrees, Color.Red);

            var newImage = clone.ToBitmapImage();

            return (newImage, beams);
        });
    }

    private static BitmapImage GetPixelCountGraph(Dictionary<Range, int> pixelCounts)
    {
        var labels = pixelCounts.Keys.Select(value => value.ToString()).ToArray();
        var values = pixelCounts.Values.Select(value => (double) value).ToArray();
        var plot = new Plot();

        plot.AddBar(values);
        plot.YAxis.Label("Pixel Count");

        plot.XTicks(labels);
        plot.XAxis.TickLabelStyle(rotation: 90);
        plot.XAxis.Label("Pixel Intensity");

        return plot.Render().ToBitmapImage();
    }

    private static BitmapImage GetCumulativePixelCountGraph(Dictionary<Range, int> pixelCounts)
    {
        var intensityValues = pixelCounts.Values.ToList();
        var values = new double[intensityValues.Count];
        var cumulative = 0;
        for (var index = 0; index < intensityValues.Count; index++)
        {
            cumulative += intensityValues[index];
            values[index] = cumulative;
        }

        var plot = new Plot();
        plot.AddBar(values);

        plot.YAxis.Label("Intensity Sum");

        var xLabels = pixelCounts.Keys.Select(value => value.ToString()).ToArray();
        plot.XTicks(xLabels);
        plot.XAxis.TickLabelStyle(rotation: 90);
        plot.XAxis.Label("Pixel Intensity");

        return plot.Render().ToBitmapImage();
    }

    private static BitmapImage GetCumulativePixelCountImage(int width, int height, Dictionary<Range, int> beamPixelIntensityPixelCounts)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var imagePixels = (width - 1) * (height - 1);
        var pixelProportions = GetPixelProportions(imagePixels, beamPixelIntensityPixelCounts);

        var nonZeroPixels = pixelProportions.Where((pair, _) => pair.Value != 0).ToList();

        var rangeIndex = 0;
        var pixelIndex = 0;

        var (range, pixelCount) = nonZeroPixels[rangeIndex];

        for (var column = 0; column < width - 1; column++)
        {
            for (var row = 0; row < height - 1; row++)
            {
                if (pixelIndex >= pixelCount && rangeIndex != nonZeroPixels.Count - 1)
                {
                    (range, pixelCount) = nonZeroPixels[++rangeIndex];
                }

                bitmap.SetPixel(column, row, Color.FromArgb(range.End.Value, range.End.Value, range.End.Value));
                pixelIndex++;
            }
        }

        return bitmap.ToGrayScaleBitmapImage();
    }

    private static Dictionary<Range, int> GetPixelProportions(int imagePixelCount, Dictionary<Range, int> beamPixelIntensityPixelCounts)
    {
        var cumulativeValues = new Dictionary<Range, double>(beamPixelIntensityPixelCounts.Count);
        var accumulator = 0;
        foreach (var (range, count) in beamPixelIntensityPixelCounts)
        {
            accumulator += count;
            cumulativeValues.Add(range, accumulator);
        }

        var mappedPixelCounts = new Dictionary<Range, int>();
        var cumulativeSum = cumulativeValues.Values.Sum();
        foreach (var (range, value) in cumulativeValues)
        {
            mappedPixelCounts.Add(range, (int) Math.Round(imagePixelCount * (value / cumulativeSum)));
        }

        return mappedPixelCounts;
    }

    private static BitmapImage GetBeamPixelCountGraph(string[] xLabels, double[] intensities)
    {
        var plot = new Plot();
        plot.AddBar(intensities);
        plot.XTicks(xLabels);
        plot.YAxis.Label("Pixel Intensity Sum Under a Beam");
        plot.XAxis.TickLabelStyle(rotation: 90);

        return plot.Render().ToBitmapImage();
    }

    private static BitmapImage GetCumulativeBeamPixelCountGraph(string[] labels, double[] intensitySums)
    {
        var values = new double[intensitySums.Length];
        var cumulative = 0.0;
        for (var index = 0; index < intensitySums.Length; index++)
        {
            cumulative += intensitySums[index];
            values[index] = cumulative;
        }

        var plot = new Plot();
        plot.AddBar(values);
        plot.YAxis.Label("Cumulative Pixel Intensity Sum Under a Beam");
        plot.XTicks(labels);
        plot.XAxis.TickLabelStyle(rotation: 90);

        return plot.Render().ToBitmapImage();
    }

    private void BeamSelectComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            return;
        }

        var selectedBeam = e.AddedItems[0];
        var images = BeamImages[selectedBeam.ToString()];

        BeamGraphsPanel.Children.Clear();
        foreach (var image in images)
        {
            BeamGraphsPanel.Children.Add(image);
        }
    }
}