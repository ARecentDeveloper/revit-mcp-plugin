using revit_mcp_plugin.UI.Models;
using revit_mcp_plugin.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace revit_mcp_plugin.UI.ViewModels
{
    public class CommandManagerViewModel : INotifyPropertyChanged
    {
        private readonly CommandDataService _dataService;
        private ObservableCollection<revit_mcp_plugin.UI.Models.CommandSet> _commandSets;
        private ObservableCollection<Command> _visibleCommands;
        private string _searchText = "";
        private Models.CommandSet _selectedCommandSet;

        public CommandManagerViewModel()
        {
            _dataService = new CommandDataService();
            LoadData();
        }

        public ObservableCollection<revit_mcp_plugin.UI.Models.CommandSet> CommandSets
        {
            get => _commandSets;
            set
            {
                _commandSets = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Command> VisibleCommands
        {
            get => _visibleCommands;
            set
            {
                _visibleCommands = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FilterCommands();
            }
        }

        public Models.CommandSet SelectedCommandSet
        {
            get => _selectedCommandSet;
            set
            {
                _selectedCommandSet = value;
                OnPropertyChanged();
                FilterCommands();
            }
        }

        public void LoadData()
        {
            CommandSets = _dataService.LoadCommandSets();
            UpdateVisibleCommands();
        }

        public bool SaveData()
        {
            return _dataService.SaveCommandSets(CommandSets.ToList());
        }

        private void UpdateVisibleCommands()
        {
            VisibleCommands = new ObservableCollection<Command>();
            if (CommandSets == null) return;
            foreach (var set in CommandSets)
            {
                if (set.Commands == null) continue;

                foreach (var command in set.Commands)
                {
                    if (command.IsEnabled)
                    {
                        VisibleCommands.Add(command);
                    }
                }
            }
            FilterCommands();
        }

        private void FilterCommands()
        {
            if (VisibleCommands == null) return;
            var filteredCommands = new ObservableCollection<Command>();

            var commands = CommandSets.SelectMany(s => s.Commands)
                                     .Where(c => c.IsEnabled &&
                                                (string.IsNullOrEmpty(SearchText) ||
                                                 c.Name.ToLower().Contains(SearchText.ToLower())) &&
                                                (SelectedCommandSet == null ||
                                                 CommandSets.First(cs => cs.Commands.Contains(c)) == SelectedCommandSet))
                                     .ToList();
            VisibleCommands = new ObservableCollection<Command>(commands);
            OnPropertyChanged(nameof(VisibleCommands));
        }

        public void DeleteCommandSet(Models.CommandSet commandSet)
        {
            if (commandSet != null && CommandSets.Contains(commandSet))
            {
                if (MessageBox.Show($"Are you sure you want to delete '{commandSet.Name}'?",
                                    "Confirm Delete",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    CommandSets.Remove(commandSet);
                    UpdateVisibleCommands();
                }
            }
        }

        public void UpdateCommandEnabledState(Command command, bool isEnabled)
        {
            if (command != null)
            {
                command.IsEnabled = isEnabled;

                // 更新命令集中的启用命令计数
                foreach (var set in CommandSets)
                {
                    if (set.Commands.Contains(command))
                    {
                        set.UpdateEnabledCount();
                        break;
                    }
                }

                UpdateVisibleCommands();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
