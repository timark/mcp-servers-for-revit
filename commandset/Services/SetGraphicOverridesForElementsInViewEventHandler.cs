using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class SetGraphicOverridesForElementsInViewEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private string _viewId;
        private List<string> _elementIds;
        private List<int> _colorRgb;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(string viewId, List<string> elementIds, List<int> colorRgb)
        {
            _viewId = viewId;
            _elementIds = elementIds;
            _colorRgb = colorRgb;
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
                if (view == null)
                {
                    Result = new { error = "View not found." };
                    return;
                }

                var color = (_colorRgb != null && _colorRgb.Count == 3)
                    ? new Color((byte)_colorRgb[0], (byte)_colorRgb[1], (byte)_colorRgb[2])
                    : new Color(255, 0, 0);

                using (var tx = new Transaction(doc, "Set Graphic Overrides"))
                {
                    tx.Start();
                    var ogs = new OverrideGraphicSettings();
                    ogs.SetProjectionLineColor(color);
                    ogs.SetSurfaceForegroundPatternColor(color);

                    foreach (var idStr in _elementIds)
                    {
#if REVIT2024_OR_GREATER
                        if (!long.TryParse(idStr, out long idVal)) continue;
                        var eid = new ElementId(idVal);
#else
                        if (!int.TryParse(idStr, out int idVal)) continue;
                        var eid = new ElementId(idVal);
#endif
                        var el = doc.GetElement(eid);
                        if (el == null) continue;
                        view.SetElementOverrides(el.Id, ogs);
                    }
                    tx.Commit();
                }

                Result = new
                {
                    viewId = _viewId,
                    elementCount = _elementIds.Count,
                    color = new { r = (int)color.Red, g = (int)color.Green, b = (int)color.Blue }
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

        public string GetName() => "Set Graphic Overrides For Elements In View";
    }
}
