using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MHArmory.ViewModels;

namespace MHArmory
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window, IWindow
    {
        private readonly HelpRootViewModel helpRootViewModel;

        public HelpWindow()
        {
            InitializeComponent();

            InputBindings.Add(new InputBinding(new AnonymousCommand(OnCancel), new KeyGesture(Key.Escape, ModifierKeys.None)));

            helpRootViewModel = new HelpRootViewModel();

            DataContext = helpRootViewModel;
        }

        public void OnOpen(bool isAlreadyOpened, object argument)
        {
            HelpCategory helpCategory;

            if (argument is string str)
            {
                if (Enum.TryParse(str, out helpCategory) == false)
                    return;
            }
            else if (argument is HelpCategory category)
                helpCategory = category;
            else
                return;

            helpRootViewModel.SelectCategory(helpCategory);
        }

        private void OnCancel(object parameter)
        {
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = true;
            Hide();
        }
    }
}
