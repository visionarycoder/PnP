namespace Snippets.DesignPatterns.Behavioral.Iterator;

public class Matrix<T>(int rows, int columns)
{
    private readonly T[,] matrix = new T[rows, columns];

    public T this[int row, int col]
    {
        get => matrix[row, col];
        set => matrix[row, col] = value;
    }

    public int Rows => rows;
    public int Columns => columns;

    // Row-wise iteration
    public IEnumerable<T> IterateByRows()
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                yield return matrix[i, j];
            }
        }
    }

    // Column-wise iteration
    public IEnumerable<T> IterateByColumns()
    {
        for (int j = 0; j < columns; j++)
        {
            for (int i = 0; i < rows; i++)
            {
                yield return matrix[i, j];
            }
        }
    }

    // Diagonal iteration
    public IEnumerable<T> IterateDiagonal()
    {
        int minDimension = Math.Min(rows, columns);
        for (int i = 0; i < minDimension; i++)
        {
            yield return matrix[i, i];
        }
    }

    // Spiral iteration (clockwise from outside to inside)
    public IEnumerable<T> IterateSpiral()
    {
        if (rows == 0 || columns == 0) yield break;

        int top = 0, bottom = rows - 1;
        int left = 0, right = columns - 1;

        while (top <= bottom && left <= right)
        {
            // Top row (left to right)
            for (int j = left; j <= right; j++)
            {
                yield return matrix[top, j];
            }

            top++;

            // Right column (top to bottom)
            for (int i = top; i <= bottom; i++)
            {
                yield return matrix[i, right];
            }

            right--;

            // Bottom row (right to left)
            if (top <= bottom)
            {
                for (int j = right; j >= left; j--)
                {
                    yield return matrix[bottom, j];
                }

                bottom--;
            }

            // Left column (bottom to top)
            if (left <= right)
            {
                for (int i = bottom; i >= top; i--)
                {
                    yield return matrix[i, left];
                }

                left++;
            }
        }
    }
}