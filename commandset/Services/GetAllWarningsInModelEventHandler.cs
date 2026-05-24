using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetAllWarningsInModelEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
                var warnings = doc.GetWarnings();
                var list = warnings.Select(w => new
                {
                    description = w.GetDescriptionText(),
                    severity = w.GetSeverity().ToString(),
                    elementIds = w.GetFailingElements().Select(e =>
#if REVIT2024_OR_GREATER
                        e.Value
#else
                        (object)e.IntegerValue
#endif
                    ).ToList()
                }).ToList();

                Result = new { warningCount = list.Count, warnings = list };
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

        public string GetName() => "Get All Warnings In Model";
    }
}
