using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetModelCategoriesEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
                var categories = doc.Settings.Categories
                    .Cast<Category>()
                    .Select(c => new
                    {
#if REVIT2024_OR_GREATER
                        id = c.Id.Value,
#else
                        id = (object)c.Id.IntegerValue,
#endif
                        name = c.Name,
                        type = c.CategoryType.ToString()
                    })
                    .OrderBy(c => c.name)
                    .ToList();

                Result = new { categoryCount = categories.Count, categories };
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

        public string GetName() => "Get Model Categories";
    }
}
