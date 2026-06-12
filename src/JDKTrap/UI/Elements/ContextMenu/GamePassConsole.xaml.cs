using JDKTrap.Integrations;
using JDKTrap.UI.Elements.Base;
using JDKTrap.UI.ViewModels.ContextMenu;

namespace JDKTrap.UI.Elements.ContextMenu
{
    public partial class GamePassConsole
    {
        public GamePassConsole(long userId)
        {
            InitializeComponent();
            var vm = new GamePassConsoleViewModel();
            DataContext = vm;
            vm.LoadGamePassesCommand.Execute(userId);
        }
    }
}
