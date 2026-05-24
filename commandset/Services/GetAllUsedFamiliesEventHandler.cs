using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetAllUsedFamiliesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters()
        {
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
                var families = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .Where(f => !f.IsInPlace)
                    .Select(f => new
                    {
#if REVIT2024_OR_GREATER
                        id = f.Id.Value,
#else
                        id = (object)f.Id.IntegerValue,
#endif
                        name = f.Name
                    })
                    .OrderBy(f => f.name)
                    .ToList();

                Result = new { familyCount = families.Count, families };
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

        public string GetName() => "Get All Used Families In Model";
    }
}
