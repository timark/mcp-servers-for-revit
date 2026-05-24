using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetParameterValueForElementIdsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private string _parameterName;
        private List<string> _elementIds;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(string parameterName, List<string> elementIds)
        {
            _parameterName = parameterName;
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

                    var param = el.LookupParameter(_parameterName);
                    if (param == null)
                    {
                        results.Add(new { elementId = idStr, value = (string)null, found = false });
                        continue;
                    }

                    var val = param.AsValueString()
                        ?? param.AsString()
                        ?? (param.StorageType == StorageType.Double ? param.AsDouble().ToString("F4") : null)
                        ?? (param.StorageType == StorageType.Integer ? param.AsInteger().ToString() : null)
                        ?? "";

                    results.Add(new { elementId = idStr, value = val, found = true });
                }

                Result = new { parameterName = _parameterName, count = results.Count, results };
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

        public string GetName() => "Get Parameter Value For Element IDs";
    }
}
