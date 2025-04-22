using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace revit_mcp_plugin.UI.Models
{
    public class CommandSet: INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private ObservableCollection<Command> _commands;
        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<Command> Commands
        {
            get => _commands;
            set
            {
                _commands = value;
                OnPropertyChanged();
            }
        }
        public string EnabledCountText
        {
            get
            {
                int enabledCount = 0;
                if (Commands != null)
                {
                    foreach (var command in Commands)
                    {
                        if (command.IsEnabled)
                            enabledCount++;
                    }
                    return $"{enabledCount}/{Commands.Count} Enabled";
                }
                return "0/0 Enabled";
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        // 更新启用命令计数
        public void UpdateEnabledCount()
        {
            OnPropertyChanged(nameof(EnabledCountText));
        }
    }
}
