using System.Diagnostics;

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

public struct Vector2
{
    public int _x {get; set;}
    public int _y {get; set;}

    public Vector2(int x, int y)
    {
        _x = x;
        _y = y;
    }

    public static Vector2 operator -(Vector2 a, Vector2 b)
    {
        return new Vector2(
            a._x - b._x,
            a._y - b._y);
    }

    public static int Dot(Vector2 a, Vector2 b)
    {
        return a._x * b._x + a._y * b._y;
    }

    public static Vector2 GetPerpendicular(Vector2 vec)
    {
        return new Vector2(vec._y, -vec._x);
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

public struct Triangle
{
    public Vector2Int[] _points = new Vector2Int[3];
    public Color _color = new Color();

    public Triangle(
        Vector2Int a, 
        Vector2Int b, 
        Vector2Int c,
        Color color = new Color(),
        Vector2Int velocity = new Vector2Int())
    {
        _points[0] = a;
        _points[1] = b;
        _points[2] = c;

        _color = color;
    }
}

// ----------------------------------------------------------------------------
// Main Program Class
class Program
{
    static void Main(string[] args)
    {
        GenerateAnimation();
    }

    // ------------------------------------------------------------------------
    // Animation Writer Stuff
    public static void GenerateAnimation()
    {
        string ffmpegExeName = "ffmpeg";
        string outputVideoPath = "Test.mp4";
        Vector2Int resolution = new Vector2Int(1024, 512);
        int frameRate = 30;
        int totalFrameCount = 30;
        int triangleCount = 3;

        // Create the array of triangles and init them with random states (pos & velocity)
        Triangle[] triangles = new Triangle[triangleCount];
        InitRandomTriangleStates(triangles, resolution);

        DrawFrames(resolution, totalFrameCount, triangles, true);

        return;

        // Init the video writer
        ProcessStartInfo processInfo = new ProcessStartInfo();
        processInfo.FileName = ffmpegExeName;
        // processInfo.Arguments = $"-framerate {frameRate} -i \"{outputFolder}/frame%d.bmp\" -c:v libx264 -pix_fmt yuv420p -y \"{outputVideoPath}\"";
        processInfo.RedirectStandardOutput = true;
        processInfo.RedirectStandardError = true;
        processInfo.UseShellExecute = false;
        processInfo.CreateNoWindow = true;
    }

    public static void DrawFrames(
            Vector2Int resolution, 
            int totalFrameCount, 
            Triangle[] objects,
            bool debug = false)
    {
        // Store the dir we store the frames in
        string frameDir = "frames/";

        // Create the frame object
        Color[,] frame = new Color[resolution._x, resolution._y];

        int paddingSize = totalFrameCount.ToString().Length;
        int cursorTopPos = Console.CursorTop;
        int cursorLeftPos = Console.CursorLeft;

        // Loop until our count reaches the requested total frames
        for (int i = 0; i < totalFrameCount; i++)
        {
            if (debug)
            {
                Console.SetCursorPosition(cursorLeftPos, cursorTopPos);
                Console.Write($"Renering Frame: {(i + 1).ToString($"D{paddingSize}")} / {totalFrameCount}\n");
            }

            // Iterate the scene to the next frame
            PropogateObjectsToNextFrame(objects);

            // Draw the new frame
            // TODO: Create some sort of scene object
            DrawSceneOnFrame(frame, objects);
            
            // Write the frame to the frameDir
            WriteImageToFile(frame, frameDir + $"frame-{i}.bmp");
        }
    }

    public static void DrawSceneOnFrame(Color[,] frame, Triangle[] triangles)
    {
       for (int i = 0; i < triangles.Length; i++)
       {
           AddTriangleToImage(frame, triangles[i]);
       }
    }

    // ------------------------------------------------------------------------
    // Image Wrtier Stuff
    public static void CreateTriangleTestImage()
    {
        Vector2Int dimmensions = new Vector2Int(1024, 512);
        Color[,] image = new Color[dimmensions._x, dimmensions._y];

        CreateTestTriangles(image, 3);

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

    public static void WriteImageToFile(
            Color[,] image, 
            string filename,
            bool debug = false)
    {
        // Create a text file
        using FileStream stream = new FileStream(filename, FileMode.Create);
        using BinaryWriter writer = new BinaryWriter(stream);

        // Each pixel row needs to be a power of 4 (1, 4, 8, 12, 16, ...)
        // So we will get the size of each row by considering the image width and padding needed
        int paddedRowSizeInBytes = UsefulMath.Power4(image.GetLength(0) * 3);

        // The image size is the padded size multiplyied by the number of rows
        int imageSizeInBytes = image.GetLength(1) * paddedRowSizeInBytes;

        // Header + DIB Header + Pixel Data (Each y in the image will need be padded by 4 bytes, ex: pixel rows * 4)
        uint[] totalByteCount = {14, 40, (uint)imageSizeInBytes};

        if (debug)
        {
            Console.WriteLine($"Pixel Size: {imageSizeInBytes}");
            Console.WriteLine($"Image Size: width={image.GetLength(0)}, height{image.GetLength(1)}");
            Console.WriteLine($"Byte Counts: {totalByteCount[0]} {totalByteCount[1]} {totalByteCount[2]}");
        }
        
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
        // for(int y = 0; y < image.GetLength(1); y++)
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
    // Image Wrtier Stuff
    // Used to generate a bunch of test triangles
    private static void CreateTestTriangles( 
        Color[,] image,
        int triangleCount)
    {
        // Log the image bounds
        Vector2Int imageBounds = new Vector2Int(
            (int) image.GetLength(0),
            (int) image.GetLength(1));

        // Init the array of triangles
        Triangle[] triangles = new Triangle[triangleCount];

        // Make the Random Object
        Random rng = new Random();

        for (int i = 0; i < triangleCount; i++)
        {
            // Generate random points for the triangle
            Vector2Int a = new Vector2Int(
                rng.Next(imageBounds._x),
                rng.Next(imageBounds._y));
            Vector2Int b = new Vector2Int(
                rng.Next(imageBounds._x),
                rng.Next(imageBounds._y));
            Vector2Int c = new Vector2Int(
                rng.Next(imageBounds._x),
                rng.Next(imageBounds._y));

            // Assign a random color
            Color color = new Color(
                rng.Next(256) / 256f,
                rng.Next(256) / 256f,
                rng.Next(256) / 256f);

            // Init the new triangle object
            triangles[i] = new Triangle(a, b, c, color);

            Console.WriteLine($"Triangle #{i}:");
            Console.WriteLine($"    A:     {a._x}, {a._y}");
            Console.WriteLine($"    B:     {b._x}, {b._y}");
            Console.WriteLine($"    C:     {c._x}, {c._y}\n");
            Console.WriteLine($"    Color: {color._red}, {color._green}, {color._blue}\n");

            AddTriangleToImage(image, triangles[i]);
        }
    }

    public static void InitRandomTriangleStates(
            Triangle[] triangles,
            Vector2Int spawnBounds,
            bool debug=false)
    {
        // Make the Random Object
        Random rng = new Random();

        for (int i = 0; i < triangles.Length; i++)
        {
            // Generate random points for the triangle between 0 and the spawn bounds
            Vector2Int a = new Vector2Int(
                rng.Next(spawnBounds._x),
                rng.Next(spawnBounds._y));
            Vector2Int b = new Vector2Int(
                rng.Next(spawnBounds._x),
                rng.Next(spawnBounds._y));
            Vector2Int c = new Vector2Int(
                rng.Next(spawnBounds._x),
                rng.Next(spawnBounds._y));

            // Assign a random color
            Color color = new Color(
                rng.Next(256) / 256f,
                rng.Next(256) / 256f,
                rng.Next(256) / 256f);

            // Generate a random velocity Vecotr2Int
            Vector2Int velocity = new Vector2Int(
                rng.Next(-10, 10), 
                rng.Next(-10, 10));

            // Init the new triangle object
            triangles[i] = new Triangle(a, b, c, color, velocity);

            if (debug)
            {
                Console.WriteLine($"Triangle #{i}:");
                Console.WriteLine($"    A:     {a._x}, {a._y}");
                Console.WriteLine($"    B:     {b._x}, {b._y}");
                Console.WriteLine($"    C:     {c._x}, {c._y}\n");
                Console.WriteLine($"    Color: {color._red}, {color._green}, {color._blue}\n");
            }
        }
    }

    public static void PropogateObjectsToNextFrame(Triangle[] triangles)
    {
    }

    
    // Used to add a triangle to the scene
    private static void AddTriangleToImage(
        Color[,] image,
        Triangle triangle)
    {
        Vector2Int A = triangle._points[0];
        Vector2Int B = triangle._points[1];
        Vector2Int C = triangle._points[2];
        Color color = triangle._color;

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

    // --------------------------------------------------------------------- //
    // Utilites
    public static class UsefulMath
    {
        public static int Power4(int value, bool debug = false)
        {
            int power = value;
            while (power % 4 != 0)
            {
                power++; 
            }

            if (debug)
            {
                Console.WriteLine($"Value: {value} | Power: {power}");
            }
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
