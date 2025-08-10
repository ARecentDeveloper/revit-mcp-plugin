using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using revit_mcp_sdk.API.Base;
using System;
using System.Collections.Generic;

namespace SampleCommandSet.Commands.Access
{
    /// <summary>
    /// Command to get elements in the current view
    /// </summary>
    public class GetCurrentViewElementsCommand : ExternalEventCommandBase
    {
        private GetCurrentViewElementsEventHandler _handler => (GetCurrentViewElementsEventHandler)Handler;

        public override string CommandName => "get_current_view_elements";

        public GetCurrentViewElementsCommand(UIApplication uiApp)
            : base(new GetCurrentViewElementsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                // Parse parameters
                List<string> modelCategoryList = parameters?["modelCategoryList"]?.ToObject<List<string>>() ?? new List<string>();
                List<string> annotationCategoryList = parameters?["annotationCategoryList"]?.ToObject<List<string>>() ?? new List<string>();
                bool includeHidden = parameters?["includeHidden"]?.Value<bool>() ?? false;
                int limit = parameters?["limit"]?.Value<int>() ?? 100;

                // Set query parameters
                _handler.SetQueryParameters(modelCategoryList, annotationCategoryList, includeHidden, limit);

                // Trigger external event and wait for completion
                if (RaiseAndWaitForCompletion(60000)) // 60-second timeout
                {
                    return _handler.ResultInfo;
                }
                else
                {
                    throw new TimeoutException("Timeout getting view elements");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get view elements: {ex.Message}");
            }
        }
    }
}
