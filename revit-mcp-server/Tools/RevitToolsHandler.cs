using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;
using RevitMcpServer.Database;
using RevitMcpServer.WebSocket;

namespace RevitMcpServer.Tools;

[McpServerToolType]
public class RevitToolsHandler(RevitWebSocketClient wsClient, DatabaseService db)
{
    // ── Helper ───────────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _pretty = new() { WriteIndented = true };

    private static string Serialize(object? value) =>
        JsonSerializer.Serialize(value, _pretty);

    // ── Revit relay tools ────────────────────────────────────────────────────

    [McpServerTool(Name = "ai_element_filter")]
    [Description("An intelligent Revit element querying tool designed specifically for AI assistants to retrieve detailed element information from Revit projects. This tool allows the AI to request elements matching specific criteria (such as category, type, visibility, or spatial location) and then perform further analysis on the returned data to answer complex user queries about Revit model elements. Example: When a user asks 'Find all walls taller than 5m in the project', the AI would: 1) Call this tool with parameters: {\"filterCategory\": \"OST_Walls\", \"includeInstances\": true}, 2) Receive detailed information about all wall instances in the project, 3) Process the returned data to filter walls with height > 5000mm, 4) Present the filtered results to the user with relevant details.")]
    public async Task<string> AiElementFilter(
        [Description("Configuration parameters for the Revit element filter tool. These settings determine which elements will be selected from the Revit project based on various filtering criteria. Multiple filters can be combined to achieve precise element selection. All spatial coordinates should be provided in millimeters. Fields: filterCategory (string, optional) - Revit built-in category e.g. OST_Walls; filterElementType (string, optional); filterFamilySymbolId (number, optional); includeTypes (bool, default false); includeInstances (bool, default true); filterVisibleInCurrentView (bool, optional); boundingBoxMin (object, optional); boundingBoxMax (object, optional); maxElements (number, optional, default 50).")]
        JsonElement data)
    {
        return await wsClient.SendCommandAsync("ai_element_filter", new { data });
    }

    [McpServerTool(Name = "analyze_model_statistics")]
    [Description("Analyze model complexity with element counts. Returns detailed statistics about the Revit model including total element counts, total types, total families, views, sheets, counts by category (with type/family breakdown), and level-by-level element distribution. Useful for model auditing, performance analysis, and understanding model composition.")]
    public async Task<string> AnalyzeModelStatistics(
        [Description("Whether to include detailed breakdown by family and type within each category. Defaults to true.")]
        bool includeDetailedTypes = true)
    {
        return await wsClient.SendCommandAsync("analyze_model_statistics",
            new { includeDetailedTypes });
    }

    [McpServerTool(Name = "color_elements")]
    [Description("Color elements in the current view based on a category and parameter value. Each unique parameter value gets assigned a distinct color.")]
    public async Task<string> ColorElements(
        [Description("The name of the Revit category to color (e.g., 'Walls', 'Doors', 'Rooms')")]
        string categoryName,
        [Description("The name of the parameter to use for grouping and coloring elements")]
        string parameterName,
        [Description("Whether to use a gradient color scheme instead of random colors")]
        bool useGradient = false,
        [Description("Optional JSON array of custom RGB colors, e.g. [{\"r\":255,\"g\":0,\"b\":0}]. Omit or pass null for automatic colors.")]
        string? customColors = null)
    {
        object? colorsValue = customColors != null
            ? JsonSerializer.Deserialize<JsonElement>(customColors)
            : null;
        return await wsClient.SendCommandAsync("color_splash",
            new { categoryName, parameterName, useGradient, customColors = colorsValue });
    }

    [McpServerTool(Name = "create_dimensions")]
    [Description("Create dimension annotations in the current Revit view. Supports dimensioning between elements (walls, doors, windows) by element IDs, or between two points with automatic reference detection. All coordinates are in millimeters (mm).")]
    public async Task<string> CreateDimensions(
        [Description("Array of dimension objects to create. Each object: startPoint {x,y,z}, endPoint {x,y,z}, linePoint {x,y,z} (optional), elementIds (number[], optional), dimensionType (string, default 'Linear'), dimensionStyleId (number, default -1), viewId (number, default -1).")]
        JsonElement dimensions)
    {
        return await wsClient.SendCommandAsync("create_dimensions", new { dimensions });
    }

    [McpServerTool(Name = "create_grid")]
    [Description("Create a grid system in Revit with smart spacing generation. Supports both X-axis (vertical) and Y-axis (horizontal) grids with customizable naming styles (alphabetic A,B,C or numeric 1,2,3). All units are in millimeters (mm).")]
    public async Task<string> CreateGrid(
        [Description("Number of grid lines along X-axis (vertical grids)")] int xCount,
        [Description("Spacing between X-axis grid lines in millimeters")] double xSpacing,
        [Description("Number of grid lines along Y-axis (horizontal grids)")] int yCount,
        [Description("Spacing between Y-axis grid lines in millimeters")] double ySpacing,
        [Description("Starting label for X-axis grids (e.g., 'A' or '1')")] string xStartLabel = "A",
        [Description("Naming style for X-axis: 'alphabetic' (A,B,C...) or 'numeric' (1,2,3...)")] string xNamingStyle = "alphabetic",
        [Description("Starting label for Y-axis grids (e.g., '1' or 'A')")] string yStartLabel = "1",
        [Description("Naming style for Y-axis: 'alphabetic' (A,B,C...) or 'numeric' (1,2,3...)")] string yNamingStyle = "numeric",
        [Description("Minimum extent along X-axis in mm (where Y-axis grids start)")] double xExtentMin = 0,
        [Description("Maximum extent along X-axis in mm (where Y-axis grids end)")] double xExtentMax = 50000,
        [Description("Minimum extent along Y-axis in mm (where X-axis grids start)")] double yExtentMin = 0,
        [Description("Maximum extent along Y-axis in mm (where X-axis grids end)")] double yExtentMax = 50000,
        [Description("Elevation for grid lines in mm (Z-coordinate)")] double elevation = 0,
        [Description("Starting position for first X-axis grid in mm")] double xStartPosition = 0,
        [Description("Starting position for first Y-axis grid in mm")] double yStartPosition = 0)
    {
        return await wsClient.SendCommandAsync("create_grid",
            new { xCount, xSpacing, xStartLabel, xNamingStyle, yCount, ySpacing, yStartLabel,
                  yNamingStyle, xExtentMin, xExtentMax, yExtentMin, yExtentMax,
                  elevation, xStartPosition, yStartPosition });
    }

    [McpServerTool(Name = "create_level")]
    [Description("Create one or more levels in Revit at specified elevations. Levels define horizontal planes in the building and are used to host floor plans, ceilings, and other level-based elements. All elevation units are in millimeters (mm).")]
    public async Task<string> CreateLevel(
        [Description("Array of level objects to create. Each object: name (string), elevation (number, mm), description (string, optional), isMainLevel (bool, default true), isBuildingStory (bool, default true), computationHeight (number, optional), viewPlanOffset (number, optional), viewSectionOffset (number, optional), viewElevationOffset (number, optional), createFloorPlan (bool, default true), createCeilingPlan (bool, default true).")]
        JsonElement data)
    {
        return await wsClient.SendCommandAsync("create_level", new { data });
    }

    [McpServerTool(Name = "create_line_based_element")]
    [Description("Create one or more line-based elements in Revit such as walls, beams, or pipes. Supports batch creation with detailed parameters including family type ID, start and end points, thickness, height, and level information. All units are in millimeters (mm).")]
    public async Task<string> CreateLineBasedElement(
        [Description("Array of line-based element objects. Each object: category (string, e.g. OST_Walls), typeId (number, optional), locationLine {p0:{x,y,z}, p1:{x,y,z}}, thickness (number), height (number), baseLevel (number), baseOffset (number).")]
        JsonElement data)
    {
        return await wsClient.SendCommandAsync("create_line_based_element", new { data });
    }

    [McpServerTool(Name = "create_point_based_element")]
    [Description("Create one or more point-based elements in Revit such as doors, windows, or furniture. Supports batch creation with detailed parameters including family type ID, position, dimensions, and level information. All units are in millimeters (mm).")]
    public async Task<string> CreatePointBasedElement(
        [Description("Array of point-based element objects. Each object: name (string), typeId (number, optional), locationPoint {x,y,z}, width (number), depth (number, optional), height (number), baseLevel (number), baseOffset (number), rotation (number, optional, degrees), hostWallId (number, optional), facingFlipped (bool, optional, default false).")]
        JsonElement data)
    {
        return await wsClient.SendCommandAsync("create_point_based_element", new { data });
    }

    [McpServerTool(Name = "create_room")]
    [Description("Create and place rooms in Revit at specified locations. Rooms are placed within enclosed wall boundaries and can be named and numbered. The location point should be inside an enclosed area bounded by walls. All coordinates are in millimeters (mm).")]
    public async Task<string> CreateRoom(
        [Description("Array of room objects. Each object: name (string), number (string, optional), location {x,y,z}, levelId (number, optional), upperLimitId (number, optional), limitOffset (number, optional), baseOffset (number, optional), department (string, optional), comments (string, optional).")]
        JsonElement data)
    {
        return await wsClient.SendCommandAsync("create_room", new { data });
    }

    [McpServerTool(Name = "create_sheet")]
    [Description("Create one or more sheets in Revit. Each sheet requires a sheet number and name. Optionally specify a title block by type ID, family name, or type name — if omitted the first available title block is used.")]
    public async Task<string> CreateSheet(
        [Description("Array of sheet objects to create. Each object: sheetNumber (string), sheetName (string), titleBlockTypeId (number, optional), titleBlockFamilyName (string, optional), titleBlockTypeName (string, optional), parameters (object, optional key-value pairs for additional sheet parameters).")]
        JsonElement data)
    {
        return await wsClient.SendCommandAsync("create_sheet", new { data });
    }

    [McpServerTool(Name = "create_structural_framing_system")]
    [Description("Create a structural beam framing system in Revit. Generates beams within a rectangular boundary at fixed spacing intervals. The system uses Revit's BeamSystem API to create properly connected beam layouts. All units are in millimeters (mm).")]
    public async Task<string> CreateStructuralFramingSystem(
        [Description("Name of the level to place the beam system on (e.g., 'Level 1'). If the level doesn't exist but follows 'Level N' pattern, it will be auto-created at 4000mm floor-to-floor height.")]
        string levelName,
        [Description("Minimum X coordinate of the rectangular boundary in millimeters")] double xMin,
        [Description("Maximum X coordinate of the rectangular boundary in millimeters")] double xMax,
        [Description("Minimum Y coordinate of the rectangular boundary in millimeters")] double yMin,
        [Description("Maximum Y coordinate of the rectangular boundary in millimeters")] double yMax,
        [Description("Spacing between beams in millimeters")] double spacing,
        [Description("Which edge defines the beam direction. Beams run perpendicular to this edge. 'bottom'/'top' = beams run in Y direction, 'left'/'right' = beams run in X direction.")]
        string directionEdge = "bottom",
        [Description("Layout rule type. Currently only 'fixed_distance' is supported.")]
        string layoutRule = "fixed_distance",
        [Description("Beam justification within the layout. 'center' places beams symmetrically.")]
        string justify = "center",
        [Description("Name of the beam family type to use (e.g., 'W10x12'). If not provided, the first available structural framing type will be used.")]
        string? beamTypeName = null,
        [Description("Elevation offset from the level in millimeters.")]
        double elevation = 0,
        [Description("Whether to create a 3D beam system.")]
        bool is3d = false)
    {
        return await wsClient.SendCommandAsync("create_structural_framing_system",
            new { levelName, xMin, xMax, yMin, yMax, spacing, directionEdge,
                  layoutRule, justify, beamTypeName, elevation, is3d });
    }

    [McpServerTool(Name = "create_surface_based_element")]
    [Description("Create one or more surface-based elements in Revit such as floors, ceilings, or roofs. Supports batch creation with detailed parameters including family type ID, boundary lines, thickness, and level information. All units are in millimeters (mm).")]
    public async Task<string> CreateSurfaceBasedElement(
        [Description("Array of surface-based element objects. Each object: name (string), category (string, optional, one of OST_Floors/OST_Ceilings/OST_Roofs), typeId (number, optional), boundary {outerLoop: [{p0:{x,y,z}, p1:{x,y,z}}]}, thickness (number), baseLevel (number), baseOffset (number).")]
        JsonElement data)
    {
        return await wsClient.SendCommandAsync("create_surface_based_element", new { data });
    }

    [McpServerTool(Name = "delete_element")]
    [Description("Delete one or more elements from the Revit model by their element IDs.")]
    public async Task<string> DeleteElement(
        [Description("The IDs of the elements to delete")]
        string[] elementIds)
    {
        return await wsClient.SendCommandAsync("delete_element", new { elementIds });
    }

    [McpServerTool(Name = "export_room_data")]
    [Description("Export all room data from the current Revit project. Returns detailed information about each room including name, number, level, area, volume, perimeter, department, and more. Useful for generating room schedules, space analysis, and facility management data.")]
    public async Task<string> ExportRoomData(
        [Description("Whether to include unplaced rooms (rooms not yet placed in the model). Defaults to false.")]
        bool includeUnplacedRooms = false,
        [Description("Whether to include rooms that are not fully enclosed. Defaults to false.")]
        bool includeNotEnclosedRooms = false)
    {
        return await wsClient.SendCommandAsync("export_room_data",
            new { includeUnplacedRooms, includeNotEnclosedRooms });
    }

    [McpServerTool(Name = "get_available_family_types")]
    [Description("Get available family types in the current Revit project. You can filter by category and family name, and limit the number of returned types.")]
    public async Task<string> GetAvailableFamilyTypes(
        [Description("List of Revit category names to filter by (e.g., 'OST_Walls', 'OST_Doors', 'OST_Furniture')")]
        string[]? categoryList = null,
        [Description("Filter family types by family name (partial match)")]
        string? familyNameFilter = null,
        [Description("Maximum number of family types to return")]
        int? limit = null)
    {
        return await wsClient.SendCommandAsync("get_available_family_types",
            new { categoryList = categoryList ?? Array.Empty<string>(),
                  familyNameFilter = familyNameFilter ?? "",
                  limit = limit ?? 100 });
    }

    [McpServerTool(Name = "get_current_view_elements")]
    [Description("Get elements from the current active view in Revit. You can filter by model categories (like Walls, Floors) or annotation categories (like Dimensions, Text). Use includeHidden to show/hide invisible elements and limit to control the number of returned elements.")]
    public async Task<string> GetCurrentViewElements(
        [Description("List of Revit model category names (e.g., 'OST_Walls', 'OST_Doors', 'OST_Floors')")]
        string[]? modelCategoryList = null,
        [Description("List of Revit annotation category names (e.g., 'OST_Dimensions', 'OST_WallTags', 'OST_TextNotes')")]
        string[]? annotationCategoryList = null,
        [Description("Whether to include hidden elements in the results")]
        bool includeHidden = false,
        [Description("Maximum number of elements to return")]
        int limit = 100)
    {
        return await wsClient.SendCommandAsync("get_current_view_elements",
            new { modelCategoryList = modelCategoryList ?? Array.Empty<string>(),
                  annotationCategoryList = annotationCategoryList ?? Array.Empty<string>(),
                  includeHidden,
                  limit });
    }

    [McpServerTool(Name = "get_current_view_info")]
    [Description("Get detailed information about the current active Revit view, including view type, name, scale, and other properties.")]
    public async Task<string> GetCurrentViewInfo()
    {
        return await wsClient.SendCommandAsync("get_current_view_info", new { });
    }

    [McpServerTool(Name = "get_parameters_from_elementid")]
    [Description("Get parameters for one or more Revit elements by element ID. Returns parameter name, value, storage type, and read-only status for each element. Elements not found in the model are reported in a notFound list.")]
    public async Task<string> GetParametersFromElementId(
        [Description("Array of Revit element IDs to query")]
        string[] elementIds,
        [Description("Optional list of parameter names to filter by. If omitted or empty, all parameters are returned.")]
        string[]? parameterNames = null)
    {
        return await wsClient.SendCommandAsync("get_parameters_from_elementid",
            new { elementIds, parameterNames = parameterNames ?? Array.Empty<string>() });
    }

    [McpServerTool(Name = "get_material_quantities")]
    [Description("Calculate material quantities and takeoffs from the current Revit project. Returns detailed information about each material including name, class, area, volume, and element counts. Useful for cost estimation, material ordering, and sustainability analysis.")]
    public async Task<string> GetMaterialQuantities(
        [Description("Optional list of Revit category names to filter by (e.g., ['OST_Walls', 'OST_Floors', 'OST_Roofs']). If not specified, all categories are included.")]
        string[]? categoryFilters = null,
        [Description("Whether to only analyze currently selected elements. Defaults to false (analyze entire project).")]
        bool selectedElementsOnly = false)
    {
        return await wsClient.SendCommandAsync("get_material_quantities",
            new { categoryFilters = (object?)categoryFilters, selectedElementsOnly });
    }

    [McpServerTool(Name = "get_selected_elements")]
    [Description("Get elements currently selected in Revit. You can limit the number of returned elements.")]
    public async Task<string> GetSelectedElements(
        [Description("Maximum number of elements to return")]
        int limit = 100)
    {
        return await wsClient.SendCommandAsync("get_selected_elements", new { limit });
    }

    [McpServerTool(Name = "operate_element")]
    [Description("Operate on Revit elements by performing actions such as select, selectionBox, setColor, setTransparency, delete, hide, etc.")]
    public async Task<string> OperateElement(
        [Description("Parameters for operating on Revit elements. Fields: elementIds (number[]) - array of Revit element IDs; action (string) - one of Select, SelectionBox, SetColor, SetTransparency, Delete, Hide, TempHide, Isolate, Unhide, ResetIsolate, Highlight; transparencyValue (number, default 50); colorValue (number[], default [255,0,0]).")]
        JsonElement data)
    {
        return await wsClient.SendCommandAsync("operate_element", new { data });
    }

    [McpServerTool(Name = "say_hello")]
    [Description("Display a greeting dialog in Revit. Useful for testing the connection between Claude and Revit.")]
    public async Task<string> SayHello(
        [Description("Optional custom message to display in the dialog. Defaults to 'Hello MCP!'")]
        string? message = null)
    {
        return await wsClient.SendCommandAsync("say_hello", new { message });
    }

    [McpServerTool(Name = "send_code_to_revit")]
    [Description("Send C# code to Revit for execution. The code will be inserted into a template with access to the Revit Document and parameters. Your code should be written to work within the Execute method of the template.")]
    public async Task<string> SendCodeToRevit(
        [Description("The C# code to execute in Revit. This code will be inserted into the Execute method of a template with access to Document and parameters.")]
        string code,
        [Description("Optional JSON array of execution parameters passed to your code, e.g. [\"value1\", 42]. Omit if not needed.")]
        string? parameters = null)
    {
        object? paramsValue = parameters != null
            ? JsonSerializer.Deserialize<JsonElement>(parameters)
            : (object?)Array.Empty<object>();
        return await wsClient.SendCommandAsync("send_code_to_revit",
            new { code, parameters = paramsValue });
    }

    [McpServerTool(Name = "tag_all_rooms")]
    [Description("Create tags for all rooms in the current active view. Tags will be placed at the center point of each room, displaying the room name and number.")]
    public async Task<string> TagAllRooms(
        [Description("Whether to use a leader line when creating the tags")]
        bool useLeader = false,
        [Description("The ID of the specific room tag family type to use. If not provided, the default room tag type will be used.")]
        string? tagTypeId = null,
        [Description("Optional array of specific room element IDs to tag. If not provided, all rooms in the current view will be tagged.")]
        int[]? roomIds = null)
    {
        return await wsClient.SendCommandAsync("tag_rooms", new { useLeader, tagTypeId, roomIds });
    }

    [McpServerTool(Name = "tag_all_walls")]
    [Description("Create tags for all walls in the current active view. Tags will be placed at the middle point of each wall.")]
    public async Task<string> TagAllWalls(
        [Description("Whether to use a leader line when creating the tags")]
        bool useLeader = false,
        [Description("The ID of the specific wall tag family type to use. If not provided, the default wall tag type will be used.")]
        string? tagTypeId = null)
    {
        return await wsClient.SendCommandAsync("tag_walls", new { useLeader, tagTypeId });
    }

    // ── Category / family / type queries ────────────────────────────────────

    [McpServerTool(Name = "get_category_by_keyword")]
    [Description("Search Revit categories by keyword. Returns matching categories with their IDs, names, and types.")]
    public async Task<string> GetCategoryByKeyword(
        [Description("Keyword to search for in category names (case-insensitive partial match)")]
        string keyword)
    {
        return await wsClient.SendCommandAsync("get_category_by_keyword", new { keyword });
    }

    [McpServerTool(Name = "get_model_categories")]
    [Description("Get all categories defined in the Revit model, ordered by name. Returns category IDs, names, and category types.")]
    public async Task<string> GetModelCategories()
    {
        return await wsClient.SendCommandAsync("get_model_categories", new { });
    }

    [McpServerTool(Name = "get_all_used_families_in_model")]
    [Description("Get all loaded (non-in-place) families in the Revit model. Returns family IDs and names ordered alphabetically.")]
    public async Task<string> GetAllUsedFamiliesInModel()
    {
        return await wsClient.SendCommandAsync("get_all_used_families_in_model", new { });
    }

    [McpServerTool(Name = "get_all_used_types_of_a_family")]
    [Description("Get all types (FamilySymbols) belonging to a specific family. Returns type IDs and names.")]
    public async Task<string> GetAllUsedTypesOfAFamily(
        [Description("The exact name of the family to query")]
        string familyName)
    {
        return await wsClient.SendCommandAsync("get_all_used_types_of_a_family", new { familyName });
    }

    [McpServerTool(Name = "get_all_elements_of_specific_families")]
    [Description("Get all placed instances (FamilyInstances) belonging to one or more named families.")]
    public async Task<string> GetAllElementsOfSpecificFamilies(
        [Description("List of family names to search for (case-insensitive)")]
        string[] familyNames)
    {
        return await wsClient.SendCommandAsync("get_all_elements_of_specific_families", new { familyNames });
    }

    [McpServerTool(Name = "get_categories_from_elementids")]
    [Description("Given a list of element IDs, return the Revit categories they belong to, grouped by category name.")]
    public async Task<string> GetCategoriesFromElementIds(
        [Description("Array of element IDs as strings")]
        string[] elementIds)
    {
        return await wsClient.SendCommandAsync("get_categories_from_elementids", new { elementIds });
    }

    [McpServerTool(Name = "get_element_types_for_element_ids")]
    [Description("Given a list of element IDs, return the element type (TypeId and type name) for each element.")]
    public async Task<string> GetElementTypesForElementIds(
        [Description("Array of element IDs as strings")]
        string[] elementIds)
    {
        return await wsClient.SendCommandAsync("get_element_types_for_element_ids", new { elementIds });
    }

    [McpServerTool(Name = "get_object_classes_from_elementids")]
    [Description("Given a list of element IDs, return the .NET class name (FullName) of each element's runtime type.")]
    public async Task<string> GetObjectClassesFromElementIds(
        [Description("Array of element IDs as strings")]
        string[] elementIds)
    {
        return await wsClient.SendCommandAsync("get_object_classes_from_elementids", new { elementIds });
    }

    // ── Spatial / element queries ────────────────────────────────────────────

    [McpServerTool(Name = "get_elements_by_category")]
    [Description("Get all element IDs belonging to a specific Revit category. Use get_model_categories first to find the category ID.")]
    public async Task<string> GetElementsByCategory(
        [Description("The Revit BuiltInCategory integer value (e.g. -2000011 for OST_Walls). Use get_model_categories to find IDs.")]
        int categoryId)
    {
        return await wsClient.SendCommandAsync("get_elements_by_category", new { categoryId });
    }

    [McpServerTool(Name = "get_elements_on_level")]
    [Description("Get all elements associated with a named Revit level. Checks LEVEL_PARAM, WALL_BASE_CONSTRAINT, FAMILY_LEVEL_PARAM, and the 'Level' parameter.")]
    public async Task<string> GetElementsOnLevel(
        [Description("The exact name of the level (e.g. 'Level 1')")]
        string levelName,
        [Description("Optional list of category names to filter results (e.g. ['Walls', 'Floors'])")]
        string[]? categories = null)
    {
        return await wsClient.SendCommandAsync("get_elements_on_level",
            new { levelName, categories = categories ?? Array.Empty<string>() });
    }

    [McpServerTool(Name = "get_all_elements_shown_in_view")]
    [Description("Get all element IDs visible (not hidden) in a specific Revit view.")]
    public async Task<string> GetAllElementsShownInView(
        [Description("The element ID of the view to query")]
        long viewId)
    {
        return await wsClient.SendCommandAsync("get_all_elements_shown_in_view", new { viewId });
    }

    [McpServerTool(Name = "get_location_for_element_ids")]
    [Description("Get the location of Revit elements. Returns a point (x,y,z) for point-based elements or start/end points for curve-based elements. Coordinates are in Revit internal units (feet).")]
    public async Task<string> GetLocationForElementIds(
        [Description("Array of element IDs as strings")]
        string[] elementIds)
    {
        return await wsClient.SendCommandAsync("get_location_for_element_ids", new { elementIds });
    }

    [McpServerTool(Name = "get_boundingboxes_for_element_ids")]
    [Description("Get the axis-aligned bounding box (min and max corners) for each element. Coordinates are in Revit internal units (feet).")]
    public async Task<string> GetBoundingBoxesForElementIds(
        [Description("Array of element IDs as strings")]
        string[] elementIds)
    {
        return await wsClient.SendCommandAsync("get_boundingboxes_for_element_ids", new { elementIds });
    }

    [McpServerTool(Name = "get_parameter_value_for_element_ids")]
    [Description("Get the value of a single named parameter across multiple elements. More efficient than get_parameters_from_elementid when you only need one parameter.")]
    public async Task<string> GetParameterValueForElementIds(
        [Description("The name of the parameter to read")]
        string parameterName,
        [Description("Array of element IDs as strings")]
        string[] elementIds)
    {
        return await wsClient.SendCommandAsync("get_parameter_value_for_element_ids", new { parameterName, elementIds });
    }

    [McpServerTool(Name = "get_all_warnings_in_model")]
    [Description("Get all warnings currently in the Revit model, including description, severity, and the element IDs involved.")]
    public async Task<string> GetAllWarningsInModel()
    {
        return await wsClient.SendCommandAsync("get_all_warnings_in_model", new { });
    }

    // ── Workset queries ──────────────────────────────────────────────────────

    [McpServerTool(Name = "get_worksets_from_elementids")]
    [Description("Get workset assignment for each element by ID. Returns workset name and ID. Only works on workshared models.")]
    public async Task<string> GetWorksetsFromElementIds(
        [Description("Array of Revit element IDs as strings")]
        string[] elementIds)
    {
        return await wsClient.SendCommandAsync("get_worksets_from_elementids", new { elementIds });
    }

    [McpServerTool(Name = "get_all_workset_information")]
    [Description("Get all user worksets in the current Revit model, including name, owner, and open/closed state. Only works on workshared models.")]
    public async Task<string> GetAllWorksetInformation()
    {
        return await wsClient.SendCommandAsync("get_all_workset_information", new { });
    }

    // ── Modify / write tools ─────────────────────────────────────────────────

    [McpServerTool(Name = "set_parameter_value_for_elements")]
    [Description("Set a named parameter to a given value for multiple Revit elements. Handles String, Double, Integer, and ElementId storage types.")]
    public async Task<string> SetParameterValueForElements(
        [Description("Name of the parameter to set")]
        string parameterName,
        [Description("Array of element IDs to update")]
        string[] elementIds,
        [Description("Value to set (will be coerced to the parameter's storage type)")]
        string value)
    {
        return await wsClient.SendCommandAsync("set_parameter_value_for_elements", new { parameterName, elementIds, value });
    }

    [McpServerTool(Name = "set_movement_for_elements")]
    [Description("Move Revit elements by a translation vector. Values are in Revit internal units (feet). Divide mm by 304.8 to convert.")]
    public async Task<string> SetMovementForElements(
        [Description("Array of element IDs to move")]
        string[] elementIds,
        [Description("Translation in X direction (feet)")]
        double dx,
        [Description("Translation in Y direction (feet)")]
        double dy,
        [Description("Translation in Z direction (feet)")]
        double dz)
    {
        return await wsClient.SendCommandAsync("set_movement_for_elements", new { elementIds, dx, dy, dz });
    }

    [McpServerTool(Name = "set_rotation_for_elements")]
    [Description("Rotate Revit elements around a vertical (Z) axis by a given angle in radians.")]
    public async Task<string> SetRotationForElements(
        [Description("Array of element IDs to rotate")]
        string[] elementIds,
        [Description("Rotation angle in radians")]
        double radians,
        [Description("X coordinate of the rotation axis start point (feet, default 0)")]
        double axisStartX = 0,
        [Description("Y coordinate of the rotation axis start point (feet, default 0)")]
        double axisStartY = 0,
        [Description("Z coordinate of the rotation axis start point (feet, default 0)")]
        double axisStartZ = 0)
    {
        return await wsClient.SendCommandAsync("set_rotation_for_elements",
            new { elementIds, radians, axisStartX, axisStartY, axisStartZ });
    }

    [McpServerTool(Name = "set_user_selection_in_revit")]
    [Description("Set the active user selection in Revit to the specified element IDs. This highlights elements in the Revit UI.")]
    public async Task<string> SetUserSelectionInRevit(
        [Description("Array of element IDs to select")]
        string[] elementIds)
    {
        return await wsClient.SendCommandAsync("set_user_selection_in_revit", new { elementIds });
    }

    [McpServerTool(Name = "set_isolated_elements_in_view")]
    [Description("Temporarily isolate specific elements in a Revit view, hiding all others.")]
    public async Task<string> SetIsolatedElementsInView(
        [Description("ID of the view to apply isolation in")]
        string viewId,
        [Description("Array of element IDs to isolate")]
        string[] elementIds)
    {
        return await wsClient.SendCommandAsync("set_isolated_elements_in_view", new { viewId, elementIds });
    }

    [McpServerTool(Name = "set_category_visibility_in_view")]
    [Description("Show or hide specific categories in a Revit view by category ID. Use get_model_categories to find category IDs.")]
    public async Task<string> SetCategoryVisibilityInView(
        [Description("ID of the view to modify")]
        string viewId,
        [Description("Array of built-in category integer IDs to show or hide")]
        long[] categoryIds,
        [Description("True to show the categories, false to hide them")]
        bool visible)
    {
        return await wsClient.SendCommandAsync("set_category_visibility_in_view", new { viewId, categoryIds, visible });
    }

    [McpServerTool(Name = "set_isolate_categories_in_view")]
    [Description("Hide all categories in a view except the ones specified. All other categories will be hidden.")]
    public async Task<string> SetIsolateCategoriesInView(
        [Description("ID of the view to modify")]
        string viewId,
        [Description("Array of built-in category integer IDs to keep visible")]
        long[] categoryIds)
    {
        return await wsClient.SendCommandAsync("set_isolate_categories_in_view", new { viewId, categoryIds });
    }

    [McpServerTool(Name = "set_reset_category_visibility_in_view")]
    [Description("Reset all category visibility overrides in a view, making every category visible again.")]
    public async Task<string> SetResetCategoryVisibilityInView(
        [Description("ID of the view to reset")]
        string viewId)
    {
        return await wsClient.SendCommandAsync("set_reset_category_visibility_in_view", new { viewId });
    }

    [McpServerTool(Name = "set_graphic_overrides_for_elements_in_view")]
    [Description("Apply a colour graphic override to elements in a Revit view. Overrides both projection line and surface foreground pattern colour.")]
    public async Task<string> SetGraphicOverridesForElementsInView(
        [Description("ID of the view to apply overrides in")]
        string viewId,
        [Description("Array of element IDs to override")]
        string[] elementIds,
        [Description("RGB colour as [R, G, B] integers 0-255. Defaults to red [255, 0, 0] if omitted.")]
        int[]? colorRgb = null)
    {
        return await wsClient.SendCommandAsync("set_graphic_overrides_for_elements_in_view",
            new { viewId, elementIds, colorRgb });
    }

    // ── Local database tools (no Revit call) ────────────────────────────────

    [McpServerTool(Name = "store_project_data")]
    [Description("Store or update Revit project metadata in the local database. This captures project information with a timestamp for later retrieval.")]
    public Task<string> StoreProjectData(
        [Description("The name of the Revit project")] string project_name,
        [Description("File path to the project")] string? project_path = null,
        [Description("Project number or identifier")] string? project_number = null,
        [Description("Project address or location")] string? project_address = null,
        [Description("Client name")] string? client_name = null,
        [Description("Project status (e.g., Active, Completed, On Hold)")] string? project_status = null,
        [Description("Project author or creator")] string? author = null,
        [Description("Additional project metadata as a JSON object string")] string? metadata = null)
    {
        try
        {
            long projectId = db.StoreProject(project_name, project_path, project_number,
                project_address, client_name, project_status, author, metadata);
            var project = db.GetProjectById(projectId);
            return Task.FromResult(Serialize(new
            {
                success = true,
                message = "Project data stored successfully",
                project_id = projectId,
                project
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Serialize(new { success = false, error = ex.Message }));
        }
    }

    [McpServerTool(Name = "store_room_data")]
    [Description("Store or update room metadata for a specific Revit project in the local database. Rooms are linked to a project by project name. The project must exist before storing room data.")]
    public Task<string> StoreRoomData(
        [Description("The name of the Revit project this room belongs to")] string project_name,
        [Description("Array of room objects to store. Each room: room_id (string, required), room_name (string, optional), room_number (string, optional), department (string, optional), level (string, optional), area (number, optional), perimeter (number, optional), occupancy (string, optional), comments (string, optional), metadata (object, optional).")]
        JsonElement rooms)
    {
        try
        {
            var project = db.GetProjectByName(project_name);
            if (project == null)
            {
                return Task.FromResult(Serialize(new
                {
                    success = false,
                    error = $"Project \"{project_name}\" not found. Please store project data first using store_project_data tool."
                }));
            }

            var roomList = ParseRooms(rooms);
            long projectId = (long)project["id"]!;
            int count = db.StoreRoomsBatch(projectId, roomList);
            var storedRooms = db.GetRoomsByProjectId(projectId);

            return Task.FromResult(Serialize(new
            {
                success = true,
                message = $"Stored {count} room(s) successfully",
                project_id = projectId,
                project_name,
                total_rooms = storedRooms.Count,
                rooms_stored = count
            }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Serialize(new { success = false, error = ex.Message }));
        }
    }

    [McpServerTool(Name = "query_stored_data")]
    [Description("Query stored Revit project and room data from the local database. Supports various query types: get all projects, get project by ID/name, get rooms by project, get all rooms, or get database statistics.")]
    public Task<string> QueryStoredData(
        [Description("Type of query to perform. One of: all_projects, project_by_id, project_by_name, rooms_by_project_id, rooms_by_project_name, all_rooms, stats.")]
        string query_type,
        [Description("Project ID (required for 'project_by_id' and 'rooms_by_project_id')")]
        long? project_id = null,
        [Description("Project name (required for 'project_by_name' and 'rooms_by_project_name')")]
        string? project_name = null)
    {
        try
        {
            object? result = query_type switch
            {
                "all_projects" => db.GetAllProjects(),
                "project_by_id" => project_id.HasValue
                    ? db.GetProjectById(project_id.Value)
                        ?? throw new InvalidOperationException($"Project with ID {project_id} not found.")
                    : throw new ArgumentException("project_id is required for this query type"),
                "project_by_name" => !string.IsNullOrEmpty(project_name)
                    ? db.GetProjectByName(project_name)
                        ?? throw new InvalidOperationException($"Project \"{project_name}\" not found.")
                    : throw new ArgumentException("project_name is required for this query type"),
                "rooms_by_project_id" => project_id.HasValue
                    ? db.GetRoomsByProjectId(project_id.Value)
                    : throw new ArgumentException("project_id is required for this query type"),
                "rooms_by_project_name" => !string.IsNullOrEmpty(project_name)
                    ? db.GetRoomsByProjectId(
                        (long)(db.GetProjectByName(project_name)
                            ?? throw new InvalidOperationException($"Project \"{project_name}\" not found."))["id"]!)
                    : throw new ArgumentException("project_name is required for this query type"),
                "all_rooms" => db.GetAllRoomsWithProject(),
                "stats" => db.GetStats(),
                _ => throw new ArgumentException($"Unknown query type: {query_type}")
            };

            return Task.FromResult(Serialize(new { success = true, query_type, data = result }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Serialize(new { success = false, error = ex.Message }));
        }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static List<RoomData> ParseRooms(JsonElement roomsElement)
    {
        var list = new List<RoomData>();
        foreach (var item in roomsElement.EnumerateArray())
        {
            list.Add(new RoomData
            {
                RoomId = item.GetProperty("room_id").GetString()!,
                RoomName = item.TryGetProperty("room_name", out var v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null,
                RoomNumber = item.TryGetProperty("room_number", out v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null,
                Department = item.TryGetProperty("department", out v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null,
                Level = item.TryGetProperty("level", out v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null,
                Area = item.TryGetProperty("area", out v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null,
                Perimeter = item.TryGetProperty("perimeter", out v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null,
                Occupancy = item.TryGetProperty("occupancy", out v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null,
                Comments = item.TryGetProperty("comments", out v) && v.ValueKind != JsonValueKind.Null ? v.GetString() : null,
                Metadata = item.TryGetProperty("metadata", out v) && v.ValueKind == JsonValueKind.Object
                    ? JsonSerializer.Deserialize<Dictionary<string, object?>>(v.GetRawText())
                    : null
            });
        }
        return list;
    }
}
