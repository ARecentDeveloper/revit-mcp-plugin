using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Models.JsonRPC;
using RevitMCPSDK.API.Interfaces;
using revit_mcp_plugin.Configuration;
using revit_mcp_plugin.Utils;

namespace revit_mcp_plugin.Core
{
    public class SocketService
    {
        private static SocketService _instance;
        private TcpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning;
        private int _port = 8080;
        private UIApplication _uiApp;
        private ICommandRegistry _commandRegistry;
        private ILogger _logger;
        private CommandExecutor _commandExecutor;

        public static SocketService Instance
        {
            get
            {
                if(_instance == null)
                    _instance = new SocketService();
                return _instance;
            }
        }

        private SocketService()
        {
            _commandRegistry = new RevitCommandRegistry();
            _logger = new Logger();
        }

        public bool IsRunning => _isRunning;

        public int Port
        {
            get => _port;
            set => _port = value;
        }

        // Initialize
        public void Initialize(UIApplication uiApp)
        {
            _uiApp = uiApp;

            // Initialize event manager
            ExternalEventManager.Instance.Initialize(uiApp, _logger);

            // Log current Revit version
            var versionAdapter = new RevitMCPSDK.API.Utils.RevitVersionAdapter(_uiApp.Application);
            string currentVersion = versionAdapter.GetRevitVersion();
            _logger.Info("Current Revit version: {0}", currentVersion);



            // Create command executor
            _commandExecutor = new CommandExecutor(_commandRegistry, _logger);

            // Load configuration and register commands
            ConfigurationManager configManager = new ConfigurationManager(_logger);
            configManager.LoadConfiguration();
            

            //// Read service port from configuration
            //if (configManager.Config.Settings.Port > 0)
            //{
            //    _port = configManager.Config.Settings.Port;
            //}
            _port = 8081; // Fixed port number (avoid conflicts with Revit built-in services)

            // Load commands
            CommandManager commandManager = new CommandManager(
                _commandRegistry, _logger, configManager, _uiApp);
            commandManager.LoadCommands();

            _logger.Info($"Socket service initialized on port {_port}");
        }

        public void Start()
        {
            if (_isRunning) 
            {
                _logger.Info("Socket service is already running");
                return;
            }

            try
            {
                _logger.Info($"Starting Socket service on port: {_port}");
                _isRunning = true;
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _logger.Info("TcpListener started");

                _listenerThread = new Thread(ListenForClients)
                {
                    IsBackground = true
                };
                _listenerThread.Start();
                _logger.Info("Listener thread started");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to start Socket service: {ex.Message}");
                
                // Clean up resources
                try
                {
                    _listener?.Stop();
                    _listener = null;
                    _logger.Info("TcpListener resources cleaned up");
                }
                catch (Exception cleanupEx)
                {
                    _logger.Error($"Error occurred while cleaning up resources: {cleanupEx.Message}");
                }
                
                _isRunning = false;
            }
        }

        public void Stop()
        {
            if (!_isRunning) 
            {
                _logger.Info("Socket service is not running");
                return;
            }

            try
            {
                _logger.Info("Stopping Socket service...");
                _isRunning = false;

                _listener?.Stop();
                _listener = null;
                _logger.Info("TcpListener stopped");

                if(_listenerThread!=null && _listenerThread.IsAlive)
                {
                    _logger.Info("Waiting for listener thread to end...");
                    _listenerThread.Join(1000);
                    _logger.Info("Listener thread ended");
                }
                _logger.Info("Socket service completely stopped");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error occurred while stopping Socket service: {ex.Message}");
            }
        }

        private void ListenForClients()
        {
            _logger.Info("Starting to listen for client connections...");
            try
            {
                while (_isRunning)
                {
                    _logger.Info("Waiting for client connection...");
                    TcpClient client = _listener.AcceptTcpClient();
                    _logger.Info($"Accepted client connection: {client.Client.RemoteEndPoint}");

                    Thread clientThread = new Thread(HandleClientCommunication)
                    {
                        IsBackground = true
                    };
                    clientThread.Start(client);
                    _logger.Info("Client handling thread started");
                }
            }
            catch (SocketException ex)
            {
                _logger.Warning($"Socket exception: {ex.Message}");
            }
            catch(Exception ex)
            {
                _logger.Error($"Error occurred while listening for clients: {ex.Message}");
            }
            _logger.Info("Stopped listening for client connections");
        }

        private void HandleClientCommunication(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            NetworkStream stream = tcpClient.GetStream();
            
            _logger.Info("New client connection established");

            try
            {
                byte[] buffer = new byte[8192];

                while (_isRunning && tcpClient.Connected)
                {
                    // Read client message
                    int bytesRead = 0;

                    try
                    {
                        _logger.Info("Waiting for client message...");
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                        _logger.Info($"Read {bytesRead} bytes of data");
                    }
                    catch (IOException ex)
                    {
                        _logger.Warning($"Client connection interrupted: {ex.Message}");
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        _logger.Info("Client disconnected (0 bytes)");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    _logger.Info($"Received message: {message}");

                    string response = ProcessJsonRPCRequest(message);
                    _logger.Info($"Generated response: {response}");

                    // Send response
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);
                    _logger.Info("Response sent");
                }
            }
            catch(Exception)
            {
                // log
            }
            finally
            {
                tcpClient.Close();
            }
        }

        private string ProcessJsonRPCRequest(string requestJson)
        {
            _logger.Info("Starting to process JSON-RPC request");
            JsonRPCRequest request;

            try
            {
                _logger.Info("Parsing JSON-RPC request...");
                // Parse JSON-RPC request
                request = JsonConvert.DeserializeObject<JsonRPCRequest>(requestJson);

                // Validate if request format is valid
                if (request == null || !request.IsValid())
                {
                    _logger.Warning("Invalid JSON-RPC request format");
                    return CreateErrorResponse(
                        null,
                        JsonRPCErrorCodes.InvalidRequest,
                        "Invalid JSON-RPC request"
                    );
                }

                _logger.Info($"Request method: {request.Method}, ID: {request.Id}");

                // Find command
                if (!_commandRegistry.TryGetCommand(request.Method, out var command))
                {
                    _logger.Warning($"Command not found: {request.Method}");
                    return CreateErrorResponse(request.Id, JsonRPCErrorCodes.MethodNotFound,
                        $"Method '{request.Method}' not found");
                }

                _logger.Info($"Found command: {request.Method}, starting execution...");

                // Execute command
                try
                {                
                    object result = command.Execute(request.GetParamsObject(), request.Id);
                    _logger.Info($"Command executed successfully: {request.Method}");

                    return CreateSuccessResponse(request.Id, result);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Command execution failed: {request.Method}, error: {ex.Message}");
                    return CreateErrorResponse(request.Id, JsonRPCErrorCodes.InternalError, ex.Message);
                }
            }
            catch (JsonException ex)
            {
                _logger.Error($"JSON parsing error: {ex.Message}");
                // JSON parsing error
                return CreateErrorResponse(
                    null,
                    JsonRPCErrorCodes.ParseError,
                    "Invalid JSON"
                );
            }
            catch (Exception ex)
            {
                _logger.Error($"Error occurred while processing request: {ex.Message}");
                // Other errors when processing request
                return CreateErrorResponse(
                    null,
                    JsonRPCErrorCodes.InternalError,
                    $"Internal error: {ex.Message}"
                );
            }
        }

        private string CreateSuccessResponse(string id, object result)
        {
            var response = new JsonRPCSuccessResponse
            {
                Id = id,
                Result = result is JToken jToken ? jToken : JToken.FromObject(result)
            };

            return response.ToJson();
        }

        private string CreateErrorResponse(string id, int code, string message, object data = null)
        {
            var response = new JsonRPCErrorResponse
            {
                Id = id,
                Error = new JsonRPCError
                {
                    Code = code,
                    Message = message,
                    Data = data != null ? JToken.FromObject(data) : null
                }
            };

            return response.ToJson();
        }
    }
}
