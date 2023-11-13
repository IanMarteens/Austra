using Austra.Library.MVO;
using System.Collections.Frozen;
using System.Xml.Schema;

namespace Austra.Parser;

/// <summary>Syntactic and lexical analysis for AUSTRA.</summary>
internal sealed partial class Parser
{
    /// <summary>Most common argument list in functions.</summary>
    private static readonly Type[] DoubleArg = [typeof(double)];
    /// <summary>Second common argument list in functions.</summary>
    private static readonly Type[] IntArg = [typeof(int)];
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] VectorArg = [typeof(Vector)];
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] ComplexArg = [typeof(Complex)];
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] DoubleDoubleArg = [typeof(double), typeof(double)];
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] VectorVectorArg = [typeof(Vector), typeof(Vector)];
    /// <summary>Another common argument list in functions.</summary>
    private static readonly Type[] DoubleVectorArg = [typeof(double), typeof(Vector)];
    /// <summary>Constructor for <see cref="Index"/>.</summary>
    private static readonly ConstructorInfo IndexCtor =
        typeof(Index).GetConstructor([typeof(int), typeof(bool)])!;
    /// <summary>Constructor for <see cref="Range"/>.</summary>
    private static readonly ConstructorInfo RangeCtor =
        typeof(Range).GetConstructor([typeof(Index), typeof(Index)])!;
    /// <summary>The <see cref="Expression"/> for <see langword="false"/>.</summary>
    private static readonly ConstantExpression FalseExpr = Expression.Constant(false);
    /// <summary>The <see cref="Expression"/> for <see langword="true"/>.</summary>
    private static readonly ConstantExpression TrueExpr = Expression.Constant(true);
    /// <summary>The <see cref="Expression"/> for <see cref="Complex.ImaginaryOne"/>.</summary>
    private static readonly ConstantExpression ImExpr = Expression.Constant(Complex.ImaginaryOne);
    /// <summary>The <see cref="Expression"/> for <see cref="Math.PI"/>.</summary>
    private static readonly ConstantExpression PiExpr = Expression.Constant(Math.PI);
    /// <summary>Method for multiplying by a transposed matrix.</summary>
    private static readonly MethodInfo MatrixMultiplyTranspose =
        typeof(Matrix).Get(nameof(Matrix.MultiplyTranspose));
    /// <summary>Method for multiplying a vector by a transposed matrix.</summary>
    private static readonly MethodInfo MatrixTransposeMultiply =
        typeof(Matrix).Get(nameof(Matrix.TransposeMultiply));
    /// <summary>Method for linear vector combinations.</summary>
    private static readonly MethodInfo VectorCombine2 =
        typeof(Vector).GetMethod(nameof(Vector.Combine2),
            [typeof(double), typeof(double), typeof(Vector), typeof(Vector)])!;
    /// <summary>Method for linear vector combinations.</summary>
    private static readonly MethodInfo MatrixCombine =
        typeof(Matrix).GetMethod(nameof(Matrix.MultiplyAdd),
            [typeof(Vector), typeof(double), typeof(Vector)])!;

    private static readonly HashSet<string> classNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "complexvector", "matrix", "math", "model", "series", "vector", "spline", "seq",
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsClassName(string identifier) => classNames.Contains(identifier);

    /// <summary>Allowed series methods.</summary>
    private static readonly FrozenDictionary<Type, Dictionary<string, MethodInfo>> methods =
        new Dictionary<Type, Dictionary<string, MethodInfo>>()
        {
            [typeof(Series)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["autocorr"] = typeof(Series).Get(nameof(Series.AutoCorrelation)),
                ["corr"] = typeof(Series).Get(nameof(Series.Correlation)),
                ["correlogram"] = typeof(Series).Get(nameof(Series.Correlogram)),
                ["cov"] = typeof(Series).Get(nameof(Series.Covariance)),
                ["stats"] = typeof(Series).GetMethod(nameof(Series.GetSliceStats), [typeof(Date)])!,
                ["ncdf"] = typeof(Series).GetMethod(nameof(Series.NCdf), DoubleArg)!,
                ["movingAvg"] = typeof(Series).Get(nameof(Series.MovingAvg)),
                ["movingStd"] = typeof(Series).GetMethod(nameof(Series.MovingStd), IntArg)!,
                ["movingNcdf"] = typeof(Series).Get(nameof(Series.MovingNcdf)),
                ["ewma"] = typeof(Series).Get(nameof(Series.EWMA)),
                ["map"] = typeof(Series).Get(nameof(Series.Map)),
                ["filter"] = typeof(Series).Get(nameof(Series.Filter)),
                ["any"] = typeof(Series).Get(nameof(Series.Any)),
                ["all"] = typeof(Series).Get(nameof(Series.All)),
                ["zip"] = typeof(Series).Get(nameof(Series.Zip)),
                ["indexof"] = typeof(Series).GetMethod(nameof(Series.IndexOf), DoubleArg)!,
                ["linear"] = typeof(Series).Get(nameof(Series.LinearModel)),
                ["linearModel"] = typeof(Series).Get(nameof(Series.FullLinearModel)),
                ["ar"] = typeof(Series).Get(nameof(Series.AutoRegression)),
                ["arModel"] = typeof(Series).Get(nameof(Series.ARModel)),
            },
            [typeof(DateSpline)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["poly"] = typeof(DateSpline).Get(nameof(DateSpline.GetPoly)),
                ["derivative"] = typeof(DateSpline).Get(nameof(DateSpline.Derivative)),
                ["deriv"] = typeof(DateSpline).Get(nameof(DateSpline.Derivative)),
                ["der"] = typeof(DateSpline).Get(nameof(DateSpline.Derivative)),
            },
            [typeof(VectorSpline)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["poly"] = typeof(VectorSpline).Get(nameof(VectorSpline.GetPoly)),
                ["derivative"] = typeof(VectorSpline).Get(nameof(VectorSpline.Derivative)),
                ["deriv"] = typeof(VectorSpline).Get(nameof(VectorSpline.Derivative)),
                ["der"] = typeof(VectorSpline).Get(nameof(VectorSpline.Derivative)),
            },
            [typeof(Vector)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["autocorr"] = typeof(Vector).Get(nameof(Vector.AutoCorrelation)),
                ["correlogram"] = typeof(Vector).Get(nameof(Vector.Correlogram)),
                ["map"] = typeof(Vector).Get(nameof(Vector.Map)),
                ["any"] = typeof(Vector).Get(nameof(Vector.Any)),
                ["all"] = typeof(Vector).Get(nameof(Vector.All)),
                ["reduce"] = typeof(Vector).Get(nameof(Vector.Reduce)),
                ["zip"] = typeof(Vector).Get(nameof(Vector.Zip)),
                ["filter"] = typeof(Vector).Get(nameof(Vector.Filter)),
                ["indexof"] = typeof(Vector).GetMethod(nameof(Vector.IndexOf), DoubleArg)!,
                ["linear"] = typeof(Vector).Get(nameof(Vector.LinearModel)),
                ["linearModel"] = typeof(Vector).Get(nameof(Vector.FullLinearModel)),
                ["ar"] = typeof(Vector).Get(nameof(Vector.AutoRegression)),
                ["arModel"] = typeof(Vector).Get(nameof(Vector.ARModel)),
            },
            [typeof(ComplexVector)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["map"] = typeof(ComplexVector).Get(nameof(ComplexVector.Map)),
                ["mapreal"] = typeof(ComplexVector).Get(nameof(ComplexVector.MapReal)),
                ["mapr"] = typeof(ComplexVector).Get(nameof(ComplexVector.MapReal)),
                ["any"] = typeof(ComplexVector).Get(nameof(ComplexVector.Any)),
                ["all"] = typeof(ComplexVector).Get(nameof(ComplexVector.All)),
                ["reduce"] = typeof(ComplexVector).Get(nameof(ComplexVector.Reduce)),
                ["zip"] = typeof(ComplexVector).Get(nameof(ComplexVector.Zip)),
                ["filter"] = typeof(ComplexVector).Get(nameof(ComplexVector.Filter)),
                ["indexof"] = typeof(ComplexVector).GetMethod(nameof(ComplexVector.IndexOf),
                    ComplexArg)!,
            },
            [typeof(Date)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["addmonths"] = typeof(Date).GetMethod(nameof(Date.AddMonths), IntArg)!,
                ["addyears"] = typeof(Date).Get(nameof(Date.AddYears)),
            },
            [typeof(Matrix)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["getcol"] = typeof(Matrix).GetMethod(nameof(Matrix.GetColumn), IntArg)!,
                ["getrow"] = typeof(Matrix).GetMethod(nameof(Matrix.GetRow), IntArg)!,
                ["map"] = typeof(Matrix).Get(nameof(Matrix.Map)),
                ["any"] = typeof(Matrix).Get(nameof(Matrix.Any)),
                ["all"] = typeof(Matrix).Get(nameof(Matrix.All)),
            },
            [typeof(Polynomial)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["eval"] = typeof(Polynomial).Get(nameof(Polynomial.Eval)),
                ["derivative"] = typeof(Polynomial).Get(nameof(Polynomial.Derivative)),
                ["deriv"] = typeof(Polynomial).Get(nameof(Polynomial.Derivative)),
                ["der"] = typeof(Polynomial).Get(nameof(Polynomial.Derivative)),
            },
            [typeof(DoubleSequence)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["filter"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Filter)),
                ["map"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Map)),
                ["zip"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Zip)),
                ["reduce"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Reduce)),
                ["any"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Any)),
                ["all"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.All)),
            },
        }.ToFrozenDictionary();

    internal readonly struct MethodData
    {
        public const uint Mλ1 = 1u, Mλ2 = 2u;

        public Type[] Args { get; }
        public uint TypeMask { get; }
        public int ExpectedArgs { get; }
        public MethodInfo? MInfo { get; }
        public ConstructorInfo? CInfo { get; }

        public MethodData(Type implementor, string? memberName, params Type[] args)
        {
            Args = args;
            for (int i = 0, m = 0; i < args.Length; i++, m += 2)
            {
                Type t1 = args[i];
                if (t1.IsAssignableTo(typeof(Delegate)))
                    TypeMask |= (t1.GetGenericArguments().Length == 2 ? Mλ1 : Mλ2) << m;
            }
            Type t = Args[^1];
            ExpectedArgs = t.IsArray
                ? int.MaxValue
                : t == typeof(Random) || t == typeof(NormalRandom) || t == typeof(One) || t == typeof(Zero)
                ? Args.Length - 1
                : Args.Length;
            Args[^1] = t == typeof(Zero) || t == typeof(One) ? typeof(double) : t;
            if (memberName != null)
                MInfo = implementor.GetMethod(memberName, Args);
            else
                CInfo = implementor.GetConstructor(Args)!;
            Args[^1] = t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetMask(int typeId) => (TypeMask >> (typeId * 2)) & 3u;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Expression GetExpression(List<Expression> actualArguments) =>
            MInfo != null
            ? Expression.Call(MInfo, actualArguments)
            : Expression.New(CInfo!, actualArguments);
    }

    internal readonly struct MethodList
    {
        public MethodData[] Methods { get; }
        public bool[] IsLambda { get; }

        public MethodList(params MethodData[] methods)
        {
            Methods = methods;
            int maxArgs = methods.Max(m => m.Args.Length);
            IsLambda = new bool[maxArgs];
            for (int i = 0; i < IsLambda.Length; i++)
                foreach (MethodData method in methods)
                    if (method.Args.Length > i
                        && method.Args[i].IsAssignableTo(typeof(Delegate)))
                    {
                        IsLambda[i] = true;
                        break;
                    }
        }
    }

    private static readonly MethodList MatrixEye = new(
        typeof(Matrix).MD(nameof(Matrix.Identity), IntArg));
    private static readonly MethodList MatrixCovariance = new(
        typeof(Series<Date>).MD(nameof(Series.CovarianceMatrix), typeof(Series[])));
    private static readonly MethodList MatrixCorrelation = new(
        typeof(Series<Date>).MD(nameof(Series.CorrelationMatrix), typeof(Series[])));
    private static readonly MethodList ModelCompare = new(
        typeof(Tuple<Vector, Vector>).MD(VectorVectorArg),
        typeof(Tuple<ComplexVector, ComplexVector>).MD(typeof(ComplexVector), typeof(ComplexVector)),
        typeof(Tuple<Series, Series>).MD(typeof(Series), typeof(Series)));
    private static readonly MethodList PolyDerivative = new(
        typeof(Polynomials).MD(nameof(Polynomials.PolyDerivative), DoubleVectorArg),
        typeof(Polynomials).MD(nameof(Polynomials.PolyDerivative), typeof(double), typeof(double[])),
        typeof(Polynomials).MD(nameof(Polynomials.PolyDerivative), typeof(Complex), typeof(Vector)),
        typeof(Polynomials).MD(nameof(Polynomials.PolyDerivative), typeof(Complex), typeof(double[])));

    private static readonly FrozenDictionary<string, MethodList> classMethods =
        new Dictionary<string, MethodList>()
        {
            ["series.new"] = new(
                typeof(Series).MD(nameof(Series.Combine), typeof(Vector), typeof(Series[]))),
            ["spline.new"] = new(
                typeof(DateSpline).MD(typeof(Series)),
                typeof(VectorSpline).MD(VectorVectorArg),
                typeof(VectorSpline).MD(
                    typeof(double), typeof(double), typeof(int), typeof(Func<double, double>))),
            ["vector.new"] = new(
                typeof(Vector).MD(IntArg),
                typeof(Vector).MD(nameof(Vector.Combine), typeof(Vector), typeof(Vector[])),
                typeof(Vector).MD(typeof(int), typeof(Func<int, double>)),
                typeof(Vector).MD(typeof(int), typeof(Func<int, Vector, double>))),
            ["vector.nrandom"] = new(
                typeof(Vector).MD(typeof(int), typeof(NormalRandom))),
            ["vector.random"] = new(
                typeof(Vector).MD(typeof(int), typeof(Random))),
            ["vector.ones"] = new(
                typeof(Vector).MD(typeof(int), typeof(One))),
            ["complexvector.new"] = new(
                typeof(ComplexVector).MD(VectorArg),
                typeof(ComplexVector).MD(VectorVectorArg),
                typeof(ComplexVector).MD(IntArg),
                typeof(ComplexVector).MD(typeof(int), typeof(Func<int, Complex>)),
                typeof(ComplexVector).MD(typeof(int), typeof(Func<int, ComplexVector, Complex>))),
            ["complexvector.nrandom"] = new(
                typeof(ComplexVector).MD(typeof(int), typeof(NormalRandom))),
            ["complexvector.random"] = new(
                typeof(ComplexVector).MD(typeof(int), typeof(Random))),
            ["matrix.new"] = new(
                typeof(Matrix).MD(IntArg),
                typeof(Matrix).MD(typeof(int), typeof(int)),
                typeof(Matrix).MD(typeof(int), typeof(Func<int, int, double>)),
                typeof(Matrix).MD(typeof(int), typeof(int), typeof(Func<int, int, double>))),
            ["matrix.rows"] = new(
                typeof(Matrix).MD(typeof(Vector[]))),
            ["matrix.cols"] = new(
                typeof(Matrix).MD(nameof(Matrix.FromColumns), typeof(Vector[]))),
            ["matrix.diag"] = new(
                typeof(Matrix).MD(VectorArg),
                typeof(Matrix).MD(typeof(double[]))),
            ["matrix.i"] = MatrixEye,
            ["matrix.eye"] = MatrixEye,
            ["matrix.random"] = new(
                typeof(Matrix).MD(typeof(int), typeof(Random)),
                typeof(Matrix).MD(typeof(int), typeof(int), typeof(Random))),
            ["matrix.nrandom"] = new(
                typeof(Matrix).MD(typeof(int), typeof(NormalRandom)),
                typeof(Matrix).MD(typeof(int), typeof(int), typeof(NormalRandom))),
            ["matrix.lrandom"] = new(
                typeof(LMatrix).MD(typeof(int), typeof(Random)),
                typeof(LMatrix).MD(typeof(int), typeof(int), typeof(Random))),
            ["matrix.lnrandom"] = new(
                typeof(LMatrix).MD(typeof(int), typeof(NormalRandom)),
                typeof(LMatrix).MD(typeof(int), typeof(int), typeof(NormalRandom))),
            ["matrix.cov"] = MatrixCovariance,
            ["matrix.covariance"] = MatrixCovariance,
            ["matrix.corr"] = MatrixCorrelation,
            ["matrix.correlation"] = MatrixCorrelation,
            ["model.compare"] = ModelCompare,
            ["model.comp"] = ModelCompare,
            ["model.mvo"] = new(
                typeof(MvoModel).MD(typeof(Vector), typeof(Matrix)),
                typeof(MvoModel).MD(typeof(Vector), typeof(Matrix), typeof(Vector), typeof(Vector)),
                typeof(MvoModel).MD(typeof(Vector), typeof(Matrix), typeof(Series[])),
                typeof(MvoModel).MD(typeof(Vector), typeof(Matrix),
                    typeof(Vector), typeof(Vector), typeof(Series[])),
                typeof(MvoModel).MD(typeof(Vector), typeof(Matrix), typeof(string[])),
                typeof(MvoModel).MD(typeof(Vector), typeof(Matrix),
                    typeof(Vector), typeof(Vector), typeof(string[]))),
            ["math.polysolve"] = new(
                typeof(Polynomials).MD(nameof(Polynomials.PolySolve), VectorArg),
                typeof(Polynomials).MD(nameof(Polynomials.PolySolve), typeof(double[]))),
            ["math.polyeval"] = new(
                typeof(Polynomials).MD(nameof(Polynomials.PolyEval), DoubleVectorArg),
                typeof(Polynomials).MD(nameof(Polynomials.PolyEval), typeof(double), typeof(double[])),
                typeof(Polynomials).MD(nameof(Polynomials.PolyEval), typeof(Complex), typeof(Vector)),
                typeof(Polynomials).MD(nameof(Polynomials.PolyEval), typeof(Complex), typeof(double[]))),
            ["math.polyderivative"] = PolyDerivative,
            ["math.polyderiv"] = PolyDerivative,
            ["math.abs"] = new(
                typeof(Math).MD(nameof(Math.Abs), IntArg),
                typeof(Math).MD(nameof(Math.Abs), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Abs), ComplexArg)),
            ["math.acos"] = new(
                typeof(Math).MD(nameof(Math.Acos), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Acos), ComplexArg)),
            ["math.asin"] = new(
                typeof(Math).MD(nameof(Math.Asin), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Asin), ComplexArg)),
            ["math.atan"] = new(
                typeof(Math).MD(nameof(Math.Atan), DoubleArg),
                typeof(Math).MD(nameof(Math.Atan2), DoubleDoubleArg),
                typeof(Complex).MD(nameof(Complex.Atan), ComplexArg)),
            ["math.beta"] = new(
                typeof(Functions).MD(nameof(Functions.Beta), DoubleDoubleArg)),
            ["math.cbrt"] = new(
                typeof(Math).MD(nameof(Math.Cbrt), DoubleArg)),
            ["math.cos"] = new(
                typeof(Math).MD(nameof(Math.Cos), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Cos), ComplexArg)),
            ["math.cosh"] = new(
                typeof(Math).MD(nameof(Math.Cosh), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Cosh), ComplexArg)),
            ["math.erf"] = new(
                typeof(Functions).MD(nameof(Functions.Erf), DoubleArg)),
            ["math.exp"] = new(
                typeof(Math).MD(nameof(Math.Exp), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Exp), ComplexArg)),
            ["math.gamma"] = new(
                typeof(Functions).MD(nameof(Functions.Gamma), DoubleArg)),
            ["math.lngamma"] = new(
                typeof(Functions).MD(nameof(Functions.GammaLn), DoubleArg)),
            ["math.log"] = new(
                typeof(Math).MD(nameof(Math.Log), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Log), ComplexArg)),
            ["math.log10"] = new(
                typeof(Math).MD(nameof(Math.Log10), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Log10), ComplexArg)),
            ["math.ncdf"] = new(
                typeof(Functions).MD(nameof(Functions.NCdf), DoubleArg)),
            ["math.probit"] = new(
                typeof(Functions).MD(nameof(Functions.Probit), DoubleArg)),
            ["math.sign"] = new(
                typeof(Math).MD(nameof(Math.Sign), IntArg),
                typeof(Math).MD(nameof(Math.Sign), DoubleArg)),
            ["math.sin"] = new(
                typeof(Math).MD(nameof(Math.Sin), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Sin), ComplexArg)),
            ["math.sinh"] = new(
                typeof(Math).MD(nameof(Math.Sinh), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Sinh), ComplexArg)),
            ["math.tan"] = new(
                typeof(Math).MD(nameof(Math.Tan), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Tan), ComplexArg)),
            ["math.tanh"] = new(
                typeof(Math).MD(nameof(Math.Tanh), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Tanh), ComplexArg)),
            ["math.sqrt"] = new(
                typeof(Math).MD(nameof(Math.Sqrt), DoubleArg),
                typeof(Complex).MD(nameof(Complex.Sqrt), ComplexArg)),
            ["math.trunc"] = new(
                typeof(Math).MD(nameof(Math.Truncate), DoubleArg)),
            ["math.round"] = new(
                typeof(Math).MD(nameof(Math.Round), DoubleArg),
                typeof(Math).MD(nameof(Math.Round), typeof(double), typeof(int))),
            ["math.compare"] = ModelCompare,
            ["math.comp"] = ModelCompare,
            ["math.complex"] = new(
                typeof(Complex).MD(DoubleDoubleArg),
                typeof(Complex).MD(typeof(double), typeof(Zero))),
            ["math.polar"] = new(
                typeof(Complex).MD(nameof(Complex.FromPolarCoordinates), DoubleDoubleArg),
                typeof(Complex).MD(nameof(Complex.FromPolarCoordinates), typeof(double), typeof(Zero))),
            ["math.min"] = new(
                typeof(Date).MD(nameof(Date.Min), typeof(Date), typeof(Date)),
                typeof(Math).MD(nameof(Math.Min), typeof(int), typeof(int)),
               typeof(Math).MD(nameof(Math.Min), DoubleDoubleArg)),
            ["math.max"] = new(
                typeof(Date).MD(nameof(Date.Max), typeof(Date), typeof(Date)),
                typeof(Math).MD(nameof(Math.Max), typeof(int), typeof(int)),
                typeof(Math).MD(nameof(Math.Max), DoubleDoubleArg)),
            ["math.solve"] = new(
                typeof(Solver).MD(nameof(Solver.Solve),
                    typeof(Func<double, double>), typeof(Func<double, double>), typeof(double)),
                typeof(Solver).MD(nameof(Solver.Solve),
                    typeof(Func<double, double>), typeof(Func<double, double>), typeof(double),
                    typeof(double)),
                typeof(Solver).MD(nameof(Solver.Solve),
                    typeof(Func<double, double>), typeof(Func<double, double>), typeof(double),
                    typeof(double), typeof(int))),
            ["seq.new"] = new(
                typeof(DoubleSequence).MD(nameof(DoubleSequence.Create), typeof(int), typeof(int)),
                typeof(DoubleSequence).MD(nameof(DoubleSequence.Create),
                    typeof(double), typeof(double), typeof(int)),
                typeof(DoubleSequence).MD(nameof(DoubleSequence.Create), typeof(Vector)),
                typeof(DoubleSequence).MD(nameof(DoubleSequence.Create), typeof(Series))),
            ["seq.random"] = new(
                typeof(DoubleSequence).MD(nameof(DoubleSequence.Random), typeof(int))),
            ["seq.nrandom"] = new(
                typeof(DoubleSequence).MD(nameof(DoubleSequence.NormalRandom), typeof(int))),
        }.ToFrozenDictionary();

    /// <summary>Allowed properties and their implementations.</summary>
    private static readonly FrozenDictionary<Type, Dictionary<string, MethodInfo>> allProps =
        new Dictionary<Type, Dictionary<string, MethodInfo>>()
        {
            [typeof(Complex)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["real"] = typeof(Complex).Prop(nameof(Complex.Real)),
                ["re"] = typeof(Complex).Prop(nameof(Complex.Real)),
                ["imaginary"] = typeof(Complex).Prop(nameof(Complex.Imaginary)),
                ["imag"] = typeof(Complex).Prop(nameof(Complex.Imaginary)),
                ["im"] = typeof(Complex).Prop(nameof(Complex.Imaginary)),
                ["magnitude"] = typeof(Complex).Prop(nameof(Complex.Magnitude)),
                ["mag"] = typeof(Complex).Prop(nameof(Complex.Magnitude)),
                ["phase"] = typeof(Complex).Prop(nameof(Complex.Phase)),
            },
            [typeof(FftRModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["amplitudes"] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
                ["magnitudes"] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
                ["phases"] = typeof(FftModel).Prop(nameof(FftModel.Phases)),
                ["length"] = typeof(FftModel).Prop(nameof(FftModel.Length)),
                ["values"] = typeof(FftModel).Prop(nameof(FftModel.Spectrum)),
                ["inverse"] = typeof(FftRModel).Get(nameof(FftRModel.Inverse)),
            },
            [typeof(FftCModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["amplitudes"] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
                ["magnitudes"] = typeof(FftModel).Prop(nameof(FftModel.Amplitudes)),
                ["phases"] = typeof(FftModel).Prop(nameof(FftModel.Phases)),
                ["length"] = typeof(FftModel).Prop(nameof(FftModel.Length)),
                ["values"] = typeof(FftModel).Prop(nameof(FftModel.Spectrum)),
                ["inverse"] = typeof(FftCModel).Get(nameof(FftCModel.Inverse)),
            },
            [typeof(Series)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["count"] = typeof(Series).Prop(nameof(Series.Count)),
                ["length"] = typeof(Series).Prop(nameof(Series.Count)),
                ["min"] = typeof(Series).Prop(nameof(Series.Minimum)),
                ["max"] = typeof(Series).Prop(nameof(Series.Maximum)),
                ["mean"] = typeof(Series).Prop(nameof(Series.Mean)),
                ["var"] = typeof(Series).Prop(nameof(Series.Variance)),
                ["varp"] = typeof(Series).Prop(nameof(Series.PopulationVariance)),
                ["std"] = typeof(Series).Prop(nameof(Series.StandardDeviation)),
                ["stdp"] = typeof(Series).Prop(nameof(Series.PopulationStandardDeviation)),
                ["skew"] = typeof(Series).Prop(nameof(Series.Skewness)),
                ["skewp"] = typeof(Series).Prop(nameof(Series.PopulationSkewness)),
                ["kurt"] = typeof(Series).Prop(nameof(Series.Kurtosis)),
                ["kurtp"] = typeof(Series).Prop(nameof(Series.PopulationKurtosis)),
                ["stats"] = typeof(Series).Prop(nameof(Series.Stats)),
                ["first"] = typeof(Series).Prop(nameof(Series.First)),
                ["last"] = typeof(Series).Prop(nameof(Series.Last)),
                ["rets"] = typeof(Series).Get(nameof(Series.AsReturns)),
                ["logs"] = typeof(Series).Get(nameof(Series.AsLogReturns)),
                ["fft"] = typeof(Series).Get(nameof(Series.Fft)),
                ["perc"] = typeof(Series).Get(nameof(Series.Percentiles)),
                ["values"] = typeof(Series).Get(nameof(Series.GetValues)),
                ["random"] = typeof(Series).Get(nameof(Series.Random)),
                ["movingret"] = typeof(Series).Get(nameof(Series.MovingRet)),
                ["sum"] = typeof(Series).Get(nameof(Series.Sum)),
                ["type"] = typeof(Series).Prop(nameof(Series.Type)),
                ["amax"] = typeof(Series).Get(nameof(Series.AbsMax)),
                ["amin"] = typeof(Series).Get(nameof(Series.AbsMin)),
                ["ncdf"] = typeof(Series).GetMethod(nameof(Series.NCdf), Type.EmptyTypes)!,
                ["fit"] = typeof(Series).Get(nameof(Series.Fit)),
                ["linearfit"] = typeof(Series).Get(nameof(Series.LinearFit)),
                ["acf"] = typeof(Series).Get(nameof(Series.ACF)),
            },
            [typeof(Series<int>)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["stats"] = typeof(Series<int>).Prop(nameof(Series<int>.Stats)),
                ["first"] = typeof(Series<int>).Prop(nameof(Series<int>.First)),
                ["last"] = typeof(Series<int>).Prop(nameof(Series<int>.Last)),
                ["values"] = typeof(Series<int>).Get(nameof(Series<int>.GetValues)),
                ["sum"] = typeof(Series<int>).Get(nameof(Series<int>.Sum)),
            },
            [typeof(Series<double>)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["stats"] = typeof(Series<double>).Prop(nameof(Series<double>.Stats)),
                ["first"] = typeof(Series<double>).Prop(nameof(Series<double>.First)),
                ["last"] = typeof(Series<double>).Prop(nameof(Series<double>.Last)),
                ["values"] = typeof(Series<double>).Get(nameof(Series<double>.GetValues)),
                ["sum"] = typeof(Series<double>).Get(nameof(Series<double>.Sum)),
            },
            [typeof(Acc)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["count"] = typeof(Acc).Prop(nameof(Acc.Count)),
                ["min"] = typeof(Acc).Prop(nameof(Acc.Minimum)),
                ["max"] = typeof(Acc).Prop(nameof(Acc.Maximum)),
                ["mean"] = typeof(Acc).Prop(nameof(Acc.Mean)),
                ["var"] = typeof(Acc).Prop(nameof(Acc.Variance)),
                ["varp"] = typeof(Acc).Prop(nameof(Acc.PopulationVariance)),
                ["std"] = typeof(Acc).Prop(nameof(Acc.StandardDeviation)),
                ["stdp"] = typeof(Acc).Prop(nameof(Acc.PopulationStandardDeviation)),
                ["skew"] = typeof(Acc).Prop(nameof(Acc.Skewness)),
                ["skewp"] = typeof(Acc).Prop(nameof(Acc.PopulationSkewness)),
                ["kurt"] = typeof(Acc).Prop(nameof(Acc.Kurtosis)),
                ["kurtp"] = typeof(Acc).Prop(nameof(Acc.PopulationKurtosis)),
            },
            [typeof(Matrix)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["det"] = typeof(Matrix).Get(nameof(Matrix.Determinant)),
                ["chol"] = typeof(Matrix).Get(nameof(Matrix.CholeskyMatrix)),
                ["evd"] = typeof(Matrix).GetMethod(nameof(Matrix.EVD), Type.EmptyTypes)!,
                ["trace"] = typeof(Matrix).Get(nameof(Matrix.Trace)),
                ["rows"] = typeof(Matrix).Prop(nameof(Matrix.Rows)),
                ["cols"] = typeof(Matrix).Prop(nameof(Matrix.Cols)),
                ["amax"] = typeof(Matrix).Get(nameof(Matrix.AMax)),
                ["amin"] = typeof(Matrix).Get(nameof(Matrix.AMin)),
                ["max"] = typeof(Matrix).Get(nameof(Matrix.Maximum)),
                ["min"] = typeof(Matrix).Get(nameof(Matrix.Minimum)),
                ["diag"] = typeof(Matrix).Get(nameof(Matrix.Diagonal)),
                ["inverse"] = typeof(Matrix).Get(nameof(Matrix.Inverse)),
                ["issym"] = typeof(Matrix).Get(nameof(Matrix.IsSymmetric)),
                ["sym"] = typeof(Matrix).Get(nameof(Matrix.IsSymmetric)),
                ["issymmetric"] = typeof(Matrix).Get(nameof(Matrix.IsSymmetric)),
                ["stats"] = typeof(Matrix).Get(nameof(Matrix.Stats)),
            },
            [typeof(LMatrix)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["det"] = typeof(LMatrix).Get(nameof(LMatrix.Determinant)),
                ["trace"] = typeof(LMatrix).Get(nameof(LMatrix.Trace)),
                ["rows"] = typeof(LMatrix).Prop(nameof(LMatrix.Rows)),
                ["cols"] = typeof(LMatrix).Prop(nameof(LMatrix.Cols)),
                ["amax"] = typeof(LMatrix).Get(nameof(LMatrix.AMax)),
                ["amin"] = typeof(LMatrix).Get(nameof(LMatrix.AMin)),
                ["max"] = typeof(LMatrix).Get(nameof(LMatrix.Maximum)),
                ["min"] = typeof(LMatrix).Get(nameof(LMatrix.Minimum)),
                ["diag"] = typeof(LMatrix).Get(nameof(LMatrix.Diagonal))
            },
            [typeof(RMatrix)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["det"] = typeof(RMatrix).Get(nameof(RMatrix.Determinant)),
                ["trace"] = typeof(RMatrix).Get(nameof(RMatrix.Trace)),
                ["rows"] = typeof(RMatrix).Prop(nameof(RMatrix.Rows)),
                ["cols"] = typeof(RMatrix).Prop(nameof(RMatrix.Cols)),
                //["amax"] = typeof(RMatrix).Get(nameof(RMatrix.AMax)),
                //["amin"] = typeof(RMatrix).Get(nameof(RMatrix.AMin)),
                //["max"] = typeof(RMatrix).Get(nameof(RMatrix.Maximum)),
                //["min"] = typeof(RMatrix).Get(nameof(RMatrix.Minimum)),
                ["diag"] = typeof(RMatrix).Get(nameof(RMatrix.Diagonal))
            },
            [typeof(EVD)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["vectors"] = typeof(EVD).Prop(nameof(EVD.Vectors)),
                ["values"] = typeof(EVD).Prop(nameof(EVD.Values)),
                ["d"] = typeof(EVD).Prop(nameof(EVD.D)),
                ["rank"] = typeof(EVD).Get(nameof(EVD.Rank)),
                ["det"] = typeof(EVD).Get(nameof(EVD.Determinant)),
            },
            [typeof(LinearSModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["original"] = typeof(LinearSModel).Prop(nameof(LinearSModel.Original)),
                ["prediction"] = typeof(LinearSModel).Prop(nameof(LinearSModel.Prediction)),
                ["weights"] = typeof(LinearSModel).Prop(nameof(LinearSModel.Weights)),
                ["r2"] = typeof(LinearSModel).Prop(nameof(LinearSModel.R2)),
                ["rss"] = typeof(LinearSModel).Prop(nameof(LinearSModel.ResidualSumSquares)),
                ["tss"] = typeof(LinearSModel).Prop(nameof(LinearSModel.TotalSumSquares)),
            },
            [typeof(LinearVModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["original"] = typeof(LinearVModel).Prop(nameof(LinearVModel.Original)),
                ["prediction"] = typeof(LinearVModel).Prop(nameof(LinearVModel.Prediction)),
                ["weights"] = typeof(LinearVModel).Prop(nameof(LinearVModel.Weights)),
                ["r2"] = typeof(LinearVModel).Prop(nameof(LinearVModel.R2)),
                ["rss"] = typeof(LinearVModel).Prop(nameof(LinearVModel.ResidualSumSquares)),
                ["tss"] = typeof(LinearVModel).Prop(nameof(LinearVModel.TotalSumSquares)),
            },
            [typeof(Vector)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["norm"] = typeof(Vector).Get(nameof(Vector.Norm)),
                ["length"] = typeof(Vector).Prop(nameof(Vector.Length)),
                ["abs"] = typeof(Vector).Get(nameof(Vector.Abs)),
                ["sqr"] = typeof(Vector).Get(nameof(Vector.Squared)),
                ["stats"] = typeof(Vector).Get(nameof(Vector.Stats)),
                ["sum"] = typeof(Vector).Get(nameof(Vector.Sum)),
                ["prod"] = typeof(Vector).Get(nameof(Vector.Product)),
                ["product"] = typeof(Vector).Get(nameof(Vector.Product)),
                ["sqrt"] = typeof(Vector).Get(nameof(Vector.Sqrt)),
                ["amax"] = typeof(Vector).Get(nameof(Vector.AMax)),
                ["amin"] = typeof(Vector).Get(nameof(Vector.AMin)),
                ["max"] = typeof(Vector).Get(nameof(Vector.Maximum)),
                ["min"] = typeof(Vector).Get(nameof(Vector.Minimum)),
                ["mean"] = typeof(Vector).Get(nameof(Vector.Mean)),
                ["reverse"] = typeof(Vector).Get(nameof(Vector.Reverse)),
                ["distinct"] = typeof(Vector).Get(nameof(Vector.Distinct)),
                ["sort"] = typeof(Vector).Get(nameof(Vector.Sort)),
                ["fft"] = typeof(Vector).Get(nameof(Vector.Fft)),
                ["first"] = typeof(Vector).Prop(nameof(Vector.First)),
                ["last"] = typeof(Vector).Prop(nameof(Vector.Last)),
                ["acf"] = typeof(Vector).Get(nameof(Vector.ACF)),
            },
            [typeof(ComplexVector)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["length"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Length)),
                ["norm"] = typeof(ComplexVector).Get(nameof(ComplexVector.Norm)),
                ["amax"] = typeof(ComplexVector).Get(nameof(ComplexVector.AbsMax)),
                ["sqr"] = typeof(ComplexVector).Get(nameof(ComplexVector.Squared)),
                ["first"] = typeof(ComplexVector).Prop(nameof(ComplexVector.First)),
                ["last"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Last)),
                ["fft"] = typeof(ComplexVector).Get(nameof(ComplexVector.Fft)),
                ["sum"] = typeof(ComplexVector).Get(nameof(ComplexVector.Sum)),
                ["mean"] = typeof(ComplexVector).Get(nameof(ComplexVector.Mean)),
                ["reverse"] = typeof(ComplexVector).Get(nameof(ComplexVector.Reverse)),
                ["distinct"] = typeof(ComplexVector).Get(nameof(ComplexVector.Distinct)),
                ["magnitudes"] = typeof(ComplexVector).Get(nameof(ComplexVector.Magnitudes)),
                ["amplitudes"] = typeof(ComplexVector).Get(nameof(ComplexVector.Magnitudes)),
                ["mags"] = typeof(ComplexVector).Get(nameof(ComplexVector.Magnitudes)),
                ["mag"] = typeof(ComplexVector).Get(nameof(ComplexVector.Magnitudes)),
                ["phases"] = typeof(ComplexVector).Get(nameof(ComplexVector.Phases)),
                ["real"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Real)),
                ["re"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Real)),
                ["imaginary"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Imaginary)),
                ["imag"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Imaginary)),
                ["im"] = typeof(ComplexVector).Prop(nameof(ComplexVector.Imaginary)),
            },
            [typeof(MvoModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["length"] = typeof(MvoModel).Prop(nameof(MvoModel.Length)),
                ["first"] = typeof(MvoModel).Prop(nameof(MvoModel.First)),
                ["last"] = typeof(MvoModel).Prop(nameof(MvoModel.Last)),
                ["size"] = typeof(MvoModel).Prop(nameof(MvoModel.Size)),
            },
            [typeof(Portfolio)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["weights"] = typeof(Portfolio).Prop(nameof(Portfolio.Weights)),
                ["lambda"] = typeof(Portfolio).Prop(nameof(Portfolio.Lambda)),
                ["ret"] = typeof(Portfolio).Prop(nameof(Portfolio.Mean)),
                ["var"] = typeof(Portfolio).Prop(nameof(Portfolio.Variance)),
                ["std"] = typeof(Portfolio).Prop(nameof(Portfolio.StdDev)),
            },
            [typeof(ARSModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["original"] = typeof(ARSModel).Prop(nameof(ARSModel.Original)),
                ["prediction"] = typeof(ARSModel).Prop(nameof(ARSModel.Prediction)),
                ["coefficients"] = typeof(ARSModel).Prop(nameof(ARSModel.Coefficients)),
                ["coeff"] = typeof(ARSModel).Prop(nameof(ARSModel.Coefficients)),
                ["r2"] = typeof(ARSModel).Prop(nameof(ARSModel.R2)),
                ["rss"] = typeof(ARSModel).Prop(nameof(ARSModel.ResidualSumSquares)),
                ["tss"] = typeof(ARSModel).Prop(nameof(ARSModel.TotalSumSquares)),
            },
            [typeof(ARVModel)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["original"] = typeof(ARVModel).Prop(nameof(ARVModel.Original)),
                ["prediction"] = typeof(ARVModel).Prop(nameof(ARVModel.Prediction)),
                ["coefficients"] = typeof(ARVModel).Prop(nameof(ARVModel.Coefficients)),
                ["coeff"] = typeof(ARVModel).Prop(nameof(ARVModel.Coefficients)),
                ["r2"] = typeof(ARVModel).Prop(nameof(ARVModel.R2)),
                ["rss"] = typeof(ARVModel).Prop(nameof(ARVModel.ResidualSumSquares)),
                ["tss"] = typeof(ARVModel).Prop(nameof(ARVModel.TotalSumSquares)),
            },
            [typeof(Point<Date>)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["value"] = typeof(Point<Date>).Prop(nameof(Point<Date>.Value)),
                ["date"] = typeof(Point<Date>).Prop(nameof(Point<Date>.Arg)),
            },
            [typeof(DateSpline)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["length"] = typeof(DateSpline).Prop(nameof(DateSpline.Length)),
            },
            [typeof(VectorSpline)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["length"] = typeof(DateSpline).Prop(nameof(DateSpline.Length)),
            },
            [typeof(Date)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["day"] = typeof(Date).Prop(nameof(Date.Day)),
                ["month"] = typeof(Date).Prop(nameof(Date.Month)),
                ["year"] = typeof(Date).Prop(nameof(Date.Year)),
                ["dow"] = typeof(Date).Prop(nameof(Date.DayOfWeek)),
                ["isleap"] = typeof(Date).Get(nameof(Date.IsLeap)),
            },
            [typeof(DoubleSequence)] = new(StringComparer.OrdinalIgnoreCase)
            {
                ["sum"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Sum)),
                ["prod"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Product)),
                ["product"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Product)),
                ["min"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Min)),
                ["max"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Max)),
                ["stats"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Stats)),
                ["length"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Length)),
                ["tovector"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.ToVector)),
                ["sort"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Sort)),
                ["sortasc"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Sort)),
                ["sortdesc"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.SortDescending)),
                ["distinct"] = typeof(DoubleSequence).Get(nameof(DoubleSequence.Distinct)),
            },
        }.ToFrozenDictionary();

    /// <summary>Code completion descriptors for properties and methods.</summary>
    private static readonly FrozenDictionary<Type, Member[]> members =
        new Dictionary<Type, Member[]>()
        {
            [typeof(Series)] = [
                new("count", "Gets the number of points"),
                new("min", "Gets the minimum value"),
                new("max", "Gets the maximum value"),
                new("mean", "Gets the mean value"),
                new("var", "Gets the variance"),
                new("varp", "Gets the variance of the population"),
                new("std", "Gets the standard deviation"),
                new("stdp", "Gets the standard deviation of the population"),
                new("skew", "Gets the skewness"),
                new("skewp", "Gets the skewness of the population"),
                new("kurt", "Gets the kurtosis"),
                new("kurtp", "Gets the kurtosis of the population"),
                new("sum", "Gets the sum of all values"),
                new("stats", "Gets all statistics"),
                new("first", "Gets the first point"),
                new("last", "Gets the last point"),
                new("rets", "Gets the linear returns"),
                new("logs", "Gets the logarithmic returns"),
                new("fft", "Performs a Fast Fourier Transform"),
                new("perc", "Gets the percentiles"),
                new("values", "Gets the underlying vector of values"),
                new("random", "Generates a random series"),
                new("movingRet", "Gets the moving monthly/yearly return"),
                new("type", "Gets the type of the series"),
                new("amax", "Gets the maximum absolute value"),
                new("amin", "Gets the minimum absolute value"),
                new("ncdf", "Gets the percentile according to the normal distribution"),
                new("fit", "Gets coefficients for a linear fit"),
                new("linearfit", "Gets a line fitting the original series"),
                new("autocorr(", "Gets the autocorrelation given a lag"),
                new("corr(", "Gets the correlation with another given series"),
                new("cov(", "Gets the covariance with another given series"),
                new("correlogram(", "Gets all autocorrelations up to a given lag"),
                new("linear(", "Gets the regression coefficients given a list of series"),
                new("linearModel(", "Creates a linear model"),
                new("stats(", "Gets monthly statistics for a given date"),
                new("movingAvg(", "Calculates a Simple Moving Average"),
                new("movingStd(", "Calculates a Moving Standard Deviation"),
                new("movingNcdf(", "Calculates a Moving Normal Percentile"),
                new("ewma(", "Calculates an Exponentially Weighted Moving Average"),
                new("map(x => ", "Pointwise transformation of the series"),
                new("any(x => ", "Existential operator"),
                new("all(x => ", "Universal operator"),
                new("zip(", "Combines two series"),
                new("filter(x => ", "Filters points by values or dates"),
                new("indexof(", "Returns the index where a value is stored"),
                new("ar(", "Calculates the autoregression coefficients"),
                new("arModel(", "Creates an AR(p) model"),
                new("acf", "AutoCorrelation Function"),
            ],
            [typeof(Series<int>)] = [
                new("stats", "Gets all statistics"),
                new("first", "Gets the first point"),
                new("last", "Gets the last point"),
                new("values", "Gets the underlying vector of values"),
            ],
            [typeof(Series<double>)] = [
                new("stats", "Gets all statistics"),
                new("first", "Gets the first point"),
                new("last", "Gets the last point"),
                new("values", "Gets the underlying vector of values"),
            ],
            [typeof(Acc)] = [
                new("count", "Gets the number of points"),
                new("min", "Gets the minimum value"),
                new("max", "Gets the maximum value"),
                new("mean", "Gets the mean value"),
                new("var", "Gets the variance"),
                new("varp", "Gets the variance of the population"),
                new("std", "Gets the standard deviation"),
                new("stdp", "Gets the standard deviation of the population"),
                new("skew", "Gets the skewness"),
                new("skewp", "Gets the skewness of the population"),
                new("kurt", "Gets the kurtosis"),
                new("kurtp", "Gets the kurtosis of the population"),
            ],
            [typeof(Matrix)] = [
                new("rows", "Gets the number of rows"),
                new("cols", "Gets the number of columns"),
                new("det", "Calculates the determinant"),
                new("trace", "Gets the sum of the main diagonal"),
                new("amax", "Gets the maximum absolute value"),
                new("amin", "Gets the minimum absolute value"),
                new("max", "Gets the maximum value"),
                new("min", "Gets the minimum absolute value"),
                new("chol", "Calculates the Cholesky Decomposition"),
                new("evd", "Calculates the EigenValues Decomposition"),
                new("diag", "Extracts the diagonal as a vector"),
                new("inverse", "Calculates the inverse of a square matrix"),
                new("getRow(", "Extracts a row as a vector"),
                new("getCol(", "Extracts a column as a vector"),
                new("isSymmetric", "Checks if a matrix is symmetric"),
                new("stats", "Calculates statistics on the cells"),
                new("map(x => ", "Pointwise transformation of matrix cells"),
                new("any(x => ", "Existential operator"),
                new("all(x => ", "Universal operator"),
            ],
            [typeof(LMatrix)] = [
                new("rows", "Gets the number of rows"),
                new("cols", "Gets the number of columns"),
                new("det", "Calculates the determinant"),
                new("trace", "Gets the sum of the main diagonal"),
                new("amax", "Gets the maximum absolute value"),
                new("amin", "Gets the minimum absolute value"),
                new("max", "Gets the maximum value"),
                new("min", "Gets the minimum absolute value"),
                new("diag", "Extracts the diagonal as a vector"),
            ],
            [typeof(RMatrix)] = [
                new("rows", "Gets the number of rows"),
                new("cols", "Gets the number of columns"),
                new("det", "Calculates the determinant"),
                new("trace", "Gets the sum of the main diagonal"),
                //new("amax", "Gets the maximum absolute value"),
                //new("amin", "Gets the minimum absolute value"),
                //new("max", "Gets the maximum value"),
                //new("min", "Gets the minimum absolute value"),
                new("diag", "Extracts the diagonal as a vector"),
            ],
            [typeof(EVD)] = [
                new("vectors", "Gets a matrix with eigenvectors as its columns"),
                new("values", "Gets all the eigenvalues"),
                new("d", "Gets a quasi-diagonal real matrix with all eigenvalues"),
                new("rank", "Gets the rank of the original matrix"),
            ],
            [typeof(LinearSModel)] = [
                new("original", "Gets the series to be explained"),
                new("prediction", "Gets the predicted series"),
                new("weights", "Gets the regression coefficients"),
                new("r2", "Gets the regression coefficient"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
            ],
            [typeof(LinearVModel)] = [
                new("original", "Gets the vector to be explained"),
                new("prediction", "Gets the predicted vector"),
                new("weights", "Gets the regression coefficients"),
                new("r2", "Gets the regression coefficient"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
            ],
            [typeof(ARSModel)] = [
                new("original", "Gets the series to be explained"),
                new("prediction", "Gets the predicted series"),
                new("coefficients", "Gets the autoregression coefficients"),
                new("r2", "Gets the regression coefficient"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
            ],
            [typeof(ARVModel)] = [
                new("original", "Gets the vector to be explained"),
                new("prediction", "Gets the predicted vector"),
                new("coefficients", "Gets the autoregression coefficients"),
                new("r2", "Gets the regression coefficient"),
                new("rss", "Gets the Residual Sum of Squares"),
                new("tss", "Gets the Total Sum of Squares"),
            ],
            [typeof(Vector)] = [
                new("length", "Gets the number of items"),
                new("norm", "Gets the norm of the vector"),
                new("sqr", "Gets the squared norm of the vector"),
                new("sum", "Gets the sum of all values"),
                new("prod", "Gets the product of all values"),
                new("mean", "Gets the mean value"),
                new("sqrt", "Pointwise squared root"),
                new("abs", "Pointwise absolute value"),
                new("stats", "Gets all the statistics"),
                new("amax", "Gets the maximum absolute value"),
                new("amin", "Gets the minimum absolute value"),
                new("max", "Gets the maximum  value"),
                new("min", "Gets the minimum value"),
                new("reverse", "Gets a reversed copy"),
                new("distinct", "Gets a new vector with distinct values"),
                new("sort", "Gets a new vector with sorted values"),
                new("first", "Gets the first item from the vector"),
                new("last", "Gets the last item from the vector"),
                new("fft", "Performs a Fast Fourier Transform"),
                new("autocorr(", "Gets the autocorrelation given a lag"),
                new("correlogram(", "Gets all autocorrelations up to a given lag"),
                new("map(x => ", "Pointwise transformation of vector items"),
                new("filter(x => ", "Filters items by value"),
                new("any(x => ", "Existential operator"),
                new("all(x => ", "Universal operator"),
                new("zip(", "Combines two vectors"),
                new("reduce(", "Reduces a vector to a single value"),
                new("indexOf(", "Returns the index where a value is stored"),
                new("linear(", "Gets the regression coefficients given a list of vectors"),
                new("linearModel(", "Creates a linear model"),
                new("ar(", "Calculates the autoregression coefficients"),
                new("arModel(", "Creates an AR(p) model"),
                new("indexof(", "Returns the index where a value is stored"),
                new("linear(", "Gets the regression coefficients given a list of vectors"),
                new("linearModel(", "Creates a linear model"),
                new("ar(", "Calculates the autoregression coefficients"),
                new("arModel(", "Creates an AR(p) model"),
                new("acf", "AutoCorrelation Function"),
            ],
            [typeof(ComplexVector)] = [
                new("length", "Gets the number of items"),
                new("norm", "Gets the norm of the vector"),
                new("amax", "Gets the maximum absolute value"),
                new("sqr", "Gets the squared norm of the vector"),
                new("sum", "Gets the sum of all values"),
                new("mean", "Gets the mean value"),
                new("first", "Gets the first item from the vector"),
                new("last", "Gets the last item from the vector"),
                new("reverse", "Gets a reversed copy"),
                new("distinct", "Gets a new vector with distinct values"),
                new("fft", "Performs a Fast Fourier Transform"),
                new("magnitudes", "Gets magnitudes as a vector"),
                new("phases", "Gets phases as a vector"),
                new("real", "Gets the real components as a vector"),
                new("imag", "Gets the imaginary components as a vector"),
                new("map(x => ", "Pointwise transformation of complex values"),
                new("mapreal(x => ", "Transforms complex vector into a real one"),
                new("filter(x => ", "Filters items by value"),
                new("any(x => ", "Existential operator"),
                new("all(x => ", "Universal operator"),
                new("zip(", "Combines two complex vectors"),
                new("reduce(", "Reduces a complex vector to a single value"),
                new("indexof(", "Returns the index where a value is stored"),
            ],
            [typeof(MvoModel)] = [
                new("length", "Gets the number of corner portfolios"),
                new("first", "Gets the first corner portfolio"),
                new("last", "Gets the last corner portfolio"),
                new("size", "Gets the number of assets in the model"),
            ],
            [typeof(Portfolio)] = [
                new("weights", "Gets weights of the portfolio"),
                new("lambda", "Gets the lambda of a corner portfolio"),
                new("ret", "Gets the expected return of the portfolio"),
                new("std", "Gets the standard deviation of the portfolio"),
                new("var", "Gets the variance of the portfolio"),
            ],
            [typeof(Point<Date>)] = [
                new("value", "Gets the numerical value of the point"),
                new("date", "Gets the date argument"),
            ],
            [typeof(Date)] = [
                new("day", "Gets the day of the date"),
                new("month", "Gets the month of the date"),
                new("year", "Gets the year of the date"),
                new("dow", "Gets the day of week the date"),
                new("isleap", "Checks if the date belong to a leap year"),
                new("addMonths(", "Adds a number of months to the date"),
                new("addYears(", "Adds a number of years to the date"),
            ],
            [typeof(Complex)] = [
                new("real", "Gets the real part of the complex number"),
                new("imag", "Gets the imaginary part of the complex number"),
                new("mag", "Gets the magnitude of the complex number"),
                new("phase", "Gets the phase of the complex number"),
            ],
            [typeof(FftCModel)] = [
                new("amplitudes", "Gets the amplitudes of the FFT"),
                new("phases", "Gets the phases of the FFT"),
                new("length", "Gets the length of the FFT"),
                new("inverse", "Gets the inverse of the transform as a complex vector"),
                new("values", "Gets the full spectrum as a complex vector"),
            ],
            [typeof(FftRModel)] = [
                new("amplitudes", "Gets the amplitudes of the FFT"),
                new("phases", "Gets the phases of the FFT"),
                new("length", "Gets the length of the FFT"),
                new("inverse", "Gets the inverse of the transform as a real vector"),
                new("values", "Gets the full spectrum as a complex vector"),
            ],
            [typeof(Polynomial)] = [
                new("eval", "Evaluates the polynomial at a point between 0 and 1"),
                new("derivative", "Gets the derivative at a point between 0 and 1"),
            ],
            [typeof(DoubleSequence)] = [
                new("filter(x => ", "Filters the sequence according to a predicate"),
                new("map(x => ", "Transforms the sequence according to a mapping function"),
                new("zip(", "Combines two sequence using a lambda function"),
                new("reduce(", "Combines all values in the sequence into a single value"),
                new("prod", "Gets the product of all values in the sequence"),
                new("sum", "Gets the sum of all values in the sequence"),
                new("min", "Gets the minimum value from the sequence"),
                new("max", "Gets the maximum value from the sequence"),
                new("stats", "Gets the common statistics of the sequence"),
                new("sort", "Sorts the sequence in ascending order"),
                new("sortDesc", "Sorts the sequence in descending order"),
                new("distinct", "Get the unique values in the sequence"),
                new("length", "Gets the number of values in the sequence"),
                new("any(x => ", "Existential operator"),
                new("all(x => ", "Universal operator"),
                new("toVector", "Converts the sequence to a vector"),
            ],
        }.ToFrozenDictionary();

    /// <summary>Code completion descriptors for class methods or constructors.</summary>
    private static readonly FrozenDictionary<string, Member[]> classMembers =
        new Dictionary<string, Member[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["series"] = [
                new("new(", "Creates a new series using weights and a list of series"),
            ],
            ["seq"] = [
                new("new(", "Creates a sequence from a range, a grid or a vector"),
                new("random(", "Creates a sequence from random numbers"),
                new("nrandom(", "Creates a sequence from normal random numbers"),
            ],
            ["spline"] = [
                new("new(", "Creates a new interpolator either from two vectors, a series, or from a function"),
            ],
            ["model"] = [
                new("mvo(", "Creates a model for a Mean Variance Optimizer"),
                new("compare(", "Compares two series or two vectors"),
            ],
            ["vector"] = [
                new("new(", "Create a vector given a length and an optional lambda"),
                new("random(", "Creates a random vector given a length"),
                new("nrandom(", "Creates a random vector using a standard normal distribution given a length"),
                new("ones(", "Creates a vector with ones given a length"),
            ],
            ["complexvector"] = [
                new("new(", "Create a complex vector given a size and an optional lambda"),
                new("random(", "Creates a random complex vector given a length"),
                new("nrandom(", "Creates a random vector using a standard normal distribution given a length"),
            ],
            ["matrix"] = [
                new("new(", "Create a rectangular matrix given a size and an optional lambda"),
                new("random(", "Creates a random matrix given a size"),
                new("nrandom(", "Creates a random matrix using a standard normal distribution given a size"),
                new("lrandom(", "Creates a random lower triangular matrix given a size"),
                new("lnrandom(", "Creates a random lower triangular matrix with a standard normal distribution"),
                new("eye(", "Creates an identity matrix given a size"),
                new("diag(", "Creates an diagonal matrix from a vector"),
                new("rows(", "Creates a matrix given its rows as vectors"),
                new("cols(", "Creates a matrix given its columns as vectors"),
                new("corr(", "Creates a correlation matrix given a list of series"),
                new("cov(", "Creates a covariance matrix given a list of series"),
            ],
            ["math"] = [
                new("abs(", "Absolute value"),
                new("solve(", "Newton-Raphson solver"),
                new("round(", "Rounds a real value"),
                new("sqrt(", "Squared root"),
                new("cbrt(", "Cubic root"),
                new("gamma(", "The Gamma function"),
                new("beta(", "The Beta function"),
                new("erf(", "Error function"),
                new("ncdf(", "Normal cummulative function"),
                new("probit(", "Probit function"),
                new("log(", "Natural logarithm"),
                new("log10(", "Base 10 logarithm"),
                new("exp(", "Exponential function"),
                new("sin(", "Sine function"),
                new("cos(", "Cosine function"),
                new("tan(", "Tangent function"),
                new("asin(", "Arcsine function"),
                new("acos(", "Arccosine function"),
                new("atan(", "Arctangent function"),
                new("min(", "Minimum function"),
                new("max(", "Maximum function"),
                new("polySolve(", "Solves a polynomial equation"),
                new("polyEval(", "Evaluates a polynomial"),
                new("polyDerivative(", "Evaluates the derivative of a polynomial"),
                new("complex(", "Creates a complex number from its real and imaginary components"),
                new("polar(", "Creates a complex number from its magnitude and phase components"),
                new("random", "Generate a random number from a uniform distribution"),
                new("nrandom", "Generate a random number from a normal standard distribution"),
                new("e", "Euler's constant"),
                new("i", "The imaginary unit"),
                new("pi", "Don't be irrational: be trascendent!"),
                new("today", "Gets the current date"),
                new("pearl", "Try me!"),
                new("τ", "Twice π"),
            ]
        }.ToFrozenDictionary();

    private class Zero { }

    private class One { }

    /// <summary>Gets a regex that matches a set statement.</summary>
    [GeneratedRegex("^\\s*(?'header'let\\s+.+\\s+in\\s+)", RegexOptions.IgnoreCase)]
    private static partial Regex LetHeaderRegex();

    /// <summary>Gets a regex that matches a lambda header with one parameter.</summary>
    [GeneratedRegex(@"^\w+\s*\=\>")]
    private static partial Regex LambdaHeader1();

    /// <summary>Gets a regex that matches a lambda header with two parameters.</summary>
    [GeneratedRegex(@"^\(\s*\w+\s*\,\s*\w+\s*\)\s*\=\>")]
    private static partial Regex LambdaHeader2();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsLambda() => kind == Token.Id
        ? LambdaHeader1().IsMatch(text.AsSpan()[start..])
        : LambdaHeader2().IsMatch(text.AsSpan()[start..]);

    /// <summary>Gets a list of members for a given type.</summary>
    /// <param name="source">A data source.</param>
    /// <param name="text">An expression fragment.</param>
    /// <param name="type">The type of the expression fragment.</param>
    /// <returns>A list of pairs member name/description.</returns>
    public static IList<Member> GetMembers(
        IDataSource source,
        string text,
        out Type? type)
    {
        ReadOnlySpan<char> trimmedText = ExtractObjectPath(text);
        if (!trimmedText.IsEmpty)
            try
            {
                return ExtractType(source, trimmedText.ToString());
            }
            catch
            {
                // Give it a second chance, if a let clause was not included.
                Match m = LetHeaderRegex().Match(text);
                if (m.Success && !LetHeaderRegex().IsMatch(trimmedText))
                    try
                    {
                        return ExtractType(source, m.Groups["header"] + trimmedText.ToString());
                    }
                    catch { }
            }
        type = null;
        return [];

        static IList<Member> ExtractType(IDataSource source, string text) =>
            members.TryGetValue(new Parser(source, text).ParseType(), out Member[]? list) ? list : [];
    }

    /// <summary>Gets a list of class members for a given type.</summary>
    /// <param name="text">An expression fragment.</param>
    /// <returns>A list of pairs member name/description.</returns>
    public static IList<Member> GetClassMembers(
        string text)
    {
        return classMembers.TryGetValue(ExtractClassName(text),
            out Member[]? list) ? list : [];

        static string ExtractClassName(string text)
        {
            ref char c = ref Unsafe.As<Str>(text).FirstChar;
            int i = Unsafe.Add(ref c, text.Length - 1) == ':'
                && Unsafe.Add(ref c, text.Length - 2) == ':' ? text.Length - 2 : text.Length - 1;
            if (i < 0 || Unsafe.Add(ref c, i--) != ':')
                return "";
            while (i >= 0 && char.IsWhiteSpace(Unsafe.Add(ref c, i)))
                i--;
            if (i < 0)
                return "";
            int end = i + 1;
            while (i >= 0 && char.IsLetter(Unsafe.Add(ref c, i)))
                i--;
            return text[(i + 1)..end].Trim();
        }
    }

    /// <summary>Extracts an object path from an expression fragment.</summary>
    /// <param name="text">A fragment of an expression.</param>
    /// <returns>The final object path.</returns>
    private static ReadOnlySpan<char> ExtractObjectPath(string text)
    {
        ref char c = ref Unsafe.As<Str>(text).FirstChar;
        int i = text.Length - 1;
        while (i >= 0)
        {
            char ch = Unsafe.Add(ref c, i);
            if (char.IsLetterOrDigit(ch) ||
                ch is '_' or '.' or ':' or '=' or '\'' ||
                char.IsWhiteSpace(ch))
                i--;
            else if (ch is '(' or '[')
                return text.AsSpan()[(i + 1)..];
            else if (ch == ')')
            {
                int count = 1;
                while (--i >= 0)
                {
                    ch = Unsafe.Add(ref c, i);
                    if (ch == ')')
                        count++;
                    else if (ch == '(')
                    {
                        if (--count == 0)
                            break;
                    }
                }
                if (count > 0)
                    return [];
                i--;
            }
            else if (ch == ']')
            {
                int count = 1;
                while (--i >= 0)
                {
                    ch = Unsafe.Add(ref c, i);
                    if (ch == ']')
                        count++;
                    else if (ch == '[')
                    {
                        if (--count == 0)
                            break;
                    }
                }
                if (count > 0)
                    return [];
                i--;
            }
            else
                break;
        }
        return text.AsSpan()[(i + 1)..].Trim();
    }
}
