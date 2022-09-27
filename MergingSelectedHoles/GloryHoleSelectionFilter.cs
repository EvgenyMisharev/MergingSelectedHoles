using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MergingSelectedHoles
{
    class GloryHoleSelectionFilter : ISelectionFilter
    {
		public bool AllowElement(Autodesk.Revit.DB.Element elem)
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

		public bool AllowReference(Autodesk.Revit.DB.Reference reference, Autodesk.Revit.DB.XYZ position)
		{
			return false;
		}
	}
}
