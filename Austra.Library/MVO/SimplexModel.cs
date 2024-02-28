using System.Drawing;

namespace Austra.Library.MVO;

/// <summary>Represents the result of a simplex algorithm.</summary>
public class SimplexModel
{
    /// <summary>Initializes and solves a new instance of the <see cref="SimplexModel"/> class.</summary>
    /// <remarks>The objective function value is maximized.</remarks>
    /// <param name="objective">Objective function coefficients.</param>
    /// <param name="constraintLHS">A matrix of Constraints * Securities size.</param>
    /// <param name="constraintRHS">A vector of Constraints size.</param>
    /// <param name="constraintTypes">One constraint sign for each constraint.</param>
    /// <param name="labels">The name of the variables.</param>
    public SimplexModel(DVector objective, Matrix constraintLHS, DVector constraintRHS, NVector constraintTypes, string[] labels)
    {
        int len = objective.Length;
        Objective = objective;
        if (constraintLHS.Cols != len)
            throw new MatrixSizeException($"Matrix should be {constraintLHS.Rows}x{len}");
        if (constraintRHS.Length != constraintLHS.Rows)
            throw new VectorLengthException($"Vector length should be {constraintLHS.Rows}");
        if (constraintRHS.Length != constraintTypes.Length)
            throw new VectorLengthException($"The must be a constraint type for each constraint");
        if (labels == null || labels.Length == 0)
            labels = Enumerable.Range(0, len).Select(i => i.ToString()).ToArray();
        else if (labels.Length > len)
            labels = labels[0..len];
        else if (labels.Length < len)
        {
            string[] newLabels = new string[len];
            Array.Copy(labels, newLabels, labels.Length);
            for (int i = labels.Length; i < len; i++)
                newLabels[i] = i.ToString();
            labels = newLabels;
        }
        Labels = labels;
        Inputs input = new(Objective, constraintTypes);
        input.SetCovariance(new Matrix(len));
        input.SetLowerBoundaries(new DVector(len));
        input.SetUpperBoundaries(new DVector(len, double.MaxValue));
        input.SetConstraints(constraintLHS, constraintRHS);
        input.TransformConstraints();
        States state = new(input);
        switch (Simplex.Run(input, state))
        {
            case Simplex.Result.INFEASIBLE:
                throw new ApplicationException(
                    "Infeasible problem. Check constraints and limits.");
            case Simplex.Result.UNBOUNDED:
                throw new ApplicationException(
                    "Unbounded. Make sure you have a valid budget constraint.");
            case Simplex.Result.DEGENERATE:
                throw new ApplicationException("Degenerate Problem.");
        }
        Weights = state.Weights[..len];
        Value = Objective * Weights;
    }

    /// <summary>Initializes a new instance of the <see cref="SimplexModel"/> class.</summary>
    /// <param name="objective">Objective function coefficients.</param>
    /// <param name="constraintLHS">A matrix of Constraints * Securities size.</param>
    /// <param name="constraintRHS">A vector of Constraints size.</param>
    /// <param name="constraintTypes">One constraint sign for each constraint.</param>
    public SimplexModel(DVector objective, Matrix constraintLHS, DVector constraintRHS, NVector constraintTypes)
        : this(objective, constraintLHS, constraintRHS, constraintTypes, []) { }

    /// <summary>Initializes a new instance of the <see cref="SimplexModel"/> class.</summary>
    /// <remarks>Assumes all constraints are of type EQUAL.</remarks>
    /// <param name="objective">Objective function coefficients.</param>
    /// <param name="constraintLHS">A matrix of Constraints * Securities size.</param>
    /// <param name="constraintRHS">A vector of Constraints size.</param>
    /// <param name="constraintType">The sign of all constraints.</param>
    public SimplexModel(DVector objective, Matrix constraintLHS, DVector constraintRHS, int constraintType)
        : this(objective, constraintLHS, constraintRHS, new NVector(constraintRHS.Length, constraintType), []) { }

    /// <summary>Initializes a new instance of the <see cref="SimplexModel"/> class.</summary>
    /// <remarks>Assumes all constraints are of type EQUAL.</remarks>
    /// <param name="objective">Objective function coefficients.</param>
    /// <param name="constraintLHS">A matrix of Constraints * Securities size.</param>
    /// <param name="constraintRHS">A vector of Constraints size.</param>
    public SimplexModel(DVector objective, Matrix constraintLHS, DVector constraintRHS)
        : this(objective, constraintLHS, constraintRHS, new NVector(constraintRHS.Length), []) { }

    /// <summary>
    /// Initializes and solves a new instance of the <see cref="SimplexModel"/> class
    /// assuming that we want to minimize the objective function instead of maximizing it.
    /// </summary>
    /// <param name="objective">Objective function coefficients.</param>
    /// <param name="constraintLHS">A matrix of Constraints * Securities size.</param>
    /// <param name="constraintRHS">A vector of Constraints size.</param>
    /// <param name="constraintTypes">One constraint sign for each constraint.</param>
    /// <param name="labels">The name of the variables.</param>
    /// <returns>A simplex model containing the optimal solution.</returns>
    public static SimplexModel Minimize(DVector objective,
        Matrix constraintLHS, DVector constraintRHS, NVector constraintTypes, string[] labels)
    {
        SimplexModel result = new(-objective, constraintLHS, constraintRHS, constraintTypes, labels);
        result.Objective = -result.Objective;
        result.Value = -result.Value;
        return result;
    }

    /// <summary>
    /// Initializes and solves a new instance of the <see cref="SimplexModel"/> class
    /// assuming that we want to minimize the objective function instead of maximizing it.
    /// </summary>
    /// <param name="objective">Objective function coefficients.</param>
    /// <param name="constraintLHS">A matrix of Constraints * Securities size.</param>
    /// <param name="constraintRHS">A vector of Constraints size.</param>
    /// <param name="constraintTypes">One constraint sign for each constraint.</param>
    /// <returns>A simplex model containing the optimal solution.</returns>
    public static SimplexModel Minimize(DVector objective,
        Matrix constraintLHS, DVector constraintRHS, NVector constraintTypes) =>
        Minimize(objective, constraintLHS, constraintRHS, constraintTypes, []);

    /// <summary>
    /// Initializes and solves a new instance of the <see cref="SimplexModel"/> class
    /// assuming that we want to minimize the objective function instead of maximizing it.
    /// </summary>
    /// <param name="objective">Objective function coefficients.</param>
    /// <param name="constraintLHS">A matrix of Constraints * Securities size.</param>
    /// <param name="constraintRHS">A vector of Constraints size.</param>
    /// <param name="constraintType">The constraint sign for all constraints.</param>
    /// <returns>A simplex model containing the optimal solution.</returns>
    public static SimplexModel Minimize(DVector objective,
        Matrix constraintLHS, DVector constraintRHS, int constraintType) =>
        Minimize(objective, constraintLHS, constraintRHS, new NVector(constraintRHS.Length, constraintType));

    /// <summary>
    /// Initializes and solves a new instance of the <see cref="SimplexModel"/> class
    /// assuming that we want to minimize the objective function instead of maximizing it.
    /// </summary>
    /// <param name="objective">Objective function coefficients.</param>
    /// <param name="constraintLHS">A matrix of Constraints * Securities size.</param>
    /// <param name="constraintRHS">A vector of Constraints size.</param>
    /// <returns>A simplex model containing the optimal solution.</returns>
    public static SimplexModel Minimize(DVector objective, Matrix constraintLHS, DVector constraintRHS) =>
        Minimize(objective, constraintLHS, constraintRHS, 0);

    /// <summary>Objective function coefficients.</summary>
    public DVector Objective { get; private set; }

    /// <summary>The name of the variables.</summary>
    public string[] Labels { get; }

    /// <summary>Weights of variables in the optimal solution.</summary>
    public DVector Weights { get; } = new DVector(0);

    /// <summary>Gets the value of the objective function at the optimal solution.</summary>
    public double Value { get; private set; }

    /// <summary>Gets a textual representation of the model.</summary>
    /// <returns>The representation of optimal weights.</returns>
    public override string ToString() =>
        new StringBuilder($"LP Model ({Objective.Length} variables)")
            .AppendLine()
            .Append("Value: ")
            .AppendLine(Value.ToString("G6"))
            .AppendLine("Weights:")
            .Append(((double[])Weights).ToString(v => v.ToString("G6")))
            .ToString();
}
