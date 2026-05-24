using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class SetCategoryVisibilityInViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private string _viewId;
        private List<long> _categoryIds;
        private bool _visible;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(string viewId, List<long> categoryIds, bool visible)
        {
            _viewId = viewId;
            _categoryIds = categoryIds;
            _visible = visible;
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

                var modified = new List<string>();
                var failed = new List<string>();

                using (var tx = new Transaction(doc, "Set Category Visibility"))
                {
                    tx.Start();
                    foreach (var catId in _categoryIds)
                    {
#if REVIT2024_OR_GREATER
                        var category = Category.GetCategory(doc, new ElementId(catId));
#else
                        var category = Category.GetCategory(doc, new ElementId((int)catId));
#endif
                        if (category == null)
                        {
                            failed.Add($"Category {catId} not found");
                            continue;
                        }
                        if (!view.CanCategoryBeHidden(category.Id))
                        {
                            failed.Add($"{category.Name} cannot be hidden");
                            continue;
                        }
                        view.SetCategoryHidden(category.Id, !_visible);
                        modified.Add(category.Name);
                    }
                    tx.Commit();
                }

                Result = new
                {
                    viewId = _viewId,
                    action = _visible ? "shown" : "hidden",
                    modifiedCount = modified.Count,
                    modified,
                    failedCount = failed.Count,
                    failed
                };
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

        public string GetName() => "Set Category Visibility In View";
    }
}
