using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Enodatio.Logic;
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
using Point = System.Drawing.Point;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace Enodatio.UI;

public partial class ImageAnalysis : Window
{
    private readonly ImageAnalyser _imageAnalyser;
    private readonly WindowFactory _windowFactory;

    public ImageAnalysis(ImageAnalyser imageAnalyser, WindowFactory windowFactory)
    {
        InitializeComponent();

        this.DataContext = new ImageAnalysisViewModel();

        this._imageAnalyser = imageAnalyser;
        this._windowFactory = windowFactory;
    }

    private void Reset_OnClickAsync(object sender, RoutedEventArgs e)
    {
        ((ImageAnalysisViewModel) this.DataContext).Reset();
    }

    private async void UploadImageButton_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var viewModel = (ImageAnalysisViewModel) this.DataContext;

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
        await UpdateBeamInfoAsync();
    }

    private async void SubmitDegrees_OnClickAsync(object sender, RoutedEventArgs e)
    {
        BeamGraphsPanel.Children.Clear();
        await UpdateBeamInfoAsync();
    }

    private void IntensitySpan_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = Common.Regexes.NumbersOnly.IsMatch(e.Text);
    }

    private async void SubmitIntensitySpan_OnClickAsync(object sender, RoutedEventArgs e)
    {
        var viewModel = (ImageAnalysisViewModel) DataContext;

        var value = byte.Parse(IntensitySpan.Text);

        if (value == 1)
        {
            value = 1;
        }

        viewModel.IntensitySpan = value;

        await UpdatePixelCountPlotsAsync(viewModel);
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
                Source = GetBeamPixelCountGraph(label, xLabels, values)
            };

            var cumulativeBeamPixelCountFigure = new Image()
            {
                Source = GetCumulativeBeamPixelCountGraph(label, xLabels, values)
            };

            BeamGraphsPanel.Children.Add(
                new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children = {beamPixelCountGraph, cumulativeBeamPixelCountFigure}
                });
        }

        viewModel.Image = newImage;
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

    private static BitmapImage GetBeamPixelCountGraph(string graphLabel, string[] xLabels, double[] intensities)
    {
        var plot = new Plot();
        plot.Title(graphLabel);
        plot.AddBar(intensities);
        plot.XTicks(xLabels);
        plot.YAxis.Label("Pixel Intensity Sum Under a Beam");
        plot.XAxis.TickLabelStyle(rotation: 90);
        
        return plot.Render().ToBitmapImage();
    }

    private static BitmapImage GetCumulativeBeamPixelCountGraph(string graphLabel, string[] labels, double[] intensitySums)
    {
        var values = new double[intensitySums.Length];
        var cumulative = 0.0;
        for (var index = 0; index < intensitySums.Length; index++)
        {
            cumulative += intensitySums[index];
            values[index] = cumulative;
        }

        var plot = new Plot();
        plot.Title(graphLabel);
        plot.AddBar(values);
        plot.YAxis.Label("Cumulative Pixel Intensity Sum Under a Beam");
        plot.XTicks(labels);
        plot.XAxis.TickLabelStyle(rotation: 90);

        return plot.Render().ToBitmapImage();
    }
}