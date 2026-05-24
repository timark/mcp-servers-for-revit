using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetBoundingBoxesForElementIdsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private List<string> _elementIds;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(List<string> elementIds)
        {
            _elementIds = elementIds;
            TaskCompleted = false;
            _resetEvent.Reset();
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            _resetEvent.Reset();
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app.ActiveUIDocument.Document;
                var results = new List<object>();

                foreach (var idStr in _elementIds ?? new List<string>())
                {
#if REVIT2024_OR_GREATER
                    if (!long.TryParse(idStr, out long idVal)) continue;
                    var eid = new ElementId(idVal);
#else
                    if (!int.TryParse(idStr, out int idVal)) continue;
                    var eid = new ElementId(idVal);
#endif
                    var el = doc.GetElement(eid);
                    if (el == null) continue;

                    var bbox = el.get_BoundingBox(null);
                    if (bbox == null) continue;

                    results.Add(new
                    {
                        elementId = idStr,
                        min = new { x = bbox.Min.X, y = bbox.Min.Y, z = bbox.Min.Z },
                        max = new { x = bbox.Max.X, y = bbox.Max.Y, z = bbox.Max.Z }
                    });
                }

                Result = new { count = results.Count, boundingBoxes = results };
            }
            catch (Exception ex)
            {
                Result = new { error = ex.Message };
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        public string GetName() => "Get Bounding Boxes For Element IDs";
    }
}
