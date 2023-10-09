using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Enodatio.UI.Services;

public class WindowFactory
{
    private readonly IServiceProvider _serviceProvider;

    public WindowFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T Create<T>() where T : Window
    {
        return _serviceProvider.GetRequiredService<T>();
    }
}