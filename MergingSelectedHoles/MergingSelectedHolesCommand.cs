using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergingSelectedHoles
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class MergingSelectedHolesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Получение текущего документа
            Document doc = commandData.Application.ActiveUIDocument.Document;
            //Получение доступа к Selection
            Selection sel = commandData.Application.ActiveUIDocument.Selection;
            //Общие параметры размеров
            Guid intersectionPointWidthGuid = new Guid("8f2e4f93-9472-4941-a65d-0ac468fd6a5d");
            Guid intersectionPointHeightGuid = new Guid("da753fe3-ecfa-465b-9a2c-02f55d0c2ff1");
            Guid intersectionPointThicknessGuid = new Guid("293f055d-6939-4611-87b7-9a50d0c1f50e");

            Guid heightOfBaseLevelGuid = new Guid("9f5f7e49-616e-436f-9acc-5305f34b6933");
            Guid levelOffsetGuid = new Guid("515dc061-93ce-40e4-859a-e29224d80a10");

            MergingSelectedHolesWPF mergingSelectedHolesWPF = new MergingSelectedHolesWPF();
            mergingSelectedHolesWPF.ShowDialog();
            if (mergingSelectedHolesWPF.DialogResult != true)
            {
                return Result.Cancelled;
            }
            string roundHolesPositionButtonName = mergingSelectedHolesWPF.RoundHolesPositionButtonName;
            double roundHoleSizesUpIncrement = mergingSelectedHolesWPF.RoundHoleSizesUpIncrement;
            double roundHolePosition = mergingSelectedHolesWPF.RoundHolePositionIncrement;

            //Выбор болванок для объединения
            GloryHoleSelectionFilter gloryHoleSelectionFilter = new GloryHoleSelectionFilter();
            IList<Reference> selHolesReferenceList = null;
            try
            {
                selHolesReferenceList = sel.PickObjects(ObjectType.Element, gloryHoleSelectionFilter, "Выберите болванки для объединения!");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            List<FamilyInstance> intersectionWallRectangularSolidIntersectCombineList = new List<FamilyInstance>();
            List<FamilyInstance> intersectionFloorRectangularSolidIntersectCombineList = new List<FamilyInstance>();

            foreach (Reference selectedRef in selHolesReferenceList)
            {
                FamilyInstance gHole = doc.GetElement(selectedRef) as FamilyInstance;
                if (gHole != null)
                {
                    if(gHole.Symbol.Family.Name == "Пересечение_Стена_Прямоугольное")
                    {
                        intersectionWallRectangularSolidIntersectCombineList.Add(gHole);
                    }
                    else if(gHole.Symbol.Family.Name == "Пересечение_Плита_Прямоугольное")
                    {
                        intersectionFloorRectangularSolidIntersectCombineList.Add(gHole);
                    }
                }
            }

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Объединение выбранных отверстий");
                if (intersectionWallRectangularSolidIntersectCombineList.Count > 1)
                {
                    FamilySymbol intersectionWallRectangularFamilySymbol = intersectionWallRectangularSolidIntersectCombineList.First().Symbol;
                    List <XYZ> pointsList = new List<XYZ>();
                    double intersectionPointThickness = 0;

                    foreach (FamilyInstance intPount in intersectionWallRectangularSolidIntersectCombineList)
                    {
                        XYZ originPoint = (intPount.Location as LocationPoint).Point;

                        XYZ downLeftPoint = originPoint + (intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation;
                        pointsList.Add(downLeftPoint);

                        XYZ downRightPoint = originPoint + (intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation.Negate();
                        pointsList.Add(downRightPoint);

                        XYZ upLeftPoint = originPoint + ((intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation)
                            + intPount.get_Parameter(intersectionPointHeightGuid).AsDouble() * XYZ.BasisZ;
                        pointsList.Add(upLeftPoint);

                        XYZ upRightPoint = originPoint + ((intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation.Negate())
                            + intPount.get_Parameter(intersectionPointHeightGuid).AsDouble() * XYZ.BasisZ;
                        pointsList.Add(upRightPoint);

                        if (intPount.get_Parameter(intersectionPointThicknessGuid).AsDouble() > intersectionPointThickness)
                        {
                            intersectionPointThickness = intPount.get_Parameter(intersectionPointThicknessGuid).AsDouble();
                        }
                    }

                    //Найти центр спроецировать точки на одну отметку и померить расстояние
                    double maxHorizontalDistance = 0;
                    double maxVerticalDistance = 0;
                    XYZ pointP1 = null;
                    XYZ pointP2 = null;
                    XYZ pointP3 = null;
                    XYZ pointP4 = null;
                    foreach (XYZ p1 in pointsList)
                    {
                        foreach (XYZ p2 in pointsList)
                        {
                            if (new XYZ(p1.X, p1.Y, 0).DistanceTo(new XYZ(p2.X, p2.Y, 0)) > maxHorizontalDistance)
                            {
                                maxHorizontalDistance = new XYZ(p1.X, p1.Y, 0).DistanceTo(new XYZ(p2.X, p2.Y, 0));
                                pointP1 = p1;
                                pointP2 = p2;
                            }

                            if (new XYZ(0, 0, p1.Z).DistanceTo(new XYZ(0, 0, p2.Z)) > maxVerticalDistance)
                            {
                                maxVerticalDistance = new XYZ(0, 0, p1.Z).DistanceTo(new XYZ(0, 0, p2.Z));
                                pointP3 = p1;
                                pointP4 = p2;
                            }
                        }
                    }
                    XYZ midPointLeftRight = (pointP1 + pointP2) / 2;
                    XYZ midPointUpDown = (pointP3 + pointP4) / 2;
                    XYZ centroidIntersectionPoint = new XYZ(midPointLeftRight.X, midPointLeftRight.Y, midPointUpDown.Z);

                    List<XYZ> combineDownLeftPointList = new List<XYZ>();
                    List<XYZ> combineDownRightPointList = new List<XYZ>();
                    List<XYZ> combineUpLeftPointList = new List<XYZ>();
                    List<XYZ> combineUpRightPointList = new List<XYZ>();

                    XYZ pointFacingOrientation = intersectionWallRectangularSolidIntersectCombineList.First().FacingOrientation;
                    XYZ pointHandOrientation = intersectionWallRectangularSolidIntersectCombineList.First().HandOrientation;

                    foreach (XYZ p in pointsList)
                    {
                        XYZ vectorToPoint = (p - centroidIntersectionPoint).Normalize();

                        //Нижний левый угол
                        if (pointFacingOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2
                            && pointHandOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2)
                        {
                            combineDownLeftPointList.Add(p);
                        }

                        //Нижний правый угол
                        if (pointFacingOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2
                            && pointHandOrientation.AngleTo(vectorToPoint) <= Math.PI / 2)
                        {
                            combineDownRightPointList.Add(p);
                        }

                        //Верхний левый угол
                        if (pointFacingOrientation.AngleTo(vectorToPoint) <= Math.PI / 2
                            && pointHandOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2)
                        {
                            combineUpLeftPointList.Add(p);
                        }

                        //Верхний правый угол
                        if (pointFacingOrientation.AngleTo(vectorToPoint) <= Math.PI / 2
                            && pointHandOrientation.AngleTo(vectorToPoint) <= Math.PI / 2)
                        {
                            combineUpRightPointList.Add(p);
                        }
                    }

                    List<XYZ> maxRightPointList = new List<XYZ>();
                    maxRightPointList.AddRange(combineDownRightPointList);
                    maxRightPointList.AddRange(combineUpRightPointList);
                    double maxRightDistance = -1000000;
                    foreach (XYZ p in pointsList)
                    {
                        XYZ x = Line.CreateBound(centroidIntersectionPoint, centroidIntersectionPoint + 1000000 * pointHandOrientation).Project(p).XYZPoint;
                        if (x.DistanceTo(centroidIntersectionPoint) > maxRightDistance)
                        {
                            maxRightDistance = x.DistanceTo(centroidIntersectionPoint);
                        }
                    }

                    List<XYZ> maxLeftPointList = new List<XYZ>();
                    maxLeftPointList.AddRange(combineDownLeftPointList);
                    maxLeftPointList.AddRange(combineUpLeftPointList);
                    double maxLeftDistance = -1000000;
                    foreach (XYZ p in pointsList)
                    {
                        XYZ x = Line.CreateBound(centroidIntersectionPoint, centroidIntersectionPoint + 1000000 * pointHandOrientation.Negate()).Project(p).XYZPoint;
                        if (x.DistanceTo(centroidIntersectionPoint) > maxLeftDistance)
                        {
                            maxLeftDistance = x.DistanceTo(centroidIntersectionPoint);
                        }
                    }

                    double minZ = 10000000000;
                    XYZ minZPoint = null;
                    double maxZ = -10000000000;
                    XYZ maxZPoint = null;

                    foreach (XYZ p in pointsList)
                    {
                        if (p.Z < minZ)
                        {
                            minZ = p.Z;
                            minZPoint = p;
                        }
                        if (p.Z > maxZ)
                        {
                            maxZ = p.Z;
                            maxZPoint = p;
                        }
                    }

                    double intersectionPointHeight = RoundUpToIncrement(maxZPoint.Z - minZPoint.Z, roundHoleSizesUpIncrement);
                    double intersectionPointWidth = RoundUpToIncrement(maxLeftDistance + maxRightDistance, roundHoleSizesUpIncrement);

                    XYZ newCenterPoint = null;
                    if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                    {
                        newCenterPoint = new XYZ(RoundToIncrement(centroidIntersectionPoint.X, roundHolePosition)
                            , RoundToIncrement(centroidIntersectionPoint.Y, roundHolePosition)
                            , RoundToIncrement((centroidIntersectionPoint.Z - intersectionPointHeight / 2)
                            - (doc.GetElement(intersectionWallRectangularSolidIntersectCombineList.First().LevelId) as Level).Elevation, roundHolePosition));
                    }
                    else
                    {
                        newCenterPoint = new XYZ(centroidIntersectionPoint.X
                            , centroidIntersectionPoint.Y
                            , (centroidIntersectionPoint.Z - intersectionPointHeight / 2)
                            - (doc.GetElement(intersectionWallRectangularSolidIntersectCombineList.First().LevelId) as Level).Elevation);
                    }

                    FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(newCenterPoint
                        , intersectionWallRectangularFamilySymbol
                        , doc.GetElement(intersectionWallRectangularSolidIntersectCombineList.First().LevelId) as Level
                        , StructuralType.NonStructural) as FamilyInstance;

                    if (Math.Round(intersectionWallRectangularSolidIntersectCombineList.First().FacingOrientation.AngleTo(intersectionPoint.FacingOrientation), 6) != 0)
                    {
                        Line rotationLine = Line.CreateBound(newCenterPoint, newCenterPoint + 1 * XYZ.BasisZ);
                        ElementTransformUtils.RotateElement(doc, intersectionPoint.Id, rotationLine, intersectionWallRectangularSolidIntersectCombineList.First().FacingOrientation.AngleTo(intersectionPoint.FacingOrientation));
                    }

                    intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(intersectionPointHeight);
                    intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(intersectionPointWidth);
                    intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);

                    intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set((doc.GetElement(intersectionPoint.LevelId) as Level).Elevation);
                    intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(newCenterPoint.Z);
                    intersectionPoint.get_Parameter(levelOffsetGuid).Set(newCenterPoint.Z);

                    foreach (FamilyInstance forDel in intersectionWallRectangularSolidIntersectCombineList)
                    {
                        doc.Delete(forDel.Id);
                    }
                }
                if (intersectionFloorRectangularSolidIntersectCombineList.Count > 1)
                {
                    FamilySymbol intersectionFloorRectangularFamilySymbol = intersectionFloorRectangularSolidIntersectCombineList.First().Symbol;
                    List<XYZ> pointsList = new List<XYZ>();
                    double intersectionPointThickness = 0;

                    foreach (FamilyInstance intPount in intersectionFloorRectangularSolidIntersectCombineList)
                    {
                        XYZ originPoint = (intPount.Location as LocationPoint).Point;

                        XYZ downLeftPoint = originPoint + (intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation.Negate()
                            + (intPount.get_Parameter(intersectionPointHeightGuid).AsDouble() / 2) * intPount.FacingOrientation.Negate();
                        pointsList.Add(downLeftPoint);

                        XYZ downRightPoint = originPoint + (intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation
                            + (intPount.get_Parameter(intersectionPointHeightGuid).AsDouble() / 2) * intPount.FacingOrientation.Negate();
                        pointsList.Add(downRightPoint);

                        XYZ upLeftPoint = originPoint + (intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation.Negate()
                            + (intPount.get_Parameter(intersectionPointHeightGuid).AsDouble() / 2) * intPount.FacingOrientation;
                        pointsList.Add(upLeftPoint);

                        XYZ upRightPoint = originPoint + (intPount.get_Parameter(intersectionPointWidthGuid).AsDouble() / 2) * intPount.HandOrientation
                            + (intPount.get_Parameter(intersectionPointHeightGuid).AsDouble() / 2) * intPount.FacingOrientation;
                        pointsList.Add(upRightPoint);

                        if (intPount.get_Parameter(intersectionPointThicknessGuid).AsDouble() > intersectionPointThickness)
                        {
                            intersectionPointThickness = intPount.get_Parameter(intersectionPointThicknessGuid).AsDouble();
                        }
                    }
                    double maxX = pointsList.Max(p => p.X);
                    double minX = pointsList.Min(p => p.X);
                    double maxY = pointsList.Max(p => p.Y);
                    double minY = pointsList.Min(p => p.Y);
                    XYZ centroidIntersectionPoint = new XYZ((maxX + minX) / 2, (maxY + minY) / 2, pointsList.First().Z);

                    List<XYZ> combineDownLeftPointList = new List<XYZ>();
                    List<XYZ> combineDownRightPointList = new List<XYZ>();
                    List<XYZ> combineUpLeftPointList = new List<XYZ>();
                    List<XYZ> combineUpRightPointList = new List<XYZ>();

                    XYZ pointFacingOrientation = intersectionFloorRectangularSolidIntersectCombineList.First().FacingOrientation;
                    XYZ pointHandOrientation = intersectionFloorRectangularSolidIntersectCombineList.First().HandOrientation;
                    Level pointLevel = doc.GetElement(intersectionFloorRectangularSolidIntersectCombineList.First().LevelId) as Level;
                    double pointLevelElevation = pointLevel.Elevation;
                    foreach (XYZ p in pointsList)
                    {
                        XYZ vectorToPoint = (p - centroidIntersectionPoint).Normalize();

                        //Нижний левый угол
                        if (pointFacingOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2
                            && pointHandOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2)
                        {
                            combineDownLeftPointList.Add(p);
                        }

                        //Нижний правый угол
                        if (pointFacingOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2
                            && pointHandOrientation.AngleTo(vectorToPoint) <= Math.PI / 2)
                        {
                            combineDownRightPointList.Add(p);
                        }

                        //Верхний левый угол
                        if (pointFacingOrientation.AngleTo(vectorToPoint) <= Math.PI / 2
                            && pointHandOrientation.Negate().AngleTo(vectorToPoint) <= Math.PI / 2)
                        {
                            combineUpLeftPointList.Add(p);
                        }

                        //Верхний правый угол
                        if (pointFacingOrientation.AngleTo(vectorToPoint) <= Math.PI / 2
                            && pointHandOrientation.AngleTo(vectorToPoint) <= Math.PI / 2)
                        {
                            combineUpRightPointList.Add(p);
                        }
                    }

                    List<XYZ> maxUpPointList = new List<XYZ>();
                    maxUpPointList.AddRange(combineUpLeftPointList);
                    maxUpPointList.AddRange(combineUpRightPointList);
                    double maxUpDistance = -1000000;
                    foreach (XYZ p in maxUpPointList)
                    {
                        XYZ x = Line.CreateBound(centroidIntersectionPoint, centroidIntersectionPoint + 1000000 * pointFacingOrientation).Project(p).XYZPoint;
                        if (x.DistanceTo(centroidIntersectionPoint) > maxUpDistance)
                        {
                            maxUpDistance = x.DistanceTo(centroidIntersectionPoint);
                        }
                    }

                    List<XYZ> maxDownPointList = new List<XYZ>();
                    maxDownPointList.AddRange(combineDownLeftPointList);
                    maxDownPointList.AddRange(combineDownRightPointList);
                    double maxDownDistance = -1000000;
                    foreach (XYZ p in maxDownPointList)
                    {
                        XYZ x = Line.CreateBound(centroidIntersectionPoint, centroidIntersectionPoint + 1000000 * pointFacingOrientation.Negate()).Project(p).XYZPoint;
                        if (x.DistanceTo(centroidIntersectionPoint) > maxDownDistance)
                        {
                            maxDownDistance = x.DistanceTo(centroidIntersectionPoint);
                        }
                    }

                    List<XYZ> maxRightPointList = new List<XYZ>();
                    maxRightPointList.AddRange(combineDownRightPointList);
                    maxRightPointList.AddRange(combineUpRightPointList);
                    double maxRightDistance = -1000000;
                    foreach (XYZ p in maxRightPointList)
                    {
                        XYZ x = Line.CreateBound(centroidIntersectionPoint, centroidIntersectionPoint + 1000000 * pointHandOrientation).Project(p).XYZPoint;
                        if (x.DistanceTo(centroidIntersectionPoint) > maxRightDistance)
                        {
                            maxRightDistance = x.DistanceTo(centroidIntersectionPoint);
                        }
                    }

                    List<XYZ> maxLeftPointList = new List<XYZ>();
                    maxLeftPointList.AddRange(combineDownLeftPointList);
                    maxLeftPointList.AddRange(combineUpLeftPointList);
                    double maxLeftDistance = -1000000;
                    foreach (XYZ p in maxLeftPointList)
                    {
                        XYZ x = Line.CreateBound(centroidIntersectionPoint, centroidIntersectionPoint + 1000000 * pointHandOrientation.Negate()).Project(p).XYZPoint;
                        if (x.DistanceTo(centroidIntersectionPoint) > maxLeftDistance)
                        {
                            maxLeftDistance = x.DistanceTo(centroidIntersectionPoint);
                        }
                    }

                    double intersectionPointHeight = RoundUpToIncrement(maxUpDistance + maxDownDistance, roundHoleSizesUpIncrement);
                    double intersectionPointWidth = RoundUpToIncrement(maxLeftDistance + maxRightDistance, roundHoleSizesUpIncrement);

                    XYZ newCenterPoint = null;
                    if (roundHolesPositionButtonName == "radioButton_RoundHolesPositionYes")
                    {
                        newCenterPoint = new XYZ(RoundToIncrement(centroidIntersectionPoint.X, roundHolePosition)
                            , RoundToIncrement(centroidIntersectionPoint.Y, roundHolePosition)
                            , RoundToIncrement(centroidIntersectionPoint.Z
                            - pointLevelElevation, roundHolePosition));
                    }
                    else
                    {
                        newCenterPoint = new XYZ(centroidIntersectionPoint.X
                            , centroidIntersectionPoint.Y
                            , centroidIntersectionPoint.Z - pointLevelElevation);
                    }

                    FamilyInstance intersectionPoint = doc.Create.NewFamilyInstance(newCenterPoint
                        , intersectionFloorRectangularFamilySymbol
                        , pointLevel
                        , StructuralType.NonStructural) as FamilyInstance;
                    intersectionPoint.get_Parameter(intersectionPointWidthGuid).Set(intersectionPointWidth);
                    intersectionPoint.get_Parameter(intersectionPointHeightGuid).Set(intersectionPointHeight);
                    intersectionPoint.get_Parameter(intersectionPointThicknessGuid).Set(intersectionPointThickness);

                    intersectionPoint.get_Parameter(heightOfBaseLevelGuid).Set(pointLevelElevation);
                    intersectionPoint.get_Parameter(levelOffsetGuid).Set(intersectionPoint.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM).AsDouble() - 50 / 304.8);

                    double rotationAngle = pointFacingOrientation.AngleTo(intersectionPoint.FacingOrientation);
                    if (rotationAngle != 0)
                    {
                        Line rotationAxis = Line.CreateBound(newCenterPoint, newCenterPoint + 1 * XYZ.BasisZ);
                        ElementTransformUtils.RotateElement(doc
                            , intersectionPoint.Id
                            , rotationAxis
                            , rotationAngle);
                    }

                    foreach (FamilyInstance forDel in intersectionFloorRectangularSolidIntersectCombineList)
                    {
                        doc.Delete(forDel.Id);
                    }
                }
                t.Commit();
            }
            return Result.Succeeded;
        }
        private double RoundToIncrement(double x, double m)
        {
            return (Math.Round((x * 304.8) / m) * m) / 304.8;
        }
        private double RoundUpToIncrement(double x, double m)
        {
            return (((int)Math.Ceiling(x * 304.8 / m)) * m) / 304.8;
        }
    }
}
