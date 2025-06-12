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

    public static Vector2Int operator -(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(
            a._x - b._x,
            a._y - b._y);
    }

    public static int Dot(Vector2Int a, Vector2Int b)
    {
        return a._x * b._x + a._y * b._y;
    }

    public static Vector2Int GetPerpendicular(Vector2Int vec)
    {
        return new Vector2Int(vec._y, -vec._x);
    }
}

class Program
{
    static void Main(string[] args)
    {
        CreateImage();
    }

    // --------------------------------------------------------------------- //
    // Image Wrtier Stuff
    public static void CreateImage()
    {
        Vector2Int dimmensions = new Vector2Int(128, 128);
        Color[,] image = new Color[dimmensions._x, dimmensions._y];

        Vector2Int a = new Vector2Int(
            (int) (image.GetLength(0) * 0.1), 
            (int)( image.GetLength(1) * 0.8));
        Vector2Int b = new Vector2Int(
            (int) (image.GetLength(0) * 0.4), 
            (int)( image.GetLength(1) * 0.2));
        Vector2Int c = new Vector2Int(
            (int) (image.GetLength(0) * 0.8), 
            (int)( image.GetLength(1) * 0.7));

        AddTriangleToImage(image, a, b, c, new Color(1f, 0, 0));

        a = new Vector2Int(
            (int) (image.GetLength(0) * 0.1), 
            (int)( image.GetLength(1) * 0.1));
        b = new Vector2Int(
            (int) (image.GetLength(0) * 0.4), 
            (int)( image.GetLength(1) * 0.8));
        c = new Vector2Int(
            (int) (image.GetLength(0) * 0.8), 
            (int)( image.GetLength(1) * 0.1));

        AddTriangleToImage(image, a, b, c, new Color(0, 0, 1f));

        // Write the image file
        WriteImageToFile(image, "test.bmp");
    }

    // Used to generate the default red/green fade texture 
    private static void DefaultTexture(Color[,] image)
    {
        // Iterate down the rows
        for (int y = 0; y < image.GetLength(1); y++)
        {
            // Iterate across the cols
            for (int x = 0; x < image.GetLength(0); x++)
            {
                float red = x / (image.GetLength(0) - 1f);
                float green = y / (image.GetLength(1) - 1f);
                image[x, y] = new Color(red, green, 0);
            }
        }
    }
    
    // Used to add a triangle to the scene
    private static void AddTriangleToImage(
        Color[,] image,
        Vector2Int A,
        Vector2Int B,
        Vector2Int C,
        Color color)
    {
        // Loop over the pixels in the image
        for (int x = 0; x < image.GetLength(0); x++)
        {
            for (int y = 0; y < image.GetLength(1); y++)
            {
                bool isInside = UsefulMath.IsInsideTriangle(
                    A, 
                    B, 
                    C, 
                    new Vector2Int(x, y));

                if (isInside)
                {
                    image[x, y] = color;
                }
            }
        }
    }

    public static void WriteImageToFile(Color[,] image, string filename)
    {
        // Create a text file
        using FileStream stream = new FileStream(filename, FileMode.Create);
        using BinaryWriter writer = new BinaryWriter(stream);

        // Each pixel row needs to be a power of 4 (1, 4, 8, 12, 16, ...)
        // So we will get the size of each row by considering the image width and padding needed
        int paddedRowSizeInBytes = UsefulMath.Power4(image.GetLength(0) * 3);

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

    // --------------------------------------------------------------------- //
    // Utilites
    public static class UsefulMath
    {
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

       public static bool IsInsideTriangle(
            Vector2Int A,
            Vector2Int B,
            Vector2Int C,
            Vector2Int P)
       {
           // Define our triangle vectors AB, BC, CA
           Vector2Int AB = B - A;
           Vector2Int BC = C - B;
           Vector2Int CA = A - C;

           // Define our point vectors AP, BP, CP
           Vector2Int AP = P - A;
           Vector2Int BP = P - B;
           Vector2Int CP = P - C;

           // Get the sign the dot prodcuts for each point vector and the coresponding triangle vector
           int APxAB = Math.Sign(Vector2Int.Dot(AP, Vector2Int.GetPerpendicular(AB)));
           int BPxBC = Math.Sign(Vector2Int.Dot(BP, Vector2Int.GetPerpendicular(BC)));
           int CPxCA = Math.Sign(Vector2Int.Dot(CP, Vector2Int.GetPerpendicular(CA)));

           // Check if all three dot products are equal in sign,
           // thus implying the point is "inside" the triangle space
           return APxAB == BPxBC && BPxBC == CPxCA;
       }
    }
}
