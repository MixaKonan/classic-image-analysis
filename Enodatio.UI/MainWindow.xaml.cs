using System.Windows;
using System.Windows.Controls;
using Enodatio.UI.Services;

namespace Enodatio.UI;

public partial class MainWindow : Window
{
    private readonly WindowFactory _windowFactory;
    
    public MainWindow(WindowFactory windowFactory)
    {
        InitializeComponent();
        
        _windowFactory = windowFactory;
    }

    private void ModeItem_OnClick(object sender, RoutedEventArgs e)
    {
        var clickedButton = (Button) sender;
        switch (clickedButton.Name)
        {
            case "ImageDifference":
                _windowFactory.Create<ImageDifference>().Show();
                this.Close();
                break;

            case "ImageAnalysis":
                _windowFactory.Create<ImageAnalysis>().Show();
                this.Close();
                break;
        }
    }
}