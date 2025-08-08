using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace QRiskTreeEditor.Importers
{
    internal static class OpenThreatModelImporter
    {
        public static OpenThreatModel? Import(string filePath)
        {
            OpenThreatModel? result = null;

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                result = JsonConvert.DeserializeObject<OpenThreatModel>(File.ReadAllText(filePath));
            }

            return result;
        }
       
    }
}
