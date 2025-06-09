namespace App_01;

public struct Color
{
    public float _red {get; set;}
    public float _green {get; set;}
    public float _blue {get; set;}

    public Color(float x, float y, float z)
    {
        _red = x;
        _green = y;
        _blue = z;
    }
}

public struct Vector2Int
{
    public int _x {get; set;}
    public int _y {get; set;}

    public Vector2Int(int x, int y)
    {
        _x = x;
        _y = y;
    }
}

class Program
{
    static void Main(string[] args)
    {
        CreateImage();
    }

    public static void CreateImage()
    {
        Vector2Int dimmensions = new Vector2Int(64, 64);
        Color[,] image = new Color[dimmensions._x, dimmensions._y];

        Console.WriteLine($"dims: {dimmensions._x}, {dimmensions._y}");
        Console.WriteLine($"image: {image.GetLength(0)}, {image.GetLength(1)}");

        // Iterate down the rows
        for (int y = 0; y < dimmensions._y; y++)
        {
            // Iterate across the cols
            for (int x = 0; x < dimmensions._x; x++)
            {
                // float red = 1.0f;
                // float green = 0.0f;
                image[x, y] = new Color(1f, 0, 0);
            }
        }

        // Write the image file
        WriteImageToFile(image, "test.bmp");
    }

    public static int Power4(int value)
    {
        int power = value;
        while (power % 4 != 0)
        {
            power++; 
        }

        Console.WriteLine($"Value: {value} | Power: {power}");
        return power;
    }

    public static void WriteImageToFile(Color[,] image, string filename)
    {
        // Create a text file
        using FileStream stream = new FileStream(filename, FileMode.Create);
        using BinaryWriter writer = new BinaryWriter(stream);

        // Get the File Byte Size
        // Header + DIB Header + Pixel Data (Each y in the image will need be padded by 4 bytes, ex: pixel rows * 4)
        int paddedWidth = Power4(image.GetLength(0) * 3);
        int pixelSize = image.GetLength(1) * paddedWidth;
        Console.WriteLine($"Pixel Size: {pixelSize}");
        uint[] byteCounts = {14, 40, (uint)pixelSize};
        
        Console.WriteLine($"Image Size: width={image.GetLength(0)}, height{image.GetLength(1)}");
        Console.WriteLine($"Byte Counts: {byteCounts[0]} {byteCounts[1]} {byteCounts[2]}");

        // Write the headers to the file
        writer.Write("BM"u8.ToArray()); // Signature BM
        writer.Write(byteCounts[0] + byteCounts[1] + byteCounts[2]); // Total File Size
        writer.Write((ushort)0); // Unused
        writer.Write((ushort)0); // Unused
        writer.Write(byteCounts[0] + byteCounts[1]); // Offset to pixel data

        // DIB Header
        writer.Write(byteCounts[1]); // DIB Header Size
        writer.Write((uint)image.GetLength(0)); // Image Width
        writer.Write((uint)image.GetLength(1)); // Image Height
        writer.Write((ushort)1); // Number of planes being used
        writer.Write((short)(24)); // Number of bits per pixel
        writer.Write((uint)0); // Compresssion
        writer.Write(byteCounts[2]); // The image size
        writer.Write((uint)0); // Pixel per X meter (DPI) | ignore for now
        writer.Write((uint)0); // Pixel per Y meter (DPI) | ignore for now
        writer.Write((uint)0); // Number of colors in the pallete
        writer.Write((uint)0); // Number of important colors (0 means all colors are important)

        // Pixel Data
        int paddingSize = Power4(image.GetLength(0) * 3) - (image.GetLength(0) * 3);
        for(int y = image.GetLength(1) - 1; y >= 0; y--)
        {
            for(int x = 0; x < image.GetLength(0); x++)
            {
                Color pixel = image[x, y];
                writer.Write((byte) (pixel._blue * 255));
                writer.Write((byte) (pixel._green * 255));
                writer.Write((byte) (pixel._red * 255));
            }
            writer.Write(new Byte[paddingSize]);
        }

        // Close the file
        stream.Close();

        var hexBytes = File.ReadAllBytes(filename);
        string hexString = BitConverter.ToString(hexBytes).Replace("-", "");;
        // Console.WriteLine($"{hexString}");
        // Console.WriteLine($"424D7A000000000000007A0000006C000000380000000300000003000000010018000000000000000000130B0000130B00000000000000000000FF0000FF0000FF000000FF0000FF0000FF000000FF0000FF0000FF00");
    }
}
