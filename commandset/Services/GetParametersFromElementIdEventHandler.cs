using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPCommandSet.Services
{
    public class GetParametersFromElementIdEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private List<string> _elementIds;
        private List<string> _parameterNames;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetQueryParameters(List<string> elementIds, List<string> parameterNames)
        {
            _elementIds = elementIds;
            _parameterNames = parameterNames;
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
                var filterNames = new HashSet<string>(_parameterNames ?? new List<string>(),
                    StringComparer.OrdinalIgnoreCase);
                bool filterActive = filterNames.Count > 0;

                var elements = new List<object>();
                var notFound = new List<string>();

                foreach (var idStr in _elementIds ?? new List<string>())
                {
#if REVIT2024_OR_GREATER
                    if (!long.TryParse(idStr, out long idLong))
                    {
                        notFound.Add(idStr);
                        continue;
                    }
                    var elementId = new ElementId(idLong);
#else
                    if (!int.TryParse(idStr, out int idInt))
                    {
                        notFound.Add(idStr);
                        continue;
                    }
                    var elementId = new ElementId(idInt);
#endif
                    var element = doc.GetElement(elementId);
                    if (element == null)
                    {
                        notFound.Add(idStr);
                        continue;
                    }

                    var parameters = element.Parameters.Cast<Parameter>()
                        .Where(p => !filterActive || filterNames.Contains(p.Definition.Name))
                        .Select(p => new
                        {
                            name = p.Definition.Name,
                            value = GetParameterValue(p),
                            storageType = p.StorageType.ToString(),
                            isReadOnly = p.IsReadOnly
                        })
                        .OrderBy(p => p.name)
                        .ToList<object>();

                    elements.Add(new
                    {
#if REVIT2024_OR_GREATER
                        elementId = element.Id.Value,
#else
                        elementId = element.Id.IntegerValue,
#endif
                        name = element.Name,
                        category = element.Category?.Name ?? "unknown",
                        parameters
                    });
                }

                Result = new { elements, notFound };
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

        private static string GetParameterValue(Parameter param)
        {
            if (param.StorageType == StorageType.String)
                return param.AsString() ?? "";
            if (param.StorageType == StorageType.Double)
                return param.AsDouble().ToString("F4");
            if (param.StorageType == StorageType.Integer)
                return param.AsInteger().ToString();
            if (param.StorageType == StorageType.ElementId)
            {
#if REVIT2024_OR_GREATER
                return param.AsElementId().Value.ToString();
#else
                return param.AsElementId().IntegerValue.ToString();
#endif
            }
            return "";
        }

        public string GetName() => "Get Parameters From ElementId";
    }
}
