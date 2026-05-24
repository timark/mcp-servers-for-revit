using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class SetRotationForElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private List<string> _elementIds;
        private double _radians;
        private double _axisStartX;
        private double _axisStartY;
        private double _axisStartZ;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(List<string> elementIds, double radians, double axisStartX, double axisStartY, double axisStartZ)
        {
            _elementIds = elementIds;
            _radians = radians;
            _axisStartX = axisStartX;
            _axisStartY = axisStartY;
            _axisStartZ = axisStartZ;
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
                var basePoint = new XYZ(_axisStartX, _axisStartY, _axisStartZ);
                var axis = Line.CreateBound(basePoint, basePoint + XYZ.BasisZ);
                var rotated = 0;

                using (var tx = new Transaction(doc, "Rotate Elements"))
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
                            ElementTransformUtils.RotateElement(doc, el.Id, axis, _radians);
                            rotated++;
                        }
                        catch { }
                    }
                    tx.Commit();
                }

                Result = new { rotatedCount = rotated, radians = _radians };
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

        public string GetName() => "Set Rotation For Elements";
    }
}
