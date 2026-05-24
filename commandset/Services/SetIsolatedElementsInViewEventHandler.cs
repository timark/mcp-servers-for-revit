using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class SetIsolatedElementsInViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private string _viewId;
        private List<string> _elementIds;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(string viewId, List<string> elementIds)
        {
            _viewId = viewId;
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

#if REVIT2024_OR_GREATER
                if (!long.TryParse(_viewId, out long vIdVal))
                {
                    Result = new { error = "Invalid view ID." };
                    return;
                }
                var viewEid = new ElementId(vIdVal);
#else
                if (!int.TryParse(_viewId, out int vIdVal))
                {
                    Result = new { error = "Invalid view ID." };
                    return;
                }
                var viewEid = new ElementId(vIdVal);
#endif

                var view = doc.GetElement(viewEid) as View;
                if (view == null)
                {
                    Result = new { error = "View not found." };
                    return;
                }

                var elIds = new List<ElementId>();
                foreach (var idStr in _elementIds)
                {
#if REVIT2024_OR_GREATER
                    if (!long.TryParse(idStr, out long idVal)) continue;
                    elIds.Add(new ElementId(idVal));
#else
                    if (!int.TryParse(idStr, out int idVal)) continue;
                    elIds.Add(new ElementId(idVal));
#endif
                }

                using (var tx = new Transaction(doc, "Isolate Elements in View"))
                {
                    tx.Start();
                    view.IsolateElementsTemporary(elIds);
                    tx.Commit();
                }

                Result = new { viewId = _viewId, isolatedCount = elIds.Count };
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

        public string GetName() => "Set Isolated Elements In View";
    }
}
