using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class SetUserSelectionInRevitEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
                var uidoc = app.ActiveUIDocument;
                var ids = new List<ElementId>();

                foreach (var idStr in _elementIds)
                {
#if REVIT2024_OR_GREATER
                    if (!long.TryParse(idStr, out long idVal)) continue;
                    ids.Add(new ElementId(idVal));
#else
                    if (!int.TryParse(idStr, out int idVal)) continue;
                    ids.Add(new ElementId(idVal));
#endif
                }

                uidoc.Selection.SetElementIds(ids);
                Result = new { count = ids.Count };
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

        public string GetName() => "Set User Selection In Revit";
    }
}
