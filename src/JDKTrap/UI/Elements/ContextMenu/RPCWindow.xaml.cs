using System.Windows;
using JDKTrap.UI.ViewModels;
using JDKTrap.UI.ViewModels.ContextMenu;

namespace JDKTrap.UI.Elements.ContextMenu
{
    public partial class RPCWindow
    {
        public RPCWindow()
        {
            InitializeComponent();
            DataContext = new RPCCustomizerViewModel();
        }
    }
}
