using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using revit_mcp_sdk.API.Base;
using System;

namespace SampleCommandSet.Commands.Access
{
    /// <summary>
    /// Command to get currently selected elements
    /// </summary>
    public class GetSelectedElementsCommand : ExternalEventCommandBase
    {
        private GetSelectedElementsEventHandler _handler => (GetSelectedElementsEventHandler)Handler;

        public override string CommandName => "get_selected_elements";

        public GetSelectedElementsCommand(UIApplication uiApp)
            : base(new GetSelectedElementsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                // Parse parameters
                int? limit = parameters?["limit"]?.Value<int>();

                // Set limit
                _handler.Limit = limit;

                // Trigger external event and wait for completion
                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.ResultElements;
                }
                else
                {
                    throw new TimeoutException("Timeout getting selected elements");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get selected elements: {ex.Message}");
            }
        }
    }
}