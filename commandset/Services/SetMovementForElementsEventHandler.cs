using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class SetMovementForElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private List<string> _elementIds;
        private double _dx;
        private double _dy;
        private double _dz;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(List<string> elementIds, double dx, double dy, double dz)
        {
            _elementIds = elementIds;
            _dx = dx;
            _dy = dy;
            _dz = dz;
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
                var vector = new XYZ(_dx, _dy, _dz);
                var moved = 0;

                using (var tx = new Transaction(doc, "Move Elements"))
                {
                    tx.Start();
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

                        try
                        {
                            ElementTransformUtils.MoveElement(doc, el.Id, vector);
                            moved++;
                        }
                        catch { }
                    }
                    tx.Commit();
                }

                Result = new
                {
                    movedCount = moved,
                    translation = new { dx = _dx, dy = _dy, dz = _dz }
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

        public string GetName() => "Set Movement For Elements";
    }
}
