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
        _x = 0;
        _y = 0;
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

        // Iterate down the rows
        for (int row = 0; row < dimmensions._y; row++)
        {
            // Iterate across the cols
            for (int col = 0; col < dimmensions._x; col++)
            {
                float red = 1;
                float green = 1;
                image[col, row] = new Color(red, green, 0);
            }
        }

        // Write the image file
        WriteImageToFile(image, "test");
    }
    
    public static void WriteImageToFile(Color[,] image, string filename)
    {
    }
}
