using Newtonsoft.Json;
using revit_mcp_plugin.UI.Models;
using revit_mcp_plugin.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace revit_mcp_plugin.UI.Services
{
    public class CommandDataService
    {
        private readonly string _jsonFilePath;

        public CommandDataService(string jsonFilePath = null)
        {
            _jsonFilePath = jsonFilePath ?? PathManager.GetCommandDataFilePath();
        }

        public ObservableCollection<revit_mcp_plugin.UI.Models.CommandSet> LoadCommandSets()
        {
            try
            {
                if (!File.Exists(_jsonFilePath))
                {
                    // 如果文件不存在，返回空集合
                    return new ObservableCollection<revit_mcp_plugin.UI.Models.CommandSet>();
                }
                string json = File.ReadAllText(_jsonFilePath);
                var data = JsonConvert.DeserializeObject<CommandData>(json);
                var result = new ObservableCollection<revit_mcp_plugin.UI.Models.CommandSet>();
                if (data?.CommandSets != null)
                {
                    foreach (var set in data.CommandSets)
                    {
                        set.Commands = new ObservableCollection<Command>(set.Commands ?? new ObservableCollection<Command>());
                        result.Add(set);
                    }
                }
                return result ?? new ObservableCollection<revit_mcp_plugin.UI.Models.CommandSet>();
            }
            catch (Exception ex)
            {
                // 在实际应用中应该记录这个异常
                Console.WriteLine($"Error loading command sets: {ex.Message}");
                return new ObservableCollection<revit_mcp_plugin.UI.Models.CommandSet>();
            }
        }

        public bool SaveCommandSets(IList<Models.CommandSet> commandSets)
        {
            try
            {
                var data = new CommandData
                {
                    CommandSets = new List<Models.CommandSet>(commandSets)
                };
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(_jsonFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                // 在实际应用中应该记录这个异常
                Console.WriteLine($"Error saving command sets: {ex.Message}");
                return false;
            }
        }
    }
}
