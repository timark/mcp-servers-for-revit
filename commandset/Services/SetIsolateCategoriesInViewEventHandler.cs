using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class SetIsolateCategoriesInViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private string _viewId;
        private List<long> _categoryIds;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(string viewId, List<long> categoryIds)
        {
            _viewId = viewId;
            _categoryIds = categoryIds;
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

                var categoryIdSet = new HashSet<long>(_categoryIds);

                using (var tx = new Transaction(doc, "Isolate Categories"))
                {
                    tx.Start();
                    foreach (Category category in doc.Settings.Categories)
                    {
                        if (category == null) continue;
                        if (!view.CanCategoryBeHidden(category.Id)) continue;

#if REVIT2024_OR_GREATER
                        bool shouldHide = !categoryIdSet.Contains(category.Id.Value);
#else
                        bool shouldHide = !categoryIdSet.Contains((long)category.Id.IntegerValue);
#endif
                        try { view.SetCategoryHidden(category.Id, shouldHide); } catch { }
                    }
                    tx.Commit();
                }

                Result = new { viewId = _viewId, isolatedCategoryCount = _categoryIds.Count };
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

        public string GetName() => "Set Isolate Categories In View";
    }
}
