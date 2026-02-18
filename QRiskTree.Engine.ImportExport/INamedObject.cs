using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace QRiskTree.Engine.ImportExport
{
    public interface INamedObject
    {
        string? Name { get; set; }

        string? Description { get; set; }
    }
}
