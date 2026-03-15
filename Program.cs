using System;

while (true)
{
    Console.Write("Введите первую строку (или 'exit' для выхода): ");
    string? input1 = Console.ReadLine();

    if (input1 == null)
    {
        break;
    }

    if (input1.ToLower() == "exit")
    {
        Console.WriteLine("Программа завершена.");
        break;
    }

    Console.Write("Введите вторую строку: ");
    string? input2 = Console.ReadLine();

    if (input2 == null)
    {
        input2 = "";
    }

    int distance = CalculateDamerauLevenshteinDistance(input1, input2);

    Console.WriteLine($"Расстояние Дамерау-Левенштейна между '{input1}' и '{input2}' равно: {distance}");
    Console.WriteLine();
}

static int CalculateDamerauLevenshteinDistance(string s1, string s2)
{
    string str1 = s1.ToUpper();
    string str2 = s2.ToUpper();

    int len1 = str1.Length;
    int len2 = str2.Length;

    if (len1 == 0) return len2;
    if (len2 == 0) return len1;

    int[,] matrix = new int[len1 + 1, len2 + 1];

    for (int i = 0; i <= len1; i++)
    {
        matrix[i, 0] = i;
    }

    for (int j = 0; j <= len2; j++)
    {
        matrix[0, j] = j;
    }

    for (int i = 1; i <= len1; i++)
    {
        for (int j = 1; j <= len2; j++)
        {
            int cost = (str1[i - 1] == str2[j - 1]) ? 0 : 1;

            int deletion = matrix[i - 1, j] + 1;

            int insertion = matrix[i, j - 1] + 1;

            int substitution = matrix[i - 1, j - 1] + cost;

            matrix[i, j] = Math.Min(Math.Min(deletion, insertion), substitution);

            if (i > 1 && j > 1 &&
                str1[i - 1] == str2[j - 2] &&
                str1[i - 2] == str2[j - 1])
            {
                int transposition = matrix[i - 2, j - 2] + cost;
                matrix[i, j] = Math.Min(matrix[i, j], transposition);
            }
        }
    }
    return matrix[len1, len2];
}