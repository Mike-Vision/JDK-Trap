using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using JDKTrap.Enums;
using JDKTrap.UI.Elements.Base;

namespace JDKTrap.UI.Elements.Dialogs
{
    /// <summary>
    /// Dialog for previewing cursor types before applying them
    /// </summary>
    public partial class CursorPreviewDialog : WpfUiWindow
    {
        public JDKTrap.Enums.CursorType? SelectedCursor { get; private set; }

        public CursorPreviewDialog()
        {
            InitializeComponent();
            LoadCursorPreviews();
        }

        private void LoadCursorPreviews()
        {
            var cursors = new[]
            {
                JDKTrap.Enums.CursorType.Default,
                JDKTrap.Enums.CursorType.FPSCursor,
                JDKTrap.Enums.CursorType.CleanCursor,
                JDKTrap.Enums.CursorType.DotCursor,
                JDKTrap.Enums.CursorType.StoofsCursor,
                JDKTrap.Enums.CursorType.From2006,
                JDKTrap.Enums.CursorType.From2013,
                JDKTrap.Enums.CursorType.WhiteDotCursor,
                JDKTrap.Enums.CursorType.VerySmallWhiteDot
            };

            foreach (var cursor in cursors)
            {
                var previewItem = CreateCursorPreviewItem(cursor);
                CursorStackPanel.Children.Add(previewItem);
            }
        }

        private FrameworkElement CreateCursorPreviewItem(JDKTrap.Enums.CursorType cursor)
        {
            var border = new System.Windows.Controls.Border
            {
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                Background = new SolidColorBrush(Colors.Transparent),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var stackPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            // Load cursor image for preview
            var image = new System.Windows.Controls.Image
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0)
            };

            try
            {
                var imagePath = GetCursorImagePath(cursor);
                if (!string.IsNullOrEmpty(imagePath))
                {
                    var uri = new Uri($"pack://application:,,,/Resources/Mods/{imagePath}");
                    image.Source = new BitmapImage(uri);
                }
            }
            catch
            {
                // Use default image if cursor image can't be loaded
                image.Source = null;
            }

            var nameLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetCursorDisplayName(cursor),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14
            };

            stackPanel.Children.Add(image);
            stackPanel.Children.Add(nameLabel);
            border.Child = stackPanel;

            border.MouseLeftButtonUp += (s, e) =>
            {
                SelectedCursor = cursor;
                DialogResult = true;
                Close();
            };

            border.MouseEnter += (s, e) =>
            {
                border.Background = new SolidColorBrush(Color.FromArgb(50, 100, 149, 237));
            };

            border.MouseLeave += (s, e) =>
            {
                border.Background = new SolidColorBrush(Colors.Transparent);
            };

            return border;
        }

        private string GetCursorImagePath(JDKTrap.Enums.CursorType cursor)
        {
            return cursor switch
            {
                JDKTrap.Enums.CursorType.FPSCursor => "Cursor/FPSCursor/ArrowCursor.png",
                JDKTrap.Enums.CursorType.CleanCursor => "Cursor/CleanCursor/ArrowCursor.png",
                JDKTrap.Enums.CursorType.DotCursor => "Cursor/DotCursor/ArrowCursor.png",
                JDKTrap.Enums.CursorType.StoofsCursor => "Cursor/StoofsCursor/ArrowCursor.png",
                JDKTrap.Enums.CursorType.From2006 => "Cursor/From2006/ArrowCursor.png",
                JDKTrap.Enums.CursorType.From2013 => "Cursor/From2013/ArrowCursor.png",
                JDKTrap.Enums.CursorType.WhiteDotCursor => "Cursor/WhiteDotCursor/ArrowCursor.png",
                JDKTrap.Enums.CursorType.VerySmallWhiteDot => "Cursor/VerySmallWhiteDot/ArrowCursor.png",
                _ => string.Empty
            };
        }

        private string GetCursorDisplayName(JDKTrap.Enums.CursorType cursor)
        {
            return cursor switch
            {
                JDKTrap.Enums.CursorType.Default => "Default",
                JDKTrap.Enums.CursorType.FPSCursor => "FPS Cursor (V1)",
                JDKTrap.Enums.CursorType.CleanCursor => "Clean Cursor",
                JDKTrap.Enums.CursorType.DotCursor => "Dot Cursor",
                JDKTrap.Enums.CursorType.StoofsCursor => "Stoofs Cursor",
                JDKTrap.Enums.CursorType.From2006 => "2006 Legacy Cursor",
                JDKTrap.Enums.CursorType.From2013 => "2013 Legacy Cursor",
                JDKTrap.Enums.CursorType.WhiteDotCursor => "White Dot Cursor",
                JDKTrap.Enums.CursorType.VerySmallWhiteDot => "Very Small White Dot",
                _ => cursor.ToString()
            };
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}