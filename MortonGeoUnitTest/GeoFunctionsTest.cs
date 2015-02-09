using Microsoft.SqlServer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MortonGeo;
using System;
using System.Collections.Generic;
using System.Data.Entity.Spatial;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MortonGeoUnitTest
{
    [TestClass]
    public class GeoFunctionsTest
    {

        [TestMethod]
        public void SquareTest()
        {
            var test = string.Format("{0:0.000000}", 12256.58344444678);
            var geo = new GeoLogic();
            var polygon = geo.MBR2GEOMETRY(0, 0, 100, 100, 4326);
            var polygonstr = polygon.GetSqlGeometryAsString();
            Assert.AreEqual(polygonstr, "POLYGON ((0 0, 100 0, 100 100, 0 100, 0 0))");
        }


        [TestMethod]
        public void MortonTest()
        {
            List<DimentionObject> dos = new List<DimentionObject>();
            var geo = new GeoLogic();
            var series1 = geo.GenerateSeries(0, 7, 1);
            var series2 = geo.GenerateSeries(0, 7, 1);
            foreach (var x in series1)
            {
                foreach (var y in series2)
                {
                    dos.Add(new DimentionObject { col = x, row = y, MortonKey = geo.Morton(x, y), geom = geo.MBR2GEOMETRY(x, y, 10.0, 10.0, 0) });
                }
            }
            dos = dos.OrderBy(x => x.MortonKey).ToList();
            Assert.AreEqual(dos[0].geom.GetSqlGeometryAsString(), "POLYGON ((0 0, 10 0, 10 10, 0 10, 0 0))");
            Assert.AreEqual(dos[1].geom.GetSqlGeometryAsString(), "POLYGON ((1 0, 10 0, 10 10, 1 10, 1 0))");
            Assert.AreEqual(dos[2].geom.GetSqlGeometryAsString(), "POLYGON ((0 1, 10 1, 10 10, 0 10, 0 1))");
            Assert.AreEqual(dos[3].geom.GetSqlGeometryAsString(), "POLYGON ((1 1, 10 1, 10 10, 1 10, 1 1))");
            Assert.AreEqual(dos[4].geom.GetSqlGeometryAsString(), "POLYGON ((2 0, 10 0, 10 10, 2 10, 2 0))");
            Assert.AreEqual(dos[5].geom.GetSqlGeometryAsString(), "POLYGON ((3 0, 10 0, 10 10, 3 10, 3 0))");
            Assert.AreEqual(dos[59].geom.GetSqlGeometryAsString(), "POLYGON ((5 7, 10 7, 10 10, 5 10, 5 7))");
            Assert.AreEqual(dos[60].geom.GetSqlGeometryAsString(), "POLYGON ((6 6, 10 6, 10 10, 6 10, 6 6))");
            Assert.AreEqual(dos[61].geom.GetSqlGeometryAsString(), "POLYGON ((7 6, 10 6, 10 10, 7 10, 7 6))");
            Assert.AreEqual(dos[62].geom.GetSqlGeometryAsString(), "POLYGON ((6 7, 10 7, 10 10, 6 10, 6 7))");
            Assert.AreEqual(dos[63].geom.GetSqlGeometryAsString(), "POLYGON ((7 7, 10 7, 10 10, 7 10, 7 7))");
        }


        [TestMethod]
        public void SerieGenerationTest()
        {
            var geo = new GeoLogic();
            var series = geo.GenerateSeries(2, 4, 1);
            Assert.AreEqual(series[0], 2);
            Assert.AreEqual(series[1], 3);
            Assert.AreEqual(series[2], 4);

            series = geo.GenerateSeries(100, 200, 10);
            Assert.AreEqual(series[0], 100);
            Assert.AreEqual(series[1], 110);
            Assert.AreEqual(series[2], 120);
        }

        [TestMethod]
        public void GeomQueryObjectTest()
        {
            var geologic = new GeoLogic();
            var geom = geologic.GenerateQueryObject(geologic.GenerateSampleGeometry_HardCoded());

            Assert.AreEqual(geom.minx.ToString(), "8,25000000000001");
            Assert.AreEqual(geom.miny.ToString(), "8,25000000000001");
            Assert.AreEqual(geom.maxx.ToString(), "11,75");
            Assert.AreEqual(geom.maxy.ToString(), "11,75");
        }

        [TestMethod]
        public void REGULARGRIDXYTest()
        {
            var geologic = new GeoLogic();

            var QueryObject = geologic.GenerateQueryObject(geologic.GenerateSampleGeometry_HardCoded());
            var dimentionObjects = geologic.REGULARGRIDXY(QueryObject.minx.Value, QueryObject.miny.Value, QueryObject.maxx.Value,
                QueryObject.maxy.Value, QueryObject.gridX, QueryObject.gridY, QueryObject.geom.STSrid.Value, QueryObject);
            string[] arr = { "POLYGON", "MULTIPOLYGON" };

            var newDimentionObjects = dimentionObjects.Where(y => arr.Contains(y.geom.STGeometryType().Value.ToUpper()))
                            .Select(x =>
                                {
                                    var tmp =
                                    new DimentionObject
                                    {
                                        mKey = geologic.Morton(x.col - x.GeoQueryObject.loCol, x.row - x.GeoQueryObject.loRow),
                                        col = x.col,
                                        row = x.row,
                                        geom = x.geom
                                    };
                                    if (arr.Contains(QueryObject.geom.STGeometryType().Value.ToUpper()))
                                    {
                                        tmp.geom = QueryObject.geom.STIntersection(x.geom);
                                    }
                                    else
                                        tmp.geom = QueryObject.geom;
                                    tmp.geomStr = tmp.geom.GetSqlGeometryAsString();
                                    return tmp;
                                }
                             )
                            .OrderBy(z => z.mKey);


            var colrowdynamic = from row in newDimentionObjects
                                group row by true into r
                                select new
                                {
                                    mincol = r.Min(z => z.col),
                                    maxcol = r.Max(z => z.col),
                                    minrow = r.Min(z => z.row),
                                    maxrow = r.Max(z => z.row),
                                    minmkey = r.Min(z => z.mKey),
                                    maxmkey = r.Max(z => z.mKey)
                                };
            //boudary check
            Assert.AreEqual(165, colrowdynamic.FirstOrDefault().mincol);
            Assert.AreEqual(235, colrowdynamic.FirstOrDefault().maxcol);

            Assert.AreEqual(165, colrowdynamic.FirstOrDefault().minrow);
            Assert.AreEqual(235, colrowdynamic.FirstOrDefault().maxrow);

            Assert.AreEqual(52603, colrowdynamic.FirstOrDefault().minmkey);
            Assert.AreEqual(63876, colrowdynamic.FirstOrDefault().maxmkey);


            //row check
            var testObj1 = newDimentionObjects.Where(x => x.mKey == 58387).FirstOrDefault();
            Assert.IsNotNull(testObj1);
            Assert.AreEqual(testObj1.col, 165);
            Assert.AreEqual(testObj1.row, 193);
            Assert.AreEqual(testObj1.geomStr, "POLYGON ((8.3000000000000078 9.6898307303336466, 8.3 9.7, 8.2969215727512 9.7000000000000171, 8.3000000000000078 9.6898307303336466))");


            var testObj2 = newDimentionObjects.Where(x => x.mKey == 52603).FirstOrDefault();
            Assert.IsNotNull(testObj2);
            Assert.AreEqual(testObj2.col, 189);
            Assert.AreEqual(testObj2.row, 167);
            Assert.AreEqual(testObj2.geomStr, "POLYGON ((9.5000000000000178 8.384714988852938, 9.5 8.4, 9.47483888956154 8.400000000000011, 9.5000000000000178 8.384714988852938))");


            var testObj3 = newDimentionObjects.Where(x => x.mKey == 52671).FirstOrDefault();
            Assert.IsNotNull(testObj3);
            Assert.AreEqual(testObj3.col, 183);
            Assert.AreEqual(testObj3.row, 175);
            Assert.AreEqual(testObj3.geomStr, "POLYGON ((9.15 8.75, 9.2 8.75, 9.2 8.8, 9.15 8.8, 9.15 8.75))");


            var testObj4 = newDimentionObjects.Where(x => x.mKey == 52925).FirstOrDefault();
            Assert.IsNotNull(testObj4);
            Assert.AreEqual(testObj4.col, 167);
            Assert.AreEqual(testObj4.row, 190);
            Assert.AreEqual(testObj4.geomStr, "POLYGON ((8.38471498885292 9.5000000000000178, 8.4 9.5, 8.4 9.55, 8.3579431022561028 9.5500000000000114, 8.3742999937743754 9.5171444324682426, 8.38471498885292 9.5000000000000178))");
        }
    }
}

