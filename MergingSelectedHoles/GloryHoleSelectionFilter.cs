using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace MergingSelectedHoles
{
    class GloryHoleSelectionFilter : ISelectionFilter
    {
		public bool AllowElement(Element elem)
		{
			if (elem is FamilyInstance
				&& elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_GenericModel
				&& ((elem as FamilyInstance).Symbol.Family.Name == "Пересечение_Стена_Прямоугольное"
				|| (elem as FamilyInstance).Symbol.Family.Name == "Пересечение_Плита_Прямоугольное"))
			{
				return true;
			}
			return false;
		}

		public bool AllowReference(Reference reference, XYZ position)
		{
			return false;
		}
	}
}
