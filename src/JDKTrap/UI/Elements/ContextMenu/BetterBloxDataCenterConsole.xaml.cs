using JDKTrap.Integrations;
using JDKTrap.UI.Elements.Base;
using JDKTrap.UI.ViewModels.ContextMenu;

namespace JDKTrap.UI.Elements.ContextMenu
{
    public partial class BetterBloxDataCenterConsole
    {
        public BetterBloxDataCenterConsole()
        {
            InitializeComponent();
            var vm = new BetterBloxDataCenterConsoleViewModel();
            DataContext = vm;
        }
    }
}
