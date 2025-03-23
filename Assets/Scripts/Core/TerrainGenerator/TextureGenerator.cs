using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point; // Remove blurring (Bilinear by default)
        texture.wrapMode = TextureWrapMode.Clamp; // Allow texture to go right to the edge
        texture.SetPixels(colourMap);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap, float heightMultiplier = 1)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Get colour that corresponds to current point on heightMap, where black = 0 and white = 1
                float scaledHeight = heightMap[x, y] * heightMultiplier;
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, Mathf.Clamp01(scaledHeight));
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }

    public static Texture2D TextureFromColoredHeightMap(float[,] heightMap, float maxHeight)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Create a color gradient based on height
                float heightPercent = heightMap[x, y] * maxHeight;
                colourMap[y * width + x] = GetHeightColor(heightPercent);
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }

    private static Color GetHeightColor(float height)
    {
        // Deep blue for water, green for grass, brown for mountains, white for snow peaks
        Color deepBlue = new Color(0.0f, 0.0f, 0.5f);
        Color shallowBlue = new Color(0.0f, 0.5f, 1.0f);
        Color sand = new Color(0.8f, 0.8f, 0.2f);
        Color grass = new Color(0.1f, 0.6f, 0.1f);
        Color rock = new Color(0.5f, 0.4f, 0.3f);
        Color snow = Color.white;

        if (height < 0.3f)
            return Color.Lerp(deepBlue, shallowBlue, height / 0.3f);
        else if (height < 0.4f)
            return Color.Lerp(shallowBlue, sand, (height - 0.3f) / 0.1f);
        else if (height < 0.7f)
            return Color.Lerp(grass, rock, (height - 0.4f) / 0.3f);
        else
            return Color.Lerp(rock, snow, (height - 0.7f) / 0.3f);
    }
}
