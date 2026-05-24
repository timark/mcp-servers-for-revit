using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetAllElementsShownInViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private long _viewId;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(long viewId)
        {
            _viewId = viewId;
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
                var viewElementId = new ElementId(_viewId);
#else
                var viewElementId = new ElementId((int)_viewId);
#endif
                var view = doc.GetElement(viewElementId) as View;
                if (view == null)
                {
                    Result = new { error = $"View with ID {_viewId} not found." };
                    return;
                }

                var elements = new FilteredElementCollector(doc, view.Id)
                    .WhereElementIsNotElementType()
                    .Select(e => new
                    {
#if REVIT2024_OR_GREATER
                        id = e.Id.Value,
#else
                        id = (object)e.Id.IntegerValue,
#endif
                        name = e.Name,
                        category = e.Category?.Name ?? ""
                    })
                    .ToList();

                Result = new { viewId = _viewId, elementCount = elements.Count, elements };
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

        public string GetName() => "Get All Elements Shown In View";
    }
}
