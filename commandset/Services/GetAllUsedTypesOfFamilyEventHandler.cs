using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetAllUsedTypesOfFamilyEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private string _familyName;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(string familyName)
        {
            _familyName = familyName;
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
                var types = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .Where(fs => fs.Family.Name.Equals(_familyName, StringComparison.OrdinalIgnoreCase))
                    .Select(fs => new
                    {
#if REVIT2024_OR_GREATER
                        typeId = fs.Id.Value,
#else
                        typeId = (object)fs.Id.IntegerValue,
#endif
                        typeName = fs.Name
                    })
                    .ToList();

                Result = new { familyName = _familyName, typeCount = types.Count, types };
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

        public string GetName() => "Get All Used Types Of Family";
    }
}
