using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace Enodatio.UI.ViewModels;

public class ImageDifferenceViewModel : INotifyPropertyChanged
{
    private BitmapImage? _first;

    public BitmapImage? First
    {
        get => _first;
        set
        {
            _first = value;
            if (_first is not null)
            {
                _first.Freeze();
            }

            OnPropertyChanged();
        }
    }

    private BitmapImage? _second;

    public BitmapImage? Second
    {
        get => _second;
        set
        {
            _second = value;
            if (_second is not null)
            {
                _second.Freeze();
            }

            OnPropertyChanged();
        }
    }

    private BitmapImage? _difference;

    public BitmapImage? Difference
    {
        get => _difference;
        set
        {
            _difference = value;
            if (_difference is not null)
            {
                _difference.Freeze();
            }

            OnPropertyChanged();
        }
    }

    private byte _threshold = 15;

    public byte Threshold
    {
        get => (byte) (_threshold == 0 ? 1 : _threshold);
        set
        {
            _threshold = value;
            OnPropertyChanged();
        }
    }

    public bool IsDifferenceImageSet => Difference is not null;

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
        First = null;
        Second = null;
        Difference = null;
    }
}