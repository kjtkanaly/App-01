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
        Vector2Int dimmensions = new Vector2Int(2, 2);
        Color[,] image = new Color[dimmensions._x, dimmensions._y];

        Console.WriteLine($"dims: {dimmensions._x}, {dimmensions._y}");
        Console.WriteLine($"image: {image.GetLength(0)}, {image.GetLength(1)}");

        // Iterate down the rows
        image[0, 0] = new Color(0, 0, 1);
        image[1, 0] = new Color(0, 1, 0);
        image[0, 1] = new Color(1, 0, 0);
        image[1, 1] = new Color(1, 1, 1);
        /*
        for (int row = 0; row < dimmensions._y; row++)
        {
            // Iterate across the cols
            for (int col = 0; col < dimmensions._x; col++)
            {
                Console.WriteLine($"Write Color: {row}, {col}");
                float red = 1;
                float green = 1;
                image[col, row] = new Color(red, green, 0);
            }
        }
        */

        // Write the image file
        WriteImageToFile(image, "test");
    }
    
    public static void WriteImageToFile(Color[,] image, string filename)
    {
        // Create a text file
        // using FileStream stream = new FileStream(filename + ".txt", FileMode.Create);
        using FileStream headerStream = new FileStream("header.txt", FileMode.Create);
        using FileStream dibStream = new FileStream("dib.txt", FileMode.Create);
        using FileStream pixelStream = new FileStream("pixel.txt", FileMode.Create);
        // using BinaryWriter writer = new BinaryWriter(stream);
        using BinaryWriter headerWriter = new BinaryWriter(headerStream);
        using BinaryWriter dibWriter = new BinaryWriter(dibStream);
        using BinaryWriter pixelWriter = new BinaryWriter(pixelStream);
        

        // Get the File Byte Size
        // Header + DIB Header + Pixel Data (Each row in the image will need be padded by 4 bytes, ex: pixel rows * 4)
        uint[] byteCounts = {14, 40, (uint)image.GetLength(0) * 8};
        
        Console.WriteLine($"Image Size: width={image.GetLength(0)}, height{image.GetLength(1)}");
        Console.WriteLine($"Byte Counts: {byteCounts[0]} {byteCounts[1]} {byteCounts[2]}");
        
        // Write the headers to the file
        headerWriter.Write("BM"u8.ToArray()); // Signature BM
        headerWriter.Write(byteCounts[0] + byteCounts[1] + byteCounts[2]); // Total File Size
        headerWriter.Write((ushort)0); // Unused
        headerWriter.Write((ushort)0); // Unused
        headerWriter.Write(byteCounts[0] + byteCounts[1]); // Offset to pixel data

        // DIB Header
        dibWriter.Write(byteCounts[1]); // DIB Header Size
        dibWriter.Write((uint)image.GetLength(0)); // Image Width
        dibWriter.Write((uint)image.GetLength(1)); // Image Height
        dibWriter.Write((ushort)1); // Number of planes being used
        dibWriter.Write((ushort)(24)); // Number of bits per pixel
        dibWriter.Write((uint)0); // Compresssion
        dibWriter.Write(byteCounts[2]); // The image size
        dibWriter.Write((uint)0); // Pixel per X meter (DPI) | ignore for now
        dibWriter.Write((uint)0); // Pixel per Y meter (DPI) | ignore for now
        dibWriter.Write((uint)0); // Number of colors in the pallete
        dibWriter.Write((uint)0); // Number of important colors (0 means all colors are important)

        // Pixel Data
        for(int y = image.GetLength(1) - 1; y >= 0; y--)
        {
            for(int x = 0; x < image.GetLength(0); x++)
            {
                Color pixel = image[x, y];
                pixelWriter.Write((byte) (pixel._blue * 255));
                pixelWriter.Write((byte) (pixel._green * 255));
                pixelWriter.Write((byte) (pixel._red * 255));
                Console.WriteLine($"{x}, {y}: {pixel._red}, {pixel._green}, {pixel._blue} ");
            }
            pixelWriter.Write(new Byte[2]);
        }

        // Close the file
        // stream.Close();
        headerStream.Close();
        dibStream.Close();
        pixelStream.Close();

        // Debug
        // byte[] fileBytes = File.ReadAllBytes(filename + ".txt");
        byte[] headerBytes = File.ReadAllBytes("header.txt");
        byte[] dibBytes = File.ReadAllBytes("dib.txt");
        byte[] pixelBytes = File.ReadAllBytes("pixel.txt");
        // string hexString = BitConverter.ToString(fileBytes).Replace("-", ""); // Hex values are separated by hyphens by default, remove them
        string headerString = BitConverter.ToString(headerBytes).Replace("-", ""); // Hex values are separated by hyphens by default, remove them
        string dibString = BitConverter.ToString(dibBytes).Replace("-", ""); // Hex values are separated by hyphens by default, remove them
        string pixelString = BitConverter.ToString(pixelBytes).Replace("-", ""); // Hex values are separated by hyphens by default, remove them

        // Console.WriteLine($"File Bytes: {hexString}");
        Console.WriteLine($"Header : {headerString}");
        Console.WriteLine($"Example: 424D460000000000000036000000\n");
        Console.WriteLine($"DIB    : {dibString}");
        Console.WriteLine($"Example: 28000000020000000200000001001800000000001000000000000000000000000000000000000000\n");
        Console.WriteLine($"Header : {pixelString}");
        Console.WriteLine($"Example: 0000FFFFFFFF0000FF000000FF000000\n");
    }
}
