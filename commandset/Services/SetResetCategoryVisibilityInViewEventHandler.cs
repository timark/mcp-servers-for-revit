using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class SetResetCategoryVisibilityInViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private string _viewId;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(string viewId)
        {
            _viewId = viewId;
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

#if REVIT2024_OR_GREATER
                if (!long.TryParse(_viewId, out long vIdVal))
                {
                    Result = new { error = "Invalid view ID." };
                    return;
                }
                var viewEid = new ElementId(vIdVal);
#else
                if (!int.TryParse(_viewId, out int vIdVal))
                {
                    Result = new { error = "Invalid view ID." };
                    return;
                }
                var viewEid = new ElementId(vIdVal);
#endif

                var view = doc.GetElement(viewEid) as View;
                if (view == null || view.IsTemplate)
                {
                    Result = new { error = "View not found or is a view template." };
                    return;
                }

                using (var tx = new Transaction(doc, "Reset Category Visibility"))
                {
                    tx.Start();
                    foreach (Category category in doc.Settings.Categories)
                    {
                        if (category == null) continue;
                        try
                        {
                            if (view.CanCategoryBeHidden(category.Id))
                                view.SetCategoryHidden(category.Id, false);
                        }
                        catch { }
                    }
                    tx.Commit();
                }

                Result = new { viewId = _viewId, message = "All categories are now visible." };
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

        public string GetName() => "Set Reset Category Visibility In View";
    }
}
