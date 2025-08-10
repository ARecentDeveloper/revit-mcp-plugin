using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using revit_mcp_sdk.API.Interfaces;
using SampleCommandSet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SampleCommandSet.Commands.Create
{
    /// <summary>
    /// External event handler for creating a wall
    /// </summary>
    public class CreateWallEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        // Parameters for creating the wall
        private double _startX;
        private double _startY;
        private double _endX;
        private double _endY;
        private double _height;
        private double _thickness;

        // Information of the created wall
        private Wall _createdWall;
        public WallInfo CreatedWallInfo { get; private set; }

        // Flag indicating whether the operation has completed
        private bool _taskCompleted;

        // Event wait handle
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        /// <summary>
        /// Set parameters for creating the wall
        /// </summary>
        public void SetWallParameters(double startX, double startY, double endX, double endY, double height, double thickness)
        {
            _startX = startX;
            _startY = startY;
            _endX = endX;
            _endY = endY;
            _height = height;
            _thickness = thickness;

            _taskCompleted = false;
            _resetEvent.Reset();
        }

        /// <summary>
        /// Wait for wall creation to complete
        /// </summary>
        /// <param name="timeoutMilliseconds">Timeout in milliseconds</param>
        /// <returns>Whether the operation completed before timeout</returns>
        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        /// <summary>
        /// IExternalEventHandler.Execute implementation
        /// </summary>
        public void Execute(UIApplication app)
        {
            try
            {
                Document doc = app.ActiveUIDocument.Document;

                using (Transaction trans = new Transaction(doc, "Create Wall"))
                {
                    trans.Start();

                    // Define start and end points for the wall
                    XYZ startPoint = new XYZ(_startX, _startY, 0);
                    XYZ endPoint = new XYZ(_endX, _endY, 0);

                    // Create the wall curve
                    Line curve = Line.CreateBound(startPoint, endPoint);

                    // Get a wall type from the current document (prefer a "Generic" type if available)
                    var allWallTypes = new FilteredElementCollector(doc)
                        .OfClass(typeof(WallType))
                        .Cast<WallType>()
                        .ToList();
                    WallType wallType = allWallTypes
                        .FirstOrDefault(w => w.Name.IndexOf("Generic", StringComparison.OrdinalIgnoreCase) >= 0)
                        ?? allWallTypes.FirstOrDefault();

                    // Create the wall
                    _createdWall = Wall.Create(
                        doc,
                        curve,
                        wallType.Id,
                        doc.ActiveView.GenLevel.Id,
                        _height,
                        0.0,  // Base offset
                        false,  // Do not flip
                        false); // Not a structural wall

                    trans.Commit();

                    // Get wall details
                    CreatedWallInfo = new WallInfo
                    {
                        ElementId = _createdWall.Id.IntegerValue,
                        StartPoint = new Models.Point { X = startPoint.X, Y = startPoint.Y, Z = 0 },
                        EndPoint = new Models.Point { X = endPoint.X, Y = endPoint.Y, Z = 0 },
                        Height = _height,
                        Thickness = _thickness,
                    };
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to create wall: {ex.Message}");

            }
            finally
            {
                _taskCompleted = true;
                _resetEvent.Set(); // Notify waiting thread that the operation has completed
            }
        }

        /// <summary>
        /// IExternalEventHandler.GetName implementation
        /// </summary>
        public string GetName()
        {
            return "Create Wall";
        }
    }
}
