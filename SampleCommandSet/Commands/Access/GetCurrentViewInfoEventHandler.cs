using Autodesk.Revit.UI;
using revit_mcp_sdk.API.Interfaces;
using System;
using System.Threading;
using SampleCommandSet.Extensions;
using SampleCommandSet.Models;

namespace SampleCommandSet.Commands.Access
{
    public class GetCurrentViewInfoEventHandler: IExternalEventHandler, IWaitableExternalEventHandler
    {
        // Execution result
        public ViewInfo ResultInfo { get; private set; }

        // State synchronization object
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        // Implement IWaitableExternalEventHandler interface
        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var uiDoc = app.ActiveUIDocument;
                var doc = uiDoc.Document;
                var activeView = doc.ActiveView;

                ResultInfo = new ViewInfo
                {
                    Id = activeView.Id.GetIdValue(),
                    UniqueId = activeView.UniqueId,
                    Name = activeView.Name,
                    ViewType = activeView.ViewType.ToString(),
                    IsTemplate = activeView.IsTemplate,
                    Scale = activeView.Scale,
                    DetailLevel = activeView.DetailLevel.ToString(),
                };
            }
            catch (Exception ex)
            {
                TaskDialog.Show("error", "Failed to get information");
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        public string GetName()
        {
            return "Get Current View Information";
        }
    }
}
