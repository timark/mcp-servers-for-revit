using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Views;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services.Views
{
    public class CreateSheetEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private List<SheetCreationInfo> _sheetData;

        public object Result { get; private set; }
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(List<SheetCreationInfo> sheetData)
        {
            _sheetData = sheetData;
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
                var created = new List<object>();
                var errors = new List<string>();

                // Find a fallback title block type if none specified
                ElementId fallbackTitleBlockId = GetFallbackTitleBlockId(doc);

                using (var tx = new Transaction(doc, "Create Sheets"))
                {
                    tx.Start();

                    foreach (var info in _sheetData ?? new List<SheetCreationInfo>())
                    {
                        try
                        {
                            // Resolve title block type ID
                            ElementId titleBlockId = ResolveTitleBlockId(doc, info, fallbackTitleBlockId);

                            // Create the sheet
                            var sheet = ViewSheet.Create(doc, titleBlockId);
                            sheet.SheetNumber = info.SheetNumber;
                            sheet.Name = info.SheetName;

                            // Apply any extra parameters
                            foreach (var kvp in info.Parameters ?? new Dictionary<string, object>())
                            {
                                var param = sheet.LookupParameter(kvp.Key);
                                if (param == null || param.IsReadOnly) continue;
                                try
                                {
                                    if (param.StorageType == StorageType.String)
                                        param.Set(kvp.Value?.ToString() ?? "");
                                    else if (param.StorageType == StorageType.Double
                                             && double.TryParse(kvp.Value?.ToString(), out double d))
                                        param.Set(d);
                                    else if (param.StorageType == StorageType.Integer
                                             && int.TryParse(kvp.Value?.ToString(), out int i))
                                        param.Set(i);
                                }
                                catch { }
                            }

                            created.Add(new
                            {
#if REVIT2024_OR_GREATER
                                id = sheet.Id.Value,
#else
                                id = sheet.Id.IntegerValue,
#endif
                                sheetNumber = sheet.SheetNumber,
                                sheetName = sheet.Name,
                                uniqueId = sheet.UniqueId
                            });
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Sheet '{info.SheetNumber} - {info.SheetName}': {ex.Message}");
                        }
                    }

                    tx.Commit();
                }

                Result = new
                {
                    createdCount = created.Count,
                    sheets = created,
                    errorCount = errors.Count,
                    errors
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

        private static ElementId GetFallbackTitleBlockId(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType()
                .FirstOrDefault()?.Id ?? ElementId.InvalidElementId;
        }

        private static ElementId ResolveTitleBlockId(Document doc, SheetCreationInfo info, ElementId fallback)
        {
            // Try explicit type ID first
            if (info.TitleBlockTypeId != 0)
            {
#if REVIT2024_OR_GREATER
                var eid = new ElementId((long)info.TitleBlockTypeId);
#else
                var eid = new ElementId(info.TitleBlockTypeId);
#endif
                if (doc.GetElement(eid) != null)
                    return eid;
            }

            // Try matching by family/type name
            if (!string.IsNullOrEmpty(info.TitleBlockFamilyName) || !string.IsNullOrEmpty(info.TitleBlockTypeName))
            {
                var match = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .WhereElementIsElementType()
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(fs =>
                        (string.IsNullOrEmpty(info.TitleBlockFamilyName) ||
                         fs.Family.Name.Equals(info.TitleBlockFamilyName, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrEmpty(info.TitleBlockTypeName) ||
                         fs.Name.Equals(info.TitleBlockTypeName, StringComparison.OrdinalIgnoreCase)));

                if (match != null)
                    return match.Id;
            }

            return fallback;
        }

        public string GetName() => "Create Sheet";
    }
}
