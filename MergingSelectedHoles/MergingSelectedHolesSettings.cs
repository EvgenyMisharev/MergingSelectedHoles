using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MergingSelectedHoles
{
    public class MergingSelectedHolesSettings
    {
        public string RoundHolesPositionButtonName { get; set; }
        public string RoundHoleSizesUpIncrementValue { get; set; }
        public string RoundHolePositionIncrementValue { get; set; }
        public MergingSelectedHolesSettings GetSettings()
        {
            MergingSelectedHolesSettings mergingSelectedHolesSettings = null;
            string assemblyPathAll = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string fileName = "MergingSelectedHolesSettings.xml";
            string assemblyPath = assemblyPathAll.Replace("MergingSelectedHoles.dll", fileName);

            if (File.Exists(assemblyPath))
            {
                using (FileStream fs = new FileStream(assemblyPath, FileMode.Open))
                {
                    XmlSerializer xSer = new XmlSerializer(typeof(MergingSelectedHolesSettings));
                    mergingSelectedHolesSettings = xSer.Deserialize(fs) as MergingSelectedHolesSettings;
                    fs.Close();
                }
            }
            else
            {
                mergingSelectedHolesSettings = new MergingSelectedHolesSettings();
            }

            return mergingSelectedHolesSettings;
        }

        public void SaveSettings()
        {
            string assemblyPathAll = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string fileName = "MergingSelectedHolesSettings.xml";
            string assemblyPath = assemblyPathAll.Replace("MergingSelectedHoles.dll", fileName);

            if (File.Exists(assemblyPath))
            {
                File.Delete(assemblyPath);
            }

            using (FileStream fs = new FileStream(assemblyPath, FileMode.Create))
            {
                XmlSerializer xSer = new XmlSerializer(typeof(MergingSelectedHolesSettings));
                xSer.Serialize(fs, this);
                fs.Close();
            }
        }
    }
}
