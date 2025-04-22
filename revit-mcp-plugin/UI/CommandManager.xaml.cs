using revit_mcp_plugin.UI.Models;
using revit_mcp_plugin.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace revit_mcp_plugin.UI
{
    /// <summary>
    /// CommandManager.xaml 的交互逻辑
    /// </summary>
    public partial class CommandManager : Page
    {
        private CommandManagerViewModel ViewModel => (CommandManagerViewModel)DataContext;
        public CommandManager()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SaveData())
            {
                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to save settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCommandSet_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Models.CommandSet commandSet)
            {
                ViewModel.DeleteCommandSet(commandSet);
            }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton && toggleButton.DataContext is Command command)
            {
                ViewModel.UpdateCommandEnabledState(command, true);
            }
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggleButton && toggleButton.DataContext is Command command)
            {
                ViewModel.UpdateCommandEnabledState(command, false);
            }
        }
    }
}
