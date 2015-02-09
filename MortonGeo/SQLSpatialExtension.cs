using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortonGeo
{
    public static class SQLSpatialExtension
    {
        public static string GetSqlGeometryAsString(this SqlGeometry geom)
        {
            return new string(geom.STAsText().Value);
        }
    }
}
