using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace Enodatio.UI.ViewModels;

public class ImageAnalysisViewModel : INotifyPropertyChanged
{
    public BitmapImage? OriginalImage { get; set; }
    private BitmapImage? _image;

    public BitmapImage? Image
    {
        get => _image;
        set
        {
            _image = value;
            if (value is not null)
            {
                _image!.Freeze();
            }

            OnPropertyChanged(nameof(IsImageSet));
            OnPropertyChanged();
        }
    }

    private BitmapImage? _pixelCountFigure;

    public BitmapImage? PixelCountFigure
    {
        get => _pixelCountFigure;
        set
        {
            _pixelCountFigure = value;
            if (_pixelCountFigure is not null)
            {
                _pixelCountFigure.Freeze();
            }

            OnPropertyChanged();
        }
    }

    private BitmapImage? _cumulativePixelCountFigure;

    public BitmapImage? CumulativePixelCountFigure
    {
        get => _cumulativePixelCountFigure;
        set
        {
            _cumulativePixelCountFigure = value;
            if (_cumulativePixelCountFigure is not null)
            {
                _cumulativePixelCountFigure.Freeze();
            }

            OnPropertyChanged();
        }
    }

    private BitmapImage? _beamPixelCountFigure;

    public BitmapImage? BeamPixelCountFigure
    {
        get => _beamPixelCountFigure;
        set
        {
            _beamPixelCountFigure = value;
            if (_beamPixelCountFigure is not null)
            {
                _beamPixelCountFigure.Freeze();
            }

            OnPropertyChanged();
        }
    }

    private BitmapImage? _cumulativeBeamPixelCountFigure;

    public BitmapImage? CumulativeBeamPixelCountFigure
    {
        get => _cumulativeBeamPixelCountFigure;
        set
        {
            _cumulativeBeamPixelCountFigure = value;
            if (_cumulativeBeamPixelCountFigure is not null)
            {
                _cumulativeBeamPixelCountFigure.Freeze();
            }

            OnPropertyChanged();
        }
    }

    private byte _intensitySpan = 15;

    public byte IntensitySpan
    {
        get => (byte) (_intensitySpan == 0 ? 1 : _intensitySpan);
        set
        {
            _intensitySpan = value;
            OnPropertyChanged();
        }
    }

    private int _degrees = 45;

    public int Degrees
    {
        get => _degrees;
        set
        {
            _degrees = value % 360;
            OnPropertyChanged();
        }
    }

    public bool IsImageSet => Image is not null;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public void Reset()
    {
        Image = null;
        OriginalImage = null;
        PixelCountFigure = null;
        CumulativePixelCountFigure = null;
        BeamPixelCountFigure = null;
        CumulativeBeamPixelCountFigure = null;
        Degrees = 45;
        IntensitySpan = 15;
    }
}