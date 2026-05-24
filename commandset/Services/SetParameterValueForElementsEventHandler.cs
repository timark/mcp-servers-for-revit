using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class SetParameterValueForElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private string _parameterName;
        private List<string> _elementIds;
        private string _value;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(string parameterName, List<string> elementIds, string value)
        {
            _parameterName = parameterName;
            _elementIds = elementIds;
            _value = value;
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
                var updated = 0;

                using (var tx = new Transaction(doc, "Set Parameter Values"))
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

                        var p = el.LookupParameter(_parameterName);
                        if (p == null || p.IsReadOnly) continue;

                        try
                        {
                            if (p.StorageType == StorageType.String)
                                p.Set(_value);
                            else if (p.StorageType == StorageType.Double && double.TryParse(_value, out var d))
                                p.Set(d);
                            else if (p.StorageType == StorageType.Integer && int.TryParse(_value, out var i))
                                p.Set(i);
                            else if (p.StorageType == StorageType.ElementId && long.TryParse(_value, out var eIdVal))
#if REVIT2024_OR_GREATER
                                p.Set(new ElementId(eIdVal));
#else
                                p.Set(new ElementId((int)eIdVal));
#endif
                            updated++;
                        }
                        catch { }
                    }
                    tx.Commit();
                }

                Result = new
                {
                    parameterName = _parameterName,
                    updatedCount = updated,
                    elementCount = _elementIds.Count
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

        public string GetName() => "Set Parameter Value For Elements";
    }
}
