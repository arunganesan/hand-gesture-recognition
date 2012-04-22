using System;
using System.Collections.Generic;
using System.Linq;
using FeatureExtractionLib;
using System.Diagnostics;
namespace ColorGlove {
    /*
    public class DBScanPoint
    {
        public const int NOISE = -1;
        public const int UNCLASSIFIED = 0;
        // doesn't have to 2-dimension
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
     */ 
    public static class DBSCAN
    {
        public static void Test()
        {
            /*
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
            //List<List<DBScanPoint>> clusters = GetClusters(points, eps, minPts);
            // print points to console
            */
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
                reach (DBScanPoint p in clusters[i]) Console.Write(" {0} ", p);
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
        private const int NOISE = -1;
        private const int UNCLASSIFIED = 0;
        // 0 means background, >0 means target labels.
        private static int[] predict_label_;
        // -1 means NOISE, 0 means UNCLASSIFIED
        private static int[] pool_;
        private static int background_label_;
        private static List<List<int>>  clusters = new List<List<int>>();
        private static Queue<int> seeds = new Queue<int>();
        /* 
        // an array to store the label information.
        // 0 means background, >0 means target labels.
        private int [] predict_label_;
        // an array to store additional information for DBSCAN 
        // -1 means NOISE, -2 means UNCLASSIFIED 
        private int[] addtion_info_;
        private int background_label_;
        // Construct function
        public DBSCAN(int[] predict_label, int background_label)
        {
            predict_label_ = predict_label;
            background_label_ = background_label;
        }
        
         */ 
        // Get Clusters
        // input to GetCluster is a 1-dim array of predicted label, with the label of background
        // predict_label will be modified. 0 means background, >0 means target labels, -1 means NOISE, -2 means UNCLASSIFIED
        // output is a list of clusters. List[i] represents cluster i, ...  
        public static List<List<int>> GetClusters(double eps, int minPts, int [] predict_label, int background_label, int [] pool)
        {
            if (predict_label == null) return null;
            predict_label_ = predict_label;
            pool_ = pool;
            background_label_ = background_label;
            // treat every pixel as unclassified
            Array.Clear(pool_, 0, pool.Length);
            // a data structure to store cluster
            //eps *= eps; // square eps
            // cluster starts from 1 
            clusters.Clear();
            int clusterId = 0;
            clusters.Add(new List<int>());
            
          
            Debug.WriteLine("DBScan starts!");

            for (int i = 0; i < predict_label.Length; i++)
                if (predict_label_[i]!= background_label)
                {
                   if (pool_[i] == UNCLASSIFIED)
                    {
                        if (ExpandCluster(i, clusterId, eps, minPts))
                        {
                            clusterId++;
                            clusters.Add(new List<int>());
                        }
                    }
                }
            /*
            // sort out points into their clusters, if any
            // is it expensive
            // TODO
            int maxClusterId = points.OrderBy(p => p.ClusterId).Last().ClusterId;
            if (maxClusterId < 1) return clusters; // no clusters, so list is empty
            for (int i = 0; i < maxClusterId; i++) clusters.Add(new List<DBScanPoint>());
            foreach (DBScanPoint p in points)
            {
                if (p.ClusterId > 0) clusters[p.ClusterId - 1].Add(p);
            }
            return clusters;
            // ENDTODO
             */
            return clusters;
        }
       
        static List<int> GetRegion(int p_index, double eps)
        {
            // maybe can be improved by preallocation
            List<int> region =new List<int>();
           
            int radius = (int) (eps);
            System.Drawing.Point p = Util.toXY(p_index, 640, 480, 1);
            for (int new_x = p.X - radius; new_x <= p.X + radius; new_x++)
                for (int new_y = p.Y - radius; new_y <= p.Y + radius; new_y++)
                {
                    if (new_x >= 0 && new_x < 640 && new_y >= 0 && new_y < 480)
                    {
                        int one_dim_index = Util.toID( new_x, new_y, 640, 480, 1);
                        if (p_index != one_dim_index)
                            if (predict_label_[one_dim_index] != background_label_)
                             region.Add(one_dim_index);
                    }
                }
            return region;
        }
        
        // Test if the current unvisited point has enough density. If yes, make a new cluster by bread-firth searching
        static bool ExpandCluster( int index, int clusterId, double eps, int minPts)
        {
            // used to store all interested points
            seeds.Clear();
            // get all neighbors that are within eps distance of i
            List<int> p_neighbor = GetRegion(index, eps);
            // Current index is gauranteed to be a non-background point
            if (p_neighbor.Count < minPts) // no core point
            {
                pool_[index] =  NOISE;
                return false;
            }
            else // all points in seeds are density reachable from point 'p'
            {
                clusters[clusterId].Add(index);
                for (int i = 0; i < p_neighbor.Count; i++) {
                    // mark p_neighbor[i] belonged to a cluster
                    pool_[p_neighbor[i]] = clusterId;
                    seeds.Enqueue(p_neighbor[i]);
                    clusters[clusterId].Add(p_neighbor[i]);
                } 
                // Note: seeds shouldn't contain the current index any more
                while (seeds.Count > 0)
                {
                    int currentP = seeds.Dequeue();
                    // currentP is gaurantted to be a labeled point and belongs to the clusterid
                    List<int> result = GetRegion(currentP, eps);
                    if (result.Count >= minPts)
                    {
                        for (int i = 0; i < result.Count; i++)
                        {
                            int  resultP = result[i];
                            // resultP is gaurantted to be a non-background point
                            // it can however belongs to a already existed cluster or noise
                            Debug.Assert(predict_label_[resultP] > 0);
                            if (pool_[resultP] == UNCLASSIFIED)
                            {
                                seeds.Enqueue(resultP);
                                pool_[resultP] = clusterId;
                                clusters[clusterId].Add(resultP);
                            }
                        }
                    }
                    //seeds.Dequeue();
                }
                return true;
            }
        }
    }
}