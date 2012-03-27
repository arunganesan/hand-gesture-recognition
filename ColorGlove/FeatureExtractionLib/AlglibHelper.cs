using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FeatureExtractionLib
{   
    public partial class alglib
    {

        #region a lot of helper functions from alglib
        /********************************************************************
          Class defining an ALGLIB exception
          ********************************************************************/
        public class alglibexception : System.Exception
        {
            public string msg;
            public alglibexception(string s)
            {
                msg = s;
            }

        }
        /********************************************************************
         math functions
         ********************************************************************/
        public class math
        {
            //public static System.Random RndObject = new System.Random(System.DateTime.Now.Millisecond);
            public static System.Random rndobject = new System.Random(System.DateTime.Now.Millisecond + 1000 * System.DateTime.Now.Second + 60 * 1000 * System.DateTime.Now.Minute);

            public const double machineepsilon = 5E-16;
            public const double maxrealnumber = 1E300;
            public const double minrealnumber = 1E-300;

            public static bool isfinite(double d)
            {
                return !System.Double.IsNaN(d) && !System.Double.IsInfinity(d);
            }

            public static double randomreal()
            {
                double r = 0;
                lock (rndobject) { r = rndobject.NextDouble(); }
                return r;
            }
            public static int randominteger(int N)
            {
                int r = 0;
                lock (rndobject) { r = rndobject.Next(N); }
                return r;
            }
            public static double sqr(double X)
            {
                return X * X;
            }
            /*
            public static double abscomplex(complex z)
            {
                double w;
                double xabs;
                double yabs;
                double v;

                xabs = System.Math.Abs(z.x);
                yabs = System.Math.Abs(z.y);
                w = xabs > yabs ? xabs : yabs;
                v = xabs < yabs ? xabs : yabs;
                if (v == 0)
                    return w;
                else
                {
                    double t = v / w;
                    return w * System.Math.Sqrt(1 + t * t);
                }
            }
            public static complex conj(complex z)
            {
                return new complex(z.x, -z.y);
            }
            public static complex csqr(complex z)
            {
                return new complex(z.x * z.x - z.y * z.y, 2 * z.x * z.y);
            }
            */
        }

        public class apserv
        {
            /*************************************************************************
            Buffers for internal functions which need buffers:
            * check for size of the buffer you want to use.
            * if buffer is too small, resize it; leave unchanged, if it is larger than
              needed.
            * use it.

            We can pass this structure to multiple functions;  after first run through
            functions buffer sizes will be finally determined,  and  on  a next run no
            allocation will be required.
            *************************************************************************/
            public class apbuffers
            {
                public int[] ia0;
                public int[] ia1;
                public int[] ia2;
                public int[] ia3;
                public double[] ra0;
                public double[] ra1;
                public double[] ra2;
                public double[] ra3;
                public apbuffers()
                {
                    ia0 = new int[0];
                    ia1 = new int[0];
                    ia2 = new int[0];
                    ia3 = new int[0];
                    ra0 = new double[0];
                    ra1 = new double[0];
                    ra2 = new double[0];
                    ra3 = new double[0];
                }
            };




            /*************************************************************************
            This  function  generates  1-dimensional  general  interpolation task with
            moderate Lipshitz constant (close to 1.0)

            If N=1 then suborutine generates only one point at the middle of [A,B]

              -- ALGLIB --
                 Copyright 02.12.2009 by Bochkanov Sergey
            *************************************************************************/
            public static void taskgenint1d(double a,
                double b,
                int n,
                ref double[] x,
                ref double[] y)
            {
                int i = 0;
                double h = 0;

                x = new double[0];
                y = new double[0];

                ap.assert(n >= 1, "TaskGenInterpolationEqdist1D: N<1!");
                x = new double[n];
                y = new double[n];
                if (n > 1)
                {
                    x[0] = a;
                    y[0] = 2 * math.randomreal() - 1;
                    h = (b - a) / (n - 1);
                    for (i = 1; i <= n - 1; i++)
                    {
                        if (i != n - 1)
                        {
                            x[i] = a + (i + 0.2 * (2 * math.randomreal() - 1)) * h;
                        }
                        else
                        {
                            x[i] = b;
                        }
                        y[i] = y[i - 1] + (2 * math.randomreal() - 1) * (x[i] - x[i - 1]);
                    }
                }
                else
                {
                    x[0] = 0.5 * (a + b);
                    y[0] = 2 * math.randomreal() - 1;
                }
            }


            /*************************************************************************
            This function generates  1-dimensional equidistant interpolation task with
            moderate Lipshitz constant (close to 1.0)

            If N=1 then suborutine generates only one point at the middle of [A,B]

              -- ALGLIB --
                 Copyright 02.12.2009 by Bochkanov Sergey
            *************************************************************************/
            public static void taskgenint1dequidist(double a,
                double b,
                int n,
                ref double[] x,
                ref double[] y)
            {
                int i = 0;
                double h = 0;

                x = new double[0];
                y = new double[0];

                ap.assert(n >= 1, "TaskGenInterpolationEqdist1D: N<1!");
                x = new double[n];
                y = new double[n];
                if (n > 1)
                {
                    x[0] = a;
                    y[0] = 2 * math.randomreal() - 1;
                    h = (b - a) / (n - 1);
                    for (i = 1; i <= n - 1; i++)
                    {
                        x[i] = a + i * h;
                        y[i] = y[i - 1] + (2 * math.randomreal() - 1) * h;
                    }
                }
                else
                {
                    x[0] = 0.5 * (a + b);
                    y[0] = 2 * math.randomreal() - 1;
                }
            }


            /*************************************************************************
            This function generates  1-dimensional Chebyshev-1 interpolation task with
            moderate Lipshitz constant (close to 1.0)

            If N=1 then suborutine generates only one point at the middle of [A,B]

              -- ALGLIB --
                 Copyright 02.12.2009 by Bochkanov Sergey
            *************************************************************************/
            public static void taskgenint1dcheb1(double a,
                double b,
                int n,
                ref double[] x,
                ref double[] y)
            {
                int i = 0;

                x = new double[0];
                y = new double[0];

                ap.assert(n >= 1, "TaskGenInterpolation1DCheb1: N<1!");
                x = new double[n];
                y = new double[n];
                if (n > 1)
                {
                    for (i = 0; i <= n - 1; i++)
                    {
                        x[i] = 0.5 * (b + a) + 0.5 * (b - a) * Math.Cos(Math.PI * (2 * i + 1) / (2 * n));
                        if (i == 0)
                        {
                            y[i] = 2 * math.randomreal() - 1;
                        }
                        else
                        {
                            y[i] = y[i - 1] + (2 * math.randomreal() - 1) * (x[i] - x[i - 1]);
                        }
                    }
                }
                else
                {
                    x[0] = 0.5 * (a + b);
                    y[0] = 2 * math.randomreal() - 1;
                }
            }


            /*************************************************************************
            This function generates  1-dimensional Chebyshev-2 interpolation task with
            moderate Lipshitz constant (close to 1.0)

            If N=1 then suborutine generates only one point at the middle of [A,B]

              -- ALGLIB --
                 Copyright 02.12.2009 by Bochkanov Sergey
            *************************************************************************/
            public static void taskgenint1dcheb2(double a,
                double b,
                int n,
                ref double[] x,
                ref double[] y)
            {
                int i = 0;

                x = new double[0];
                y = new double[0];

                ap.assert(n >= 1, "TaskGenInterpolation1DCheb2: N<1!");
                x = new double[n];
                y = new double[n];
                if (n > 1)
                {
                    for (i = 0; i <= n - 1; i++)
                    {
                        x[i] = 0.5 * (b + a) + 0.5 * (b - a) * Math.Cos(Math.PI * i / (n - 1));
                        if (i == 0)
                        {
                            y[i] = 2 * math.randomreal() - 1;
                        }
                        else
                        {
                            y[i] = y[i - 1] + (2 * math.randomreal() - 1) * (x[i] - x[i - 1]);
                        }
                    }
                }
                else
                {
                    x[0] = 0.5 * (a + b);
                    y[0] = 2 * math.randomreal() - 1;
                }
            }


            /*************************************************************************
            This function checks that all values from X[] are distinct. It does more
            than just usual floating point comparison:
            * first, it calculates max(X) and min(X)
            * second, it maps X[] from [min,max] to [1,2]
            * only at this stage actual comparison is done

            The meaning of such check is to ensure that all values are "distinct enough"
            and will not cause interpolation subroutine to fail.

            NOTE:
                X[] must be sorted by ascending (subroutine ASSERT's it)

              -- ALGLIB --
                 Copyright 02.12.2009 by Bochkanov Sergey
            *************************************************************************/
            public static bool aredistinct(double[] x,
                int n)
            {
                bool result = new bool();
                double a = 0;
                double b = 0;
                int i = 0;
                bool nonsorted = new bool();

                ap.assert(n >= 1, "APSERVAreDistinct: internal error (N<1)");
                if (n == 1)
                {

                    //
                    // everything is alright, it is up to caller to decide whether it
                    // can interpolate something with just one point
                    //
                    result = true;
                    return result;
                }
                a = x[0];
                b = x[0];
                nonsorted = false;
                for (i = 1; i <= n - 1; i++)
                {
                    a = Math.Min(a, x[i]);
                    b = Math.Max(b, x[i]);
                    nonsorted = nonsorted | (double)(x[i - 1]) >= (double)(x[i]);
                }
                ap.assert(!nonsorted, "APSERVAreDistinct: internal error (not sorted)");
                for (i = 1; i <= n - 1; i++)
                {
                    if ((double)((x[i] - a) / (b - a) + 1) == (double)((x[i - 1] - a) / (b - a) + 1))
                    {
                        result = false;
                        return result;
                    }
                }
                result = true;
                return result;
            }


            /*************************************************************************
            If Length(X)<N, resizes X

              -- ALGLIB --
                 Copyright 20.03.2009 by Bochkanov Sergey
            *************************************************************************/
            public static void bvectorsetlengthatleast(ref bool[] x,
                int n)
            {
                if (ap.len(x) < n)
                {
                    x = new bool[n];
                }
            }


            /*************************************************************************
            If Length(X)<N, resizes X

              -- ALGLIB --
                 Copyright 20.03.2009 by Bochkanov Sergey
            *************************************************************************/
            public static void ivectorsetlengthatleast(ref int[] x,
                int n)
            {
                if (ap.len(x) < n)
                {
                    x = new int[n];
                }
            }


            /*************************************************************************
            If Length(X)<N, resizes X

              -- ALGLIB --
                 Copyright 20.03.2009 by Bochkanov Sergey
            *************************************************************************/
            public static void rvectorsetlengthatleast(ref double[] x,
                int n)
            {
                if (ap.len(x) < n)
                {
                    x = new double[n];
                }
            }


            /*************************************************************************
            If Cols(X)<N or Rows(X)<M, resizes X

              -- ALGLIB --
                 Copyright 20.03.2009 by Bochkanov Sergey
            *************************************************************************/
            public static void rmatrixsetlengthatleast(ref double[,] x,
                int m,
                int n)
            {
                if (ap.rows(x) < m | ap.cols(x) < n)
                {
                    x = new double[m, n];
                }
            }


            /*************************************************************************
            Resizes X and:
            * preserves old contents of X
            * fills new elements by zeros

              -- ALGLIB --
                 Copyright 20.03.2009 by Bochkanov Sergey
            *************************************************************************/
            public static void rmatrixresize(ref double[,] x,
                int m,
                int n)
            {
                double[,] oldx = new double[0, 0];
                int i = 0;
                int j = 0;
                int m2 = 0;
                int n2 = 0;

                m2 = ap.rows(x);
                n2 = ap.cols(x);
                ap.swap(ref x, ref oldx);
                x = new double[m, n];
                for (i = 0; i <= m - 1; i++)
                {
                    for (j = 0; j <= n - 1; j++)
                    {
                        if (i < m2 & j < n2)
                        {
                            x[i, j] = oldx[i, j];
                        }
                        else
                        {
                            x[i, j] = 0.0;
                        }
                    }
                }
            }


            /*************************************************************************
            This function checks that all values from X[] are finite

              -- ALGLIB --
                 Copyright 18.06.2010 by Bochkanov Sergey
            *************************************************************************/
            public static bool isfinitevector(double[] x,
                int n)
            {
                bool result = new bool();
                int i = 0;

                ap.assert(n >= 0, "APSERVIsFiniteVector: internal error (N<0)");
                for (i = 0; i <= n - 1; i++)
                {
                    if (!math.isfinite(x[i]))
                    {
                        result = false;
                        return result;
                    }
                }
                result = true;
                return result;
            }


            /*************************************************************************
            This function checks that all values from X[] are finite

              -- ALGLIB --
                 Copyright 18.06.2010 by Bochkanov Sergey
            *************************************************************************/
            /*
            public static bool isfinitecvector(complex[] z,
                int n)
            {
                bool result = new bool();
                int i = 0;

                ap.assert(n >= 0, "APSERVIsFiniteCVector: internal error (N<0)");
                for (i = 0; i <= n - 1; i++)
                {
                    if (!math.isfinite(z[i].x) | !math.isfinite(z[i].y))
                    {
                        result = false;
                        return result;
                    }
                }
                result = true;
                return result;
            }

            */
            /*************************************************************************
            This function checks that all values from X[0..M-1,0..N-1] are finite

              -- ALGLIB --
                 Copyright 18.06.2010 by Bochkanov Sergey
            *************************************************************************/
            public static bool apservisfinitematrix(double[,] x,
                int m,
                int n)
            {
                bool result = new bool();
                int i = 0;
                int j = 0;

                ap.assert(n >= 0, "APSERVIsFiniteMatrix: internal error (N<0)");
                ap.assert(m >= 0, "APSERVIsFiniteMatrix: internal error (M<0)");
                for (i = 0; i <= m - 1; i++)
                {
                    for (j = 0; j <= n - 1; j++)
                    {
                        if (!math.isfinite(x[i, j]))
                        {
                            result = false;
                            return result;
                        }
                    }
                }
                result = true;
                return result;
            }


            /*************************************************************************
            This function checks that all values from X[0..M-1,0..N-1] are finite

              -- ALGLIB --
                 Copyright 18.06.2010 by Bochkanov Sergey
            *************************************************************************/
            /*
            public static bool apservisfinitecmatrix(complex[,] x,
                int m,
                int n)
            {
                bool result = new bool();
                int i = 0;
                int j = 0;

                ap.assert(n >= 0, "APSERVIsFiniteCMatrix: internal error (N<0)");
                ap.assert(m >= 0, "APSERVIsFiniteCMatrix: internal error (M<0)");
                for (i = 0; i <= m - 1; i++)
                {
                    for (j = 0; j <= n - 1; j++)
                    {
                        if (!math.isfinite(x[i, j].x) | !math.isfinite(x[i, j].y))
                        {
                            result = false;
                            return result;
                        }
                    }
                }
                result = true;
                return result;
            }

            */
            /*************************************************************************
            This function checks that all values from upper/lower triangle of
            X[0..N-1,0..N-1] are finite

              -- ALGLIB --
                 Copyright 18.06.2010 by Bochkanov Sergey
            *************************************************************************/
            public static bool isfinitertrmatrix(double[,] x,
                int n,
                bool isupper)
            {
                bool result = new bool();
                int i = 0;
                int j1 = 0;
                int j2 = 0;
                int j = 0;

                ap.assert(n >= 0, "APSERVIsFiniteRTRMatrix: internal error (N<0)");
                for (i = 0; i <= n - 1; i++)
                {
                    if (isupper)
                    {
                        j1 = i;
                        j2 = n - 1;
                    }
                    else
                    {
                        j1 = 0;
                        j2 = i;
                    }
                    for (j = j1; j <= j2; j++)
                    {
                        if (!math.isfinite(x[i, j]))
                        {
                            result = false;
                            return result;
                        }
                    }
                }
                result = true;
                return result;
            }


            /*************************************************************************
            This function checks that all values from upper/lower triangle of
            X[0..N-1,0..N-1] are finite

              -- ALGLIB --
                 Copyright 18.06.2010 by Bochkanov Sergey
            *************************************************************************/
            /*
            public static bool apservisfinitectrmatrix(complex[,] x,
                int n,
                bool isupper)
            {
                bool result = new bool();
                int i = 0;
                int j1 = 0;
                int j2 = 0;
                int j = 0;

                ap.assert(n >= 0, "APSERVIsFiniteCTRMatrix: internal error (N<0)");
                for (i = 0; i <= n - 1; i++)
                {
                    if (isupper)
                    {
                        j1 = i;
                        j2 = n - 1;
                    }
                    else
                    {
                        j1 = 0;
                        j2 = i;
                    }
                    for (j = j1; j <= j2; j++)
                    {
                        if (!math.isfinite(x[i, j].x) | !math.isfinite(x[i, j].y))
                        {
                            result = false;
                            return result;
                        }
                    }
                }
                result = true;
                return result;
            }

            */
            /*************************************************************************
            This function checks that all values from X[0..M-1,0..N-1] are  finite  or
            NaN's.

              -- ALGLIB --
                 Copyright 18.06.2010 by Bochkanov Sergey
            *************************************************************************/
            public static bool apservisfiniteornanmatrix(double[,] x,
                int m,
                int n)
            {
                bool result = new bool();
                int i = 0;
                int j = 0;

                ap.assert(n >= 0, "APSERVIsFiniteOrNaNMatrix: internal error (N<0)");
                ap.assert(m >= 0, "APSERVIsFiniteOrNaNMatrix: internal error (M<0)");
                for (i = 0; i <= m - 1; i++)
                {
                    for (j = 0; j <= n - 1; j++)
                    {
                        if (!(math.isfinite(x[i, j]) | Double.IsNaN(x[i, j])))
                        {
                            result = false;
                            return result;
                        }
                    }
                }
                result = true;
                return result;
            }


            /*************************************************************************
            Safe sqrt(x^2+y^2)

              -- ALGLIB --
                 Copyright by Bochkanov Sergey
            *************************************************************************/
            public static double safepythag2(double x,
                double y)
            {
                double result = 0;
                double w = 0;
                double xabs = 0;
                double yabs = 0;
                double z = 0;

                xabs = Math.Abs(x);
                yabs = Math.Abs(y);
                w = Math.Max(xabs, yabs);
                z = Math.Min(xabs, yabs);
                if ((double)(z) == (double)(0))
                {
                    result = w;
                }
                else
                {
                    result = w * Math.Sqrt(1 + math.sqr(z / w));
                }
                return result;
            }


            /*************************************************************************
            Safe sqrt(x^2+y^2)

              -- ALGLIB --
                 Copyright by Bochkanov Sergey
            *************************************************************************/
            public static double safepythag3(double x,
                double y,
                double z)
            {
                double result = 0;
                double w = 0;

                w = Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z)));
                if ((double)(w) == (double)(0))
                {
                    result = 0;
                    return result;
                }
                x = x / w;
                y = y / w;
                z = z / w;
                result = w * Math.Sqrt(math.sqr(x) + math.sqr(y) + math.sqr(z));
                return result;
            }


            /*************************************************************************
            Safe division.

            This function attempts to calculate R=X/Y without overflow.

            It returns:
            * +1, if abs(X/Y)>=MaxRealNumber or undefined - overflow-like situation
                  (no overlfow is generated, R is either NAN, PosINF, NegINF)
            *  0, if MinRealNumber<abs(X/Y)<MaxRealNumber or X=0, Y<>0
                  (R contains result, may be zero)
            * -1, if 0<abs(X/Y)<MinRealNumber - underflow-like situation
                  (R contains zero; it corresponds to underflow)

            No overflow is generated in any case.

              -- ALGLIB --
                 Copyright by Bochkanov Sergey
            *************************************************************************/
            public static int saferdiv(double x,
                double y,
                ref double r)
            {
                int result = 0;

                r = 0;


                //
                // Two special cases:
                // * Y=0
                // * X=0 and Y<>0
                //
                if ((double)(y) == (double)(0))
                {
                    result = 1;
                    if ((double)(x) == (double)(0))
                    {
                        r = Double.NaN;
                    }
                    if ((double)(x) > (double)(0))
                    {
                        r = Double.PositiveInfinity;
                    }
                    if ((double)(x) < (double)(0))
                    {
                        r = Double.NegativeInfinity;
                    }
                    return result;
                }
                if ((double)(x) == (double)(0))
                {
                    r = 0;
                    result = 0;
                    return result;
                }

                //
                // make Y>0
                //
                if ((double)(y) < (double)(0))
                {
                    x = -x;
                    y = -y;
                }

                //
                //
                //
                if ((double)(y) >= (double)(1))
                {
                    r = x / y;
                    if ((double)(Math.Abs(r)) <= (double)(math.minrealnumber))
                    {
                        result = -1;
                        r = 0;
                    }
                    else
                    {
                        result = 0;
                    }
                }
                else
                {
                    if ((double)(Math.Abs(x)) >= (double)(math.maxrealnumber * y))
                    {
                        if ((double)(x) > (double)(0))
                        {
                            r = Double.PositiveInfinity;
                        }
                        else
                        {
                            r = Double.NegativeInfinity;
                        }
                        result = 1;
                    }
                    else
                    {
                        r = x / y;
                        result = 0;
                    }
                }
                return result;
            }


            /*************************************************************************
            This function calculates "safe" min(X/Y,V) for positive finite X, Y, V.
            No overflow is generated in any case.

              -- ALGLIB --
                 Copyright by Bochkanov Sergey
            *************************************************************************/
            public static double safeminposrv(double x,
                double y,
                double v)
            {
                double result = 0;
                double r = 0;

                if ((double)(y) >= (double)(1))
                {

                    //
                    // Y>=1, we can safely divide by Y
                    //
                    r = x / y;
                    result = v;
                    if ((double)(v) > (double)(r))
                    {
                        result = r;
                    }
                    else
                    {
                        result = v;
                    }
                }
                else
                {

                    //
                    // Y<1, we can safely multiply by Y
                    //
                    if ((double)(x) < (double)(v * y))
                    {
                        result = x / y;
                    }
                    else
                    {
                        result = v;
                    }
                }
                return result;
            }


            /*************************************************************************
            This function makes periodic mapping of X to [A,B].

            It accepts X, A, B (A>B). It returns T which lies in  [A,B] and integer K,
            such that X = T + K*(B-A).

            NOTES:
            * K is represented as real value, although actually it is integer
            * T is guaranteed to be in [A,B]
            * T replaces X

              -- ALGLIB --
                 Copyright by Bochkanov Sergey
            *************************************************************************/
            public static void apperiodicmap(ref double x,
                double a,
                double b,
                ref double k)
            {
                k = 0;

                ap.assert((double)(a) < (double)(b), "APPeriodicMap: internal error!");
                k = (int)Math.Floor((x - a) / (b - a));
                x = x - k * (b - a);
                while ((double)(x) < (double)(a))
                {
                    x = x + (b - a);
                    k = k - 1;
                }
                while ((double)(x) > (double)(b))
                {
                    x = x - (b - a);
                    k = k + 1;
                }
                x = Math.Max(x, a);
                x = Math.Min(x, b);
            }


            /*************************************************************************
            'bounds' value: maps X to [B1,B2]

              -- ALGLIB --
                 Copyright 20.03.2009 by Bochkanov Sergey
            *************************************************************************/
            public static double boundval(double x,
                double b1,
                double b2)
            {
                double result = 0;

                if ((double)(x) <= (double)(b1))
                {
                    result = b1;
                    return result;
                }
                if ((double)(x) >= (double)(b2))
                {
                    result = b2;
                    return result;
                }
                result = x;
                return result;
            }


            /*************************************************************************
            Allocation of serializer: complex value
            *************************************************************************/
            /*
            public static void alloccomplex(alglib.serializer s,
                complex v)
            {
                s.alloc_entry();
                s.alloc_entry();
            }

            */
            /*************************************************************************
            Serialization: complex value
            *************************************************************************/
            /*
            public static void serializecomplex(alglib.serializer s,
                complex v)
            {
                s.serialize_double(v.x);
                s.serialize_double(v.y);
            }

            */
            /*************************************************************************
            Unserialization: complex value
            *************************************************************************/
            /*
            public static complex unserializecomplex(alglib.serializer s)
            {
                complex result = 0;

                result.x = s.unserialize_double();
                result.y = s.unserialize_double();
                return result;
            }

            */
            /*************************************************************************
            Allocation of serializer: real array
            *************************************************************************/
            public static void allocrealarray(alglib.serializer s,
                double[] v,
                int n)
            {
                int i = 0;

                if (n < 0)
                {
                    n = ap.len(v);
                }
                s.alloc_entry();
                for (i = 0; i <= n - 1; i++)
                {
                    s.alloc_entry();
                }
            }


            /*************************************************************************
            Serialization: complex value
            *************************************************************************/
            public static void serializerealarray(alglib.serializer s,
                double[] v,
                int n)
            {
                int i = 0;

                if (n < 0)
                {
                    n = ap.len(v);
                }
                s.serialize_int(n);
                for (i = 0; i <= n - 1; i++)
                {
                    s.serialize_double(v[i]);
                }
            }


            /*************************************************************************
            Unserialization: complex value
            *************************************************************************/
            public static void unserializerealarray(alglib.serializer s,
                ref double[] v)
            {
                int n = 0;
                int i = 0;
                double t = 0;

                v = new double[0];

                n = s.unserialize_int();
                if (n == 0)
                {
                    return;
                }
                v = new double[n];
                for (i = 0; i <= n - 1; i++)
                {
                    t = s.unserialize_double();
                    v[i] = t;
                }
            }


            /*************************************************************************
            Allocation of serializer: Integer array
            *************************************************************************/
            public static void allocintegerarray(alglib.serializer s,
                int[] v,
                int n)
            {
                int i = 0;

                if (n < 0)
                {
                    n = ap.len(v);
                }
                s.alloc_entry();
                for (i = 0; i <= n - 1; i++)
                {
                    s.alloc_entry();
                }
            }


            /*************************************************************************
            Serialization: Integer array
            *************************************************************************/
            public static void serializeintegerarray(alglib.serializer s,
                int[] v,
                int n)
            {
                int i = 0;

                if (n < 0)
                {
                    n = ap.len(v);
                }
                s.serialize_int(n);
                for (i = 0; i <= n - 1; i++)
                {
                    s.serialize_int(v[i]);
                }
            }


            /*************************************************************************
            Unserialization: complex value
            *************************************************************************/
            public static void unserializeintegerarray(alglib.serializer s,
                ref int[] v)
            {
                int n = 0;
                int i = 0;
                int t = 0;

                v = new int[0];

                n = s.unserialize_int();
                if (n == 0)
                {
                    return;
                }
                v = new int[n];
                for (i = 0; i <= n - 1; i++)
                {
                    t = s.unserialize_int();
                    v[i] = t;
                }
            }


            /*************************************************************************
            Allocation of serializer: real matrix
            *************************************************************************/
            public static void allocrealmatrix(alglib.serializer s,
                double[,] v,
                int n0,
                int n1)
            {
                int i = 0;
                int j = 0;

                if (n0 < 0)
                {
                    n0 = ap.rows(v);
                }
                if (n1 < 0)
                {
                    n1 = ap.cols(v);
                }
                s.alloc_entry();
                s.alloc_entry();
                for (i = 0; i <= n0 - 1; i++)
                {
                    for (j = 0; j <= n1 - 1; j++)
                    {
                        s.alloc_entry();
                    }
                }
            }


            /*************************************************************************
            Serialization: complex value
            *************************************************************************/
            public static void serializerealmatrix(alglib.serializer s,
                double[,] v,
                int n0,
                int n1)
            {
                int i = 0;
                int j = 0;

                if (n0 < 0)
                {
                    n0 = ap.rows(v);
                }
                if (n1 < 0)
                {
                    n1 = ap.cols(v);
                }
                s.serialize_int(n0);
                s.serialize_int(n1);
                for (i = 0; i <= n0 - 1; i++)
                {
                    for (j = 0; j <= n1 - 1; j++)
                    {
                        s.serialize_double(v[i, j]);
                    }
                }
            }


            /*************************************************************************
            Unserialization: complex value
            *************************************************************************/
            public static void unserializerealmatrix(alglib.serializer s,
                ref double[,] v)
            {
                int i = 0;
                int j = 0;
                int n0 = 0;
                int n1 = 0;
                double t = 0;

                v = new double[0, 0];

                n0 = s.unserialize_int();
                n1 = s.unserialize_int();
                if (n0 == 0 | n1 == 0)
                {
                    return;
                }
                v = new double[n0, n1];
                for (i = 0; i <= n0 - 1; i++)
                {
                    for (j = 0; j <= n1 - 1; j++)
                    {
                        t = s.unserialize_double();
                        v[i, j] = t;
                    }
                }
            }


            /*************************************************************************
            Copy integer array
            *************************************************************************/
            public static void copyintegerarray(int[] src,
                ref int[] dst)
            {
                int i = 0;

                dst = new int[0];

                if (ap.len(src) > 0)
                {
                    dst = new int[ap.len(src)];
                    for (i = 0; i <= ap.len(src) - 1; i++)
                    {
                        dst[i] = src[i];
                    }
                }
            }


            /*************************************************************************
            Copy real array
            *************************************************************************/
            public static void copyrealarray(double[] src,
                ref double[] dst)
            {
                int i = 0;

                dst = new double[0];

                if (ap.len(src) > 0)
                {
                    dst = new double[ap.len(src)];
                    for (i = 0; i <= ap.len(src) - 1; i++)
                    {
                        dst[i] = src[i];
                    }
                }
            }


            /*************************************************************************
            Copy real matrix
            *************************************************************************/
            public static void copyrealmatrix(double[,] src,
                ref double[,] dst)
            {
                int i = 0;
                int j = 0;

                dst = new double[0, 0];

                if (ap.rows(src) > 0 & ap.cols(src) > 0)
                {
                    dst = new double[ap.rows(src), ap.cols(src)];
                    for (i = 0; i <= ap.rows(src) - 1; i++)
                    {
                        for (j = 0; j <= ap.cols(src) - 1; j++)
                        {
                            dst[i, j] = src[i, j];
                        }
                    }
                }
            }


            /*************************************************************************
            This function searches integer array. Elements in this array are actually
            records, each NRec elements wide. Each record has unique header - NHeader
            integer values, which identify it. Records are lexicographically sorted by
            header.

            Records are identified by their index, not offset (offset = NRec*index).

            This function searches A (records with indices [I0,I1)) for a record with
            header B. It returns index of this record (not offset!), or -1 on failure.

              -- ALGLIB --
                 Copyright 28.03.2011 by Bochkanov Sergey
            *************************************************************************/
            public static int recsearch(ref int[] a,
                int nrec,
                int nheader,
                int i0,
                int i1,
                int[] b)
            {
                int result = 0;
                int mididx = 0;
                int cflag = 0;
                int k = 0;
                int offs = 0;

                result = -1;
                while (true)
                {
                    if (i0 >= i1)
                    {
                        break;
                    }
                    mididx = (i0 + i1) / 2;
                    offs = nrec * mididx;
                    cflag = 0;
                    for (k = 0; k <= nheader - 1; k++)
                    {
                        if (a[offs + k] < b[k])
                        {
                            cflag = -1;
                            break;
                        }
                        if (a[offs + k] > b[k])
                        {
                            cflag = 1;
                            break;
                        }
                    }
                    if (cflag == 0)
                    {
                        result = mididx;
                        return result;
                    }
                    if (cflag < 0)
                    {
                        i0 = mididx + 1;
                    }
                    else
                    {
                        i1 = mididx;
                    }
                }
                return result;
            }


        }
        public class tsort
        {
            /*************************************************************************
            This function sorts array of real keys by ascending.

            Its results are:
            * sorted array A
            * permutation tables P1, P2

            Algorithm outputs permutation tables using two formats:
            * as usual permutation of [0..N-1]. If P1[i]=j, then sorted A[i]  contains
              value which was moved there from J-th position.
            * as a sequence of pairwise permutations. Sorted A[] may  be  obtained  by
              swaping A[i] and A[P2[i]] for all i from 0 to N-1.
          
            INPUT PARAMETERS:
                A       -   unsorted array
                N       -   array size

            OUPUT PARAMETERS:
                A       -   sorted array
                P1, P2  -   permutation tables, array[N]
            
            NOTES:
                this function assumes that A[] is finite; it doesn't checks that
                condition. All other conditions (size of input arrays, etc.) are not
                checked too.

              -- ALGLIB --
                 Copyright 14.05.2008 by Bochkanov Sergey
            *************************************************************************/
            public static void tagsort(ref double[] a,
                int n,
                ref int[] p1,
                ref int[] p2)
            {
                apserv.apbuffers buf = new apserv.apbuffers();

                p1 = new int[0];
                p2 = new int[0];

                tagsortbuf(ref a, n, ref p1, ref p2, buf);
            }


            /*************************************************************************
            Buffered variant of TagSort, which accepts preallocated output arrays as
            well as special structure for buffered allocations. If arrays are too
            short, they are reallocated. If they are large enough, no memory
            allocation is done.

            It is intended to be used in the performance-critical parts of code, where
            additional allocations can lead to severe performance degradation

              -- ALGLIB --
                 Copyright 14.05.2008 by Bochkanov Sergey
            *************************************************************************/
            public static void tagsortbuf(ref double[] a,
                int n,
                ref int[] p1,
                ref int[] p2,
                apserv.apbuffers buf)
            {
                int i = 0;
                int lv = 0;
                int lp = 0;
                int rv = 0;
                int rp = 0;


                //
                // Special cases
                //
                if (n <= 0)
                {
                    return;
                }
                if (n == 1)
                {
                    apserv.ivectorsetlengthatleast(ref p1, 1);
                    apserv.ivectorsetlengthatleast(ref p2, 1);
                    p1[0] = 0;
                    p2[0] = 0;
                    return;
                }

                //
                // General case, N>1: prepare permutations table P1
                //
                apserv.ivectorsetlengthatleast(ref p1, n);
                for (i = 0; i <= n - 1; i++)
                {
                    p1[i] = i;
                }

                //
                // General case, N>1: sort, update P1
                //
                apserv.rvectorsetlengthatleast(ref buf.ra0, n);
                apserv.ivectorsetlengthatleast(ref buf.ia0, n);
                tagsortfasti(ref a, ref p1, ref buf.ra0, ref buf.ia0, n);

                //
                // General case, N>1: fill permutations table P2
                //
                // To fill P2 we maintain two arrays:
                // * PV (Buf.IA0), Position(Value). PV[i] contains position of I-th key at the moment
                // * VP (Buf.IA1), Value(Position). VP[i] contains key which has position I at the moment
                //
                // At each step we making permutation of two items:
                //   Left, which is given by position/value pair LP/LV
                //   and Right, which is given by RP/RV
                // and updating PV[] and VP[] correspondingly.
                //
                apserv.ivectorsetlengthatleast(ref buf.ia0, n);
                apserv.ivectorsetlengthatleast(ref buf.ia1, n);
                apserv.ivectorsetlengthatleast(ref p2, n);
                for (i = 0; i <= n - 1; i++)
                {
                    buf.ia0[i] = i;
                    buf.ia1[i] = i;
                }
                for (i = 0; i <= n - 1; i++)
                {

                    //
                    // calculate LP, LV, RP, RV
                    //
                    lp = i;
                    lv = buf.ia1[lp];
                    rv = p1[i];
                    rp = buf.ia0[rv];

                    //
                    // Fill P2
                    //
                    p2[i] = rp;

                    //
                    // update PV and VP
                    //
                    buf.ia1[lp] = rv;
                    buf.ia1[rp] = lv;
                    buf.ia0[lv] = rp;
                    buf.ia0[rv] = lp;
                }
            }


            /*************************************************************************
            Same as TagSort, but optimized for real keys and integer labels.

            A is sorted, and same permutations are applied to B.

            NOTES:
            1.  this function assumes that A[] is finite; it doesn't checks that
                condition. All other conditions (size of input arrays, etc.) are not
                checked too.
            2.  this function uses two buffers, BufA and BufB, each is N elements large.
                They may be preallocated (which will save some time) or not, in which
                case function will automatically allocate memory.

              -- ALGLIB --
                 Copyright 11.12.2008 by Bochkanov Sergey
            *************************************************************************/
            public static void tagsortfasti(ref double[] a,
                ref int[] b,
                ref double[] bufa,
                ref int[] bufb,
                int n)
            {
                int i = 0;
                int j = 0;
                bool isascending = new bool();
                bool isdescending = new bool();
                double tmpr = 0;
                int tmpi = 0;


                //
                // Special case
                //
                if (n <= 1)
                {
                    return;
                }

                //
                // Test for already sorted set
                //
                isascending = true;
                isdescending = true;
                for (i = 1; i <= n - 1; i++)
                {
                    isascending = isascending & a[i] >= a[i - 1];
                    isdescending = isdescending & a[i] <= a[i - 1];
                }
                if (isascending)
                {
                    return;
                }
                if (isdescending)
                {
                    for (i = 0; i <= n - 1; i++)
                    {
                        j = n - 1 - i;
                        if (j <= i)
                        {
                            break;
                        }
                        tmpr = a[i];
                        a[i] = a[j];
                        a[j] = tmpr;
                        tmpi = b[i];
                        b[i] = b[j];
                        b[j] = tmpi;
                    }
                    return;
                }

                //
                // General case
                //
                if (ap.len(bufa) < n)
                {
                    bufa = new double[n];
                }
                if (ap.len(bufb) < n)
                {
                    bufb = new int[n];
                }
                tagsortfastirec(ref a, ref b, ref bufa, ref bufb, 0, n - 1);
            }


            /*************************************************************************
            Same as TagSort, but optimized for real keys and real labels.

            A is sorted, and same permutations are applied to B.

            NOTES:
            1.  this function assumes that A[] is finite; it doesn't checks that
                condition. All other conditions (size of input arrays, etc.) are not
                checked too.
            2.  this function uses two buffers, BufA and BufB, each is N elements large.
                They may be preallocated (which will save some time) or not, in which
                case function will automatically allocate memory.

              -- ALGLIB --
                 Copyright 11.12.2008 by Bochkanov Sergey
            *************************************************************************/
            public static void tagsortfastr(ref double[] a,
                ref double[] b,
                ref double[] bufa,
                ref double[] bufb,
                int n)
            {
                int i = 0;
                int j = 0;
                bool isascending = new bool();
                bool isdescending = new bool();
                double tmpr = 0;


                //
                // Special case
                //
                if (n <= 1)
                {
                    return;
                }

                //
                // Test for already sorted set
                //
                isascending = true;
                isdescending = true;
                for (i = 1; i <= n - 1; i++)
                {
                    isascending = isascending & a[i] >= a[i - 1];
                    isdescending = isdescending & a[i] <= a[i - 1];
                }
                if (isascending)
                {
                    return;
                }
                if (isdescending)
                {
                    for (i = 0; i <= n - 1; i++)
                    {
                        j = n - 1 - i;
                        if (j <= i)
                        {
                            break;
                        }
                        tmpr = a[i];
                        a[i] = a[j];
                        a[j] = tmpr;
                        tmpr = b[i];
                        b[i] = b[j];
                        b[j] = tmpr;
                    }
                    return;
                }

                //
                // General case
                //
                if (ap.len(bufa) < n)
                {
                    bufa = new double[n];
                }
                if (ap.len(bufb) < n)
                {
                    bufb = new double[n];
                }
                tagsortfastrrec(ref a, ref b, ref bufa, ref bufb, 0, n - 1);
            }


            /*************************************************************************
            Same as TagSort, but optimized for real keys without labels.

            A is sorted, and that's all.

            NOTES:
            1.  this function assumes that A[] is finite; it doesn't checks that
                condition. All other conditions (size of input arrays, etc.) are not
                checked too.
            2.  this function uses buffer, BufA, which is N elements large. It may be
                preallocated (which will save some time) or not, in which case
                function will automatically allocate memory.

              -- ALGLIB --
                 Copyright 11.12.2008 by Bochkanov Sergey
            *************************************************************************/
            public static void tagsortfast(ref double[] a,
                ref double[] bufa,
                int n)
            {
                int i = 0;
                int j = 0;
                bool isascending = new bool();
                bool isdescending = new bool();
                double tmpr = 0;


                //
                // Special case
                //
                if (n <= 1)
                {
                    return;
                }

                //
                // Test for already sorted set
                //
                isascending = true;
                isdescending = true;
                for (i = 1; i <= n - 1; i++)
                {
                    isascending = isascending & a[i] >= a[i - 1];
                    isdescending = isdescending & a[i] <= a[i - 1];
                }
                if (isascending)
                {
                    return;
                }
                if (isdescending)
                {
                    for (i = 0; i <= n - 1; i++)
                    {
                        j = n - 1 - i;
                        if (j <= i)
                        {
                            break;
                        }
                        tmpr = a[i];
                        a[i] = a[j];
                        a[j] = tmpr;
                    }
                    return;
                }

                //
                // General case
                //
                if (ap.len(bufa) < n)
                {
                    bufa = new double[n];
                }
                tagsortfastrec(ref a, ref bufa, 0, n - 1);
            }


            /*************************************************************************
            Heap operations: adds element to the heap

            PARAMETERS:
                A       -   heap itself, must be at least array[0..N]
                B       -   array of integer tags, which are updated according to
                            permutations in the heap
                N       -   size of the heap (without new element).
                            updated on output
                VA      -   value of the element being added
                VB      -   value of the tag

              -- ALGLIB --
                 Copyright 28.02.2010 by Bochkanov Sergey
            *************************************************************************/
            public static void tagheappushi(ref double[] a,
                ref int[] b,
                ref int n,
                double va,
                int vb)
            {
                int j = 0;
                int k = 0;
                double v = 0;

                if (n < 0)
                {
                    return;
                }

                //
                // N=0 is a special case
                //
                if (n == 0)
                {
                    a[0] = va;
                    b[0] = vb;
                    n = n + 1;
                    return;
                }

                //
                // add current point to the heap
                // (add to the bottom, then move up)
                //
                // we don't write point to the heap
                // until its final position is determined
                // (it allow us to reduce number of array access operations)
                //
                j = n;
                n = n + 1;
                while (j > 0)
                {
                    k = (j - 1) / 2;
                    v = a[k];
                    if ((double)(v) < (double)(va))
                    {

                        //
                        // swap with higher element
                        //
                        a[j] = v;
                        b[j] = b[k];
                        j = k;
                    }
                    else
                    {

                        //
                        // element in its place. terminate.
                        //
                        break;
                    }
                }
                a[j] = va;
                b[j] = vb;
            }


            /*************************************************************************
            Heap operations: replaces top element with new element
            (which is moved down)

            PARAMETERS:
                A       -   heap itself, must be at least array[0..N-1]
                B       -   array of integer tags, which are updated according to
                            permutations in the heap
                N       -   size of the heap
                VA      -   value of the element which replaces top element
                VB      -   value of the tag

              -- ALGLIB --
                 Copyright 28.02.2010 by Bochkanov Sergey
            *************************************************************************/
            public static void tagheapreplacetopi(ref double[] a,
                ref int[] b,
                int n,
                double va,
                int vb)
            {
                int j = 0;
                int k1 = 0;
                int k2 = 0;
                double v = 0;
                double v1 = 0;
                double v2 = 0;

                if (n < 1)
                {
                    return;
                }

                //
                // N=1 is a special case
                //
                if (n == 1)
                {
                    a[0] = va;
                    b[0] = vb;
                    return;
                }

                //
                // move down through heap:
                // * J  -   current element
                // * K1 -   first child (always exists)
                // * K2 -   second child (may not exists)
                //
                // we don't write point to the heap
                // until its final position is determined
                // (it allow us to reduce number of array access operations)
                //
                j = 0;
                k1 = 1;
                k2 = 2;
                while (k1 < n)
                {
                    if (k2 >= n)
                    {

                        //
                        // only one child.
                        //
                        // swap and terminate (because this child
                        // have no siblings due to heap structure)
                        //
                        v = a[k1];
                        if ((double)(v) > (double)(va))
                        {
                            a[j] = v;
                            b[j] = b[k1];
                            j = k1;
                        }
                        break;
                    }
                    else
                    {

                        //
                        // two childs
                        //
                        v1 = a[k1];
                        v2 = a[k2];
                        if ((double)(v1) > (double)(v2))
                        {
                            if ((double)(va) < (double)(v1))
                            {
                                a[j] = v1;
                                b[j] = b[k1];
                                j = k1;
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            if ((double)(va) < (double)(v2))
                            {
                                a[j] = v2;
                                b[j] = b[k2];
                                j = k2;
                            }
                            else
                            {
                                break;
                            }
                        }
                        k1 = 2 * j + 1;
                        k2 = 2 * j + 2;
                    }
                }
                a[j] = va;
                b[j] = vb;
            }


            /*************************************************************************
            Heap operations: pops top element from the heap

            PARAMETERS:
                A       -   heap itself, must be at least array[0..N-1]
                B       -   array of integer tags, which are updated according to
                            permutations in the heap
                N       -   size of the heap, N>=1

            On output top element is moved to A[N-1], B[N-1], heap is reordered, N is
            decreased by 1.

              -- ALGLIB --
                 Copyright 28.02.2010 by Bochkanov Sergey
            *************************************************************************/
            public static void tagheappopi(ref double[] a,
                ref int[] b,
                ref int n)
            {
                double va = 0;
                int vb = 0;

                if (n < 1)
                {
                    return;
                }

                //
                // N=1 is a special case
                //
                if (n == 1)
                {
                    n = 0;
                    return;
                }

                //
                // swap top element and last element,
                // then reorder heap
                //
                va = a[n - 1];
                vb = b[n - 1];
                a[n - 1] = a[0];
                b[n - 1] = b[0];
                n = n - 1;
                tagheapreplacetopi(ref a, ref b, n, va, vb);
            }


            /*************************************************************************
            Internal TagSortFastI: sorts A[I1...I2] (both bounds are included),
            applies same permutations to B.

              -- ALGLIB --
                 Copyright 06.09.2010 by Bochkanov Sergey
            *************************************************************************/
            private static void tagsortfastirec(ref double[] a,
                ref int[] b,
                ref double[] bufa,
                ref int[] bufb,
                int i1,
                int i2)
            {
                int i = 0;
                int j = 0;
                int k = 0;
                int cntless = 0;
                int cnteq = 0;
                int cntgreater = 0;
                double tmpr = 0;
                int tmpi = 0;
                double v0 = 0;
                double v1 = 0;
                double v2 = 0;
                double vp = 0;


                //
                // Fast exit
                //
                if (i2 <= i1)
                {
                    return;
                }

                //
                // Non-recursive sort for small arrays
                //
                if (i2 - i1 <= 16)
                {
                    for (j = i1 + 1; j <= i2; j++)
                    {

                        //
                        // Search elements [I1..J-1] for place to insert Jth element.
                        //
                        // This code stops immediately if we can leave A[J] at J-th position
                        // (all elements have same value of A[J] larger than any of them)
                        //
                        tmpr = a[j];
                        tmpi = j;
                        for (k = j - 1; k >= i1; k--)
                        {
                            if (a[k] <= tmpr)
                            {
                                break;
                            }
                            tmpi = k;
                        }
                        k = tmpi;

                        //
                        // Insert Jth element into Kth position
                        //
                        if (k != j)
                        {
                            tmpr = a[j];
                            tmpi = b[j];
                            for (i = j - 1; i >= k; i--)
                            {
                                a[i + 1] = a[i];
                                b[i + 1] = b[i];
                            }
                            a[k] = tmpr;
                            b[k] = tmpi;
                        }
                    }
                    return;
                }

                //
                // Quicksort: choose pivot
                // Here we assume that I2-I1>=2
                //
                v0 = a[i1];
                v1 = a[i1 + (i2 - i1) / 2];
                v2 = a[i2];
                if (v0 > v1)
                {
                    tmpr = v1;
                    v1 = v0;
                    v0 = tmpr;
                }
                if (v1 > v2)
                {
                    tmpr = v2;
                    v2 = v1;
                    v1 = tmpr;
                }
                if (v0 > v1)
                {
                    tmpr = v1;
                    v1 = v0;
                    v0 = tmpr;
                }
                vp = v1;

                //
                // now pass through A/B and:
                // * move elements that are LESS than VP to the left of A/B
                // * move elements that are EQUAL to VP to the right of BufA/BufB (in the reverse order)
                // * move elements that are GREATER than VP to the left of BufA/BufB (in the normal order
                // * move elements from the tail of BufA/BufB to the middle of A/B (restoring normal order)
                // * move elements from the left of BufA/BufB to the end of A/B
                //
                cntless = 0;
                cnteq = 0;
                cntgreater = 0;
                for (i = i1; i <= i2; i++)
                {
                    v0 = a[i];
                    if (v0 < vp)
                    {

                        //
                        // LESS
                        //
                        k = i1 + cntless;
                        if (i != k)
                        {
                            a[k] = v0;
                            b[k] = b[i];
                        }
                        cntless = cntless + 1;
                        continue;
                    }
                    if (v0 == vp)
                    {

                        //
                        // EQUAL
                        //
                        k = i2 - cnteq;
                        bufa[k] = v0;
                        bufb[k] = b[i];
                        cnteq = cnteq + 1;
                        continue;
                    }

                    //
                    // GREATER
                    //
                    k = i1 + cntgreater;
                    bufa[k] = v0;
                    bufb[k] = b[i];
                    cntgreater = cntgreater + 1;
                }
                for (i = 0; i <= cnteq - 1; i++)
                {
                    j = i1 + cntless + cnteq - 1 - i;
                    k = i2 + i - (cnteq - 1);
                    a[j] = bufa[k];
                    b[j] = bufb[k];
                }
                for (i = 0; i <= cntgreater - 1; i++)
                {
                    j = i1 + cntless + cnteq + i;
                    k = i1 + i;
                    a[j] = bufa[k];
                    b[j] = bufb[k];
                }

                //
                // Sort left and right parts of the array (ignoring middle part)
                //
                tagsortfastirec(ref a, ref b, ref bufa, ref bufb, i1, i1 + cntless - 1);
                tagsortfastirec(ref a, ref b, ref bufa, ref bufb, i1 + cntless + cnteq, i2);
            }


            /*************************************************************************
            Internal TagSortFastR: sorts A[I1...I2] (both bounds are included),
            applies same permutations to B.

              -- ALGLIB --
                 Copyright 06.09.2010 by Bochkanov Sergey
            *************************************************************************/
            private static void tagsortfastrrec(ref double[] a,
                ref double[] b,
                ref double[] bufa,
                ref double[] bufb,
                int i1,
                int i2)
            {
                int i = 0;
                int j = 0;
                int k = 0;
                double tmpr = 0;
                double tmpr2 = 0;
                int tmpi = 0;
                int cntless = 0;
                int cnteq = 0;
                int cntgreater = 0;
                double v0 = 0;
                double v1 = 0;
                double v2 = 0;
                double vp = 0;


                //
                // Fast exit
                //
                if (i2 <= i1)
                {
                    return;
                }

                //
                // Non-recursive sort for small arrays
                //
                if (i2 - i1 <= 16)
                {
                    for (j = i1 + 1; j <= i2; j++)
                    {

                        //
                        // Search elements [I1..J-1] for place to insert Jth element.
                        //
                        // This code stops immediatly if we can leave A[J] at J-th position
                        // (all elements have same value of A[J] larger than any of them)
                        //
                        tmpr = a[j];
                        tmpi = j;
                        for (k = j - 1; k >= i1; k--)
                        {
                            if (a[k] <= tmpr)
                            {
                                break;
                            }
                            tmpi = k;
                        }
                        k = tmpi;

                        //
                        // Insert Jth element into Kth position
                        //
                        if (k != j)
                        {
                            tmpr = a[j];
                            tmpr2 = b[j];
                            for (i = j - 1; i >= k; i--)
                            {
                                a[i + 1] = a[i];
                                b[i + 1] = b[i];
                            }
                            a[k] = tmpr;
                            b[k] = tmpr2;
                        }
                    }
                    return;
                }

                //
                // Quicksort: choose pivot
                // Here we assume that I2-I1>=16
                //
                v0 = a[i1];
                v1 = a[i1 + (i2 - i1) / 2];
                v2 = a[i2];
                if (v0 > v1)
                {
                    tmpr = v1;
                    v1 = v0;
                    v0 = tmpr;
                }
                if (v1 > v2)
                {
                    tmpr = v2;
                    v2 = v1;
                    v1 = tmpr;
                }
                if (v0 > v1)
                {
                    tmpr = v1;
                    v1 = v0;
                    v0 = tmpr;
                }
                vp = v1;

                //
                // now pass through A/B and:
                // * move elements that are LESS than VP to the left of A/B
                // * move elements that are EQUAL to VP to the right of BufA/BufB (in the reverse order)
                // * move elements that are GREATER than VP to the left of BufA/BufB (in the normal order
                // * move elements from the tail of BufA/BufB to the middle of A/B (restoring normal order)
                // * move elements from the left of BufA/BufB to the end of A/B
                //
                cntless = 0;
                cnteq = 0;
                cntgreater = 0;
                for (i = i1; i <= i2; i++)
                {
                    v0 = a[i];
                    if (v0 < vp)
                    {

                        //
                        // LESS
                        //
                        k = i1 + cntless;
                        if (i != k)
                        {
                            a[k] = v0;
                            b[k] = b[i];
                        }
                        cntless = cntless + 1;
                        continue;
                    }
                    if (v0 == vp)
                    {

                        //
                        // EQUAL
                        //
                        k = i2 - cnteq;
                        bufa[k] = v0;
                        bufb[k] = b[i];
                        cnteq = cnteq + 1;
                        continue;
                    }

                    //
                    // GREATER
                    //
                    k = i1 + cntgreater;
                    bufa[k] = v0;
                    bufb[k] = b[i];
                    cntgreater = cntgreater + 1;
                }
                for (i = 0; i <= cnteq - 1; i++)
                {
                    j = i1 + cntless + cnteq - 1 - i;
                    k = i2 + i - (cnteq - 1);
                    a[j] = bufa[k];
                    b[j] = bufb[k];
                }
                for (i = 0; i <= cntgreater - 1; i++)
                {
                    j = i1 + cntless + cnteq + i;
                    k = i1 + i;
                    a[j] = bufa[k];
                    b[j] = bufb[k];
                }

                //
                // Sort left and right parts of the array (ignoring middle part)
                //
                tagsortfastrrec(ref a, ref b, ref bufa, ref bufb, i1, i1 + cntless - 1);
                tagsortfastrrec(ref a, ref b, ref bufa, ref bufb, i1 + cntless + cnteq, i2);
            }


            /*************************************************************************
            Internal TagSortFastI: sorts A[I1...I2] (both bounds are included),
            applies same permutations to B.

              -- ALGLIB --
                 Copyright 06.09.2010 by Bochkanov Sergey
            *************************************************************************/
            private static void tagsortfastrec(ref double[] a,
                ref double[] bufa,
                int i1,
                int i2)
            {
                int cntless = 0;
                int cnteq = 0;
                int cntgreater = 0;
                int i = 0;
                int j = 0;
                int k = 0;
                double tmpr = 0;
                int tmpi = 0;
                double v0 = 0;
                double v1 = 0;
                double v2 = 0;
                double vp = 0;


                //
                // Fast exit
                //
                if (i2 <= i1)
                {
                    return;
                }

                //
                // Non-recursive sort for small arrays
                //
                if (i2 - i1 <= 16)
                {
                    for (j = i1 + 1; j <= i2; j++)
                    {

                        //
                        // Search elements [I1..J-1] for place to insert Jth element.
                        //
                        // This code stops immediatly if we can leave A[J] at J-th position
                        // (all elements have same value of A[J] larger than any of them)
                        //
                        tmpr = a[j];
                        tmpi = j;
                        for (k = j - 1; k >= i1; k--)
                        {
                            if (a[k] <= tmpr)
                            {
                                break;
                            }
                            tmpi = k;
                        }
                        k = tmpi;

                        //
                        // Insert Jth element into Kth position
                        //
                        if (k != j)
                        {
                            tmpr = a[j];
                            for (i = j - 1; i >= k; i--)
                            {
                                a[i + 1] = a[i];
                            }
                            a[k] = tmpr;
                        }
                    }
                    return;
                }

                //
                // Quicksort: choose pivot
                // Here we assume that I2-I1>=16
                //
                v0 = a[i1];
                v1 = a[i1 + (i2 - i1) / 2];
                v2 = a[i2];
                if (v0 > v1)
                {
                    tmpr = v1;
                    v1 = v0;
                    v0 = tmpr;
                }
                if (v1 > v2)
                {
                    tmpr = v2;
                    v2 = v1;
                    v1 = tmpr;
                }
                if (v0 > v1)
                {
                    tmpr = v1;
                    v1 = v0;
                    v0 = tmpr;
                }
                vp = v1;

                //
                // now pass through A/B and:
                // * move elements that are LESS than VP to the left of A/B
                // * move elements that are EQUAL to VP to the right of BufA/BufB (in the reverse order)
                // * move elements that are GREATER than VP to the left of BufA/BufB (in the normal order
                // * move elements from the tail of BufA/BufB to the middle of A/B (restoring normal order)
                // * move elements from the left of BufA/BufB to the end of A/B
                //
                cntless = 0;
                cnteq = 0;
                cntgreater = 0;
                for (i = i1; i <= i2; i++)
                {
                    v0 = a[i];
                    if (v0 < vp)
                    {

                        //
                        // LESS
                        //
                        k = i1 + cntless;
                        if (i != k)
                        {
                            a[k] = v0;
                        }
                        cntless = cntless + 1;
                        continue;
                    }
                    if (v0 == vp)
                    {

                        //
                        // EQUAL
                        //
                        k = i2 - cnteq;
                        bufa[k] = v0;
                        cnteq = cnteq + 1;
                        continue;
                    }

                    //
                    // GREATER
                    //
                    k = i1 + cntgreater;
                    bufa[k] = v0;
                    cntgreater = cntgreater + 1;
                }
                for (i = 0; i <= cnteq - 1; i++)
                {
                    j = i1 + cntless + cnteq - 1 - i;
                    k = i2 + i - (cnteq - 1);
                    a[j] = bufa[k];
                }
                for (i = 0; i <= cntgreater - 1; i++)
                {
                    j = i1 + cntless + cnteq + i;
                    k = i1 + i;
                    a[j] = bufa[k];
                }

                //
                // Sort left and right parts of the array (ignoring middle part)
                //
                tagsortfastrec(ref a, ref bufa, i1, i1 + cntless - 1);
                tagsortfastrec(ref a, ref bufa, i1 + cntless + cnteq, i2);
            }


        }

        public class scodes
        {
            public static int getrdfserializationcode()
            {
                int result = 0;

                result = 1;
                return result;
            }


            public static int getkdtreeserializationcode()
            {
                int result = 0;

                result = 2;
                return result;
            }


            public static int getmlpserializationcode()
            {
                int result = 0;

                result = 3;
                return result;
            }


        }

        /********************************************************************
    internal functions
    ********************************************************************/
        public class ap
        {
            public static int len<T>(T[] a)
            { return a.Length; }
            public static int rows<T>(T[,] a)
            { return a.GetLength(0); }
            public static int cols<T>(T[,] a)
            { return a.GetLength(1); }
            public static void swap<T>(ref T a, ref T b)
            {
                T t = a;
                a = b;
                b = t;
            }

            public static void assert(bool cond, string s)
            {
                if (!cond)
                    throw new alglibexception(s);
            }

            public static void assert(bool cond)
            {
                assert(cond, "ALGLIB: assertion failed");
            }

            /****************************************************************
            returns dps (digits-of-precision) value corresponding to threshold.
            dps(0.9)  = dps(0.5)  = dps(0.1) = 0
            dps(0.09) = dps(0.05) = dps(0.01) = 1
            and so on
            ****************************************************************/
            public static int threshold2dps(double threshold)
            {
                int result = 0;
                double t;
                for (result = 0, t = 1; t / 10 > threshold * (1 + 1E-10); result++, t /= 10) ;
                return result;
            }

            /****************************************************************
            prints formatted complex
            ****************************************************************/
            /*
            public static string format(complex a, int _dps)
            {
                int dps = Math.Abs(_dps);
                string fmt = _dps >= 0 ? "F" : "E";
                string fmtx = String.Format("{{0:" + fmt + "{0}}}", dps);
                string fmty = String.Format("{{0:" + fmt + "{0}}}", dps);
                string result = String.Format(fmtx, a.x) + (a.y >= 0 ? "+" : "-") + String.Format(fmty, Math.Abs(a.y)) + "i";
                result = result.Replace(',', '.');
                return result;
            }
            */
            /****************************************************************
            prints formatted array
            ****************************************************************/
            public static string format(bool[] a)
            {
                string[] result = new string[len(a)];
                int i;
                for (i = 0; i < len(a); i++)
                    if (a[i])
                        result[i] = "true";
                    else
                        result[i] = "false";
                return "{" + String.Join(",", result) + "}";
            }

            /****************************************************************
            prints formatted array
            ****************************************************************/
            public static string format(int[] a)
            {
                string[] result = new string[len(a)];
                int i;
                for (i = 0; i < len(a); i++)
                    result[i] = a[i].ToString();
                return "{" + String.Join(",", result) + "}";
            }

            /****************************************************************
            prints formatted array
            ****************************************************************/
            public static string format(double[] a, int _dps)
            {
                int dps = Math.Abs(_dps);
                string sfmt = _dps >= 0 ? "F" : "E";
                string fmt = String.Format("{{0:" + sfmt + "{0}}}", dps);
                string[] result = new string[len(a)];
                int i;
                for (i = 0; i < len(a); i++)
                {
                    result[i] = String.Format(fmt, a[i]);
                    result[i] = result[i].Replace(',', '.');
                }
                return "{" + String.Join(",", result) + "}";
            }

            /****************************************************************
            prints formatted array
            ****************************************************************/
            /*
            public static string format(complex[] a, int _dps)
            {
                int dps = Math.Abs(_dps);
                string fmt = _dps >= 0 ? "F" : "E";
                string fmtx = String.Format("{{0:" + fmt + "{0}}}", dps);
                string fmty = String.Format("{{0:" + fmt + "{0}}}", dps);
                string[] result = new string[len(a)];
                int i;
                for (i = 0; i < len(a); i++)
                {
                    result[i] = String.Format(fmtx, a[i].x) + (a[i].y >= 0 ? "+" : "-") + String.Format(fmty, Math.Abs(a[i].y)) + "i";
                    result[i] = result[i].Replace(',', '.');
                }
                return "{" + String.Join(",", result) + "}";
            }
            */
            /****************************************************************
            prints formatted matrix
            ****************************************************************/
            public static string format(bool[,] a)
            {
                int i, j, m, n;
                n = cols(a);
                m = rows(a);
                bool[] line = new bool[n];
                string[] result = new string[m];
                for (i = 0; i < m; i++)
                {
                    for (j = 0; j < n; j++)
                        line[j] = a[i, j];
                    result[i] = format(line);
                }
                return "{" + String.Join(",", result) + "}";
            }

            /****************************************************************
            prints formatted matrix
            ****************************************************************/
            public static string format(int[,] a)
            {
                int i, j, m, n;
                n = cols(a);
                m = rows(a);
                int[] line = new int[n];
                string[] result = new string[m];
                for (i = 0; i < m; i++)
                {
                    for (j = 0; j < n; j++)
                        line[j] = a[i, j];
                    result[i] = format(line);
                }
                return "{" + String.Join(",", result) + "}";
            }

            /****************************************************************
            prints formatted matrix
            ****************************************************************/
            public static string format(double[,] a, int dps)
            {
                int i, j, m, n;
                n = cols(a);
                m = rows(a);
                double[] line = new double[n];
                string[] result = new string[m];
                for (i = 0; i < m; i++)
                {
                    for (j = 0; j < n; j++)
                        line[j] = a[i, j];
                    result[i] = format(line, dps);
                }
                return "{" + String.Join(",", result) + "}";
            }

            /****************************************************************
            prints formatted matrix
            ****************************************************************/
            /*
            public static string format(complex[,] a, int dps)
            {
                int i, j, m, n;
                n = cols(a);
                m = rows(a);
                complex[] line = new complex[n];
                string[] result = new string[m];
                for (i = 0; i < m; i++)
                {
                    for (j = 0; j < n; j++)
                        line[j] = a[i, j];
                    result[i] = format(line, dps);
                }
                return "{" + String.Join(",", result) + "}";
            }
            */
            /****************************************************************
            checks that matrix is symmetric.
            max|A-A^T| is calculated; if it is within 1.0E-14 of max|A|,
            matrix is considered symmetric
            ****************************************************************/
            public static bool issymmetric(double[,] a)
            {
                int i, j, n;
                double err, mx, v1, v2;
                if (rows(a) != cols(a))
                    return false;
                n = rows(a);
                if (n == 0)
                    return true;
                mx = 0;
                err = 0;
                for (i = 0; i < n; i++)
                {
                    for (j = i + 1; j < n; j++)
                    {
                        v1 = a[i, j];
                        v2 = a[j, i];
                        if (!math.isfinite(v1))
                            return false;
                        if (!math.isfinite(v2))
                            return false;
                        err = Math.Max(err, Math.Abs(v1 - v2));
                        mx = Math.Max(mx, Math.Abs(v1));
                        mx = Math.Max(mx, Math.Abs(v2));
                    }
                    v1 = a[i, i];
                    if (!math.isfinite(v1))
                        return false;
                    mx = Math.Max(mx, Math.Abs(v1));
                }
                if (mx == 0)
                    return true;
                return err / mx <= 1.0E-14;
            }

            /****************************************************************
            checks that matrix is Hermitian.
            max|A-A^H| is calculated; if it is within 1.0E-14 of max|A|,
            matrix is considered Hermitian
            ****************************************************************/
            /*
            public static bool ishermitian(complex[,] a)
            {
                int i, j, n;
                double err, mx;
                complex v1, v2, vt;
                if (rows(a) != cols(a))
                    return false;
                n = rows(a);
                if (n == 0)
                    return true;
                mx = 0;
                err = 0;
                for (i = 0; i < n; i++)
                {
                    for (j = i + 1; j < n; j++)
                    {
                        v1 = a[i, j];
                        v2 = a[j, i];
                        if (!math.isfinite(v1.x))
                            return false;
                        if (!math.isfinite(v1.y))
                            return false;
                        if (!math.isfinite(v2.x))
                            return false;
                        if (!math.isfinite(v2.y))
                            return false;
                        vt.x = v1.x - v2.x;
                        vt.y = v1.y + v2.y;
                        err = Math.Max(err, math.abscomplex(vt));
                        mx = Math.Max(mx, math.abscomplex(v1));
                        mx = Math.Max(mx, math.abscomplex(v2));
                    }
                    v1 = a[i, i];
                    if (!math.isfinite(v1.x))
                        return false;
                    if (!math.isfinite(v1.y))
                        return false;
                    err = Math.Max(err, Math.Abs(v1.y));
                    mx = Math.Max(mx, math.abscomplex(v1));
                }
                if (mx == 0)
                    return true;
                return err / mx <= 1.0E-14;
            }

            */
            /****************************************************************
            Forces symmetricity by copying upper half of A to the lower one
            ****************************************************************/
            public static bool forcesymmetric(double[,] a)
            {
                int i, j, n;
                if (rows(a) != cols(a))
                    return false;
                n = rows(a);
                if (n == 0)
                    return true;
                for (i = 0; i < n; i++)
                    for (j = i + 1; j < n; j++)
                        a[i, j] = a[j, i];
                return true;
            }

            /****************************************************************
            Forces Hermiticity by copying upper half of A to the lower one
            ****************************************************************/
            /*
            public static bool forcehermitian(complex[,] a)
            {
                int i, j, n;
                complex v;
                if (rows(a) != cols(a))
                    return false;
                n = rows(a);
                if (n == 0)
                    return true;
                for (i = 0; i < n; i++)
                    for (j = i + 1; j < n; j++)
                    {
                        v = a[j, i];
                        a[i, j].x = v.x;
                        a[i, j].y = -v.y;
                    }
                return true;
            }
             */
        };

        /********************************************************************
        serializer object (should not be used directly)
        ********************************************************************/
        #endregion
        
        public class serializer
        {
            enum SMODE { DEFAULT, ALLOC, TO_STRING, FROM_STRING };
            private const int SER_ENTRIES_PER_ROW = 5;
            private const int SER_ENTRY_LENGTH    = 11;
        
            private SMODE mode;
            private int entries_needed;
            private int entries_saved;
            private int bytes_asked;
            private int bytes_written;
            private int bytes_read;
            private char[] out_str;
            private char[] in_str;
        
            public serializer()
            {
                mode = SMODE.DEFAULT;
                entries_needed = 0;
                bytes_asked = 0;
            }

                                public void alloc_start()
        {
            entries_needed = 0;
            bytes_asked = 0;
            mode = SMODE.ALLOC;
        }

            public void alloc_entry()
            {
                if( mode!=SMODE.ALLOC )
                    throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
                entries_needed++;
            }

            private int get_alloc_size()
            {
                int rows, lastrowsize, result;
            
                // check and change mode
                if( mode!=SMODE.ALLOC )
                    throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
            
                // if no entries needes (degenerate case)
                if( entries_needed==0 )
                {
                    bytes_asked = 1;
                    return bytes_asked;
                }
            
                // non-degenerate case
                rows = entries_needed/SER_ENTRIES_PER_ROW;
                lastrowsize = SER_ENTRIES_PER_ROW;
                if( entries_needed%SER_ENTRIES_PER_ROW!=0 )
                {
                    lastrowsize = entries_needed%SER_ENTRIES_PER_ROW;
                    rows++;
                }
            
                // calculate result size
                result  = ((rows-1)*SER_ENTRIES_PER_ROW+lastrowsize)*SER_ENTRY_LENGTH;
                result +=  (rows-1)*(SER_ENTRIES_PER_ROW-1)+(lastrowsize-1);
                result += rows*2;
                bytes_asked = result;
                return result;
            }

            public void sstart_str()
            {
                int allocsize = get_alloc_size();
            
                // check and change mode
                if( mode!=SMODE.ALLOC )
                    throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
                mode = SMODE.TO_STRING;
            
                // other preparations
                out_str = new char[allocsize];
                entries_saved = 0;
                bytes_written = 0;
            }

            public void ustart_str(string s)
            {
                // check and change mode
                if( mode!=SMODE.DEFAULT )
                    throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
                mode = SMODE.FROM_STRING;
            
                in_str = s.ToCharArray();
                bytes_read = 0;
            }

            public void serialize_bool(bool v)
            {
                if( mode!=SMODE.TO_STRING )
                    throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
                bool2str(v, out_str, ref bytes_written);
                entries_saved++;
                if( entries_saved%SER_ENTRIES_PER_ROW!=0 )
                {
                    out_str[bytes_written] = ' ';
                    bytes_written++;
                }
                else
                {
                    out_str[bytes_written+0] = '\r';
                    out_str[bytes_written+1] = '\n';
                    bytes_written+=2;
                }            
            }

            public void serialize_int(int v)
            {
                if( mode!=SMODE.TO_STRING )
                    throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
                int2str(v, out_str, ref bytes_written);
                entries_saved++;
                if( entries_saved%SER_ENTRIES_PER_ROW!=0 )
                {
                    out_str[bytes_written] = ' ';
                    bytes_written++;
                }
                else
                {
                    out_str[bytes_written+0] = '\r';
                    out_str[bytes_written+1] = '\n';
                    bytes_written+=2;
                }
            }

            public void serialize_double(double v)
            {
                if( mode!=SMODE.TO_STRING )
                    throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
                double2str(v, out_str, ref bytes_written);
                entries_saved++;
                if( entries_saved%SER_ENTRIES_PER_ROW!=0 )
                {
                    out_str[bytes_written] = ' ';
                    bytes_written++;
                }
                else
                {
                    out_str[bytes_written+0] = '\r';
                    out_str[bytes_written+1] = '\n';
                    bytes_written+=2;
                }
            }

            public bool unserialize_bool()
            {
                if( mode!=SMODE.FROM_STRING )
                    throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
                return str2bool(in_str, ref bytes_read);
            }

            public int unserialize_int()
            {
                if( mode!=SMODE.FROM_STRING )
                    throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
                return str2int(in_str, ref bytes_read);
            }

            public double unserialize_double()
            {
                if( mode!=SMODE.FROM_STRING )
                    throw new alglib.alglibexception("ALGLIB: internal error during (un)serialization");
                return str2double(in_str, ref bytes_read);
            }

            public void stop()
            {
            }

            public string get_string()
            {
                return new string(out_str, 0, bytes_written);
            }


            /************************************************************************
            This function converts six-bit value (from 0 to 63)  to  character  (only
            digits, lowercase and uppercase letters, minus and underscore are used).

            If v is negative or greater than 63, this function returns '?'.
            ************************************************************************/
            private static char[] _sixbits2char_tbl = new char[64]{ 
                    '0', '1', '2', '3', '4', '5', '6', '7',
                    '8', '9', 'A', 'B', 'C', 'D', 'E', 'F',
                    'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N',
                    'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V',
                    'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 
                    'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 
                    'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 
                    'u', 'v', 'w', 'x', 'y', 'z', '-', '_' };
            private static char sixbits2char(int v)
            {
                if( v<0 || v>63 )
                    return '?';
                return _sixbits2char_tbl[v];
            }
        
            /************************************************************************
            This function converts character to six-bit value (from 0 to 63).

            This function is inverse of ae_sixbits2char()
            If c is not correct character, this function returns -1.
            ************************************************************************/
            private static int[] _char2sixbits_tbl = new int[128] {
                -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, -1, -1, -1,
                -1, -1, -1, -1, -1, 62, -1, -1,
                 0,  1,  2,  3,  4,  5,  6,  7,
                 8,  9, -1, -1, -1, -1, -1, -1,
                -1, 10, 11, 12, 13, 14, 15, 16,
                17, 18, 19, 20, 21, 22, 23, 24,
                25, 26, 27, 28, 29, 30, 31, 32,
                33, 34, 35, -1, -1, -1, -1, 63,
                -1, 36, 37, 38, 39, 40, 41, 42,
                43, 44, 45, 46, 47, 48, 49, 50,
                51, 52, 53, 54, 55, 56, 57, 58,
                59, 60, 61, -1, -1, -1, -1, -1 };
            private static int char2sixbits(char c)
            {
                return (c>=0 && c<127) ? _char2sixbits_tbl[c] : -1;
            }
        
            /************************************************************************
            This function converts three bytes (24 bits) to four six-bit values 
            (24 bits again).

            src         array
            src_offs    offset of three-bytes chunk
            dst         array for ints
            dst_offs    offset of four-ints chunk
            ************************************************************************/
            private static void threebytes2foursixbits(byte[] src, int src_offs, int[] dst, int dst_offs)
            {
                dst[dst_offs+0] =  src[src_offs+0] & 0x3F;
                dst[dst_offs+1] = (src[src_offs+0]>>6) | ((src[src_offs+1]&0x0F)<<2);
                dst[dst_offs+2] = (src[src_offs+1]>>4) | ((src[src_offs+2]&0x03)<<4);
                dst[dst_offs+3] =  src[src_offs+2]>>2;
            }

            /************************************************************************
            This function converts four six-bit values (24 bits) to three bytes
            (24 bits again).

            src         pointer to four ints
            src_offs    offset of the chunk
            dst         pointer to three bytes
            dst_offs    offset of the chunk
            ************************************************************************/
            private static void foursixbits2threebytes(int[] src, int src_offs, byte[] dst, int dst_offs)
            {
                dst[dst_offs+0] =      (byte)(src[src_offs+0] | ((src[src_offs+1]&0x03)<<6));
                dst[dst_offs+1] = (byte)((src[src_offs+1]>>2) | ((src[src_offs+2]&0x0F)<<4));
                dst[dst_offs+2] = (byte)((src[src_offs+2]>>4) |  (src[src_offs+3]<<2));
            }

            /************************************************************************
            This function serializes boolean value into buffer

            v           boolean value to be serialized
            buf         buffer, at least 11 characters wide
            offs        offset in the buffer
        
            after return from this function, offs points to the char's past the value
            being read.
            ************************************************************************/
            private static void bool2str(bool v, char[] buf, ref int offs)
            {
                char c = v ? '1' : '0';
                int i;
                for(i=0; i<SER_ENTRY_LENGTH; i++)
                    buf[offs+i] = c;
                offs += SER_ENTRY_LENGTH;
            }

            /************************************************************************
            This function unserializes boolean value from buffer

            buf         buffer which contains value; leading spaces/tabs/newlines are 
                        ignored, traling spaces/tabs/newlines are treated as  end  of
                        the boolean value.
            offs        offset in the buffer
        
            after return from this function, offs points to the char's past the value
            being read.

            This function raises an error in case unexpected symbol is found
            ************************************************************************/
            private static bool str2bool(char[] buf, ref int offs)
            {
                bool was0, was1;
                string emsg = "ALGLIB: unable to read boolean value from stream";
            
                was0 = false;
                was1 = false;
                while( buf[offs]==' ' || buf[offs]=='\t' || buf[offs]=='\n' || buf[offs]=='\r' )
                    offs++;
                while( buf[offs]!=' ' && buf[offs]!='\t' && buf[offs]!='\n' && buf[offs]!='\r' && buf[offs]!=0 )
                {
                    if( buf[offs]=='0' )
                    {
                        was0 = true;
                        offs++;
                        continue;
                    }
                    if( buf[offs]=='1' )
                    {
                        was1 = true;
                        offs++;
                        continue;
                    }
                    throw new alglib.alglibexception(emsg);
                }
                if( (!was0) && (!was1) )
                    throw new alglib.alglibexception(emsg);
                if( was0 && was1 )
                    throw new alglib.alglibexception(emsg);
                return was1 ? true : false;
            }

            /************************************************************************
            This function serializes integer value into buffer

            v           integer value to be serialized
            buf         buffer, at least 11 characters wide 
            offs        offset in the buffer
        
            after return from this function, offs points to the char's past the value
            being read.

            This function raises an error in case unexpected symbol is found
            ************************************************************************/
            private static void int2str(int v, char[] buf, ref int offs)
            {
                int i;
                byte[] _bytes = System.BitConverter.GetBytes((int)v);
                byte[]  bytes = new byte[9];
                int[] sixbits = new int[12];
                byte c;
            
                //
                // copy v to array of bytes, sign extending it and 
                // converting to little endian order. Additionally, 
                // we set 9th byte to zero in order to simplify 
                // conversion to six-bit representation
                //
                if( !System.BitConverter.IsLittleEndian )
                    System.Array.Reverse(_bytes);
                c = v<0 ? (byte)0xFF : (byte)0x00;
                for(i=0; i<sizeof(int); i++)
                    bytes[i] = _bytes[i];
                for(i=sizeof(int); i<8; i++)
                    bytes[i] = c;
                bytes[8] = 0;
            
                //
                // convert to six-bit representation, output
                //
                // NOTE: last 12th element of sixbits is always zero, we do not output it
                //
                threebytes2foursixbits(bytes, 0, sixbits, 0);
                threebytes2foursixbits(bytes, 3, sixbits, 4);
                threebytes2foursixbits(bytes, 6, sixbits, 8);        
                for(i=0; i<SER_ENTRY_LENGTH; i++)
                    buf[offs+i] = sixbits2char(sixbits[i]);
                offs += SER_ENTRY_LENGTH;
            }

            /************************************************************************
            This function unserializes integer value from string

            buf         buffer which contains value; leading spaces/tabs/newlines are 
                        ignored, traling spaces/tabs/newlines are treated as  end  of
                        the integer value.
            offs        offset in the buffer
        
            after return from this function, offs points to the char's past the value
            being read.

            This function raises an error in case unexpected symbol is found
            ************************************************************************/
            private static int str2int(char[] buf, ref int offs)
            {
                string emsg =       "ALGLIB: unable to read integer value from stream";
                string emsg3264 =   "ALGLIB: unable to read integer value from stream (value does not fit into 32 bits)";
                int[] sixbits = new int[12];
                byte[] bytes = new byte[9];
                byte[] _bytes = new byte[sizeof(int)];
                int sixbitsread, i;
                byte c;
            
                // 
                // 1. skip leading spaces
                // 2. read and decode six-bit digits
                // 3. set trailing digits to zeros
                // 4. convert to little endian 64-bit integer representation
                // 5. check that we fit into int
                // 6. convert to big endian representation, if needed
                //
                sixbitsread = 0;
                while( buf[offs]==' ' || buf[offs]=='\t' || buf[offs]=='\n' || buf[offs]=='\r' )
                    offs++;
                while( buf[offs]!=' ' && buf[offs]!='\t' && buf[offs]!='\n' && buf[offs]!='\r' && buf[offs]!=0 )
                {
                    int d;
                    d = char2sixbits(buf[offs]);
                    if( d<0 || sixbitsread>=SER_ENTRY_LENGTH )
                        throw new alglib.alglibexception(emsg);
                    sixbits[sixbitsread] = d;
                    sixbitsread++;
                    offs++;
                }
                if( sixbitsread==0 )
                    throw new alglib.alglibexception(emsg);
                for(i=sixbitsread; i<12; i++)
                    sixbits[i] = 0;
                foursixbits2threebytes(sixbits, 0, bytes, 0);
                foursixbits2threebytes(sixbits, 4, bytes, 3);
                foursixbits2threebytes(sixbits, 8, bytes, 6);
                c = (bytes[sizeof(int)-1] & 0x80)!=0 ? (byte)0xFF : (byte)0x00;
                for(i=sizeof(int); i<8; i++)
                    if( bytes[i]!=c )
                        throw new alglib.alglibexception(emsg3264);
                for(i=0; i<sizeof(int); i++)
                    _bytes[i] = bytes[i];        
                if( !System.BitConverter.IsLittleEndian )
                    System.Array.Reverse(_bytes);
                return System.BitConverter.ToInt32(_bytes,0);
            }    
        
        
            /************************************************************************
            This function serializes double value into buffer

            v           double value to be serialized
            buf         buffer, at least 11 characters wide 
            offs        offset in the buffer
        
            after return from this function, offs points to the char's past the value
            being read.
            ************************************************************************/
            private static void double2str(double v, char[] buf, ref int offs)
            {
                int i;
                int[] sixbits = new int[12];
                byte[] bytes = new byte[9];

                //
                // handle special quantities
                //
                if( System.Double.IsNaN(v) )
                {
                    buf[offs+0] = '.';
                    buf[offs+1] = 'n';
                    buf[offs+2] = 'a';
                    buf[offs+3] = 'n';
                    buf[offs+4] = '_';
                    buf[offs+5] = '_';
                    buf[offs+6] = '_';
                    buf[offs+7] = '_';
                    buf[offs+8] = '_';
                    buf[offs+9] = '_';
                    buf[offs+10] = '_';
                    offs += SER_ENTRY_LENGTH;
                    return;
                }
                if( System.Double.IsPositiveInfinity(v) )
                {
                    buf[offs+0] = '.';
                    buf[offs+1] = 'p';
                    buf[offs+2] = 'o';
                    buf[offs+3] = 's';
                    buf[offs+4] = 'i';
                    buf[offs+5] = 'n';
                    buf[offs+6] = 'f';
                    buf[offs+7] = '_';
                    buf[offs+8] = '_';
                    buf[offs+9] = '_';
                    buf[offs+10] = '_';
                    offs += SER_ENTRY_LENGTH;
                    return;
                }
                if( System.Double.IsNegativeInfinity(v) )
                {
                    buf[offs+0] = '.';
                    buf[offs+1] = 'n';
                    buf[offs+2] = 'e';
                    buf[offs+3] = 'g';
                    buf[offs+4] = 'i';
                    buf[offs+5] = 'n';
                    buf[offs+6] = 'f';
                    buf[offs+7] = '_';
                    buf[offs+8] = '_';
                    buf[offs+9] = '_';
                    buf[offs+10] = '_';
                    offs += SER_ENTRY_LENGTH;
                    return;
                }
            
                //
                // process general case:
                // 1. copy v to array of chars
                // 2. set 9th byte to zero in order to simplify conversion to six-bit representation
                // 3. convert to little endian (if needed)
                // 4. convert to six-bit representation
                //    (last 12th element of sixbits is always zero, we do not output it)
                //
                byte[] _bytes = System.BitConverter.GetBytes((double)v);
                if( !System.BitConverter.IsLittleEndian )
                    System.Array.Reverse(_bytes);
                for(i=0; i<sizeof(double); i++)
                    bytes[i] = _bytes[i];
                for(i=sizeof(double); i<9; i++)
                    bytes[i] = 0;
                threebytes2foursixbits(bytes, 0, sixbits, 0);
                threebytes2foursixbits(bytes, 3, sixbits, 4);
                threebytes2foursixbits(bytes, 6, sixbits, 8);
                for(i=0; i<SER_ENTRY_LENGTH; i++)
                    buf[offs+i] = sixbits2char(sixbits[i]);
                offs += SER_ENTRY_LENGTH;
            }

            /************************************************************************
            This function unserializes double value from string

            buf         buffer which contains value; leading spaces/tabs/newlines are 
                        ignored, traling spaces/tabs/newlines are treated as  end  of
                        the double value.
            offs        offset in the buffer
        
            after return from this function, offs points to the char's past the value
            being read.

            This function raises an error in case unexpected symbol is found
            ************************************************************************/
            private static double str2double(char[] buf, ref int offs)
            {
                string emsg = "ALGLIB: unable to read double value from stream";
                int[] sixbits = new int[12];
                byte[]  bytes = new byte[9];
                byte[] _bytes = new byte[sizeof(double)];
                int sixbitsread, i;
            
            
                // 
                // skip leading spaces
                //
                while( buf[offs]==' ' || buf[offs]=='\t' || buf[offs]=='\n' || buf[offs]=='\r' )
                    offs++;
            
              
                //
                // Handle special cases
                //
                if( buf[offs]=='.' )
                {
                    string s = new string(buf, offs, SER_ENTRY_LENGTH);
                    if( s==".nan_______" )
                    {
                        offs += SER_ENTRY_LENGTH;
                        return System.Double.NaN;
                    }
                    if( s==".posinf____" )
                    {
                        offs += SER_ENTRY_LENGTH;
                        return System.Double.PositiveInfinity;
                    }
                    if( s==".neginf____" )
                    {
                        offs += SER_ENTRY_LENGTH;
                        return System.Double.NegativeInfinity;
                    }
                    throw new alglib.alglibexception(emsg);
                }
            
                // 
                // General case:
                // 1. read and decode six-bit digits
                // 2. check that all 11 digits were read
                // 3. set last 12th digit to zero (needed for simplicity of conversion)
                // 4. convert to 8 bytes
                // 5. convert to big endian representation, if needed
                //
                sixbitsread = 0;
                while( buf[offs]!=' ' && buf[offs]!='\t' && buf[offs]!='\n' && buf[offs]!='\r' && buf[offs]!=0 )
                {
                    int d;
                    d = char2sixbits(buf[offs]);
                    if( d<0 || sixbitsread>=SER_ENTRY_LENGTH )
                        throw new alglib.alglibexception(emsg);
                    sixbits[sixbitsread] = d;
                    sixbitsread++;
                    offs++;
                }
                if( sixbitsread!=SER_ENTRY_LENGTH )
                    throw new alglib.alglibexception(emsg);
                sixbits[SER_ENTRY_LENGTH] = 0;
                foursixbits2threebytes(sixbits, 0, bytes, 0);
                foursixbits2threebytes(sixbits, 4, bytes, 3);
                foursixbits2threebytes(sixbits, 8, bytes, 6);
                for(i=0; i<sizeof(double); i++)
                    _bytes[i] = bytes[i];        
                if( !System.BitConverter.IsLittleEndian )
                    System.Array.Reverse(_bytes);        
                return System.BitConverter.ToDouble(_bytes,0);
            }
        }
    }
}
