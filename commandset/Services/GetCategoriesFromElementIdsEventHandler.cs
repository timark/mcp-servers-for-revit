using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetCategoriesFromElementIdsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
                var elements = new List<Element>();
                var notFound = new List<string>();

                foreach (var idStr in _elementIds ?? new List<string>())
                {
#if REVIT2024_OR_GREATER
                    if (!long.TryParse(idStr, out long idLong)) { notFound.Add(idStr); continue; }
                    var elementId = new ElementId(idLong);
#else
                    if (!int.TryParse(idStr, out int idInt)) { notFound.Add(idStr); continue; }
                    var elementId = new ElementId(idInt);
#endif
                    var el = doc.GetElement(elementId);
                    if (el == null) notFound.Add(idStr);
                    else elements.Add(el);
                }

                var categories = elements
                    .GroupBy(e => e.Category?.Name ?? "None")
                    .Select(g => new { categoryName = g.Key, elementCount = g.Count() })
                    .OrderBy(c => c.categoryName)
                    .ToList();

                Result = new { uniqueCategoryCount = categories.Count, categories, notFound };
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

        public string GetName() => "Get Categories From ElementIds";
    }
}
