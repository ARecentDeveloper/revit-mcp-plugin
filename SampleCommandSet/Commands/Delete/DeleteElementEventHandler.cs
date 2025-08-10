﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using revit_mcp_sdk.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SampleCommandSet.Commands.Delete
{
    public class DeleteElementEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        // Execution result
        public bool IsSuccess { get; private set; }

        // Number of successfully deleted elements
        public int DeletedCount { get; private set; }
        // State synchronization object
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        // Array of element IDs to delete
        public string[] ElementIds { get; set; }
        // Implement IWaitableExternalEventHandler interface
        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }
        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app.ActiveUIDocument.Document;
                DeletedCount = 0;
                if (ElementIds == null || ElementIds.Length == 0)
                {
                    IsSuccess = false;
                    return;
                }
                // Build the set of element IDs to delete
                List<ElementId> elementIdsToDelete = new List<ElementId>();
                List<string> invalidIds = new List<string>();
                foreach (var idStr in ElementIds)
                {
                    if (int.TryParse(idStr, out int elementIdValue))
                    {
                        var elementId = new ElementId(elementIdValue);
                        // Check whether the element exists
                        if (doc.GetElement(elementId) != null)
                        {
                            elementIdsToDelete.Add(elementId);
                        }
                    }
                    else
                    {
                        invalidIds.Add(idStr);
                    }
                }
                if (invalidIds.Count > 0)
                {
                    TaskDialog.Show("Warning", $"The following IDs are invalid or elements do not exist: {string.Join(", ", invalidIds)}");
                }
                // If there are elements to delete, perform deletion
                if (elementIdsToDelete.Count > 0)
                {
                    using (var transaction = new Transaction(doc, "Delete Elements"))
                    {
                        transaction.Start();

                        // Batch delete elements
                        ICollection<ElementId> deletedIds = doc.Delete(elementIdsToDelete);
                        DeletedCount = deletedIds.Count;

                        transaction.Commit();
                    }
                    IsSuccess = true;
                }
                else
                {
                    TaskDialog.Show("Error", "There are no valid elements to delete");
                    IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Failed to delete elements: " + ex.Message);
                IsSuccess = false;
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }
        public string GetName()
        {
            return "Delete Elements";
        }
    }
}
