using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetElementTypesForElementIdsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
                    if (el == null) { notFound.Add(idStr); continue; }

                    var typeEl = doc.GetElement(el.GetTypeId());
                    results.Add(new
                    {
#if REVIT2024_OR_GREATER
                        elementId = el.Id.Value,
                        typeId = typeEl?.Id.Value,
#else
                        elementId = (object)el.Id.IntegerValue,
                        typeId = typeEl != null ? (object)typeEl.Id.IntegerValue : null,
#endif
                        typeName = typeEl?.Name
                    });
                }

                Result = new { count = results.Count, elements = results, notFound };
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

        public string GetName() => "Get Element Types For ElementIds";
    }
}
