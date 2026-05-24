using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetCategoryByKeywordEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private string _keyword;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(string keyword)
        {
            _keyword = keyword;
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
                var matches = doc.Settings.Categories
                    .Cast<Category>()
                    .Where(c => c.Name.IndexOf(_keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    .Select(c => new
                    {
#if REVIT2024_OR_GREATER
                        id = c.Id.Value,
#else
                        id = (object)c.Id.IntegerValue,
#endif
                        name = c.Name,
                        type = c.CategoryType.ToString()
                    }).ToList();

                Result = new { keyword = _keyword, matchCount = matches.Count, categories = matches };
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

        public string GetName() => "Get Category By Keyword";
    }
}
