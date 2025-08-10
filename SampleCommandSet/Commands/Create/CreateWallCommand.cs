using Autodesk.Revit.UI;
using revit_mcp_sdk.API.Base;
using System;
using Newtonsoft.Json.Linq;

namespace SampleCommandSet.Commands.Create
{
    /// <summary>
    /// Command to create a wall
    /// </summary>
    public class CreateWallCommand : ExternalEventCommandBase
    {
        private CreateWallEventHandler _handler => (CreateWallEventHandler)Handler;

        /// <summary>
        /// Command name
        /// </summary>
        public override string CommandName => "create_Wall";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="uiApp">Revit UIApplication</param>
        public CreateWallCommand(UIApplication uiApp)
            : base(new CreateWallEventHandler(), uiApp)
        {
        }

        /// <summary>
        /// Execute command
        /// </summary>
        /// <param name="parameters">JSON parameters</param>
        /// <param name="requestId">Request ID</param>
        /// <returns>Command execution result</returns>
        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                // Parse wall parameters
                double startX = parameters["startX"].Value<double>();
                double startY = parameters["startY"].Value<double>();
                double endX = parameters["endX"].Value<double>();
                double endY = parameters["endY"].Value<double>();
                double height = parameters["height"].Value<double>();
                double thickness = parameters["thickness"].Value<double>();

                // Set wall parameters
                _handler.SetWallParameters(startX, startY, endX, endY, height, thickness);

                // Trigger external event and wait for completion
                if (RaiseAndWaitForCompletion(10000))
                {
                    return _handler.CreatedWallInfo;
                }
                else
                {
                    throw new TimeoutException("Timeout creating wall");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create wall: {ex.Message}");
            }
        }
    }
}
