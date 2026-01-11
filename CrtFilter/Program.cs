using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CrtFilter;

public class CrtPixelScaler
{
    public double CenterWeight { get; set; } = 1.0;
    public double ImmediateNeighborWeight { get; set; } = 0.35;
    public double DiagonalNeighborWeight { get; set; } = 0.25;
    public double EdgeBlendStrength { get; set; } = 0.30;
    public double GlobalBlendMultiplier { get; set; } = 1.0;

    public Image<Rgb24> ScaleAndBlur(Image<Rgb24> original)
    {
        int originalWidth = original.Width;
        int originalHeight = original.Height;
        int scaledWidth = originalWidth * 5;
        int scaledHeight = originalHeight * 5;

        var scaled = new Image<Rgb24>(scaledWidth, scaledHeight);

        for (int y = 0; y < originalHeight; y++)
        {
            for (int x = 0; x < originalWidth; x++)
            {
                var pixelGrid = CreatePixelGrid(original, x, y);
                WriteScaledPixel(scaled, x, y, pixelGrid);
            }
        }

        return scaled;
    }

    private Rgb24[,] CreatePixelGrid(Image<Rgb24> original, int centerX, int centerY)
    {
        var grid = new Rgb24[5, 5];
        int width = original.Width;
        int height = original.Height;

        for (int py = 0; py < 5; py++)
        {
            for (int px = 0; px < 5; px++)
            {
                int offsetX = px - 2;
                int offsetY = py - 2;

                int neighborX = centerX + offsetX;
                int neighborY = centerY + offsetY;

                neighborX = Math.Max(0, Math.Min(neighborX, width - 1));
                neighborY = Math.Max(0, Math.Min(neighborY, height - 1));

                Rgb24 centerPixel = original[centerX, centerY];
                Rgb24 neighborPixel = original[neighborX, neighborY];
                double weight = GetBlendWeight(px, py);
                grid[px, py] = BlendPixels(centerPixel, neighborPixel, weight);
            }
        }

        return grid;
    }

    private double GetBlendWeight(int px, int py)
    {
        int dx = Math.Abs(px - 2);
        int dy = Math.Abs(py - 2);
        int distance = dx + dy;

        double weight = distance switch
        {
            0 => CenterWeight,
            1 => ImmediateNeighborWeight,
            2 when dx == 2 || dy == 2 => EdgeBlendStrength,
            2 => DiagonalNeighborWeight,
            3 => DiagonalNeighborWeight * 0.5,
            4 => DiagonalNeighborWeight * 0.25,
            _ => 0
        };

        if (distance > 0)
        {
            weight *= GlobalBlendMultiplier;
        }

        return weight;
    }

    private Rgb24 BlendPixels(Rgb24 center, Rgb24 neighbor, double blendWeight)
    {
        blendWeight = Math.Clamp(blendWeight, 0, 1);
        double centerWeight = 1.0 - blendWeight;

        return new Rgb24(
            (byte)Math.Clamp(center.R * centerWeight + neighbor.R * blendWeight, 0, 255),
            (byte)Math.Clamp(center.G * centerWeight + neighbor.G * blendWeight, 0, 255),
            (byte)Math.Clamp(center.B * centerWeight + neighbor.B * blendWeight, 0, 255)
        );
    }

    private void WriteScaledPixel(Image<Rgb24> scaled, int x, int y, Rgb24[,] grid)
    {
        int startX = x * 5;
        int startY = y * 5;

        for (int py = 0; py < 5; py++)
        {
            for (int px = 0; px < 5; px++)
            {
                scaled[startX + px, startY + py] = grid[px, py];
            }
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: CrtFilter <image-path> [blend-multiplier]");
            Console.WriteLine("Example: CrtFilter image.png 2.0");
            return;
        }

        string inputPath = args[0];
        double blendMultiplier = args.Length > 1 ? double.Parse(args[1]) : 1.5;
        string outputPath = Path.ChangeExtension(inputPath, "_crt" + Path.GetExtension(inputPath));

        Console.WriteLine($"Loading image: {inputPath}");

        using var original = Image.Load<Rgb24>(inputPath);
        Console.WriteLine($"Original size: {original.Width}x{original.Height}");

        var scaler = new CrtPixelScaler
        {
            CenterWeight = 1.0,
            ImmediateNeighborWeight = 0.35,
            DiagonalNeighborWeight = 0.25,
            EdgeBlendStrength = 0.30,
            GlobalBlendMultiplier = blendMultiplier
        };

        Console.WriteLine($"Applying CRT pixel scaling (5x) with blend multiplier {blendMultiplier}...");
        using var scaled = scaler.ScaleAndBlur(original);
        Console.WriteLine($"Scaled size: {scaled.Width}x{scaled.Height}");

        scaled.Save(outputPath);
        Console.WriteLine($"Saved to: {outputPath}");
    }
}
