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

    // ------------------------------------------------------------------------
    // Image Wrtier Stuff
    public static void CreateImage()
    {
        Vector2Int dimmensions = new Vector2Int(128, 128);
        Color[,] image = new Color[dimmensions._x, dimmensions._y];

        // Iterate down the rows
        for (int y = 0; y < dimmensions._y; y++)
        {
            // Iterate across the cols
            for (int x = 0; x < dimmensions._x; x++)
            {
                float red = x / (image.GetLength(0) - 1f);
                float green = y / (image.GetLength(1) - 1f);
                image[x, y] = new Color(red, green, 0);
            }
        }

        // Write the image file
        WriteImageToFile(image, "test.bmp");
    }

    public static void WriteImageToFile(Color[,] image, string filename)
    {
        // Create a text file
        using FileStream stream = new FileStream(filename, FileMode.Create);
        using BinaryWriter writer = new BinaryWriter(stream);

        // Each pixel row needs to be a power of 4 (1, 4, 8, 12, 16, ...)
        // So we will get the size of each row by considering the image width and padding needed
        int paddedRowSizeInBytes = Power4(image.GetLength(0) * 3);

        // The image size is the padded size multiplyied by the number of rows
        int imageSizeInBytes = image.GetLength(1) * paddedRowSizeInBytes;
        Console.WriteLine($"Pixel Size: {imageSizeInBytes}");

        // Header + DIB Header + Pixel Data (Each y in the image will need be padded by 4 bytes, ex: pixel rows * 4)
        uint[] totalByteCount = {14, 40, (uint)imageSizeInBytes};
        
        Console.WriteLine($"Image Size: width={image.GetLength(0)}, height{image.GetLength(1)}");
        Console.WriteLine($"Byte Counts: {totalByteCount[0]} {totalByteCount[1]} {totalByteCount[2]}");

        // Standard Header
        writer.Write("BM"u8.ToArray()); // Signature BM
        writer.Write(totalByteCount[0] + totalByteCount[1] + totalByteCount[2]); // Total File Size
        writer.Write((ushort)0); // Unused
        writer.Write((ushort)0); // Unused
        writer.Write(totalByteCount[0] + totalByteCount[1]); // Offset to pixel data

        // DIB Header
        writer.Write(totalByteCount[1]); // DIB Header Size
        writer.Write((uint)image.GetLength(0)); // Image Width
        writer.Write((uint)image.GetLength(1)); // Image Height
        writer.Write((ushort)1); // Number of planes being used
        writer.Write((short)(24)); // Number of bits per pixel
        writer.Write((uint)0); // Compresssion
        writer.Write(totalByteCount[2]); // The image size
        writer.Write((uint)0); // Pixel per X meter (DPI) | ignore for now
        writer.Write((uint)0); // Pixel per Y meter (DPI) | ignore for now
        writer.Write((uint)0); // Number of colors in the pallete
        writer.Write((uint)0); // Number of important colors (0 means all colors are important)

        // Get the size of the padding appending to each row of pixels
        int paddingSizeInBytes = paddedRowSizeInBytes - (image.GetLength(0) * 3);
        // Loop through all the pixels
        for(int y = image.GetLength(1) - 1; y >= 0; y--)
        {
            for(int x = 0; x < image.GetLength(0); x++)
            {
                Color pixel = image[x, y];
                writer.Write((byte) (pixel._blue * 255));
                writer.Write((byte) (pixel._green * 255));
                writer.Write((byte) (pixel._red * 255));
            }
            writer.Write(new Byte[paddingSizeInBytes]);
        }

        // Close the file
        stream.Close();
    }

    // ------------------------------------------------------------------------
    // Utilites
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
}
