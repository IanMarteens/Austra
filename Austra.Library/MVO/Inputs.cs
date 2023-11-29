namespace Austra.Library.MVO;

/// <summary>Constraint types.</summary>
public enum ConstraintType
{
    /// <summary>An equality constraint.</summary>
    EQUAL,
    /// <summary>
    /// Value must be greater than or equal to the right-hand side of the constraint.
    /// </summary>
    GREATER_THAN,
    /// <summary>
    /// Value must be less than or equal to the right-hand side of the constraint.
    /// </summary>
    LESS_THAN,
}

/// <summary>Portfolio data for the Mean-Variance Optimizer.</summary>
public sealed class Inputs
{
    /// <summary>Gets the number of securities in the portfolio.</summary>
    public int Securities { get; }
    /// <summary>Gets the number of constraints.</summary>
    public int Constraints { get; }
    /// <summary>Gets the total number of variables.</summary>
    public int Variables { get; internal set; }

    /// <summary>Gets or sets maximum numbers of CLR iterations.</summary>
    public int MaxCornerPortfolios { get; set; } = 100;
    /// <summary>Gets or sets the minimum allowed lambda.</summary>
    public double EndLambda { get; set; } = 0.000_001;

    /// <summary>Gets the lower limits for weights.</summary>
    public double[] LowerLimits { get; }
    /// <summary>Gets the upper limits for weights.</summary>
    public double[] UpperLimits { get; }

    /// <summary>Gets coefficients from the right-hand side of constraints.</summary>
    public double[] ConstraintsRHS { get; }
    /// <summary>Gets the left-hand side of constraints.</summary>
    public double[,] ConstraintsLHS { get; set; }
    /// <summary>Gets the type for each constraint.</summary>
    public ConstraintType[] ConstraintTypes { get; }

    /// <summary>Gets the expected returns.</summary>
    public double[] Mean { get; }
    /// <summary>Gets or sets the covariance matrix.</summary>
    public double[,] Cov { get; set; }

    /// <summary>When true, degenerate problems are given a second chance.</summary>
    public bool AllowDegenerate { get; set; } = true;

    /// <summary>Initializes portfolio data for the Mean-Variance Optimizer.</summary>
    /// <param name="securities">Number of securities in the portfolio.</param>
    /// <param name="constraintTypes">One constraint type for each constraint.</param>
    public Inputs(int securities, params char[] constraintTypes)
    {
        Securities = securities;
        if (constraintTypes == null || constraintTypes.Length == 0)
        {
            Constraints = 1;
            ConstraintTypes = new ConstraintType[1] { ConstraintType.EQUAL };
            Variables = Securities;
            ConstraintsLHS = new double[1, Securities + 1];
            for (int col = 0; col < Securities; col++)
                ConstraintsLHS[0, col] = 1.0;
            ConstraintsRHS = new double[1] { 1.0 };
        }
        else
        {
            Constraints = constraintTypes.Length;
            ConstraintTypes = new ConstraintType[Constraints];
            int slackVars = 0;
            for (int i = 0; i < constraintTypes.Length; i++)
                switch (constraintTypes[i])
                {
                    case '=':
                        ConstraintTypes[i] = ConstraintType.EQUAL;
                        break;
                    case '<':
                        ConstraintTypes[i] = ConstraintType.LESS_THAN;
                        slackVars++;
                        break;
                    case '>':
                        ConstraintTypes[i] = ConstraintType.GREATER_THAN;
                        slackVars++;
                        break;
                }
            Variables = Securities + slackVars;
            ConstraintsLHS = new double[Constraints, Variables + Constraints];
            ConstraintsRHS = new double[Constraints];
        }
        Mean = new double[Variables + Constraints];
        LowerLimits = new double[Variables + Constraints];
        UpperLimits = new double[Variables + Constraints];
        Array.Fill(UpperLimits, Simplex.INFINITY);
        Cov = new double[Variables + Constraints, Variables + Constraints];
    }

    /// <summary>Sets the constraint left hand and right hand sides.</summary>
    /// <param name="constraintLHS">A matrix of Constraints * Securities size.</param>
    /// <param name="constraintRHS">A vector of Constraints size.</param>
    public void SetConstraints(Matrix constraintLHS, DVector constraintRHS)
    {
        Copy((double[,])constraintLHS, ConstraintsLHS, Constraints, Securities);
        Array.Copy((double[])constraintRHS, ConstraintsRHS, Constraints);
    }

    /// <summary>Sets the lower limits for the weights of each security.</summary>
    /// <param name="lowerBoundaries">A vector of Securities size.</param>
    public void SetLowerBoundaries(DVector lowerBoundaries) =>
        Array.Copy((double[])lowerBoundaries, LowerLimits, Securities);

    /// <summary>Sets the upper limits for the weights of each security.</summary>
    /// <param name="upperBoundaries">A vector of Securities size.</param>
    public void SetUpperBoundaries(DVector upperBoundaries) =>
        Array.Copy((double[])upperBoundaries, UpperLimits, Securities);

    /// <summary>Sets the expected returns for each security.</summary>
    /// <param name="expectedReturns">A vector of Securities size.</param>
    public void SetExpectedReturns(DVector expectedReturns) =>
        Array.Copy((double[])expectedReturns, Mean, Securities);

    /// <summary>Initializes the covariance matrix from a linear array.</summary>
    /// <param name="covariance">The covariance matrix in linear form.</param>
    public void SetCovariance(DVector covariance) =>
        Copy((double[])covariance, Cov, Securities);

    /// <summary>Initializes the covariance matrix from another matrix.</summary>
    /// <param name="covariance">The bidimensional covariance matrix.</param>
    public void SetCovariance(Matrix covariance) =>
        Copy((double[,])covariance, Cov, Securities, Securities);

    /// <summary>Copies a lower triangular matrix into a symmetric matrix.</summary>
    /// <param name="src">The source matrix.</param>
    /// <param name="dest">The destination matrix.</param>
    /// <param name="rows">Number of rows.</param>
    private static void Copy(double[] src, double[,] dest, int rows)
    {
        int sumIdx = 0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                int srcIdx = sumIdx + j;
                dest[i, j] = src[srcIdx];
                if (j != i)
                    dest[j, i] = dest[i, j];
            }
            sumIdx += i + 1;
        }
    }

    /// <summary>Copies a two-dimensional matrix into another.</summary>
    /// <param name="src">The source matrix.</param>
    /// <param name="dest">The destination matrix.</param>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    private static void Copy(double[,] src, double[,] dest, int rows, int cols)
    {
        if (cols > 0)
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    dest[i, j] = src[i, j];
    }
}
