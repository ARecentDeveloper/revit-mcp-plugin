using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using revit_mcp_sdk.API.Base;
using System;
using System.Collections.Generic;

namespace SampleCommandSet.Commands.Access
{
    /// <summary>
    /// Command to get available family types in the current project
    /// </summary>
    public class GetAvailableFamilyTypesCommand : ExternalEventCommandBase
    {
        private GetAvailableFamilyTypesEventHandler _handler => (GetAvailableFamilyTypesEventHandler)Handler;

        public override string CommandName => "get_available_family_types";

        public GetAvailableFamilyTypesCommand(UIApplication uiApp)
            : base(new GetAvailableFamilyTypesEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                // Parse parameters
                List<string> categoryList = parameters?["categoryList"]?.ToObject<List<string>>() ?? new List<string>();
                string familyNameFilter = parameters?["familyNameFilter"]?.Value<string>();
                int? limit = parameters?["limit"]?.Value<int>();

                // Set query parameters
                _handler.CategoryList = categoryList;
                _handler.FamilyNameFilter = familyNameFilter;
                _handler.Limit = limit;

                // Trigger external event and wait for completion (max 15 seconds)
                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.ResultFamilyTypes;
                }
                else
                {
                    throw new TimeoutException("Timeout getting available family types");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get available family types: {ex.Message}");
            }
        }
    }
}