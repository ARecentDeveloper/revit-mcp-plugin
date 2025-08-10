using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPSDK.API.Utils;
using revit_mcp_plugin.Configuration;
using revit_mcp_plugin.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace revit_mcp_plugin.Core
{
    /// <summary>
    /// Command manager responsible for loading and managing commands
    /// </summary>
    public class CommandManager
    {
        private readonly ICommandRegistry _commandRegistry;
        private readonly ILogger _logger;
        private readonly ConfigurationManager _configManager;
        private readonly UIApplication _uiApplication;
        private readonly RevitVersionAdapter _versionAdapter;

        public CommandManager(
            ICommandRegistry commandRegistry,
            ILogger logger,
            ConfigurationManager configManager,
            UIApplication uiApplication)
        {
            _commandRegistry = commandRegistry;
            _logger = logger;
            _configManager = configManager;
            _uiApplication = uiApplication;
            _versionAdapter = new RevitVersionAdapter(_uiApplication.Application);
        }

        /// <summary>
        /// Load all commands specified in configuration file
        /// </summary>
        public void LoadCommands()
        {
            _logger.Info("Starting to load commands");
            string currentVersion = _versionAdapter.GetRevitVersion();
            _logger.Info("Current Revit version: {0}", currentVersion);

            // Load external commands from configuration
            foreach (var commandConfig in _configManager.Config.Commands)
            {
                try
                {
                    if (!commandConfig.Enabled)
                    {
                        _logger.Info("Skipping disabled command: {0}", commandConfig.CommandName);
                        continue;
                    }

                    // Check version compatibility
                    if (commandConfig.SupportedRevitVersions != null &&
                        commandConfig.SupportedRevitVersions.Length > 0 &&
                        !_versionAdapter.IsVersionSupported(commandConfig.SupportedRevitVersions))
                    {
                        _logger.Warning("Command {0} does not support current Revit version {1}, skipped",
                            commandConfig.CommandName, currentVersion);
                        continue;
                    }

                    // Replace version placeholder in path
                    commandConfig.AssemblyPath = commandConfig.AssemblyPath.Contains("{VERSION}")
                        ? commandConfig.AssemblyPath.Replace("{VERSION}", currentVersion)
                        : commandConfig.AssemblyPath;

                    // Load external command assembly
                    LoadCommandFromAssembly(commandConfig);
                }
                catch (Exception ex)
                {
                    _logger.Error("Failed to load command {0}: {1}", commandConfig.CommandName, ex.Message);
                }
            }

            _logger.Info("Command loading completed");
        }

        /// <summary>
        /// Load specific command from specific assembly
        /// </summary>
        /// <param name="commandName">Command name</param>
        /// <param name="assemblyPath">Assembly path</param>
        private void LoadCommandFromAssembly(CommandConfig config)
        {
            try
            {
                // Determine assembly path
                string assemblyPath = config.AssemblyPath;
                if (!Path.IsPathRooted(assemblyPath))
                {
                    // If not absolute path, relative to Commands directory
                    string baseDir = PathManager.GetCommandsDirectoryPath();
                    assemblyPath = Path.Combine(baseDir, assemblyPath);
                }

                if (!File.Exists(assemblyPath))
                {
                    _logger.Error("Command assembly does not exist: {0}", assemblyPath);
                    return;
                }

                // Load assembly
                Assembly assembly = Assembly.LoadFrom(assemblyPath);

                // Find types that implement IRevitCommand interface
                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(RevitMCPSDK.API.Interfaces.IRevitCommand).IsAssignableFrom(type) &&
                        !type.IsInterface &&
                        !type.IsAbstract)
                    {
                        try
                        {
                            // Create command instance
                            RevitMCPSDK.API.Interfaces.IRevitCommand command;

                            // Check if command implements initializable interface
                            if (typeof(IRevitCommandInitializable).IsAssignableFrom(type))
                            {
                                // Create instance and initialize
                                command = (IRevitCommand)Activator.CreateInstance(type);
                                ((IRevitCommandInitializable)command).Initialize(_uiApplication);
                            }
                            else
                            {
                                // Try to find constructor that accepts UIApplication
                                var constructor = type.GetConstructor(new[] { typeof(UIApplication) });
                                if (constructor != null)
                                {
                                    command = (IRevitCommand)constructor.Invoke(new object[] { _uiApplication });
                                }
                                else
                                {
                                    // Use parameterless constructor
                                    command = (IRevitCommand)Activator.CreateInstance(type);
                                }
                            }

                            // Check if command name matches configuration
                            if (command.CommandName == config.CommandName)
                            {
                                _commandRegistry.RegisterCommand(command);
                                _logger.Info("Registered external command: {0} (from {1})",
                                    command.CommandName, Path.GetFileName(assemblyPath));
                                break; // Exit loop after finding matching command
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("Failed to create command instance [{0}]: {1}", type.FullName, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load command assembly: {0}", ex.Message);
            }
        }
    }
}
