using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using TUnit.Core;
using TUnit.Core.Executors;

namespace RevitMCPCommandSet.Tests.Access;

public class GetParametersFromElementIdTests : RevitApiTest
{
    private static Document _doc;
    private static Wall _wall;
    private static long _wallId;

    [Before(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Setup()
    {
        _doc = Application.NewProjectDocument(UnitSystem.Imperial);

        using var tx = new Transaction(_doc, "Setup Parameter Tests");
        tx.Start();

        var level = Level.Create(_doc, 0.0);
        level.Name = "Param Test Level";

        _wall = Wall.Create(
            _doc,
            Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0)),
            level.Id,
            false);

        // Set a known parameter value so we can assert on it
        var commentsParam = _wall.LookupParameter("Comments");
        if (commentsParam != null && !commentsParam.IsReadOnly)
            commentsParam.Set("test-comment");

        tx.Commit();

#if REVIT2024_OR_GREATER
        _wallId = _wall.Id.Value;
#else
        _wallId = _wall.Id.IntegerValue;
#endif
    }

    [After(HookType.Class)]
    [HookExecutor<RevitThreadExecutor>]
    public static void Cleanup()
    {
        _doc?.Close(false);
    }

    [Test]
    public async Task GetElement_ByValidId_ReturnsElement()
    {
#if REVIT2024_OR_GREATER
        var id = new ElementId(_wallId);
#else
        var id = new ElementId((int)_wallId);
#endif
        var element = _doc.GetElement(id);
        await Assert.That(element).IsNotNull();
        await Assert.That(element.Category.Name).IsEqualTo("Walls");
    }

    [Test]
    public async Task GetElement_ByInvalidId_ReturnsNull()
    {
#if REVIT2024_OR_GREATER
        var id = new ElementId(-99999L);
#else
        var id = new ElementId(-99999);
#endif
        var element = _doc.GetElement(id);
        await Assert.That(element).IsNull();
    }

    [Test]
    public async Task CollectAllParameters_FromWall_ReturnsNonEmptyList()
    {
#if REVIT2024_OR_GREATER
        var id = new ElementId(_wallId);
#else
        var id = new ElementId((int)_wallId);
#endif
        var element = _doc.GetElement(id);
        var parameters = element.Parameters.Cast<Parameter>().ToList();
        await Assert.That(parameters.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task CollectParameters_FilterByName_OnlyMatchingReturned()
    {
#if REVIT2024_OR_GREATER
        var id = new ElementId(_wallId);
#else
        var id = new ElementId((int)_wallId);
#endif
        var element = _doc.GetElement(id);
        var filterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Comments" };
        var filtered = element.Parameters.Cast<Parameter>()
            .Where(p => filterNames.Contains(p.Definition.Name))
            .ToList();

        await Assert.That(filtered.Count).IsEqualTo(1);
        await Assert.That(filtered[0].Definition.Name).IsEqualTo("Comments");
    }

    [Test]
    public async Task ParameterValue_StringType_ReturnsSetValue()
    {
#if REVIT2024_OR_GREATER
        var id = new ElementId(_wallId);
#else
        var id = new ElementId((int)_wallId);
#endif
        var element = _doc.GetElement(id);
        var param = element.LookupParameter("Comments");

        await Assert.That(param).IsNotNull();
        await Assert.That(param.StorageType).IsEqualTo(StorageType.String);
        await Assert.That(param.AsString()).IsEqualTo("test-comment");
    }

    [Test]
    public async Task ParameterValue_DoubleType_ProducesFormattedString()
    {
#if REVIT2024_OR_GREATER
        var id = new ElementId(_wallId);
#else
        var id = new ElementId((int)_wallId);
#endif
        var element = _doc.GetElement(id);
        var doubleParams = element.Parameters.Cast<Parameter>()
            .Where(p => p.StorageType == StorageType.Double)
            .ToList();

        await Assert.That(doubleParams.Count).IsGreaterThan(0);

        // Verify formatting: 4 decimal places, parseable as double
        var formatted = doubleParams[0].AsDouble().ToString("F4");
        await Assert.That(double.TryParse(formatted, out _)).IsTrue();
    }
}
