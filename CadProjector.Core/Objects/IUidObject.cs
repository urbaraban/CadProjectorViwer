using System;
using System.Collections.Generic;

namespace CadProjector.Core.Objects
{
    public interface IUidObject
    {
        Guid Uid { get; }
        string NameID { get; set; }
   double MX { get; set; }
    double MY { get; set; }
        double MZ { get; set; }
        bool IsSelected { get; set; }
 bool IsRender { get; set; }
        bool IsBlank { get; set; }
    }
}