using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetLocationForElementIdsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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

                    var loc = el.Location;
                    if (loc is LocationPoint lp)
                    {
                        results.Add(new
                        {
                            elementId = idStr,
                            locationType = "Point",
                            point = new { x = lp.Point.X, y = lp.Point.Y, z = lp.Point.Z }
                        });
                    }
                    else if (loc is LocationCurve lc)
                    {
                        var sp = lc.Curve.GetEndPoint(0);
                        var ep = lc.Curve.GetEndPoint(1);
                        results.Add(new
                        {
                            elementId = idStr,
                            locationType = "Curve",
                            start = new { x = sp.X, y = sp.Y, z = sp.Z },
                            end = new { x = ep.X, y = ep.Y, z = ep.Z }
                        });
                    }
                    else
                    {
                        results.Add(new { elementId = idStr, locationType = "None" });
                    }
                }

                Result = new { count = results.Count, locations = results };
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

        public string GetName() => "Get Location For Element IDs";
    }
}
