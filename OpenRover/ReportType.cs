using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRover
{

    /// <summary>
    /// An enumeration of [somewhat arbitrarily determined] report types. 
    /// Different parameter sets are naturally queried at different rates. 
    /// This is essentially a categorization used to specify which parameters
    /// should be queried at which rates.
    /// </summary>
    public enum ReportType
    {
        Fast,
        Medium,
        Slow,
        Aggregate
    }
}
