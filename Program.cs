﻿using System.Diagnostics;
using MyUtils;

namespace App_01;

// ----------------------------------------------------------------------------
// Main Program Class
class Program
{
    static int Main(string[] args)
    {
        // Clear the console window
        Console.Clear();

        // Generate the Animation
        return GenerateAnimation();
    }

    // ------------------------------------------------------------------------
    // Animation Writer Stuff
    public static int GenerateAnimation()
    {
        string ffmpegExeName = "ffmpeg";
        string outputVideoPath = "Test.mp4";
        Vector2Int resolution = new Vector2Int(1024, 512);
        int frameRate = 30;
        int duration = 30;
        int totalFrameCount = frameRate * duration;
        int triangleCount = 20;

        // Define the spawning bounds via it's offset and size
        Vector2Int spawnOffset = resolution * 0.25;
        Vector2Int spawnSize = resolution * 0.5;

        // Create the array of triangles and init them with random states (pos & velocity)
        Triangle[] triangles = new Triangle[triangleCount];
        InitRandomTriangleStates(triangles, spawnOffset, spawnSize);

        RenderFrames(resolution, totalFrameCount, frameRate, triangles, true);

        // Init the video writer
        ProcessStartInfo processInfo = new ProcessStartInfo();
        processInfo.FileName = ffmpegExeName;
        processInfo.Arguments = $"-framerate {frameRate} -i \"frames/frame-%d.bmp\" -vframes {totalFrameCount} -c:v libx264 -pix_fmt yuv420p -y \"{outputVideoPath}\"";
        processInfo.RedirectStandardOutput = true;
        processInfo.RedirectStandardError = true;
        processInfo.UseShellExecute = false;
        processInfo.CreateNoWindow = true;

        using Process? process = Process.Start(processInfo);
        if (process == null) return -1;

        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new Exception($"FFmpeg error: {error}");
        }

        // Delete the last progress message and print success
        Console.WriteLine($"Video saved to {outputVideoPath}");
        return 0;
    }

    public static void RenderFrames(
            Vector2Int resolution, 
            int totalFrameCount, 
            int frameRate,
            Triangle[] objects,
            bool debug = false)
    {
        // Store the dir we store the frames in
        string frameDir = "frames/";

        // Clear out any old frames
        DirectoryInfo dirInfo = new DirectoryInfo(frameDir);
        foreach (FileInfo fileInfo in dirInfo.GetFiles())
        {
            fileInfo.Delete();
        }

        // Create the frame object
        Color[,] frame = new Color[resolution.X, resolution.Y];

        // Useful debug variables
        int paddingSize = totalFrameCount.ToString().Length;
        int cursorTopPos = Console.CursorTop;
        int cursorLeftPos = Console.CursorLeft;

        // Loop until our count reaches the requested total frames
        for (int i = 0; i < totalFrameCount; i++)
        {
            // Clear the frame for the new data
            frame = new Color[resolution.X, resolution.Y];
            
            if (debug)
            {
                Console.SetCursorPosition(cursorLeftPos, cursorTopPos);
                Console.Write($"Renering Frame: {(i + 1).ToString($"D{paddingSize}")} / {totalFrameCount}\n");
            }

            // Iterate the scene to the next frame
            PropogateTrianglesToNextFrame(
                    objects, 
                    1 / (double) frameRate, 
                    resolution);

            // Draw the new frame
            // TODO: Create some sort of scene object
            DrawTrianglesOnFrame(frame, objects);
            
            // Write the frame to the frameDir
            WriteImageToFile(frame, frameDir + $"frame-{i}.bmp");
        }
    }

    // ------------------------------------------------------------------------
    // Image Wrtier Stuff
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
                writer.Write((byte) (pixel.Blue * 255));
                writer.Write((byte) (pixel.Green * 255));
                writer.Write((byte) (pixel.Red * 255));
            }
            writer.Write(new Byte[paddingSizeInBytes]);
        }

        // Close the file
        stream.Close();
    }

    // ------------------------------------------------------------------------
    // Triangle Stuff
    public struct Triangle
    {
        public Vector2[] _points = new Vector2[3];
        public Color _color = new Color();
        public Vector2[] _velocity = new Vector2[3];

        public Triangle(
                Vector2 a, 
                Vector2 b, 
                Vector2 c,
                Color color = new Color(),
                Vector2 velocity = new Vector2())
        {
            _points[0] = a;
            _points[1] = b;
            _points[2] = c;
            _color = color;
            _velocity[0] = velocity;
            _velocity[1] = velocity;
            _velocity[2] = velocity;
        }
    }

    public static void InitRandomTriangleStates(
            Triangle[] triangles,
            Vector2Int spawnOffset,
            Vector2Int spawnSize,
            bool debug=false)
    {
        // Make the Random Object
        Random rng = new Random();

        for (int i = 0; i < triangles.Length; i++)
        {
            // Generate random points for the triangle between 0 and the spawn bounds
            Vector2 a = new Vector2(
                rng.Next(spawnOffset.X, spawnOffset.X + spawnSize.X),
                rng.Next(spawnOffset.Y, spawnOffset.Y + spawnSize.Y));
            Vector2 b = new Vector2(
                rng.Next(spawnOffset.X, spawnOffset.X + spawnSize.X),
                rng.Next(spawnOffset.Y, spawnOffset.Y + spawnSize.Y));
            Vector2 c = new Vector2(
                rng.Next(spawnOffset.X, spawnOffset.X + spawnSize.X),
                rng.Next(spawnOffset.Y, spawnOffset.Y + spawnSize.Y));

            // Assign a random color
            Color color = new Color(
                rng.Next(256) / 256f,
                rng.Next(256) / 256f,
                rng.Next(256) / 256f);

            // Generate a random velocity Vecotr2Int
            Vector2 velocity = new Vector2(
                rng.Next(-50, 50), 
                rng.Next(-50, 50));

            // Init the new triangle object
            triangles[i] = new Triangle(a, b, c, color, velocity);

            if (debug)
            {
                Console.WriteLine($"Triangle #{i}:");
                Console.WriteLine($"    A:     {a.X}, {a.Y}");
                Console.WriteLine($"    B:     {b.X}, {b.Y}");
                Console.WriteLine($"    C:     {c.X}, {c.Y}\n");
                Console.WriteLine($"    Color: {color.Red}, {color.Green}, {color.Blue}\n");
            }
        }
    }

    // Used to add a triangle to the scene
    private static void DrawTrianglesOnFrame(
            Color[,] image,
            Triangle[] triangles)
    {
        // Loop over the triangles
        for (int i = 0; i < triangles.Length; i++)
        {
            Triangle triangle = triangles[i];

            Vector2Int A = (Vector2Int) triangle._points[0];
            Vector2Int B = (Vector2Int) triangle._points[1];
            Vector2Int C = (Vector2Int) triangle._points[2];
            Color color = triangle._color;

            // Loop over the pixels in the image
            for (int x = 0; x < image.GetLength(0); x++)
            {
                for (int y = 0; y < image.GetLength(1); y++)
                {
                    bool isInside = IsInsideTriangle(
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
    }

    public static void PropogateTrianglesToNextFrame(
            Triangle[] triangles, 
            double timeDelta,
            Vector2Int bounds)
    {
        for (int i = 0; i < triangles.Length; i++)
        {
            Triangle triangle = triangles[i];

            // Update the triangles's Position
            for (int j = 0; j < triangle._points.Length; j++)
            {
                triangle._points[j] += triangle._velocity[j] * timeDelta;

                // If Any point is outside the bounds (OOB) then reflect it's velocity
                bool isOutOfXBounds = 
                    triangle._points[j].X <= 0
                    || triangle._points[j].X >= bounds.X;
                if (isOutOfXBounds)
                {
                    triangle._velocity[j].X *= -1;
                }

                bool isOutOfYBounds = 
                    triangle._points[j].Y <= 0
                    || triangle._points[j].Y >= bounds.Y;
                if (isOutOfYBounds)
                {
                    triangle._velocity[j].Y *= -1;
                }
            }
        }
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
    
    // ------------------------------------------------------------------------
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

    }
}
