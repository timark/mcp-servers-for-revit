using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetElementsOnLevelEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private string _levelName;
        private List<string> _categories;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(string levelName, List<string> categories)
        {
            _levelName = levelName;
            _categories = categories;
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

                var level = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .FirstOrDefault(l => l.Name.Equals(_levelName, StringComparison.OrdinalIgnoreCase));

                if (level == null)
                {
                    Result = new { error = $"Level '{_levelName}' not found." };
                    return;
                }

                var levelId = level.Id;
                var collector = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .Where(e =>
                    {
                        try
                        {
                            var p = e.get_Parameter(BuiltInParameter.LEVEL_PARAM);
                            if (p != null && p.AsElementId() == levelId) return true;
                            p = e.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
                            if (p != null && p.AsElementId() == levelId) return true;
                            p = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                            if (p != null && p.AsElementId() == levelId) return true;
                            p = e.LookupParameter("Level");
                            if (p != null && p.AsValueString()?.Equals(_levelName, StringComparison.OrdinalIgnoreCase) == true) return true;
                        }
                        catch { }
                        return false;
                    })
                    .ToList();

                if (_categories != null && _categories.Count > 0)
                {
                    var normalizedCats = _categories.Select(c => c.ToLower()).ToHashSet();
                    collector = collector
                        .Where(e => e.Category != null && normalizedCats.Contains(e.Category.Name.ToLower()))
                        .ToList();
                }

                var elements = collector.Select(e => new
                {
#if REVIT2024_OR_GREATER
                    id = e.Id.Value,
#else
                    id = (object)e.Id.IntegerValue,
#endif
                    name = e.Name,
                    category = e.Category?.Name ?? ""
                }).ToList();

                Result = new { levelName = _levelName, elementCount = elements.Count, elements };
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

        public string GetName() => "Get Elements On Level";
    }
}
