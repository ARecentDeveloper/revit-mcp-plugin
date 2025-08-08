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

        // 初始化
        public void Initialize(UIApplication uiApp)
        {
            _uiApp = uiApp;

            // 初始化事件管理器
            ExternalEventManager.Instance.Initialize(uiApp, _logger);

            // 记录当前 Revit 版本
            var versionAdapter = new RevitMCPSDK.API.Utils.RevitVersionAdapter(_uiApp.Application);
            string currentVersion = versionAdapter.GetRevitVersion();
            _logger.Info("当前 Revit 版本: {0}", currentVersion);



            // 创建命令执行器
            _commandExecutor = new CommandExecutor(_commandRegistry, _logger);

            // 加载配置并注册命令
            ConfigurationManager configManager = new ConfigurationManager(_logger);
            configManager.LoadConfiguration();
            

            //// 从配置中读取服务端口
            //if (configManager.Config.Settings.Port > 0)
            //{
            //    _port = configManager.Config.Settings.Port;
            //}
            _port = 8081; // 固定端口号 (避免与Revit内置服务冲突)

            // 加载命令
            CommandManager commandManager = new CommandManager(
                _commandRegistry, _logger, configManager, _uiApp);
            commandManager.LoadCommands();

            _logger.Info($"Socket service initialized on port {_port}");
        }

        public void Start()
        {
            if (_isRunning) 
            {
                _logger.Info("Socket服务已在运行中");
                return;
            }

            try
            {
                _logger.Info($"启动Socket服务，端口: {_port}");
                _isRunning = true;
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _logger.Info("TcpListener已启动");

                _listenerThread = new Thread(ListenForClients)
                {
                    IsBackground = true
                };
                _listenerThread.Start();
                _logger.Info("监听线程已启动");
            }
            catch (Exception ex)
            {
                _logger.Error($"启动Socket服务失败: {ex.Message}");
                
                // 清理资源
                try
                {
                    _listener?.Stop();
                    _listener = null;
                    _logger.Info("已清理TcpListener资源");
                }
                catch (Exception cleanupEx)
                {
                    _logger.Error($"清理资源时发生错误: {cleanupEx.Message}");
                }
                
                _isRunning = false;
            }
        }

        public void Stop()
        {
            if (!_isRunning) 
            {
                _logger.Info("Socket服务未在运行中");
                return;
            }

            try
            {
                _logger.Info("停止Socket服务...");
                _isRunning = false;

                _listener?.Stop();
                _listener = null;
                _logger.Info("TcpListener已停止");

                if(_listenerThread!=null && _listenerThread.IsAlive)
                {
                    _logger.Info("等待监听线程结束...");
                    _listenerThread.Join(1000);
                    _logger.Info("监听线程已结束");
                }
                _logger.Info("Socket服务已完全停止");
            }
            catch (Exception ex)
            {
                _logger.Error($"停止Socket服务时发生错误: {ex.Message}");
            }
        }

        private void ListenForClients()
        {
            _logger.Info("开始监听客户端连接...");
            try
            {
                while (_isRunning)
                {
                    _logger.Info("等待客户端连接...");
                    TcpClient client = _listener.AcceptTcpClient();
                    _logger.Info($"接受到客户端连接: {client.Client.RemoteEndPoint}");

                    Thread clientThread = new Thread(HandleClientCommunication)
                    {
                        IsBackground = true
                    };
                    clientThread.Start(client);
                    _logger.Info("客户端处理线程已启动");
                }
            }
            catch (SocketException ex)
            {
                _logger.Warning($"Socket异常: {ex.Message}");
            }
            catch(Exception ex)
            {
                _logger.Error($"监听客户端时发生错误: {ex.Message}");
            }
            _logger.Info("停止监听客户端连接");
        }

        private void HandleClientCommunication(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            NetworkStream stream = tcpClient.GetStream();
            
            _logger.Info("新客户端连接已建立");

            try
            {
                byte[] buffer = new byte[8192];

                while (_isRunning && tcpClient.Connected)
                {
                    // 读取客户端消息
                    int bytesRead = 0;

                    try
                    {
                        _logger.Info("等待客户端消息...");
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                        _logger.Info($"读取到 {bytesRead} 字节数据");
                    }
                    catch (IOException ex)
                    {
                        _logger.Warning($"客户端连接中断: {ex.Message}");
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        _logger.Info("客户端断开连接 (0 字节)");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    _logger.Info($"收到消息: {message}");

                    string response = ProcessJsonRPCRequest(message);
                    _logger.Info($"生成响应: {response}");

                    // 发送响应
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);
                    _logger.Info("响应已发送");
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
            _logger.Info("开始处理JSON-RPC请求");
            JsonRPCRequest request;

            try
            {
                _logger.Info("解析JSON-RPC请求...");
                // 解析JSON-RPC请求
                request = JsonConvert.DeserializeObject<JsonRPCRequest>(requestJson);

                // 验证请求格式是否有效
                if (request == null || !request.IsValid())
                {
                    _logger.Warning("无效的JSON-RPC请求格式");
                    return CreateErrorResponse(
                        null,
                        JsonRPCErrorCodes.InvalidRequest,
                        "Invalid JSON-RPC request"
                    );
                }

                _logger.Info($"请求方法: {request.Method}, ID: {request.Id}");

                // 查找命令
                if (!_commandRegistry.TryGetCommand(request.Method, out var command))
                {
                    _logger.Warning($"未找到命令: {request.Method}");
                    return CreateErrorResponse(request.Id, JsonRPCErrorCodes.MethodNotFound,
                        $"Method '{request.Method}' not found");
                }

                _logger.Info($"找到命令: {request.Method}, 开始执行...");

                // 执行命令
                try
                {                
                    object result = command.Execute(request.GetParamsObject(), request.Id);
                    _logger.Info($"命令执行成功: {request.Method}");

                    return CreateSuccessResponse(request.Id, result);
                }
                catch (Exception ex)
                {
                    _logger.Error($"命令执行失败: {request.Method}, 错误: {ex.Message}");
                    return CreateErrorResponse(request.Id, JsonRPCErrorCodes.InternalError, ex.Message);
                }
            }
            catch (JsonException ex)
            {
                _logger.Error($"JSON解析错误: {ex.Message}");
                // JSON解析错误
                return CreateErrorResponse(
                    null,
                    JsonRPCErrorCodes.ParseError,
                    "Invalid JSON"
                );
            }
            catch (Exception ex)
            {
                _logger.Error($"处理请求时发生错误: {ex.Message}");
                // 处理请求时的其他错误
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
