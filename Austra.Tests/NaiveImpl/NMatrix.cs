namespace Austra.Tests;

internal sealed class NMatrix(int rows, int cols, double[,] data)
{
    private readonly int rows = rows;
    private readonly int cols = cols;
    private readonly double[,] data = data;

    public NMatrix(Matrix matrix) : this(matrix.Rows, matrix.Cols, (double[,])matrix) { }

    public double AMax(NMatrix other)
    {
        double res = Math.Abs(data[0, 0] - other.data[0, 0]);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                res = Math.Max(res, Math.Abs(data[r, c] - other.data[r, c]));
        return res;
    }

    public NMatrix Multiply(NMatrix other)
    {
        int n = rows, m = cols, p = other.cols;
        double[,] d = new double[n, p];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < p; j++)
            {
                double sum = 0;
                for (int k = 0; k < m; k++)
                    sum += data[i, k] * other.data[k, j];
                d[i, j] = sum;
            }
        return new(n, p, d);
    }
}
