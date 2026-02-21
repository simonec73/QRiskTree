using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRiskTree.Engine.ImportExport
{
    public interface IApplyMitigation<M> where M : INamedObject
    {
        bool ApplyMitigation(M mitigation);
    }
}
