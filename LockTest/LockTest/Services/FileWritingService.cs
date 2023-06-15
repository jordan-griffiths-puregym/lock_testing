using System.Text;

public class FileWritingService
{
    public void WriteContentToFileToTransfer(string filePath, int rows)
    {
        Console.WriteLine($"Creating CSV with {rows} rows @ {filePath}");

        StringBuilder sb = new StringBuilder();
        sb.Append("col1,col2,col3,col4,col5,col6,col7,col8,col9\n");
        Random rng = new Random();
        for (int i = 1; i < rows; i++)
        {
            sb.Append($"{rng.Next(1000000, 99999999)}" +
                $",{rng.Next(1000000, 99999999)}" +
                $",{rng.Next(1000000, 99999999)}" +
                $",{rng.Next(1000000, 99999999)}" +
                $",{rng.Next(1000000, 99999999)}" +
                $",{rng.Next(1000000, 99999999)}" +
                $",{rng.Next(1000000, 99999999)}" +
                $",{rng.Next(1000000, 99999999)}" +
                $",{rng.Next(1000000, 99999999)}\n");
        }

        using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate))
        using (var streamWriter = new StreamWriter(fileStream))
        {
            streamWriter.WriteLine(sb.ToString());
        }

        Console.WriteLine($"Created CSV");
    }
}
