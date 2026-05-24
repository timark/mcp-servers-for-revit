using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetElementsByCategoryEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private int _categoryId;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(int categoryId)
        {
            _categoryId = categoryId;
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
                var bic = (BuiltInCategory)_categoryId;
                var elements = new FilteredElementCollector(doc)
                    .OfCategory(bic)
                    .WhereElementIsNotElementType()
                    .Select(e => new
                    {
#if REVIT2024_OR_GREATER
                        id = e.Id.Value,
#else
                        id = (object)e.Id.IntegerValue,
#endif
                        name = e.Name
                    })
                    .ToList();

                Result = new { categoryId = _categoryId, elementCount = elements.Count, elements };
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

        public string GetName() => "Get Elements By Category";
    }
}
