using System;
using System.Collections.Generic;
using System.Data.Entity.Spatial;
using System.Linq;
using System.Web;
using Microsoft.SqlServer.Types;
using System.Data.SqlTypes;
using System.Transactions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MortonGeo
{
    public class DimentionObject
    {
        [Key]
        public int DimentionObjectId { get; set; }
        public int mKey { get; set; }
        public int col { get; set; }
        public int row { get; set; }
        public int MortonKey { get; set; }
        [NotMapped]
        public SqlGeometry geom { get; set; }
        public DbGeometry dbgeom { get; set; }
        public string geomStr { get; set; }

        public int OrderInCollection { get; set; }
        [NotMapped]
        public DimentionObject Parent { get; set; }

        [NotMapped]
        public List<DimentionObject> Children { get; set; }
        [NotMapped]
        public List<DimentionObject> Neighbours { get; set; }

        [NotMapped]
        public GeoQueryObject GeoQueryObject { get; set; }



    }


    public class GeoQueryObject
    {
        public SqlDouble minx { get; set; }
        public SqlDouble miny { get; set; }
        public SqlDouble maxx { get; set; }
        public SqlDouble maxy { get; set; }
        public SqlGeometry geom { get; set; }
        public double gridX = 0.050;
        public double gridY = 0.050;
        public int loCol = 0;
        public int loRow = 0;
    }

    public class GeoLogic
    {
        public SqlGeometry MBR2GEOMETRY(double p_minx, double p_miny, double p_maxx, double p_maxy, int p_srid)
        {
            var polygongStr = p_minx.GetCordinateString() + " " + p_miny.GetCordinateString() + "," +
                p_maxx.GetCordinateString() + " " + p_miny.GetCordinateString() + " , " +
                p_maxx.GetCordinateString() + " " + p_maxy.GetCordinateString() + " , " +
                p_minx.GetCordinateString() + " " + p_maxy.GetCordinateString() + " , " +
                p_minx.GetCordinateString() + " " + p_miny.GetCordinateString();

            polygongStr = string.Format("POLYGON(({0}))", polygongStr);
            var square = SqlGeometry.STGeomFromText(new SqlChars(polygongStr), p_srid);
            return square;
        }

        public int Morton(int p_col, int p_row)
        {
            int row = Math.Abs(p_row);
            int col = Math.Abs(p_col);
            int key = 0;
            int level = 0;
            int left_bit;
            int right_bit;
            int quadrant;

            while ((row > 0) || (col > 0))
            {

                /* Split off the row (left_bit) and column (right_bit) bits and
                   then combine them to form a bit-pair representing the
                   quadrant
                */
                left_bit = row % 2;
                right_bit = col % 2;
                quadrant = right_bit + 2 * left_bit;

                key = key + Convert.ToInt32(Math.Round(quadrant * Math.Pow(2, 2 * level), 0, MidpointRounding.AwayFromZero));
                /*   row, column, and level are then modified before the loop
                     continues                                                */
                if (row == 1 && col == 1)
                {
                    row = 0;
                    col = 0;
                }
                else
                {
                    row = row / 2;
                    col = col / 2;
                    level = level + 1;
                }



            }

            return key;


        }


        public List<int> GenerateSeries(int p_start, int p_end, int p_step = 1)
        {
            int v_i = p_start;
            int v_step = p_step;
            int v_terminating_value = p_start + Convert.ToInt32(Math.Abs(@p_start - @p_end) / Math.Abs(@v_step)) * @v_step;
            List<int> MyValues = new List<int>();

            // Check for impossible combinations
            if (!(p_start > p_end && Math.Sign(p_step) == 1)
                     &&
                     !(p_start < p_end && Math.Sign(p_step) == -1))
            {
                // Generate values
                while (true)
                {
                    MyValues.Add(v_i);
                    if (v_i == v_terminating_value)
                        break;
                    v_i = v_i + v_step;
                }
            }
            return MyValues;
        }


        /// <summary>
        /// we have an geometry sent in by the developer, one minimum-size rectangle contaning thie geometry
        /// divide the rectangle into sqaures in size of 0.05*0.05, if the squre overlaps with the geometry
        /// add the square together with row number and column number to a list
        /// </summary>
        /// <param name="p_ll_x"></param>
        /// <param name="p_ll_y"></param>
        /// <param name="p_ur_x"></param>
        /// <param name="p_ur_y"></param>
        /// <param name="p_TileSize_X"></param>
        /// <param name="p_TileSize_Y"></param>
        /// <param name="p_srid"></param>
        /// <param name="GeoQueryObject"></param>
        /// <returns></returns>
        public List<DimentionObject> REGULARGRIDXY(double p_ll_x, double p_ll_y, double p_ur_x, double p_ur_y,
            double p_TileSize_X, double p_TileSize_Y, int p_srid, GeoQueryObject GeoQueryObject)
        {
            List<DimentionObject> dos = new List<DimentionObject>();
            int v_loCol = Convert.ToInt32(Math.Floor(p_ll_x / p_TileSize_X));
            int v_hiCol = Convert.ToInt32(Math.Ceiling(p_ur_x / p_TileSize_X) - 1);
            int v_loRow = Convert.ToInt32(Math.Floor(p_ll_y / p_TileSize_Y));
            int v_hiRow = Convert.ToInt32(Math.Ceiling(p_ur_y / p_TileSize_Y) - 1);
            int v_col = v_loCol;


            while (v_col <= v_hiCol)
            {
                var v_row = v_loRow;
                while (v_row <= v_hiRow)
                {
                    //STR ( float_expression [ , length [ , decimal ] ] )
                    var polygongStr = (v_col * p_TileSize_X).GetDecimalAsStringAtFixedLength(12) + ' '
                        + (v_row * p_TileSize_Y).GetDecimalAsStringAtFixedLength(12) + ','
                        + ((v_col * p_TileSize_X) + p_TileSize_X).GetDecimalAsStringAtFixedLength(12) + ' '
                        + (v_row * p_TileSize_Y).GetDecimalAsStringAtFixedLength(12) + ','
                        + ((v_col * p_TileSize_X) + p_TileSize_X).GetDecimalAsStringAtFixedLength(12) + ' '
                        + ((v_row * p_TileSize_Y) + p_TileSize_Y).GetDecimalAsStringAtFixedLength(12) + ','
                        + (v_col * p_TileSize_X).GetDecimalAsStringAtFixedLength(12) + ' '
                        + ((v_row * p_TileSize_Y) + p_TileSize_Y).GetDecimalAsStringAtFixedLength(12) + ','
                        + (v_col * p_TileSize_X).GetDecimalAsStringAtFixedLength(12) + ' '
                        + (v_row * p_TileSize_Y).GetDecimalAsStringAtFixedLength(12);
                    polygongStr = string.Format("POLYGON(({0}))", polygongStr);


                    var dimentionObject = new DimentionObject { col = v_col, row = v_row, geom = SqlGeometry.STGeomFromText(new SqlChars(polygongStr), p_srid) };

                    dimentionObject.GeoQueryObject = GeoQueryObject;
                    if (GeoQueryObject.geom.STIntersects(dimentionObject.geom))
                        dos.Add(dimentionObject);

                    v_row = v_row + 1;
                }
                v_col = v_col + 1;
            }
            return dos;
        }


        /// <summary>
        /// hard code a geometry, same value as shown in the blog post
        /// </summary>
        /// <returns></returns>
        public SqlGeometry GenerateSampleGeometry_HardCoded()
        {
            var geo1 = SqlGeometry.STGeomFromText(new SqlChars("MULTIPOINT((09.25 10.00),(10.75 10.00),(10.00 10.75),(10.00 9.25))"), 0);
            var geo2 = geo1.STBuffer(1.000).STSymDifference(geo1.STBuffer(0.500));
            return geo2;

        }


        /// <summary>
        /// 
        /// create an wrapper object, containing the following data
        /// 1. geometry object -geoQueryObject
        /// the geo1 and geo2 functions are just used to populate a test geometry, should be replaced by developer
        /// 2. the four corner cordinates for the minimum axis-aligned bounding rectangle of the geometry object
        /// https://msdn.microsoft.com/en-us/library/bb933896.aspx
        /// minx, miny, maxx, maxy
        /// 3. size of the grid - gridX and gridYk
        /// </summary>
        /// <param name="geostr"></param>
        /// <returns></returns>
        public GeoQueryObject GenerateQueryObject(SqlGeometry geom)
        {
            var geoQueryObject = new GeoQueryObject { };
            geoQueryObject.minx = geom.STEnvelope().STPointN(1).STX;
            geoQueryObject.miny = geom.STEnvelope().STPointN(1).STY;
            geoQueryObject.maxx = geom.STEnvelope().STPointN(3).STX;
            geoQueryObject.maxy = geom.STEnvelope().STPointN(3).STY;
            geoQueryObject.geom = geom;
            geoQueryObject.gridX = 0.050;
            geoQueryObject.gridY = 0.050;
            geoQueryObject.loCol = 0;
            geoQueryObject.loRow = 0;
            return geoQueryObject;

        }


        /// <summary>
        /// comebine all the functions, generate test data in database, to view the spatial results
        /// </summary>
        public void GenerateTestData()
        {
            var QueryObject = GenerateQueryObject(GenerateSampleGeometry_HardCoded());
            var dimentionObjects = REGULARGRIDXY(QueryObject.minx.Value, QueryObject.miny.Value, QueryObject.maxx.Value,
                QueryObject.maxy.Value, QueryObject.gridX, QueryObject.gridY, QueryObject.geom.STSrid.Value, QueryObject);

            string[] arr = { "POLYGON", "MULTIPOLYGON" };


            var newDimentionObjects = dimentionObjects.Where(y => arr.Contains(y.geom.STGeometryType().Value.ToUpper()))
                            .Select(x =>
                            {
                                var tmp =
                                new DimentionObject
                                {
                                    mKey = Morton(x.col - x.GeoQueryObject.loCol, x.row - x.GeoQueryObject.loRow),
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


                                tmp.dbgeom = DbGeometry.FromBinary(tmp.geom.STAsBinary().Buffer);
                                tmp.geomStr = tmp.geom.GetSqlGeometryAsString();
                                return tmp;
                            }
                             )
                            .OrderBy(z => z.mKey);

            //replace by your own code, save the geom data to database

            //using (TransactionScope scope = new TransactionScope())
            //{
            //    var unit = new UnitOfWork();

            //    try
            //    {
            //        unit.getContext().Configuration.AutoDetectChangesEnabled = false;
            //        unit.getContext().Configuration.LazyLoadingEnabled = false;

            //        int count = 0;
            //        foreach (var dimentionObjectToInsert in newDimentionObjects)
            //        {
            //            ++count;
            //            unit.DimentionObjectRepository.InsertAndStageAutoCommit(dimentionObjectToInsert, count, 100, true, unit);
            //        }
            //        unit.Save();
            //    }
            //    catch (Exception e)
            //    {
            //        //do something
            //    }
            //    finally
            //    {
            //        unit.Dispose();
            //    }

            //    scope.Complete();
            //}
        }
    }
}
