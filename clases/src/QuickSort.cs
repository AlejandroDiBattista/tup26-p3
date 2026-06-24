using System;

public static class QuickSortExample
{
    public static void QuickSort(int[] array)
    {
        if (array == null || array.Length < 2)
            return;

        QuickSort(array, 0, array.Length - 1);
    }

    private static void QuickSort(int[] array, int left, int right)
    {
        if (left >= right)
            return;

        int pivotIndex = Partition(array, left, right);

        QuickSort(array, left, pivotIndex - 1);
        QuickSort(array, pivotIndex + 1, right);
    }

    private static int Partition(int[] array, int left, int right)
    {
        int pivot = array[right];
        int i = left - 1;

        for (int j = left; j < right; j++)
        {
            if (array[j] <= pivot)
            {
                i++;
                Swap(array, i, j);
            }
        }

        Swap(array, i + 1, right);
        return i + 1;
    }

    private static void Swap(int[] array, int i, int j)
    {
        if (i == j)
            return;

        int temp = array[i];
        array[i] = array[j];
        array[j] = temp;
    }

    public static void Main()
    {
        int[] numbers = { 8, 3, 1, 7, 0, 10, 2 };

        Console.WriteLine("Antes:  " + string.Join(", ", numbers));
        QuickSort(numbers);
        Console.WriteLine("Después: " + string.Join(", ", numbers));
    }
}
