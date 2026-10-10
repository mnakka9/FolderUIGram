using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Logging;

namespace FolderUIGram
{
    public static class MauiProgram
    {
#if WINDOWS
        private static void FlattenComboBox(Microsoft.UI.Xaml.Controls.ComboBox cb)
        {
            void Walk(Microsoft.UI.Xaml.DependencyObject d)
            {
                int n = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(d);
                for (int i = 0; i < n; i++)
                {
                    var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(d, i);
                    if (child is Microsoft.UI.Xaml.FrameworkElement fe)
                    {
                        if (fe.Name == "HeaderContentPresenter")
                            fe.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                        else if (fe.Name == "ContentPresenter")
                        {
                            fe.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                            fe.Margin = new Microsoft.UI.Xaml.Thickness(0);
                        }
                    }
                    Walk(child);
                }
            }
            Walk(cb);
        }
#endif

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
#if WINDOWS
            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("FlatPicker", (handler, view) =>
            {
                var cb = handler.PlatformView;
                var clear = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                cb.Header = null;
                cb.HeaderTemplate = null;
                cb.VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                cb.Loaded += (s, e) => FlattenComboBox(cb);
                cb.PlaceholderText = (view as Picker)?.Title ?? string.Empty;
                cb.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                cb.Padding = new Microsoft.UI.Xaml.Thickness(0);
                cb.MinHeight = 0;
                cb.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                cb.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
                cb.Background = clear;
                foreach (var key in new[] { "ComboBoxBackground", "ComboBoxBackgroundPointerOver", "ComboBoxBackgroundPressed", "ComboBoxBackgroundFocused", "ComboBoxBackgroundDisabled", "ComboBoxBorderBrush", "ComboBoxBorderBrushPointerOver", "ComboBoxBorderBrushPressed", "ComboBoxBorderBrushFocused", "ComboBoxBorderBrushDisabled" })
                    cb.Resources[key] = clear;
            });
#endif
            builder.Services.AddSingleton<IFolderPicker>(FolderPicker.Default);

            return builder.Build();
        }
    }
}



