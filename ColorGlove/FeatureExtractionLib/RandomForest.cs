using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace FeatureExtractionLib
{    
    public class dforest
    {
        public class decisionforest
        {
            public int nvars;
            public int nclasses;
            public int ntrees;
            public int bufsize;
            public double[] trees;
            public decisionforest()
            {
                trees = new double[0];
            }
        };


        public class dfreport
        {
            public double relclserror;
            public double avgce;
            public double rmserror;
            public double avgerror;
            public double avgrelerror;
            public double oobrelclserror;
            public double oobavgce;
            public double oobrmserror;
            public double oobavgerror;
            public double oobavgrelerror;
        };


        public class dfinternalbuffers
        {
            public double[] treebuf;
            public int[] idxbuf;
            public double[] tmpbufr;
            public double[] tmpbufr2;
            public int[] tmpbufi;
            public int[] classibuf;
            public double[] sortrbuf;
            public double[] sortrbuf2;
            public int[] sortibuf;
            public int[] varpool;
            public bool[] evsbin;
            public double[] evssplits;
            public dfinternalbuffers()
            {
                treebuf = new double[0];
                idxbuf = new int[0];
                tmpbufr = new double[0];
                tmpbufr2 = new double[0];
                tmpbufi = new int[0];
                classibuf = new int[0];
                sortrbuf = new double[0];
                sortrbuf2 = new double[0];
                sortibuf = new int[0];
                varpool = new int[0];
                evsbin = new bool[0];
                evssplits = new double[0];
            }
        };



        // Const -Michael
        public const int innernodewidth = 3; // -Michael takes note
        public const int leafnodewidth = 2;
        public const int dfusestrongsplits = 1;
        public const int dfuseevs = 2;
        public const int dffirstversion = 0;


        /*************************************************************************
        This subroutine builds random decision forest.

        INPUT PARAMETERS:
            XY          -   training set
            NPoints     -   training set size, NPoints>=1
            NVars       -   number of independent variables, NVars>=1
            NClasses    -   task type:
                            * NClasses=1 - regression task with one
                                           dependent variable
                            * NClasses>1 - classification task with
                                           NClasses classes.
            NTrees      -   number of trees in a forest, NTrees>=1.
                            recommended values: 50-100.
            R           -   percent of a training set used to build
                            individual trees. 0<R<=1.
                            recommended values: 0.1 <= R <= 0.66.

        OUTPUT PARAMETERS:
            Info        -   return code:
                            * -2, if there is a point with class number
                                  outside of [0..NClasses-1].
                            * -1, if incorrect parameters was passed
                                  (NPoints<1, NVars<1, NClasses<1, NTrees<1, R<=0
                                  or R>1).
                            *  1, if task has been solved
            DF          -   model built
            Rep         -   training report, contains error on a training set
                            and out-of-bag estimates of generalization error.

          -- ALGLIB --
             Copyright 19.02.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void dfbuildrandomdecisionforest(double[,] xy,
            int npoints,
            int nvars,
            int nclasses,
            int ntrees,
            double r,
            ref int info,
            decisionforest df,
            dfreport rep)
        {
            int samplesize = 0;

            info = 0;

            if ((double)(r) <= (double)(0) | (double)(r) > (double)(1))
            {
                info = -1;
                return;
            }
            samplesize = Math.Max((int)Math.Round(r * npoints), 1);
            dfbuildinternal(xy, npoints, nvars, nclasses, ntrees, samplesize, Math.Max(nvars / 2, 1), dfusestrongsplits + dfuseevs, ref info, df, rep);
        }


        /*************************************************************************
        This subroutine builds random decision forest.
        This function gives ability to tune number of variables used when choosing
        best split.

        INPUT PARAMETERS:
            XY          -   training set
            NPoints     -   training set size, NPoints>=1
            NVars       -   number of independent variables, NVars>=1
            NClasses    -   task type:
                            * NClasses=1 - regression task with one
                                           dependent variable
                            * NClasses>1 - classification task with
                                           NClasses classes.
            NTrees      -   number of trees in a forest, NTrees>=1.
                            recommended values: 50-100.
            NRndVars    -   number of variables used when choosing best split
            R           -   percent of a training set used to build
                            individual trees. 0<R<=1.
                            recommended values: 0.1 <= R <= 0.66.

        OUTPUT PARAMETERS:
            Info        -   return code:
                            * -2, if there is a point with class number
                                  outside of [0..NClasses-1].
                            * -1, if incorrect parameters was passed
                                  (NPoints<1, NVars<1, NClasses<1, NTrees<1, R<=0
                                  or R>1).
                            *  1, if task has been solved
            DF          -   model built
            Rep         -   training report, contains error on a training set
                            and out-of-bag estimates of generalization error.

          -- ALGLIB --
             Copyright 19.02.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void dfbuildrandomdecisionforestx1(double[,] xy,
            int npoints,
            int nvars,
            int nclasses,
            int ntrees,
            int nrndvars,
            double r,
            ref int info,
            decisionforest df,
            dfreport rep)
        {
            int samplesize = 0;

            info = 0;

            if ((double)(r) <= (double)(0) | (double)(r) > (double)(1))
            {
                info = -1;
                return;
            }
            if (nrndvars <= 0 | nrndvars > nvars)
            {
                info = -1;
                return;
            }
            samplesize = Math.Max((int)Math.Round(r * npoints), 1);
            dfbuildinternal(xy, npoints, nvars, nclasses, ntrees, samplesize, nrndvars, dfusestrongsplits + dfuseevs, ref info, df, rep);
        }


        public static void dfbuildinternal(double[,] xy,
            int npoints,
            int nvars,
            int nclasses,
            int ntrees,
            int samplesize,
            int nfeatures,
            int flags,
            ref int info,
            decisionforest df,
            dfreport rep)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            int tmpi = 0;
            int lasttreeoffs = 0;
            int offs = 0;
            int ooboffs = 0;
            int treesize = 0;
            int nvarsinpool = 0;
            bool useevs = new bool();
            dfinternalbuffers bufs = new dfinternalbuffers();
            int[] permbuf = new int[0];
            double[] oobbuf = new double[0];
            int[] oobcntbuf = new int[0];
            double[,] xys = new double[0, 0];
            double[] x = new double[0];
            double[] y = new double[0];
            int oobcnt = 0;
            int oobrelcnt = 0;
            double v = 0;
            double vmin = 0;
            double vmax = 0;
            bool bflag = new bool();
            int i_ = 0;
            int i1_ = 0;

            info = 0;


            //
            // Test for inputs
            //
            if ((((((npoints < 1 | samplesize < 1) | samplesize > npoints) | nvars < 1) | nclasses < 1) | ntrees < 1) | nfeatures < 1)
            {
                info = -1;
                return;
            }
            if (nclasses > 1)
            {
                for (i = 0; i <= npoints - 1; i++)
                {
                    if ((int)Math.Round(xy[i, nvars]) < 0 | (int)Math.Round(xy[i, nvars]) >= nclasses)
                    {
                        info = -2;
                        return;
                    }
                }
            }
            info = 1;

            //
            // Flags
            //
            useevs = flags / dfuseevs % 2 != 0;

            //
            // Allocate data, prepare header
            //
            treesize = 1 + innernodewidth * (samplesize - 1) + leafnodewidth * samplesize; // -Michael takes note: treesize= 5*samplesize - 2 = 5* r * Np -2. 
            permbuf = new int[npoints - 1 + 1];
            bufs.treebuf = new double[treesize - 1 + 1];
            bufs.idxbuf = new int[npoints - 1 + 1];
            bufs.tmpbufr = new double[npoints - 1 + 1];
            bufs.tmpbufr2 = new double[npoints - 1 + 1];
            bufs.tmpbufi = new int[npoints - 1 + 1];
            bufs.sortrbuf = new double[npoints];
            bufs.sortrbuf2 = new double[npoints];
            bufs.sortibuf = new int[npoints];
            bufs.varpool = new int[nvars - 1 + 1];
            bufs.evsbin = new bool[nvars - 1 + 1];
            bufs.evssplits = new double[nvars - 1 + 1];
            bufs.classibuf = new int[2 * nclasses - 1 + 1];
            oobbuf = new double[nclasses * npoints - 1 + 1];
            oobcntbuf = new int[npoints - 1 + 1];
            df.trees = new double[ntrees * treesize - 1 + 1];
            xys = new double[samplesize - 1 + 1, nvars + 1];
            x = new double[nvars - 1 + 1];
            y = new double[nclasses - 1 + 1];
            for (i = 0; i <= npoints - 1; i++)
            {
                permbuf[i] = i;
            }
            for (i = 0; i <= npoints * nclasses - 1; i++)
            {
                oobbuf[i] = 0;
            }
            for (i = 0; i <= npoints - 1; i++)
            {
                oobcntbuf[i] = 0;
            }

            //
            // Prepare variable pool and EVS (extended variable selection/splitting) buffers
            // (whether EVS is turned on or not):
            // 1. detect binary variables and pre-calculate splits for them
            // 2. detect variables with non-distinct values and exclude them from pool
            //
            for (i = 0; i <= nvars - 1; i++)
            {
                bufs.varpool[i] = i;
            }
            nvarsinpool = nvars;
            if (useevs)
            {
                for (j = 0; j <= nvars - 1; j++)
                {
                    vmin = xy[0, j];
                    vmax = vmin;
                    for (i = 0; i <= npoints - 1; i++)
                    {
                        v = xy[i, j];
                        vmin = Math.Min(vmin, v);
                        vmax = Math.Max(vmax, v);
                    }
                    if ((double)(vmin) == (double)(vmax))
                    {

                        //
                        // exclude variable from pool
                        //
                        bufs.varpool[j] = bufs.varpool[nvarsinpool - 1];
                        bufs.varpool[nvarsinpool - 1] = -1;
                        nvarsinpool = nvarsinpool - 1;
                        continue;
                    }
                    bflag = false;
                    for (i = 0; i <= npoints - 1; i++)
                    {
                        v = xy[i, j];
                        if ((double)(v) != (double)(vmin) & (double)(v) != (double)(vmax))
                        {
                            bflag = true;
                            break;
                        }
                    }
                    if (bflag)
                    {

                        //
                        // non-binary variable
                        //
                        bufs.evsbin[j] = false;
                    }
                    else
                    {

                        //
                        // Prepare
                        //
                        bufs.evsbin[j] = true;
                        bufs.evssplits[j] = 0.5 * (vmin + vmax);
                        if ((double)(bufs.evssplits[j]) <= (double)(vmin))
                        {
                            bufs.evssplits[j] = vmax;
                        }
                    }
                }
            }

            //
            // RANDOM FOREST FORMAT
            // W[0]         -   size of array
            // W[1]         -   version number
            // W[2]         -   NVars
            // W[3]         -   NClasses (1 for regression)
            // W[4]         -   NTrees
            // W[5]         -   trees offset
            //
            //
            // TREE FORMAT (Very important-Michael)
            // W[Offs]      -   size of sub-array
            //     node info:
            // W[K+0]       -   variable number        (-1 for leaf mode)
            // W[K+1]       -   threshold              (class/value for leaf node)
            // W[K+2]       -   ">=" branch index      (absent for leaf node)
            //
            //
            df.nvars = nvars;
            df.nclasses = nclasses;
            df.ntrees = ntrees;

            //
            // Build forest
            //
            offs = 0;
            for (i = 0; i <= ntrees - 1; i++)
            {

                //
                // Prepare sample
                //
                for (k = 0; k <= samplesize - 1; k++)
                {
                    j = k + alglib.math.randominteger(npoints - k);
                    tmpi = permbuf[k];
                    permbuf[k] = permbuf[j];
                    permbuf[j] = tmpi;
                    j = permbuf[k];
                    for (i_ = 0; i_ <= nvars; i_++)
                    {
                        xys[k, i_] = xy[j, i_];
                    }
                }

                //
                // build tree, copy
                //
                dfbuildtree(xys, samplesize, nvars, nclasses, nfeatures, nvarsinpool, flags, bufs);
                j = (int)Math.Round(bufs.treebuf[0]);
                i1_ = (0) - (offs);
                for (i_ = offs; i_ <= offs + j - 1; i_++)
                {
                    df.trees[i_] = bufs.treebuf[i_ + i1_];
                }
                lasttreeoffs = offs;
                offs = offs + j;

                //
                // OOB estimates
                //
                for (k = samplesize; k <= npoints - 1; k++)
                {
                    for (j = 0; j <= nclasses - 1; j++)
                    {
                        y[j] = 0;
                    }
                    j = permbuf[k];
                    for (i_ = 0; i_ <= nvars - 1; i_++)
                    {
                        x[i_] = xy[j, i_];
                    }
                    dfprocessinternal(df, lasttreeoffs, x, ref y);
                    i1_ = (0) - (j * nclasses);
                    for (i_ = j * nclasses; i_ <= (j + 1) * nclasses - 1; i_++)
                    {
                        oobbuf[i_] = oobbuf[i_] + y[i_ + i1_];
                    }
                    oobcntbuf[j] = oobcntbuf[j] + 1;
                }
            }
            df.bufsize = offs;

            //
            // Normalize OOB results
            //
            for (i = 0; i <= npoints - 1; i++)
            {
                if (oobcntbuf[i] != 0)
                {
                    v = (double)1 / (double)oobcntbuf[i];
                    for (i_ = i * nclasses; i_ <= i * nclasses + nclasses - 1; i_++)
                    {
                        oobbuf[i_] = v * oobbuf[i_];
                    }
                }
            }

            //
            // Calculate training set estimates
            //
            rep.relclserror = dfrelclserror(df, xy, npoints);
            rep.avgce = dfavgce(df, xy, npoints);
            rep.rmserror = dfrmserror(df, xy, npoints);
            rep.avgerror = dfavgerror(df, xy, npoints);
            rep.avgrelerror = dfavgrelerror(df, xy, npoints);

            //
            // Calculate OOB estimates.
            //
            rep.oobrelclserror = 0;
            rep.oobavgce = 0;
            rep.oobrmserror = 0;
            rep.oobavgerror = 0;
            rep.oobavgrelerror = 0;
            oobcnt = 0;
            oobrelcnt = 0;
            for (i = 0; i <= npoints - 1; i++)
            {
                if (oobcntbuf[i] != 0)
                {
                    ooboffs = i * nclasses;
                    if (nclasses > 1)
                    {

                        //
                        // classification-specific code
                        //
                        k = (int)Math.Round(xy[i, nvars]);
                        tmpi = 0;
                        for (j = 1; j <= nclasses - 1; j++)
                        {
                            if ((double)(oobbuf[ooboffs + j]) > (double)(oobbuf[ooboffs + tmpi]))
                            {
                                tmpi = j;
                            }
                        }
                        if (tmpi != k)
                        {
                            rep.oobrelclserror = rep.oobrelclserror + 1;
                        }
                        if ((double)(oobbuf[ooboffs + k]) != (double)(0))
                        {
                            rep.oobavgce = rep.oobavgce - Math.Log(oobbuf[ooboffs + k]);
                        }
                        else
                        {
                            rep.oobavgce = rep.oobavgce - Math.Log(alglib.math.minrealnumber);
                        }
                        for (j = 0; j <= nclasses - 1; j++)
                        {
                            if (j == k)
                            {
                                rep.oobrmserror = rep.oobrmserror + alglib.math.sqr(oobbuf[ooboffs + j] - 1);
                                rep.oobavgerror = rep.oobavgerror + Math.Abs(oobbuf[ooboffs + j] - 1);
                                rep.oobavgrelerror = rep.oobavgrelerror + Math.Abs(oobbuf[ooboffs + j] - 1);
                                oobrelcnt = oobrelcnt + 1;
                            }
                            else
                            {
                                rep.oobrmserror = rep.oobrmserror + alglib.math.sqr(oobbuf[ooboffs + j]);
                                rep.oobavgerror = rep.oobavgerror + Math.Abs(oobbuf[ooboffs + j]);
                            }
                        }
                    }
                    else
                    {

                        //
                        // regression-specific code
                        //
                        rep.oobrmserror = rep.oobrmserror + alglib.math.sqr(oobbuf[ooboffs] - xy[i, nvars]);
                        rep.oobavgerror = rep.oobavgerror + Math.Abs(oobbuf[ooboffs] - xy[i, nvars]);
                        if ((double)(xy[i, nvars]) != (double)(0))
                        {
                            rep.oobavgrelerror = rep.oobavgrelerror + Math.Abs((oobbuf[ooboffs] - xy[i, nvars]) / xy[i, nvars]);
                            oobrelcnt = oobrelcnt + 1;
                        }
                    }

                    //
                    // update OOB estimates count.
                    //
                    oobcnt = oobcnt + 1;
                }
            }
            if (oobcnt > 0)
            {
                rep.oobrelclserror = rep.oobrelclserror / oobcnt;
                rep.oobavgce = rep.oobavgce / oobcnt;
                rep.oobrmserror = Math.Sqrt(rep.oobrmserror / (oobcnt * nclasses));
                rep.oobavgerror = rep.oobavgerror / (oobcnt * nclasses);
                if (oobrelcnt > 0)
                {
                    rep.oobavgrelerror = rep.oobavgrelerror / oobrelcnt;
                }
            }
        }


        /*************************************************************************
        Procesing

        INPUT PARAMETERS:
            DF      -   decision forest model
            X       -   input vector,  array[0..NVars-1].

        OUTPUT PARAMETERS:
            Y       -   result. Regression estimate when solving regression  task,
                        vector of posterior probabilities for classification task.

        See also DFProcessI.

          -- ALGLIB --
             Copyright 16.02.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void dfprocess(decisionforest df,
            double[] x,
            ref double[] y)
        {
            int offs = 0;
            int i = 0;
            double v = 0;
            int i_ = 0;


            //
            // Proceed
            //
            //if (alglib.ap.len(y) < df.nclasses)
            if (y.Length < df.nclasses)
            {
                y = new double[df.nclasses];
            }
            offs = 0;
            for (i = 0; i <= df.nclasses - 1; i++)
            {
                y[i] = 0;
            }
            for (i = 0; i <= df.ntrees - 1; i++)
            {

                //
                // Process basic tree
                //
                dfprocessinternal(df, offs, x, ref y);

                //
                // Next tree
                //
                offs = offs + (int)Math.Round(df.trees[offs]);
            }
            v = (double)1 / (double)df.ntrees;
            for (i_ = 0; i_ <= df.nclasses - 1; i_++)
            {
                y[i_] = v * y[i_];
            }
        }

        // dfprocess for testing
        public static void dfprocess_test(decisionforest df,
            double[] x,
            ref double[] y)
        {
            int offs = 0;
            int i = 0;
            double v = 0;
            int i_ = 0;
            int visit_count = 0;

            //
            // Proceed
            //
            //if (alglib.ap.len(y) < df.nclasses)
            if (y.Length < df.nclasses)
            {
                y = new double[df.nclasses];
            }
            offs = 0;
            for (i = 0; i <= df.nclasses - 1; i++)
            {
                y[i] = 0;
            }
            for (i = 0; i <= df.ntrees - 1; i++)
            {

                //
                // Process basic tree
                //
                visit_count += dfprocessinternal_test(df, offs, x, ref y);

                //
                // Next tree
                //
                offs = offs + (int)Math.Round(df.trees[offs]);
            }
            v = (double)1 / (double)df.ntrees;
            for (i_ = 0; i_ <= df.nclasses - 1; i_++)
            {
                y[i_] = visit_count;
            }
        }


        /*************************************************************************
        'interactive' variant of DFProcess for languages like Python which support
        constructs like "Y = DFProcessI(DF,X)" and interactive mode of interpreter

        This function allocates new array on each call,  so  it  is  significantly
        slower than its 'non-interactive' counterpart, but it is  more  convenient
        when you call it from command line.

          -- ALGLIB --
             Copyright 28.02.2010 by Bochkanov Sergey
        *************************************************************************/
        public static void dfprocessi(decisionforest df,
            double[] x,
            ref double[] y)
        {
            y = new double[0];

            dfprocess(df, x, ref y);
        }


        /*************************************************************************
        Relative classification error on the test set

        INPUT PARAMETERS:
            DF      -   decision forest model
            XY      -   test set
            NPoints -   test set size

        RESULT:
            percent of incorrectly classified cases.
            Zero if model solves regression task.

          -- ALGLIB --
             Copyright 16.02.2009 by Bochkanov Sergey
        *************************************************************************/
        public static double dfrelclserror(decisionforest df,
            double[,] xy,
            int npoints)
        {
            double result = 0;

            result = (double)dfclserror(df, xy, npoints) / (double)npoints;
            return result;
        }


        /*************************************************************************
        Average cross-entropy (in bits per element) on the test set

        INPUT PARAMETERS:
            DF      -   decision forest model
            XY      -   test set
            NPoints -   test set size

        RESULT:
            CrossEntropy/(NPoints*LN(2)).
            Zero if model solves regression task.

          -- ALGLIB --
             Copyright 16.02.2009 by Bochkanov Sergey
        *************************************************************************/
        public static double dfavgce(decisionforest df,
            double[,] xy,
            int npoints)
        {
            double result = 0;
            double[] x = new double[0];
            double[] y = new double[0];
            int i = 0;
            int j = 0;
            int k = 0;
            int tmpi = 0;
            int i_ = 0;

            x = new double[df.nvars - 1 + 1];
            y = new double[df.nclasses - 1 + 1];
            result = 0;
            for (i = 0; i <= npoints - 1; i++)
            {
                for (i_ = 0; i_ <= df.nvars - 1; i_++)
                {
                    x[i_] = xy[i, i_];
                }
                dfprocess(df, x, ref y);
                if (df.nclasses > 1)
                {

                    //
                    // classification-specific code
                    //
                    k = (int)Math.Round(xy[i, df.nvars]);
                    tmpi = 0;
                    for (j = 1; j <= df.nclasses - 1; j++)
                    {
                        if ((double)(y[j]) > (double)(y[tmpi]))
                        {
                            tmpi = j;
                        }
                    }
                    if ((double)(y[k]) != (double)(0))
                    {
                        result = result - Math.Log(y[k]);
                    }
                    else
                    {
                        result = result - Math.Log(alglib.math.minrealnumber);
                    }
                }
            }
            result = result / npoints;
            return result;
        }


        /*************************************************************************
        RMS error on the test set

        INPUT PARAMETERS:
            DF      -   decision forest model
            XY      -   test set
            NPoints -   test set size

        RESULT:
            root mean square error.
            Its meaning for regression task is obvious. As for
            classification task, RMS error means error when estimating posterior
            probabilities.

          -- ALGLIB --
             Copyright 16.02.2009 by Bochkanov Sergey
        *************************************************************************/
        public static double dfrmserror(decisionforest df,
            double[,] xy,
            int npoints)
        {
            double result = 0;
            double[] x = new double[0];
            double[] y = new double[0];
            int i = 0;
            int j = 0;
            int k = 0;
            int tmpi = 0;
            int i_ = 0;

            x = new double[df.nvars - 1 + 1];
            y = new double[df.nclasses - 1 + 1];
            result = 0;
            for (i = 0; i <= npoints - 1; i++)
            {
                for (i_ = 0; i_ <= df.nvars - 1; i_++)
                {
                    x[i_] = xy[i, i_];
                }
                dfprocess(df, x, ref y);
                if (df.nclasses > 1)
                {

                    //
                    // classification-specific code
                    //
                    k = (int)Math.Round(xy[i, df.nvars]);
                    tmpi = 0;
                    for (j = 1; j <= df.nclasses - 1; j++)
                    {
                        if ((double)(y[j]) > (double)(y[tmpi]))
                        {
                            tmpi = j;
                        }
                    }
                    for (j = 0; j <= df.nclasses - 1; j++)
                    {
                        if (j == k)
                        {
                            result = result + (y[j] - 1) * (y[j] - 1); 
                        }
                        else
                        {
                            result = result + (y[j]) * (y[j]);
                        }
                    }
                }
                else
                {

                    //
                    // regression-specific code
                    //
                    result = result + (y[0] - xy[i, df.nvars])*(y[0] - xy[i, df.nvars]);
                }
            }
            result = Math.Sqrt(result / (npoints * df.nclasses));
            return result;
        }


        /*************************************************************************
        Average error on the test set

        INPUT PARAMETERS:
            DF      -   decision forest model
            XY      -   test set
            NPoints -   test set size

        RESULT:
            Its meaning for regression task is obvious. As for
            classification task, it means average error when estimating posterior
            probabilities.

          -- ALGLIB --
             Copyright 16.02.2009 by Bochkanov Sergey
        *************************************************************************/
        public static double dfavgerror(decisionforest df,
            double[,] xy,
            int npoints)
        {
            double result = 0;
            double[] x = new double[0];
            double[] y = new double[0];
            int i = 0;
            int j = 0;
            int k = 0;
            int i_ = 0;

            x = new double[df.nvars - 1 + 1];
            y = new double[df.nclasses - 1 + 1];
            result = 0;
            for (i = 0; i <= npoints - 1; i++)
            {
                for (i_ = 0; i_ <= df.nvars - 1; i_++)
                {
                    x[i_] = xy[i, i_];
                }
                dfprocess(df, x, ref y);
                if (df.nclasses > 1)
                {

                    //
                    // classification-specific code
                    //
                    k = (int)Math.Round(xy[i, df.nvars]);
                    for (j = 0; j <= df.nclasses - 1; j++)
                    {
                        if (j == k)
                        {
                            result = result + Math.Abs(y[j] - 1);
                        }
                        else
                        {
                            result = result + Math.Abs(y[j]);
                        }
                    }
                }
                else
                {

                    //
                    // regression-specific code
                    //
                    result = result + Math.Abs(y[0] - xy[i, df.nvars]);
                }
            }
            result = result / (npoints * df.nclasses);
            return result;
        }


        /*************************************************************************
        Average relative error on the test set

        INPUT PARAMETERS:
            DF      -   decision forest model
            XY      -   test set
            NPoints -   test set size

        RESULT:
            Its meaning for regression task is obvious. As for
            classification task, it means average relative error when estimating
            posterior probability of belonging to the correct class.

          -- ALGLIB --
             Copyright 16.02.2009 by Bochkanov Sergey
        *************************************************************************/
        public static double dfavgrelerror(decisionforest df,
            double[,] xy,
            int npoints)
        {
            double result = 0;
            double[] x = new double[0];
            double[] y = new double[0];
            int relcnt = 0;
            int i = 0;
            int j = 0;
            int k = 0;
            int i_ = 0;

            x = new double[df.nvars - 1 + 1];
            y = new double[df.nclasses - 1 + 1];
            result = 0;
            relcnt = 0;
            for (i = 0; i <= npoints - 1; i++)
            {
                for (i_ = 0; i_ <= df.nvars - 1; i_++)
                {
                    x[i_] = xy[i, i_];
                }
                dfprocess(df, x, ref y);
                if (df.nclasses > 1)
                {

                    //
                    // classification-specific code
                    //
                    k = (int)Math.Round(xy[i, df.nvars]);
                    for (j = 0; j <= df.nclasses - 1; j++)
                    {
                        if (j == k)
                        {
                            result = result + Math.Abs(y[j] - 1);
                            relcnt = relcnt + 1;
                        }
                    }
                }
                else
                {

                    //
                    // regression-specific code
                    //
                    if ((double)(xy[i, df.nvars]) != (double)(0))
                    {
                        result = result + Math.Abs((y[0] - xy[i, df.nvars]) / xy[i, df.nvars]);
                        relcnt = relcnt + 1;
                    }
                }
            }
            if (relcnt > 0)
            {
                result = result / relcnt;
            }
            return result;
        }


        /*************************************************************************
        Copying of DecisionForest strucure

        INPUT PARAMETERS:
            DF1 -   original

        OUTPUT PARAMETERS:
            DF2 -   copy

          -- ALGLIB --
             Copyright 13.02.2009 by Bochkanov Sergey
        *************************************************************************/
        public static void dfcopy(decisionforest df1,
            decisionforest df2)
        {
            int i_ = 0;

            df2.nvars = df1.nvars;
            df2.nclasses = df1.nclasses;
            df2.ntrees = df1.ntrees;
            df2.bufsize = df1.bufsize;
            df2.trees = new double[df1.bufsize - 1 + 1];
            for (i_ = 0; i_ <= df1.bufsize - 1; i_++)
            {
                df2.trees[i_] = df1.trees[i_];
            }
        }


        /*************************************************************************
        Serializer: allocation

          -- ALGLIB --
             Copyright 14.03.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void dfalloc(alglib.serializer s,
            decisionforest forest)
        {
            s.alloc_entry();
            s.alloc_entry();
            s.alloc_entry();
            s.alloc_entry();
            s.alloc_entry();
            s.alloc_entry();
            alglib.apserv.allocrealarray(s, forest.trees, forest.bufsize);
        }


        /*************************************************************************
        Serializer: serialization

          -- ALGLIB --
             Copyright 14.03.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void dfserialize(alglib.serializer s,
            decisionforest forest)
        {
            s.serialize_int(alglib.scodes.getrdfserializationcode());
            s.serialize_int(dffirstversion);
            s.serialize_int(forest.nvars);
            s.serialize_int(forest.nclasses);
            s.serialize_int(forest.ntrees);
            s.serialize_int(forest.bufsize);
            alglib.apserv.serializerealarray(s, forest.trees, forest.bufsize);
        }


        /*************************************************************************
        Serializer: unserialization

          -- ALGLIB --
             Copyright 14.03.2011 by Bochkanov Sergey
        *************************************************************************/
        public static void dfunserialize(alglib.serializer s,
            decisionforest forest)
        {
            int i0 = 0;
            int i1 = 0;


            //
            // check correctness of header
            //
            i0 = s.unserialize_int();
            alglib.ap.assert(i0 == alglib.scodes.getrdfserializationcode(), "DFUnserialize: stream header corrupted");
            i1 = s.unserialize_int();
            alglib.ap.assert(i1 == dffirstversion, "DFUnserialize: stream header corrupted");

            //
            // Unserialize data
            //
            forest.nvars = s.unserialize_int();
            forest.nclasses = s.unserialize_int();
            forest.ntrees = s.unserialize_int();
            forest.bufsize = s.unserialize_int();
            alglib.apserv.unserializerealarray(s, ref forest.trees);
        }


        /*************************************************************************
        Classification error
        *************************************************************************/
        private static int dfclserror(decisionforest df,
            double[,] xy,
            int npoints)
        {
            int result = 0;
            double[] x = new double[0];
            double[] y = new double[0];
            int i = 0;
            int j = 0;
            int k = 0;
            int tmpi = 0;
            int i_ = 0;

            if (df.nclasses <= 1)
            {
                result = 0;
                return result;
            }
            x = new double[df.nvars - 1 + 1];
            y = new double[df.nclasses - 1 + 1];
            result = 0;
            for (i = 0; i <= npoints - 1; i++)
            {
                for (i_ = 0; i_ <= df.nvars - 1; i_++)
                {
                    x[i_] = xy[i, i_];
                }
                dfprocess(df, x, ref y);
                k = (int)Math.Round(xy[i, df.nvars]);
                tmpi = 0;
                for (j = 1; j <= df.nclasses - 1; j++)
                {
                    if ((double)(y[j]) > (double)(y[tmpi]))
                    {
                        tmpi = j;
                    }
                }
                if (tmpi != k)
                {
                    result = result + 1;
                }
            }
            return result;
        }


        /*************************************************************************
        Internal subroutine for processing one decision tree starting at Offs (Very important-Michael)
        *************************************************************************/
        private static void dfprocessinternal(decisionforest df,
            int offs,
            double[] x,
            ref double[] y)
        {
            int k = 0;
            int idx = 0;


            //
            // Set pointer to the root
            //
            k = offs + 1;

            //
            // Navigate through the tree
            //
            while (true)
            {
                if ((double)(df.trees[k]) == (double)(-1))
                {
                    if (df.nclasses == 1)
                    {
                        y[0] = y[0] + df.trees[k + 1];
                    }
                    else
                    {
                        idx = (int)Math.Round(df.trees[k + 1]);
                        y[idx] = y[idx] + 1;
                    }
                    break;
                }
                if ((double)(x[(int)Math.Round(df.trees[k])]) < (double)(df.trees[k + 1]))
                {
                    k = k + innernodewidth;
                }
                else
                {
                    k = offs + (int)Math.Round(df.trees[k + 2]);
                }
            }
        }

        // for testing
        private static int dfprocessinternal_test(decisionforest df,
            int offs,
            double[] x,
            ref double[] y)
        {
            int k = 0;
            int idx = 0;
            int visit_count = 0;

            //
            // Set pointer to the root
            //
            k = offs + 1;

            //
            // Navigate through the tree
            //
            while (true)
            {
                visit_count++;
                if ((double)(df.trees[k]) == (double)(-1))
                {
                    if (df.nclasses == 1)
                    {
                        y[0] = y[0] + df.trees[k + 1];
                    }
                    else
                    {
                        idx = (int)Math.Round(df.trees[k + 1]);
                        y[idx] = y[idx] + 1;
                    }
                    break;
                }
                if ((double)(x[(int)Math.Round(df.trees[k])]) < (double)(df.trees[k + 1]))
                {
                    k = k + innernodewidth;
                }
                else
                {
                    k = offs + (int)Math.Round(df.trees[k + 2]);
                }
            }
            return visit_count;
        }

        /*************************************************************************
        Builds one decision tree. Just a wralglib.apper for the DFBuildTreeRec.
        *************************************************************************/
        private static void dfbuildtree(double[,] xy,
            int npoints,
            int nvars,
            int nclasses,
            int nfeatures,
            int nvarsinpool,
            int flags,
            dfinternalbuffers bufs)
        {
            int numprocessed = 0;
            int i = 0;

            alglib.ap.assert(npoints > 0);

            //
            // Prepare IdxBuf. It stores indices of the training set elements.
            // When training set is being split, contents of IdxBuf is
            // correspondingly reordered so we can know which elements belong
            // to which branch of decision tree.
            //
            for (i = 0; i <= npoints - 1; i++)
            {
                bufs.idxbuf[i] = i;
            }

            //
            // Recursive procedure
            //
            numprocessed = 1;
            dfbuildtreerec(xy, npoints, nvars, nclasses, nfeatures, nvarsinpool, flags, ref numprocessed, 0, npoints - 1, bufs);
            bufs.treebuf[0] = numprocessed;
        }


        /*************************************************************************
        Builds one decision tree (internal recursive subroutine)
        Michael: important subroutine
        Parameters:
            TreeBuf     -   large enough array, at least TreeSize
            IdxBuf      -   at least NPoints elements
            TmpBufR     -   at least NPoints
            TmpBufR2    -   at least NPoints
            TmpBufI     -   at least NPoints
            TmpBufI2    -   at least NPoints+1
        *************************************************************************/
        private static void dfbuildtreerec(double[,] xy,
            int npoints,
            int nvars,
            int nclasses,
            int nfeatures,
            int nvarsinpool,
            int flags,
            ref int numprocessed,
            int idx1,
            int idx2,
            dfinternalbuffers bufs)
        {
            int i = 0;
            int j = 0;
            int k = 0;
            bool bflag = new bool();
            int i1 = 0;
            int i2 = 0;
            int info = 0;
            double sl = 0;
            double sr = 0;
            double w = 0;
            int idxbest = 0;
            double ebest = 0;
            double tbest = 0;
            int varcur = 0;
            double s = 0;
            double v = 0;
            double v1 = 0;
            double v2 = 0;
            double threshold = 0;
            int oldnp = 0;
            double currms = 0;
            bool useevs = new bool();


            //
            // these initializers are not really necessary,
            // but without them compiler complains about uninitialized locals
            //
            tbest = 0;

            //
            // Prepare
            //
            alglib.ap.assert(npoints > 0);
            alglib.ap.assert(idx2 >= idx1);
            useevs = flags / dfuseevs % 2 != 0;

            //
            // Leaf node
            //
            if (idx2 == idx1)
            {
                bufs.treebuf[numprocessed] = -1;
                bufs.treebuf[numprocessed + 1] = xy[bufs.idxbuf[idx1], nvars];
                numprocessed = numprocessed + leafnodewidth;
                return;
            }

            //
            // Non-leaf node.
            // Select random variable, prepare split:
            // 1. prepare default solution - no splitting, class at random
            // 2. investigate possible splits, compare with default/best
            //
            idxbest = -1;
            if (nclasses > 1)
            {

                //
                // default solution for classification
                //
                for (i = 0; i <= nclasses - 1; i++)
                {
                    bufs.classibuf[i] = 0;
                }
                s = idx2 - idx1 + 1;
                for (i = idx1; i <= idx2; i++)
                {
                    j = (int)Math.Round(xy[bufs.idxbuf[i], nvars]);
                    bufs.classibuf[j] = bufs.classibuf[j] + 1;
                }
                ebest = 0;
                for (i = 0; i <= nclasses - 1; i++)
                {
                    ebest = ebest + bufs.classibuf[i] * alglib.math.sqr(1 - bufs.classibuf[i] / s) + (s - bufs.classibuf[i]) * alglib.math.sqr(bufs.classibuf[i] / s);
                }
                ebest = Math.Sqrt(ebest / (nclasses * (idx2 - idx1 + 1)));
            }
            else
            {

                //
                // default solution for regression
                //
                v = 0;
                for (i = idx1; i <= idx2; i++)
                {
                    v = v + xy[bufs.idxbuf[i], nvars];
                }
                v = v / (idx2 - idx1 + 1);
                ebest = 0;
                for (i = idx1; i <= idx2; i++)
                {
                    ebest = ebest + alglib.math.sqr(xy[bufs.idxbuf[i], nvars] - v);
                }
                ebest = Math.Sqrt(ebest / (idx2 - idx1 + 1));
            }
            i = 0;
            while (i <= Math.Min(nfeatures, nvarsinpool) - 1)
            {

                //
                // select variables from pool
                //
                j = i + alglib.math.randominteger(nvarsinpool - i);
                k = bufs.varpool[i];
                bufs.varpool[i] = bufs.varpool[j];
                bufs.varpool[j] = k;
                varcur = bufs.varpool[i];

                //
                // load variable values to working array
                //
                // apply EVS preprocessing: if all variable values are same,
                // variable is excluded from pool.
                //
                // This is necessary for binary pre-splits (see later) to work.
                //
                for (j = idx1; j <= idx2; j++)
                {
                    bufs.tmpbufr[j - idx1] = xy[bufs.idxbuf[j], varcur];
                }
                if (useevs)
                {
                    bflag = false;
                    v = bufs.tmpbufr[0];
                    for (j = 0; j <= idx2 - idx1; j++)
                    {
                        if ((double)(bufs.tmpbufr[j]) != (double)(v))
                        {
                            bflag = true;
                            break;
                        }
                    }
                    if (!bflag)
                    {

                        //
                        // exclude variable from pool,
                        // go to the next iteration.
                        // I is not increased.
                        //
                        k = bufs.varpool[i];
                        bufs.varpool[i] = bufs.varpool[nvarsinpool - 1];
                        bufs.varpool[nvarsinpool - 1] = k;
                        nvarsinpool = nvarsinpool - 1;
                        continue;
                    }
                }

                //
                // load labels to working array
                //
                if (nclasses > 1)
                {
                    for (j = idx1; j <= idx2; j++)
                    {
                        bufs.tmpbufi[j - idx1] = (int)Math.Round(xy[bufs.idxbuf[j], nvars]);
                    }
                }
                else
                {
                    for (j = idx1; j <= idx2; j++)
                    {
                        bufs.tmpbufr2[j - idx1] = xy[bufs.idxbuf[j], nvars];
                    }
                }

                //
                // calculate split
                //
                if (useevs & bufs.evsbin[varcur])
                {

                    //
                    // Pre-calculated splits for binary variables.
                    // Threshold is already known, just calculate RMS error
                    //
                    threshold = bufs.evssplits[varcur];
                    if (nclasses > 1)
                    {

                        //
                        // classification-specific code
                        //
                        for (j = 0; j <= 2 * nclasses - 1; j++)
                        {
                            bufs.classibuf[j] = 0;
                        }
                        sl = 0;
                        sr = 0;
                        for (j = 0; j <= idx2 - idx1; j++)
                        {
                            k = bufs.tmpbufi[j];
                            if ((double)(bufs.tmpbufr[j]) < (double)(threshold))
                            {
                                bufs.classibuf[k] = bufs.classibuf[k] + 1;
                                sl = sl + 1;
                            }
                            else
                            {
                                bufs.classibuf[k + nclasses] = bufs.classibuf[k + nclasses] + 1;
                                sr = sr + 1;
                            }
                        }
                        alglib.ap.assert((double)(sl) != (double)(0) & (double)(sr) != (double)(0), "DFBuildTreeRec: something strange!");
                        currms = 0;
                        for (j = 0; j <= nclasses - 1; j++)
                        {
                            w = bufs.classibuf[j];
                            currms = currms + w * alglib.math.sqr(w / sl - 1);
                            currms = currms + (sl - w) * alglib.math.sqr(w / sl);
                            w = bufs.classibuf[nclasses + j];
                            currms = currms + w * alglib.math.sqr(w / sr - 1);
                            currms = currms + (sr - w) * alglib.math.sqr(w / sr);
                        }
                        currms = Math.Sqrt(currms / (nclasses * (idx2 - idx1 + 1)));
                    }
                    else
                    {

                        //
                        // regression-specific code
                        //
                        sl = 0;
                        sr = 0;
                        v1 = 0;
                        v2 = 0;
                        for (j = 0; j <= idx2 - idx1; j++)
                        {
                            if ((double)(bufs.tmpbufr[j]) < (double)(threshold))
                            {
                                v1 = v1 + bufs.tmpbufr2[j];
                                sl = sl + 1;
                            }
                            else
                            {
                                v2 = v2 + bufs.tmpbufr2[j];
                                sr = sr + 1;
                            }
                        }
                        alglib.ap.assert((double)(sl) != (double)(0) & (double)(sr) != (double)(0), "DFBuildTreeRec: something strange!");
                        v1 = v1 / sl;
                        v2 = v2 / sr;
                        currms = 0;
                        for (j = 0; j <= idx2 - idx1; j++)
                        {
                            if ((double)(bufs.tmpbufr[j]) < (double)(threshold))
                            {
                                currms = currms + alglib.math.sqr(v1 - bufs.tmpbufr2[j]);
                            }
                            else
                            {
                                currms = currms + alglib.math.sqr(v2 - bufs.tmpbufr2[j]);
                            }
                        }
                        currms = Math.Sqrt(currms / (idx2 - idx1 + 1));
                    }
                    info = 1;
                }
                else
                {

                    //
                    // Generic splits
                    //
                    if (nclasses > 1)
                    {
                        dfsplitc(ref bufs.tmpbufr, ref bufs.tmpbufi, ref bufs.classibuf, idx2 - idx1 + 1, nclasses, dfusestrongsplits, ref info, ref threshold, ref currms, ref bufs.sortrbuf, ref bufs.sortibuf);
                    }
                    else
                    {
                        dfsplitr(ref bufs.tmpbufr, ref bufs.tmpbufr2, idx2 - idx1 + 1, dfusestrongsplits, ref info, ref threshold, ref currms, ref bufs.sortrbuf, ref bufs.sortrbuf2);
                    }
                }
                if (info > 0)
                {
                    if ((double)(currms) <= (double)(ebest))
                    {
                        ebest = currms;
                        idxbest = varcur;
                        tbest = threshold;
                    }
                }

                //
                // Next iteration
                //
                i = i + 1;
            }

            //
            // to split or not to split
            //
            if (idxbest < 0)
            {

                //
                // All values are same, cannot split.
                //
                bufs.treebuf[numprocessed] = -1;
                if (nclasses > 1)
                {

                    //
                    // Select random class label (randomness allows us to
                    // approximate distribution of the classes)
                    //
                    bufs.treebuf[numprocessed + 1] = (int)Math.Round(xy[bufs.idxbuf[idx1 + alglib.math.randominteger(idx2 - idx1 + 1)], nvars]);
                }
                else
                {

                    //
                    // Select average (for regression task).
                    //
                    v = 0;
                    for (i = idx1; i <= idx2; i++)
                    {
                        v = v + xy[bufs.idxbuf[i], nvars] / (idx2 - idx1 + 1);
                    }
                    bufs.treebuf[numprocessed + 1] = v;
                }
                numprocessed = numprocessed + leafnodewidth;
            }
            else
            {

                //
                // we can split
                //
                bufs.treebuf[numprocessed] = idxbest;
                bufs.treebuf[numprocessed + 1] = tbest;
                i1 = idx1;
                i2 = idx2;
                while (i1 <= i2)
                {

                    //
                    // Reorder indices so that left partition is in [Idx1..I1-1],
                    // and right partition is in [I2+1..Idx2]
                    //
                    if ((double)(xy[bufs.idxbuf[i1], idxbest]) < (double)(tbest))
                    {
                        i1 = i1 + 1;
                        continue;
                    }
                    if ((double)(xy[bufs.idxbuf[i2], idxbest]) >= (double)(tbest))
                    {
                        i2 = i2 - 1;
                        continue;
                    }
                    j = bufs.idxbuf[i1];
                    bufs.idxbuf[i1] = bufs.idxbuf[i2];
                    bufs.idxbuf[i2] = j;
                    i1 = i1 + 1;
                    i2 = i2 - 1;
                }
                oldnp = numprocessed;
                numprocessed = numprocessed + innernodewidth;
                dfbuildtreerec(xy, npoints, nvars, nclasses, nfeatures, nvarsinpool, flags, ref numprocessed, idx1, i1 - 1, bufs);
                bufs.treebuf[oldnp + 2] = numprocessed;
                dfbuildtreerec(xy, npoints, nvars, nclasses, nfeatures, nvarsinpool, flags, ref numprocessed, i2 + 1, idx2, bufs);
            }
        }


        /*************************************************************************
        Makes split on attribute
        *************************************************************************/
        private static void dfsplitc(ref double[] x,
            ref int[] c,
            ref int[] cntbuf,
            int n,
            int nc,
            int flags,
            ref int info,
            ref double threshold,
            ref double e,
            ref double[] sortrbuf,
            ref int[] sortibuf)
        {
            int i = 0;
            int neq = 0;
            int nless = 0;
            int ngreater = 0;
            int q = 0;
            int qmin = 0;
            int qmax = 0;
            int qcnt = 0;
            double cursplit = 0;
            int nleft = 0;
            double v = 0;
            double cure = 0;
            double w = 0;
            double sl = 0;
            double sr = 0;

            info = 0;
            threshold = 0;
            e = 0;

            alglib.tsort.tagsortfasti(ref x, ref c, ref sortrbuf, ref sortibuf, n);
            e = alglib.math.maxrealnumber;
            threshold = 0.5 * (x[0] + x[n - 1]);
            info = -3;
            if (flags / dfusestrongsplits % 2 == 0)
            {

                //
                // weak splits, split at half
                //
                qcnt = 2;
                qmin = 1;
                qmax = 1;
            }
            else
            {

                //
                // strong splits: choose best quartile
                //
                qcnt = 4;
                qmin = 1;
                qmax = 3;
            }
            for (q = qmin; q <= qmax; q++)
            {
                cursplit = x[n * q / qcnt];
                neq = 0;
                nless = 0;
                ngreater = 0;
                for (i = 0; i <= n - 1; i++)
                {
                    if ((double)(x[i]) < (double)(cursplit))
                    {
                        nless = nless + 1;
                    }
                    if ((double)(x[i]) == (double)(cursplit))
                    {
                        neq = neq + 1;
                    }
                    if ((double)(x[i]) > (double)(cursplit))
                    {
                        ngreater = ngreater + 1;
                    }
                }
                alglib.ap.assert(neq != 0, "DFSplitR: NEq=0, something strange!!!");
                if (nless != 0 | ngreater != 0)
                {

                    //
                    // set threshold between two partitions, with
                    // some tweaking to avoid problems with floating point
                    // arithmetics.
                    //
                    // The problem is that when you calculates C = 0.5*(A+B) there
                    // can be no C which lies strictly between A and B (for example,
                    // there is no floating point number which is
                    // greater than 1 and less than 1+eps). In such situations
                    // we choose right side as theshold (remember that
                    // points which lie on threshold falls to the right side).
                    //
                    if (nless < ngreater)
                    {
                        cursplit = 0.5 * (x[nless + neq - 1] + x[nless + neq]);
                        nleft = nless + neq;
                        if ((double)(cursplit) <= (double)(x[nless + neq - 1]))
                        {
                            cursplit = x[nless + neq];
                        }
                    }
                    else
                    {
                        cursplit = 0.5 * (x[nless - 1] + x[nless]);
                        nleft = nless;
                        if ((double)(cursplit) <= (double)(x[nless - 1]))
                        {
                            cursplit = x[nless];
                        }
                    }
                    info = 1;
                    cure = 0;
                    for (i = 0; i <= 2 * nc - 1; i++)
                    {
                        cntbuf[i] = 0;
                    }
                    for (i = 0; i <= nleft - 1; i++)
                    {
                        cntbuf[c[i]] = cntbuf[c[i]] + 1;
                    }
                    for (i = nleft; i <= n - 1; i++)
                    {
                        cntbuf[nc + c[i]] = cntbuf[nc + c[i]] + 1;
                    }
                    sl = nleft;
                    sr = n - nleft;
                    v = 0;
                    for (i = 0; i <= nc - 1; i++)
                    {
                        w = cntbuf[i];
                        v = v + w * alglib.math.sqr(w / sl - 1);
                        v = v + (sl - w) * alglib.math.sqr(w / sl);
                        w = cntbuf[nc + i];
                        v = v + w * alglib.math.sqr(w / sr - 1);
                        v = v + (sr - w) * alglib.math.sqr(w / sr);
                    }
                    cure = Math.Sqrt(v / (nc * n));
                    if ((double)(cure) < (double)(e))
                    {
                        threshold = cursplit;
                        e = cure;
                    }
                }
            }
        }


        /*************************************************************************
        Makes split on attribute
        *************************************************************************/
        private static void dfsplitr(ref double[] x,
            ref double[] y,
            int n,
            int flags,
            ref int info,
            ref double threshold,
            ref double e,
            ref double[] sortrbuf,
            ref double[] sortrbuf2)
        {
            int i = 0;
            int neq = 0;
            int nless = 0;
            int ngreater = 0;
            int q = 0;
            int qmin = 0;
            int qmax = 0;
            int qcnt = 0;
            double cursplit = 0;
            int nleft = 0;
            double v = 0;
            double cure = 0;

            info = 0;
            threshold = 0;
            e = 0;

            alglib.tsort.tagsortfastr(ref x, ref y, ref sortrbuf, ref sortrbuf2, n);
            e = alglib.math.maxrealnumber;
            threshold = 0.5 * (x[0] + x[n - 1]);
            info = -3;
            if (flags / dfusestrongsplits % 2 == 0)
            {

                //
                // weak splits, split at half
                //
                qcnt = 2;
                qmin = 1;
                qmax = 1;
            }
            else
            {

                //
                // strong splits: choose best quartile
                //
                qcnt = 4;
                qmin = 1;
                qmax = 3;
            }
            for (q = qmin; q <= qmax; q++)
            {
                cursplit = x[n * q / qcnt];
                neq = 0;
                nless = 0;
                ngreater = 0;
                for (i = 0; i <= n - 1; i++)
                {
                    if ((double)(x[i]) < (double)(cursplit))
                    {
                        nless = nless + 1;
                    }
                    if ((double)(x[i]) == (double)(cursplit))
                    {
                        neq = neq + 1;
                    }
                    if ((double)(x[i]) > (double)(cursplit))
                    {
                        ngreater = ngreater + 1;
                    }
                }
                alglib.ap.assert(neq != 0, "DFSplitR: NEq=0, something strange!!!");
                if (nless != 0 | ngreater != 0)
                {

                    //
                    // set threshold between two partitions, with
                    // some tweaking to avoid problems with floating point
                    // arithmetics.
                    //
                    // The problem is that when you calculates C = 0.5*(A+B) there
                    // can be no C which lies strictly between A and B (for example,
                    // there is no floating point number which is
                    // greater than 1 and less than 1+eps). In such situations
                    // we choose right side as theshold (remember that
                    // points which lie on threshold falls to the right side).
                    //
                    if (nless < ngreater)
                    {
                        cursplit = 0.5 * (x[nless + neq - 1] + x[nless + neq]);
                        nleft = nless + neq;
                        if ((double)(cursplit) <= (double)(x[nless + neq - 1]))
                        {
                            cursplit = x[nless + neq];
                        }
                    }
                    else
                    {
                        cursplit = 0.5 * (x[nless - 1] + x[nless]);
                        nleft = nless;
                        if ((double)(cursplit) <= (double)(x[nless - 1]))
                        {
                            cursplit = x[nless];
                        }
                    }
                    info = 1;
                    cure = 0;
                    v = 0;
                    for (i = 0; i <= nleft - 1; i++)
                    {
                        v = v + y[i];
                    }
                    v = v / nleft;
                    for (i = 0; i <= nleft - 1; i++)
                    {
                        cure = cure + alglib.math.sqr(y[i] - v);
                    }
                    v = 0;
                    for (i = nleft; i <= n - 1; i++)
                    {
                        v = v + y[i];
                    }
                    v = v / (n - nleft);
                    for (i = nleft; i <= n - 1; i++)
                    {
                        cure = cure + alglib.math.sqr(y[i] - v);
                    }
                    cure = Math.Sqrt(cure / n);
                    if ((double)(cure) < (double)(e))
                    {
                        threshold = cursplit;
                        e = cure;
                    }
                }
            }
        }


    }
    
}
