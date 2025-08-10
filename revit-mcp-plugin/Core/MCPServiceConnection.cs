using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace revit_mcp_plugin.Core
{
    [Transaction(TransactionMode.Manual)]
    public class MCPServiceConnection : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get socket service
                SocketService service = SocketService.Instance;

                // Debug: Log current state
                var logger = new Utils.Logger();
                logger.Info($"MCPServiceConnection: IsRunning = {service.IsRunning}");

                if (service.IsRunning)
                {
                    logger.Info("MCPServiceConnection: Calling Stop()");
                    service.Stop();
                    TaskDialog.Show("revitMCP", "Close Server");
                }
                else
                {
                    logger.Info("MCPServiceConnection: Calling Initialize() and Start()");
                    service.Initialize(commandData.Application);
                    service.Start();
                    TaskDialog.Show("revitMCP", "Open Server");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
