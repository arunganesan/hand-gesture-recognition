using System;
using System.Collections.Generic;
using System.Linq;

namespace ColorGlove {
    public class DBScanPoint
    {
        public const int NOISE = -1;
        public const int UNCLASSIFIED = 0;
        public int X, Y, ClusterId;
        public DBScanPoint(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
        public override string ToString()
        {
            return String.Format("({0}, {1})", X, Y);
        }
        public static int DistanceSquared(DBScanPoint p1, DBScanPoint p2)
        {
            int diffX = p2.X - p1.X;
            int diffY = p2.Y - p1.Y;
            return diffX * diffX + diffY * diffY;
        }
    }
    static class DBSCAN
    {
        public static void Test()
        {
            List<DBScanPoint> points = new List<DBScanPoint>();
            // sample data
            points.Add(new DBScanPoint(0, 100));
            points.Add(new DBScanPoint(0, 200));
            points.Add(new DBScanPoint(0, 275));
            points.Add(new DBScanPoint(100, 150));
            points.Add(new DBScanPoint(200, 100));
            points.Add(new DBScanPoint(250, 200));
            points.Add(new DBScanPoint(0, 300));
            points.Add(new DBScanPoint(100, 200));
            points.Add(new DBScanPoint(600, 700));
            points.Add(new DBScanPoint(650, 700));
            points.Add(new DBScanPoint(675, 700));
            points.Add(new DBScanPoint(675, 710));
            points.Add(new DBScanPoint(675, 720));
            points.Add(new DBScanPoint(50, 400));
            double eps = 100.0;
            int minPts = 3;
            List<List<DBScanPoint>> clusters = GetClusters(points, eps, minPts);
            // print points to console
            
            /*
            Console.WriteLine("The {0} points are :\n", points.Count);
            foreach (DBScanPoint p in points) Console.Write(" {0} ", p);
            Console.WriteLine();
            // print clusters to console
            int total = 0;
            for (int i = 0; i < clusters.Count; i++)
            {
                int count = clusters[i].Count;
                total += count;
                string plural = (count != 1) ? "s" : "";
                Console.WriteLine("\nCluster {0} consists of the following {1} point{2} :\n", i + 1, count, plural);
                foreach (DBScanPoint p in clusters[i]) Console.Write(" {0} ", p);
                Console.WriteLine();
            }
            // print any points which are NOISE
            total = points.Count - total;
            if (total > 0)
            {
                string plural = (total != 1) ? "s" : "";
                string verb = (total != 1) ? "are" : "is";
                Console.WriteLine("\nThe following {0} point{1} {2} NOISE :\n", total, plural, verb);
                foreach (DBScanPoint p in points)
                {
                    if (p.ClusterId == DBScanPoint.NOISE) Console.Write(" {0} ", p);
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("\nNo points are NOISE");
            }
            */
        }
        static List<List<DBScanPoint>> GetClusters(List<DBScanPoint> points, double eps, int minPts)
        {
            if (points == null) return null;
            List<List<DBScanPoint>> clusters = new List<List<DBScanPoint>>();
            eps *= eps; // square eps
            int clusterId = 1;
            for (int i = 0; i < points.Count; i++)
            {
                DBScanPoint p = points[i];
                if (p.ClusterId == DBScanPoint.UNCLASSIFIED)
                {
                    if (ExpandCluster(points, p, clusterId, eps, minPts)) clusterId++;
                }
            }
            // sort out points into their clusters, if any
            int maxClusterId = points.OrderBy(p => p.ClusterId).Last().ClusterId;
            if (maxClusterId < 1) return clusters; // no clusters, so list is empty
            for (int i = 0; i < maxClusterId; i++) clusters.Add(new List<DBScanPoint>());
            foreach (DBScanPoint p in points)
            {
                if (p.ClusterId > 0) clusters[p.ClusterId - 1].Add(p);
            }
            return clusters;
        }

        static List<DBScanPoint> GetRegion(List<DBScanPoint> points, DBScanPoint p, double eps)
        {
            List<DBScanPoint> region = new List<DBScanPoint>();
            for (int i = 0; i < points.Count; i++)
            {
                int distSquared = DBScanPoint.DistanceSquared(p, points[i]);
                if (distSquared <= eps) region.Add(points[i]);
            }
            return region;
        }
        static bool ExpandCluster(List<DBScanPoint> points, DBScanPoint p, int clusterId, double eps, int minPts)
        {
            List<DBScanPoint> seeds = GetRegion(points, p, eps);
            if (seeds.Count < minPts) // no core point
            {
                p.ClusterId = DBScanPoint.NOISE;
                return false;
            }
            else // all points in seeds are density reachable from point 'p'
            {
                for (int i = 0; i < seeds.Count; i++) seeds[i].ClusterId = clusterId;
                seeds.Remove(p);
                while (seeds.Count > 0)
                {
                    DBScanPoint currentP = seeds[0];
                    List<DBScanPoint> result = GetRegion(points, currentP, eps);
                    if (result.Count >= minPts)
                    {
                        for (int i = 0; i < result.Count; i++)
                        {
                            DBScanPoint resultP = result[i];
                            if (resultP.ClusterId == DBScanPoint.UNCLASSIFIED || resultP.ClusterId == DBScanPoint.NOISE)
                            {
                                if (resultP.ClusterId == DBScanPoint.UNCLASSIFIED) seeds.Add(resultP);
                                resultP.ClusterId = clusterId;
                            }
                        }
                    }
                    seeds.Remove(currentP);
                }
                return true;
            }
        }
    }
}