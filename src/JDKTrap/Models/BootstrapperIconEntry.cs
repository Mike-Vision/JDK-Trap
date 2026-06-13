using System.Windows.Media;

namespace JDKTrap.Models
{
    public class BootstrapperIconEntry
    {
        public BootstrapperIcon IconType { get; set; }

        private ImageSource? _imageSource;
        public ImageSource ImageSource => _imageSource ??= IconType.GetIcon().GetImageSource();
    }
}
