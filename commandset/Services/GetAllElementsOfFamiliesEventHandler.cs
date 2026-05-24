using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetAllElementsOfFamiliesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private List<string> _familyNames;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(List<string> familyNames)
        {
            _familyNames = familyNames;
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
                var elements = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>()
                    .Where(fi => _familyNames.Any(fn =>
                        fi.Symbol?.Family?.Name?.Equals(fn, StringComparison.OrdinalIgnoreCase) == true))
                    .Select(fi => new
                    {
#if REVIT2024_OR_GREATER
                        id = fi.Id.Value,
#else
                        id = (object)fi.Id.IntegerValue,
#endif
                        familyName = fi.Symbol?.Family?.Name
                    })
                    .ToList();

                Result = new { familyNames = _familyNames, elementCount = elements.Count, elements };
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

        public string GetName() => "Get All Elements Of Specific Families";
    }
}
