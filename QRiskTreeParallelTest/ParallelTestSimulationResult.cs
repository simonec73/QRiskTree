using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRiskTreeParallelTest
{
    internal class ParallelTestSimulationResult
    {
        public ParallelTestSimulationResult(long elapsedTime, double min, double mode, double max)
        {
            ElapsedTime = elapsedTime;
            Min = min;
            Mode = mode;
            Max = max;
        }

        public long ElapsedTime { get; }
        
        public double Min { get; }

        public double Mode { get; }

        public double Max { get; }
    }
}
