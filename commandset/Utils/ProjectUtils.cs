using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Commands;
using RevitMCPCommandSet.Models.Common;
using System.IO;
using System.Reflection;

namespace RevitMCPCommandSet.Utils
{
    public static class ProjectUtils
    {
        /// <summary>
        /// General method for creating family instances
        /// </summary>
        /// <param name="doc">Current document</param>
        /// <param name="familySymbol">Family type</param>
        /// <param name="locationPoint">Location point</param>
        /// <param name="locationLine">Reference line</param>
        /// <param name="baseLevel">Base level</param>
        /// <param name="topLevel">Top level (used for TwoLevelsBased)</param>
        /// <param name="baseOffset">Base offset (ft)</param>
        /// <param name="topOffset">Top offset (ft)</param>
        /// <param name="faceDirection">Reference direction</param>
        /// <param name="handDirection">Reference direction</param>
        /// <param name="view">View</param>
        /// <returns>Created family instance, or null on failure</returns>
        public static FamilyInstance CreateInstance(
            this Document doc,
            FamilySymbol familySymbol,
            XYZ locationPoint = null,
            Line locationLine = null,
            Level baseLevel = null,
            Level topLevel = null,
            double baseOffset = -1,
            double topOffset = -1,
            XYZ faceDirection = null,
            XYZ handDirection = null,
            View view = null,
            Element explicitHost = null,
            bool snapToHostCenter = true)
        {
            // Basic parameter check
            if (doc == null)
                throw new ArgumentNullException($"Required parameter {typeof(Document)} {nameof(doc)} is missing!");
            if (familySymbol == null)
                throw new ArgumentNullException($"Required parameter {typeof(FamilySymbol)} {nameof(familySymbol)} is missing!");

            // Activate family model
            if (!familySymbol.IsActive)
                familySymbol.Activate();

            FamilyInstance instance = null;

            // Select creation method based on the family placement type
            switch (familySymbol.Family.FamilyPlacementType)
            {
                // Level-based family (e.g., metric generic model)
                case FamilyPlacementType.OneLevelBased:
                    if (locationPoint == null)
                        throw new ArgumentNullException($"Required parameter {typeof(XYZ)} {nameof(locationPoint)} is missing!");
                    // With level info
                    if (baseLevel != null)
                    {
                        instance = doc.Create.NewFamilyInstance(
                            locationPoint,                  // Physical location where the instance will be placed
                            familySymbol,                   // FamilySymbol object representing the type to insert
                            baseLevel,                      // Level object used as the base level for the object
                            StructuralType.NonStructural);  // Type of structural member if it is structural
                    }
                    // Without level info
                    else
                    {
                        instance = doc.Create.NewFamilyInstance(
                            locationPoint,                  // Physical location where the instance will be placed
                            familySymbol,                   // FamilySymbol object representing the type to insert
                            StructuralType.NonStructural);  // Type of structural member if it is structural
                    }
                    break;

                // Level-and-host-based family (e.g., doors, windows)
                case FamilyPlacementType.OneLevelBasedHosted:
                    if (locationPoint == null)
                        throw new ArgumentNullException($"Required parameter {typeof(XYZ)} {nameof(locationPoint)} is missing!");

                    Element host = explicitHost;
                    XYZ placementPoint = locationPoint;

                    // If explicit host provided and it's a wall, snap to its centerline
                    if (host != null && snapToHostCenter && host is Wall explicitWall)
                    {
                        LocationCurve eLoc = explicitWall.Location as LocationCurve;
                        if (eLoc != null)
                        {
                            IntersectionResult eIr = eLoc.Curve.Project(locationPoint);
                            if (eIr != null)
                                placementPoint = new XYZ(eIr.XYZPoint.X, eIr.XYZPoint.Y, locationPoint.Z);
                        }
                    }

                    // Auto-detect host wall if not explicitly provided
                    if (host == null)
                    {
                        // Try geometric wall-centerline proximity first
                        var wallResult = doc.GetNearestWallByLocationLine(locationPoint, baseLevel);
                        if (wallResult.HasValue)
                        {
                            host = wallResult.Value.wall;
                            if (snapToHostCenter)
                                placementPoint = wallResult.Value.projectedPoint;
                        }
                        else
                        {
                            // Fall back to original ray-casting method
                            host = doc.GetNearestHostElement(locationPoint, familySymbol);
                        }
                    }

                    if (host == null)
                        throw new ArgumentNullException($"Cannot find a valid host element!");

                    if (baseLevel != null)
                    {
                        instance = doc.Create.NewFamilyInstance(
                            placementPoint,
                            familySymbol,
                            host,
                            baseLevel,
                            StructuralType.NonStructural);
                    }
                    else
                    {
                        instance = doc.Create.NewFamilyInstance(
                            placementPoint,
                            familySymbol,
                            host,
                            StructuralType.NonStructural);
                    }

                    // Set sill height for windows (baseOffset maps to sill height for hosted elements)
                    if (instance != null && baseOffset != -1)
                    {
                        Parameter sillParam = instance.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
                        if (sillParam != null && !sillParam.IsReadOnly)
                        {
                            sillParam.Set(baseOffset);
                        }
                    }
                    break;

                // Two-levels-based family (e.g., columns)
                case FamilyPlacementType.TwoLevelsBased:
                    if (locationPoint == null)
                        throw new ArgumentNullException($"Required parameter {typeof(XYZ)} {nameof(locationPoint)} is missing!");
                    if (baseLevel == null)
                        throw new ArgumentNullException($"Required parameter {typeof(Level)} {nameof(baseLevel)} is missing!");
                    // Determine if it's a structural or architectural column
                    StructuralType structuralType = StructuralType.NonStructural;
                    if (familySymbol.Category.Id.GetIntValue() == (int)BuiltInCategory.OST_StructuralColumns)
                        structuralType = StructuralType.Column;
                    instance = doc.Create.NewFamilyInstance(
                        locationPoint,              // Physical location where the instance will be placed
                        familySymbol,               // FamilySymbol object representing the type to insert
                        baseLevel,                  // Level object used as the base level for the object
                        structuralType);            // Type of structural member if it is structural
                    // Set base level, top level, base offset, and top offset
                    if (instance != null)
                    {
                        // Set column base level and top level
                        if (baseLevel != null)
                        {
                            Parameter baseLevelParam = instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);
                            if (baseLevelParam != null)
                                baseLevelParam.Set(baseLevel.Id);
                        }
                        if (topLevel != null)
                        {
                            Parameter topLevelParam = instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                            if (topLevelParam != null)
                                topLevelParam.Set(topLevel.Id);
                        }
                        // Get base offset parameter
                        if (baseOffset != -1)
                        {
                            Parameter baseOffsetParam = instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
                            if (baseOffsetParam != null && baseOffsetParam.StorageType == StorageType.Double)
                            {
                                // Convert mm to Revit internal units
                                double baseOffsetInternal = baseOffset;
                                baseOffsetParam.Set(baseOffsetInternal);
                            }
                        }
                        // Get top offset parameter
                        if (topOffset != -1)
                        {
                            Parameter topOffsetParam = instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
                            if (topOffsetParam != null && topOffsetParam.StorageType == StorageType.Double)
                            {
                                // Convert mm to Revit internal units
                                double topOffsetInternal = topOffset;
                                topOffsetParam.Set(topOffsetInternal);
                            }
                        }
                    }
                    break;

                // View-based family (e.g., detail annotations)
                case FamilyPlacementType.ViewBased:
                    if (locationPoint == null)
                        throw new ArgumentNullException($"Required parameter {typeof(XYZ)} {nameof(locationPoint)} is missing!");
                    instance = doc.Create.NewFamilyInstance(
                        locationPoint,  // Origin of the family instance; projected onto plan view if created in ViewPlan
                        familySymbol,   // FamilySymbol object representing the type to insert
                        view);          // 2D view to place the family instance in
                    break;

                // Work-plane-based family (e.g., face-based generic model, including face-based, wall-based)
                case FamilyPlacementType.WorkPlaneBased:
                    if (locationPoint == null)
                        throw new ArgumentNullException($"Required parameter {typeof(XYZ)} {nameof(locationPoint)} is missing!");
                    // Get nearest host face
                    Reference hostFace = doc.GetNearestFaceReference(locationPoint, 1000 / 304.8);
                    if (hostFace == null)
                        throw new ArgumentNullException($"Cannot find a valid host element!");
                    if (faceDirection == null || faceDirection == XYZ.Zero)
                    {
                        var result = doc.GenerateDefaultOrientation(hostFace);
                        faceDirection = result.FacingOrientation;
                    }
                    // Create family instance on face using point and direction
                    instance = doc.Create.NewFamilyInstance(
                        hostFace,               // Reference to the face
                        locationPoint,          // Point on face where instance will be placed
                        faceDirection,          // Vector defining the family instance direction; cannot be parallel to face normal
                        familySymbol);          // FamilySymbol object; must represent a family with FamilyPlacementType WorkPlaneBased
                    break;

                // Line-based work-plane family (e.g., line-based generic model)
                case FamilyPlacementType.CurveBased:
                    if (locationLine == null)
                        throw new ArgumentNullException($"Required parameter {typeof(Line)} {nameof(locationLine)} is missing!");

                    // Get nearest host face (no tolerance allowed)
                    Reference lineHostFace = doc.GetNearestFaceReference(locationLine.Evaluate(0.5, true), 1e-5);
                    if (lineHostFace != null)
                    {
                        instance = doc.Create.NewFamilyInstance(
                            lineHostFace,   // Reference to the face
                            locationLine,   // Curve the family instance is based on
                            familySymbol);  // FamilySymbol; must have FamilyPlacementType WorkPlaneBased or CurveBased
                    }
                    else
                    {
                        instance = doc.Create.NewFamilyInstance(
                            locationLine,                   // Curve the family instance is based on
                            familySymbol,                   // FamilySymbol; must have FamilyPlacementType WorkPlaneBased or CurveBased
                            baseLevel,                      // Level object used as the base level for the object
                            StructuralType.NonStructural);  // Type of structural member if it is structural
                    }
                    if (instance != null)
                    {
                        // Get base offset parameter
                        if (baseOffset != -1)
                        {
                            Parameter baseOffsetParam = instance.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                            if (baseOffsetParam != null && baseOffsetParam.StorageType == StorageType.Double)
                            {
                                // Convert mm to Revit internal units
                                double baseOffsetInternal = baseOffset;
                                baseOffsetParam.Set(baseOffsetInternal);
                            }
                        }
                    }
                    break;

                // Line-based view family (e.g., detail components)
                case FamilyPlacementType.CurveBasedDetail:
                    if (locationLine == null)
                        throw new ArgumentNullException($"Required parameter {typeof(Line)} {nameof(locationLine)} is missing!");
                    if (view == null)
                        throw new ArgumentNullException($"Required parameter {typeof(View)} {nameof(view)} is missing!");
                    instance = doc.Create.NewFamilyInstance(
                        locationLine,   // Line location of the family instance; must lie in the view plane
                        familySymbol,   // FamilySymbol object representing the type to insert
                        view);          // 2D view to place the family instance in
                    break;

                // Structural curve-driven family (e.g., beams, braces, or slanted columns)
                case FamilyPlacementType.CurveDrivenStructural:
                    if (locationLine == null)
                        throw new ArgumentNullException($"Required parameter {typeof(Line)} {nameof(locationLine)} is missing!");
                    if (baseLevel == null)
                        throw new ArgumentNullException($"Required parameter {typeof(Level)} {nameof(baseLevel)} is missing!");
                    instance = doc.Create.NewFamilyInstance(
                        locationLine,                   // Curve the family instance is based on
                        familySymbol,                   // FamilySymbol; must have FamilyPlacementType WorkPlaneBased or CurveBased
                        baseLevel,                      // Level object used as the base level for the object
                        StructuralType.Beam);           // Type of structural member if it is structural
                    break;

                // Adaptive family (e.g., adaptive generic model, curtain panels)
                case FamilyPlacementType.Adaptive:
                    throw new NotImplementedException("FamilyPlacementType.Adaptive creation method is not implemented!");

                default:
                    break;
            }
            return instance;
        }

        /// <summary>
        /// Generate default facing and hand orientations (default: long edge is HandOrientation, short edge is FacingOrientation)
        /// </summary>
        /// <param name="hostFace"></param>
        /// <returns></returns>
        public static (XYZ FacingOrientation, XYZ HandOrientation) GenerateDefaultOrientation(this Document doc, Reference hostFace)
        {
            var facingOrientation = new XYZ();  // Facing direction: orientation of the family's Y-axis after loading
            var handOrientation = new XYZ();    // Hand direction: orientation of the family's X-axis after loading

            // Step1 Get face object from Reference
            Face face = doc.GetElement(hostFace.ElementId).GetGeometryObjectFromReference(hostFace) as Face;

            // Step2 Get face profile
            List<Curve> profile = null;
            // Profile collection; each sub-list represents a complete closed profile; the first is usually the outer profile
            List<List<Curve>> profiles = new List<List<Curve>>();
            // Get all profile loops (outer profile and possible inner holes)
            EdgeArrayArray edgeLoops = face.EdgeLoops;
            // Iterate over each profile loop
            foreach (EdgeArray loop in edgeLoops)
            {
                List<Curve> currentLoop = new List<Curve>();
                // Get each edge in the loop
                foreach (Edge edge in loop)
                {
                    Curve curve = edge.AsCurve();
                    currentLoop.Add(curve);
                }
                // If current loop has edges, add to results
                if (currentLoop.Count > 0)
                {
                    profiles.Add(currentLoop);
                }
            }
            // First profile is usually the outer profile
            if (profiles != null && profiles.Any())
                profile = profiles.FirstOrDefault();

            // Step3 Get face normal vector
            XYZ faceNormal = null;
            // If it's a planar face, the normal property can be accessed directly
            if (face is PlanarFace planarFace)
                faceNormal = planarFace.FaceNormal;

            // Step4 Get the two compliant (right-hand rule) primary directions of the face
            var result = face.GetMainDirections();
            var primaryDirection = result.PrimaryDirection;
            var secondaryDirection = result.SecondaryDirection;

            // Default: long-edge direction is HandOrientation, short-edge direction is FacingOrientation
            facingOrientation = primaryDirection;
            handOrientation = secondaryDirection;

            // Check compliance with the right-hand rule (thumb: HandOrientation, index: FacingOrientation, middle: FaceNormal)
            if (!facingOrientation.IsRightHandRuleCompliant(handOrientation, faceNormal))
            {
                var newHandOrientation = facingOrientation.GenerateIndexFinger(faceNormal);
                if (newHandOrientation != null)
                {
                    handOrientation = newHandOrientation;
                }
            }

            return (facingOrientation, handOrientation);
        }

        /// <summary>
        /// Get the nearest face Reference to a point
        /// </summary>
        /// <param name="doc">Current document</param>
        /// <param name="location">Target point location</param>
        /// <param name="radius">Search radius (internal units)</param>
        /// <returns>Reference to the nearest face, or null if not found</returns>
        public static Reference GetNearestFaceReference(this Document doc, XYZ location, double radius = 1000 / 304.8)
        {
            try
            {
                // Error tolerance handling
                location = new XYZ(location.X, location.Y, location.Z + 0.1 / 304.8);

                // Create or get 3D view
                View3D view3D = null;
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(View3D));

                foreach (View3D v in collector)
                {
                    if (!v.IsTemplate)
                    {
                        view3D = v;
                        break;
                    }
                }

                if (view3D == null)
                {
                    using (Transaction trans = new Transaction(doc, "Create 3D View"))
                    {
                        trans.Start();
                        ViewFamilyType vft = new FilteredElementCollector(doc)
                            .OfClass(typeof(ViewFamilyType))
                            .Cast<ViewFamilyType>()
                            .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

                        if (vft != null)
                        {
                            view3D = View3D.CreateIsometric(doc, vft.Id);
                        }
                        trans.Commit();
                    }
                }

                if (view3D == null)
                {
                    TaskDialog.Show("Error", "Cannot create or obtain 3D view");
                    return null;
                }

                // Set rays in 6 directions
                XYZ[] directions = new XYZ[]
                {
                  XYZ.BasisX,    // +X direction
                  -XYZ.BasisX,   // -X direction
                  XYZ.BasisY,    // +Y direction
                  -XYZ.BasisY,   // -Y direction
                  XYZ.BasisZ,    // +Z direction
                  -XYZ.BasisZ    // -Z direction
                };

                // Create filters
                ElementClassFilter wallFilter = new ElementClassFilter(typeof(Wall));
                ElementClassFilter floorFilter = new ElementClassFilter(typeof(Floor));
                ElementClassFilter ceilingFilter = new ElementClassFilter(typeof(Ceiling));
                ElementClassFilter instanceFilter = new ElementClassFilter(typeof(FamilyInstance));

                // Combine filters
                LogicalOrFilter categoryFilter = new LogicalOrFilter(
                    new ElementFilter[] { wallFilter, floorFilter, ceilingFilter, instanceFilter });


                // 1. Simplest: filter for all instantiated elements
                //ElementFilter filter = new ElementIsElementTypeFilter(true);

                // Create ray intersector
                ReferenceIntersector refIntersector = new ReferenceIntersector(categoryFilter,
                    FindReferenceTarget.Face, view3D);
                refIntersector.FindReferencesInRevitLinks = true; // Set true to search for faces in linked files

                double minDistance = double.MaxValue;
                Reference nearestFace = null;

                foreach (XYZ direction in directions)
                {
                    // Cast ray from current position
                    IList<ReferenceWithContext> references = refIntersector.Find(location, direction);

                    foreach (ReferenceWithContext rwc in references)
                    {
                        double distance = rwc.Proximity; // Distance to the face

                        // If within search range and closer
                        if (distance <= radius && distance < minDistance)
                        {
                            minDistance = distance;
                            nearestFace = rwc.GetReference();
                        }
                    }
                }

                return nearestFace;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error getting nearest face: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get the nearest element that can serve as a host
        /// </summary>
        /// <param name="doc">Current document</param>
        /// <param name="location">Target point location</param>
        /// <param name="familySymbol">Family type, used to determine host type</param>
        /// <param name="radius">Search radius (internal units)</param>
        /// <returns>Nearest host element, or null if not found</returns>
        public static Element GetNearestHostElement(this Document doc, XYZ location, FamilySymbol familySymbol, double radius = 5.0)
        {
            try
            {
                // Basic parameter check
                if (doc == null || location == null || familySymbol == null)
                    return null;

                // Get family hosting behavior parameter
                Parameter hostParam = familySymbol.Family.get_Parameter(BuiltInParameter.FAMILY_HOSTING_BEHAVIOR);
                int hostingBehavior = hostParam?.AsInteger() ?? 0;

                // Create or get 3D view
                View3D view3D = null;
                FilteredElementCollector viewCollector = new FilteredElementCollector(doc)
                    .OfClass(typeof(View3D));
                foreach (View3D v in viewCollector)
                {
                    if (!v.IsTemplate)
                    {
                        view3D = v;
                        break;
                    }
                }

                if (view3D == null)
                {
                    using (Transaction trans = new Transaction(doc, "Create 3D View"))
                    {
                        trans.Start();
                        ViewFamilyType vft = new FilteredElementCollector(doc)
                            .OfClass(typeof(ViewFamilyType))
                            .Cast<ViewFamilyType>()
                            .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

                        if (vft != null)
                        {
                            view3D = View3D.CreateIsometric(doc, vft.Id);
                        }
                        trans.Commit();
                    }
                }

                if (view3D == null)
                {
                    TaskDialog.Show("Error", "Cannot create or obtain 3D view");
                    return null;
                }

                // Create type filter based on hosting behavior
                ElementFilter classFilter;
                switch (hostingBehavior)
                {
                    case 1: // Wall based
                        classFilter = new ElementClassFilter(typeof(Wall));
                        break;
                    case 2: // Floor based
                        classFilter = new ElementClassFilter(typeof(Floor));
                        break;
                    case 3: // Ceiling based
                        classFilter = new ElementClassFilter(typeof(Ceiling));
                        break;
                    case 4: // Roof based
                        classFilter = new ElementClassFilter(typeof(RoofBase));
                        break;
                    default:
                        return null; // Unsupported host type
                }

                // Set rays in 6 directions
                XYZ[] directions = new XYZ[]
                {
                    XYZ.BasisX,    // +X direction
                    -XYZ.BasisX,   // -X direction
                    XYZ.BasisY,    // +Y direction
                    -XYZ.BasisY,   // -Y direction
                    XYZ.BasisZ,    // +Z direction
                    -XYZ.BasisZ    // -Z direction
                };

                // Create ray intersector
                ReferenceIntersector refIntersector = new ReferenceIntersector(classFilter,
                    FindReferenceTarget.Element, view3D);
                refIntersector.FindReferencesInRevitLinks = true; // Set true to search for elements in linked files

                double minDistance = double.MaxValue;
                Element nearestHost = null;

                foreach (XYZ direction in directions)
                {
                    // Cast ray from current position
                    IList<ReferenceWithContext> references = refIntersector.Find(location, direction);

                    foreach (ReferenceWithContext rwc in references)
                    {
                        double distance = rwc.Proximity; // Distance to the element

                        // If within search range and closer
                        if (distance <= radius && distance < minDistance)
                        {
                            minDistance = distance;
                            nearestHost = doc.GetElement(rwc.GetReference().ElementId);
                        }
                    }
                }

                return nearestHost;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error getting nearest host element: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds the nearest wall to a point using wall location-line distance calculation.
        /// More reliable than ray-casting for door/window placement.
        /// </summary>
        /// <param name="doc">Current Revit document</param>
        /// <param name="point">Target point (internal units, feet)</param>
        /// <param name="level">Level to filter walls on</param>
        /// <param name="tolerance">Extra tolerance beyond half wall width (feet). Default ~5mm.</param>
        /// <returns>Tuple of (wall, projectedPoint, wallDirection, distance) or null</returns>
        public static (Wall wall, XYZ projectedPoint, XYZ wallDirection, double distance)?
            GetNearestWallByLocationLine(
                this Document doc,
                XYZ point,
                Level level,
                double tolerance = 5.0 / 304.8)
        {
            if (doc == null || point == null || level == null)
                return null;

            // Collect all walls on the given level
            var walls = new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .Cast<Wall>()
                .Where(w =>
                {
                    Parameter baseLevelParam = w.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
                    return baseLevelParam != null && baseLevelParam.AsElementId() == level.Id;
                })
                .ToList();

            Wall bestWall = null;
            XYZ bestProjection = null;
            XYZ bestDirection = null;
            double bestDistance = double.MaxValue;

            foreach (Wall wall in walls)
            {
                LocationCurve locCurve = wall.Location as LocationCurve;
                if (locCurve == null) continue;

                Curve curve = locCurve.Curve;
                if (curve == null) continue;

                // Use Curve.Project() which handles both lines and arcs
                IntersectionResult ir = curve.Project(new XYZ(point.X, point.Y, curve.GetEndPoint(0).Z));
                if (ir == null) continue;

                XYZ projectedPt = ir.XYZPoint;
                double distance = new XYZ(point.X - projectedPt.X, point.Y - projectedPt.Y, 0).GetLength();

                // Check if point is within half the wall width + tolerance
                double halfWidth = wall.Width / 2.0;
                if (distance <= halfWidth + tolerance && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestWall = wall;
                    bestProjection = new XYZ(projectedPt.X, projectedPt.Y, point.Z);

                    // Compute wall direction from curve tangent at projected parameter
                    XYZ p0 = curve.GetEndPoint(0);
                    XYZ p1 = curve.GetEndPoint(1);
                    bestDirection = new XYZ(p1.X - p0.X, p1.Y - p0.Y, 0).Normalize();
                }
            }

            if (bestWall == null)
                return null;

            return (bestWall, bestProjection, bestDirection, bestDistance);
        }

        /// <summary>
        /// Highlight the specified face
        /// </summary>
        /// <param name="doc">Current document</param>
        /// <param name="faceRef">Reference to the face to highlight</param>
        /// <param name="duration">Highlight duration in milliseconds, default 3000ms</param>
        public static void HighlightFace(this Document doc, Reference faceRef)
        {
            if (faceRef == null) return;

            // Get solid fill pattern
            FillPatternElement solidFill = new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(x => x.GetFillPattern().IsSolidFill);

            if (solidFill == null)
            {
                TaskDialog.Show("Error", "Solid fill pattern not found");
                return;
            }

            // Create highlight settings
            OverrideGraphicSettings ogs = new OverrideGraphicSettings();
            ogs.SetSurfaceForegroundPatternColor(new Color(255, 0, 0)); // Red
            ogs.SetSurfaceForegroundPatternId(solidFill.Id);
            ogs.SetSurfaceTransparency(0); // Opaque

            // Apply highlight
            doc.ActiveView.SetElementOverrides(faceRef.ElementId, ogs);
        }

        /// <summary>
        /// Extract the two primary direction vectors of a face
        /// </summary>
        /// <param name="face">Input face</param>
        /// <returns>Tuple containing primary and secondary directions</returns>
        /// <exception cref="ArgumentNullException">Thrown when face is null</exception>
        /// <exception cref="ArgumentException">Thrown when the face profile is insufficient to form a valid shape</exception>
        /// <exception cref="InvalidOperationException">Thrown when valid directions cannot be extracted</exception>
        public static (XYZ PrimaryDirection, XYZ SecondaryDirection) GetMainDirections(this Face face)
        {
            // 1. Parameter validation
            if (face == null)
                throw new ArgumentNullException(nameof(face), "Face cannot be null");

            // 2. Get face normal vector, used for possible perpendicular vector calculation later
            XYZ faceNormal = face.ComputeNormal(new UV(0.5, 0.5));

            // 3. Get face outer profile
            EdgeArrayArray edgeLoops = face.EdgeLoops;
            if (edgeLoops.Size == 0)
                throw new ArgumentException("Face has no valid edge loops", nameof(face));

            // Usually the first loop is the outer profile
            EdgeArray outerLoop = edgeLoops.get_Item(0);

            // 4. Calculate direction vector and length of each edge
            List<XYZ> edgeDirections = new List<XYZ>();  // Store unit direction vectors of each edge
            List<double> edgeLengths = new List<double>(); // Store lengths of each edge

            foreach (Edge edge in outerLoop)
            {
                Curve curve = edge.AsCurve();
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);

                // Calculate vector from start to end point
                XYZ direction = endPoint - startPoint;
                double length = direction.GetLength();

                // Ignore edges that are too short (may be due to vertex coincidence or numerical precision)
                if (length > 1e-10)
                {
                    edgeDirections.Add(direction.Normalize());  // Store normalized direction vector
                    edgeLengths.Add(length);                    // Store edge length
                }
            }

            if (edgeDirections.Count < 4) // Ensure at least 4 edges
            {
                throw new ArgumentException("The provided face does not have enough edges to form a valid shape", nameof(face));
            }

            // 5. Group edges by similar direction
            List<List<int>> directionGroups = new List<List<int>>();  // Store direction groups; each group contains edge indices

            for (int i = 0; i < edgeDirections.Count; i++)
            {
                bool foundGroup = false;
                XYZ currentDirection = edgeDirections[i];

                // Try to add current edge to an existing direction group
                for (int j = 0; j < directionGroups.Count; j++)
                {
                    var group = directionGroups[j];
                    // Calculate weighted average direction of current group
                    XYZ groupAvgDir = CalculateWeightedAverageDirection(group, edgeDirections, edgeLengths);

                    // Check if current direction is similar to the group average (including reverse)
                    double dotProduct = Math.Abs(groupAvgDir.DotProduct(currentDirection));
                    if (dotProduct > 0.8) // Deviation within ~30 degrees is considered similar
                    {
                        group.Add(i);  // Add current edge index to this direction group
                        foundGroup = true;
                        break;
                    }
                }

                // If current edge is not similar to any existing group, create new group
                if (!foundGroup)
                {
                    List<int> newGroup = new List<int> { i };
                    directionGroups.Add(newGroup);
                }
            }

            // 6. Calculate total weight (sum of edge lengths) and average direction for each direction group
            List<double> groupWeights = new List<double>();
            List<XYZ> groupDirections = new List<XYZ>();

            foreach (var group in directionGroups)
            {
                // Calculate total length of all edges in the group
                double totalLength = 0;
                foreach (int edgeIndex in group)
                {
                    totalLength += edgeLengths[edgeIndex];
                }
                groupWeights.Add(totalLength);

                // Calculate the weighted average direction of the group
                groupDirections.Add(CalculateWeightedAverageDirection(group, edgeDirections, edgeLengths));
            }

            // 7. Sort by weight and extract primary directions
            int[] sortedIndices = Enumerable.Range(0, groupDirections.Count)
                .OrderByDescending(i => groupWeights[i])
                .ToArray();

            // 8. Construct result
            if (groupDirections.Count >= 2)
            {
                // At least two direction groups; take the two highest-weight groups as primary and secondary directions
                int primaryIndex = sortedIndices[0];
                int secondaryIndex = sortedIndices[1];

                return (
                    PrimaryDirection: groupDirections[primaryIndex],      // Primary direction
                    SecondaryDirection: groupDirections[secondaryIndex]   // Secondary direction
                );
            }
            else if (groupDirections.Count == 1)
            {
                // Only one direction group; manually create a perpendicular secondary direction
                XYZ primaryDirection = groupDirections[0];
                // Use cross product of face normal and primary direction to create perpendicular vector
                XYZ secondaryDirection = faceNormal.CrossProduct(primaryDirection).Normalize();

                return (
                    PrimaryDirection: primaryDirection,         // Primary direction
                    SecondaryDirection: secondaryDirection      // Artificially constructed perpendicular secondary direction
                );
            }
            else
            {
                // Cannot extract valid directions (rare)
                throw new InvalidOperationException("Cannot extract valid directions from the face");
            }
        }

        /// <summary>
        /// Calculate the weighted average direction of a group of edges based on edge length
        /// </summary>
        /// <param name="edgeIndices">List of edge indices</param>
        /// <param name="directions">Direction vectors of all edges</param>
        /// <param name="lengths">Lengths of all edges</param>
        /// <returns>Normalized weighted average direction vector</returns>
        public static XYZ CalculateWeightedAverageDirection(List<int> edgeIndices, List<XYZ> directions, List<double> lengths)
        {
            if (edgeIndices.Count == 0)
                return null;

            double sumX = 0, sumY = 0, sumZ = 0;
            XYZ referenceDir = directions[edgeIndices[0]];  // Use first direction in group as reference

            foreach (int i in edgeIndices)
            {
                XYZ currentDir = directions[i];

                // Calculate dot product of current direction with reference direction to determine if reversal is needed
                double dot = referenceDir.DotProduct(currentDir);

                // If direction is opposite (negative dot product), reverse the vector before contributing
                // This ensures vectors in the same group all point in the same direction, avoiding cancellation
                double factor = (dot >= 0) ? lengths[i] : -lengths[i];

                // Accumulate vector components (weighted)
                sumX += currentDir.X * factor;
                sumY += currentDir.Y * factor;
                sumZ += currentDir.Z * factor;
            }

            // Create composite vector and normalize
            XYZ avgDir = new XYZ(sumX, sumY, sumZ);
            double magnitude = avgDir.GetLength();

            // Prevent zero vector
            if (magnitude < 1e-10)
                return referenceDir;  // Fall back to reference direction

            return avgDir.Normalize();  // Return normalized direction vector
        }

        /// <summary>
        /// Determine whether three vectors comply with the right-hand rule and are mutually perpendicular
        /// </summary>
        /// <param name="thumb">Thumb direction vector</param>
        /// <param name="indexFinger">Index finger direction vector</param>
        /// <param name="middleFinger">Middle finger direction vector</param>
        /// <param name="tolerance">Tolerance for determination, default 1e-6</param>
        /// <returns>True if the three vectors comply with the right-hand rule and are mutually perpendicular, otherwise false</returns>
        public static bool IsRightHandRuleCompliant(this XYZ thumb, XYZ indexFinger, XYZ middleFinger, double tolerance = 1e-6)
        {
            // Check whether the three vectors are mutually perpendicular (all dot products are close to 0)
            double dotThumbIndex = Math.Abs(thumb.DotProduct(indexFinger));
            double dotThumbMiddle = Math.Abs(thumb.DotProduct(middleFinger));
            double dotIndexMiddle = Math.Abs(indexFinger.DotProduct(middleFinger));

            bool areOrthogonal = (dotThumbIndex <= tolerance) &&
                                  (dotThumbMiddle <= tolerance) &&
                                  (dotIndexMiddle <= tolerance);

            // Only check right-hand rule if the three vectors are mutually perpendicular
            if (!areOrthogonal)
                return false;

            // Calculate dot product of cross product vector with thumb to determine right-hand rule compliance
            XYZ crossProduct = indexFinger.CrossProduct(middleFinger);
            double rightHandTest = crossProduct.DotProduct(thumb);

            // Positive dot product indicates right-hand rule compliance
            return rightHandTest > tolerance;
        }

        /// <summary>
        /// Generate an index finger direction complying with the right-hand rule from thumb and middle finger directions
        /// </summary>
        /// <param name="thumb">Thumb direction vector</param>
        /// <param name="middleFinger">Middle finger direction vector</param>
        /// <param name="tolerance">Tolerance for perpendicularity check, default 1e-6</param>
        /// <returns>Generated index finger direction vector, or null if input vectors are not perpendicular</returns>
        public static XYZ GenerateIndexFinger(this XYZ thumb, XYZ middleFinger, double tolerance = 1e-6)
        {
            // First normalize input vectors
            XYZ normalizedThumb = thumb.Normalize();
            XYZ normalizedMiddleFinger = middleFinger.Normalize();

            // Check whether the two vectors are perpendicular (dot product close to 0)
            double dotProduct = normalizedThumb.DotProduct(normalizedMiddleFinger);

            // If the absolute value of the dot product exceeds the tolerance, vectors are not perpendicular
            if (Math.Abs(dotProduct) > tolerance)
            {
                return null;
            }

            // Calculate index finger direction through cross product and negate
            XYZ indexFinger = normalizedMiddleFinger.CrossProduct(normalizedThumb).Negate();

            // Return normalized index finger direction vector
            return indexFinger.Normalize();
        }

        /// <summary>
        /// Create or get a level at the specified elevation
        /// </summary>
        /// <param name="doc">Revit document</param>
        /// <param name="elevation">Level elevation (ft)</param>
        /// <param name="levelName">Level name</param>
        /// <returns></returns>
        public static Level CreateOrGetLevel(this Document doc, double elevation, string levelName)
        {
            // First check if a level at the specified elevation already exists
            Level existingLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => Math.Abs(l.Elevation - elevation) < 0.1 / 304.8);

            if (existingLevel != null)
                return existingLevel;

            // Create new level
            Level newLevel = Level.Create(doc, elevation);
            // Set level name
            Level namesakeLevel = new FilteredElementCollector(doc)
                 .OfClass(typeof(Level))
                 .Cast<Level>()
                 .FirstOrDefault(l => l.Name == levelName);
            if (namesakeLevel != null)
            {
                levelName = $"{levelName}_{newLevel.Id.GetValue()}";
            }
            newLevel.Name = levelName;

            return newLevel;
        }

        /// <summary>
        /// Find the level nearest to a given height
        /// </summary>
        /// <param name="doc">Current Revit document</param>
        /// <param name="height">Target height (Revit internal units)</param>
        /// <returns>Level nearest to the target height, or null if no levels exist in the document</returns>
        public static Level FindNearestLevel(this Document doc, double height)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc), "Document cannot be null");

            // Use LINQ to get the nearest level directly
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(level => Math.Abs(level.Elevation - height))
                .FirstOrDefault();
        }

        ///// <summary>
        ///// Refresh the view and add a delay
        ///// </summary>
        //public static void Refresh(this Document doc, int waitingTime = 0, bool allowOperation = true)
        //{
        //    UIApplication uiApp = new UIApplication(doc.Application);
        //    UIDocument uiDoc = uiApp.ActiveUIDocument;

        //    // Check if document is modifiable
        //    if (uiDoc.Document.IsModifiable)
        //    {
        //        // Update model
        //        uiDoc.Document.Regenerate();
        //    }
        //    // Update UI
        //    uiDoc.RefreshActiveView();

        //    // Wait for delay
        //    if (waitingTime != 0)
        //    {
        //        System.Threading.Thread.Sleep(waitingTime);
        //    }

        //    // Allow user to perform non-safe operations
        //    if (allowOperation)
        //    {
        //        System.Windows.Forms.Application.DoEvents();
        //    }
        //}

        /// <summary>
        /// Save the specified message to the specified file on the desktop (default: overwrite)
        /// </summary>
        /// <param name="message">Message content to save</param>
        /// <param name="fileName">Target filename</param>
        public static void SaveToDesktop(this string message, string fileName = "temp.json", bool isAppend = false)
        {
            // Ensure fileName has an extension
            if (!Path.HasExtension(fileName))
            {
                fileName += ".txt"; // Default: add .txt extension
            }

            // Get desktop path
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            // Combine full file path
            string filePath = Path.Combine(desktopPath, fileName);

            // Write to file (overwrite mode)
            using (StreamWriter sw = new StreamWriter(filePath, isAppend))
            {
                sw.WriteLine($"{message}");
            }
        }

    }
}
