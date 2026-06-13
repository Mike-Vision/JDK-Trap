using System;
using System.Collections.Concurrent;
using System.Windows;
using Wpf.Ui.Mvvm.Contracts;

namespace JDKTrap.UI
{
    public class SimplePageService : IPageService
    {
        private readonly ConcurrentDictionary<Type, FrameworkElement> _pages = new();

        public T? GetPage<T>() where T : class
        {
            if (!typeof(FrameworkElement).IsAssignableFrom(typeof(T)))
                throw new InvalidOperationException("The page should be a WPF control.");

            return GetPage(typeof(T)) as T;
        }

        public FrameworkElement? GetPage(Type pageType)
        {
            if (!typeof(FrameworkElement).IsAssignableFrom(pageType))
                throw new InvalidOperationException("The page should be a WPF control.");

            return _pages.GetOrAdd(pageType, type => (FrameworkElement)Activator.CreateInstance(type)!);
        }

        public void PreWarmPages(params Type[] pageTypes)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                foreach (var type in pageTypes)
                {
                    try
                    {
                        // Create on UI thread since WPF controls require STA
                        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            GetPage(type);
                        }, System.Windows.Threading.DispatcherPriority.Background);
                    }
                    catch { /* ignore pre-warm failures */ }
                }
            });
        }
    }
}
