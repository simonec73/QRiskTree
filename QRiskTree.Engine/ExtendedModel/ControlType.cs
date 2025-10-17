using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRiskTree.Engine.ExtendedModel
{
    /// <summary>
    /// Control type enumeration.
    /// </summary>
    public enum ControlType
    {
        /// <summary>
        /// Unknown control type.
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// Preventive control type.
        /// </summary>
        Preventive = 1,
        /// <summary>
        /// Detective control type.
        /// </summary>
        Detective = 2,
        /// <summary>
        /// Corrective control type.
        /// </summary>
        Corrective = 3,
        /// <summary>
        /// Recovery control type.
        /// </summary>
        Recovery = 4,
        /// <summary>
        /// Represents a category or type that is not specifically defined by other enumeration values.
        /// </summary>
        /// <remarks>This enumeration value is used to categorize items that do not fit into predefined
        /// categories.</remarks>
        Other = 100
    }
}
