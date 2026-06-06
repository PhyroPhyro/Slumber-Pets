using Godot;
using System;
using System.Collections.Generic;

public static class ItemSpriteGenerator
{
    private const int SpriteSize = 24;
    private const int RenderSize = SpriteSize * 2;

    private static Dictionary<string, Color> _rarityColors = new()
    {
        { "common", new Color(0.7f, 0.7f, 0.7f) },
        { "common+", new Color(0.5f, 0.8f, 0.5f) },
        { "uncommon", new Color(0.4f, 0.7f, 0.9f) },
        { "rare", new Color(0.8f, 0.5f, 0.9f) },
        { "rare+", new Color(0.95f, 0.7f, 0.2f) }
    };

    private static Dictionary<string, Color> _typeColors = new()
    {
        { "food", new Color(0.9f, 0.6f, 0.3f) },
        { "drink", new Color(0.3f, 0.6f, 0.9f) },
        { "toy", new Color(0.9f, 0.4f, 0.4f) },
        { "potion", new Color(0.6f, 0.9f, 0.4f) },
        { "charm", new Color(0.9f, 0.8f, 0.3f) },
        { "crystal", new Color(0.7f, 0.5f, 0.9f) },
        { "treat", new Color(0.95f, 0.5f, 0.6f) },
        { "totem", new Color(0.6f, 0.8f, 0.6f) },
        { "equipment", new Color(0.7f, 0.6f, 0.5f) },
        { "exclusive", new Color(0.9f, 0.7f, 0.8f) }
    };

    public static Texture2D GenerateSprite(string itemId, string type, string rarity)
    {
        var image = Image.CreateEmpty(RenderSize, RenderSize, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        Color baseColor = GetTypeColor(type);
        Color rarityOverlay = GetRarityColor(rarity);
        Color outlineColor = new Color(baseColor.R * 0.5f, baseColor.G * 0.5f, baseColor.B * 0.5f);

        DrawItemShape(image, itemId, type, baseColor, outlineColor);
        ApplyRarityOverlay(image, rarityOverlay);
        DrawHighlight(image, baseColor);

        return ImageTexture.CreateFromImage(image);
    }

    private static Color GetTypeColor(string type)
    {
        return _typeColors.TryGetValue(type, out Color color) ? color : Colors.White;
    }

    private static Color GetRarityColor(string rarity)
    {
        return _rarityColors.TryGetValue(rarity, out Color color) ? color : Colors.Transparent;
    }

    private static void DrawItemShape(Image image, string itemId, string type, Color baseColor, Color outlineColor)
    {
        switch (type)
        {
            case "food":
                DrawFoodShape(image, baseColor, outlineColor);
                break;
            case "drink":
                DrawDrinkShape(image, baseColor, outlineColor);
                break;
            case "toy":
                DrawToyShape(image, baseColor, outlineColor);
                break;
            case "potion":
                DrawPotionShape(image, baseColor, outlineColor);
                break;
            case "charm":
                DrawCharmShape(image, baseColor, outlineColor);
                break;
            case "crystal":
                DrawCrystalShape(image, baseColor, outlineColor);
                break;
            case "treat":
                DrawTreatShape(image, baseColor, outlineColor);
                break;
            case "totem":
                DrawTotemShape(image, baseColor, outlineColor);
                break;
            case "equipment":
                DrawEquipmentShape(image, baseColor, outlineColor);
                break;
            case "exclusive":
                DrawExclusiveShape(image, baseColor, outlineColor);
                break;
            default:
                DrawDefaultShape(image, baseColor, outlineColor);
                break;
        }
    }

    private static void DrawFoodShape(Image image, Color baseColor, Color outlineColor)
    {
        int cx = RenderSize / 2;
        int cy = RenderSize / 2 + 2;
        int radius = 8;

        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= RenderSize || y < 0 || y >= RenderSize) continue;
                int dx = x - cx;
                int dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= radius)
                {
                    if (dist > radius - 1.5f)
                        image.SetPixel(x, y, outlineColor);
                    else
                    {
                        float shade = 1.0f - (dist / radius) * 0.25f;
                        image.SetPixel(x, y, new Color(baseColor.R * shade, baseColor.G * shade, baseColor.B * shade));
                    }
                }
            }
        }

        image.SetPixel(cx - 3, cy - 2, Colors.White);
        image.SetPixel(cx + 3, cy - 2, Colors.White);
    }

    private static void DrawDrinkShape(Image image, Color baseColor, Color outlineColor)
    {
        int cx = RenderSize / 2;
        int cy = RenderSize / 2;

        for (int y = cy - 8; y <= cy + 8; y++)
        {
            for (int x = cx - 5; x <= cx + 5; x++)
            {
                if (x < 0 || x >= RenderSize || y < 0 || y >= RenderSize) continue;
                bool isEdge = x == cx - 5 || x == cx + 5 || y == cy - 8 || y == cy + 8;
                image.SetPixel(x, y, isEdge ? outlineColor : baseColor);
            }
        }

        for (int y = cy - 12; y <= cy - 8; y++)
        {
            if (y >= 0 && y < RenderSize)
            {
                image.SetPixel(cx - 2, y, outlineColor);
                image.SetPixel(cx + 2, y, outlineColor);
            }
        }

        image.SetPixel(cx, cy - 4, Colors.White);
    }

    private static void DrawToyShape(Image image, Color baseColor, Color outlineColor)
    {
        int cx = RenderSize / 2;
        int cy = RenderSize / 2;

        DrawFilledCircle(image, cx - 4, cy, 5, outlineColor, baseColor);
        DrawFilledCircle(image, cx + 4, cy, 5, outlineColor, baseColor);

        image.SetPixel(cx, cy, outlineColor);
    }

    private static void DrawPotionShape(Image image, Color baseColor, Color outlineColor)
    {
        int cx = RenderSize / 2;
        int cy = RenderSize / 2 + 2;

        for (int y = cy - 6; y <= cy + 8; y++)
        {
            for (int x = cx - 6; x <= cx + 6; x++)
            {
                if (x < 0 || x >= RenderSize || y < 0 || y >= RenderSize) continue;
                float progress = (float)(y - (cy - 6)) / 14f;
                int width = (int)(6 * (1 - progress * 0.3f));
                if (Mathf.Abs(x - cx) <= width)
                {
                    bool isEdge = Mathf.Abs(x - cx) == width || y == cy - 6 || y == cy + 8;
                    image.SetPixel(x, y, isEdge ? outlineColor : baseColor);
                }
            }
        }

        for (int y = cy - 10; y <= cy - 6; y++)
        {
            if (y >= 0 && y < RenderSize)
            {
                image.SetPixel(cx - 2, y, outlineColor);
                image.SetPixel(cx + 2, y, outlineColor);
                image.SetPixel(cx - 1, y, baseColor);
                image.SetPixel(cx, y, baseColor);
                image.SetPixel(cx + 1, y, baseColor);
            }
        }

        image.SetPixel(cx - 2, cy + 2, Colors.White);
    }

    private static void DrawCharmShape(Image image, Color baseColor, Color outlineColor)
    {
        int cx = RenderSize / 2;
        int cy = RenderSize / 2;

        for (int i = 0; i < 5; i++)
        {
            float angle = (float)(i * 2 * Math.PI / 5) - (float)Math.PI / 2;
            int x = cx + (int)(8 * Math.Cos(angle));
            int y = cy + (int)(8 * Math.Sin(angle));
            DrawFilledCircle(image, x, y, 2, outlineColor, baseColor);
        }

        DrawFilledCircle(image, cx, cy, 3, outlineColor, new Color(baseColor.R * 1.2f, baseColor.G * 1.2f, baseColor.B * 1.2f));
    }

    private static void DrawCrystalShape(Image image, Color baseColor, Color outlineColor)
    {
        int cx = RenderSize / 2;
        int cy = RenderSize / 2;

        int[] pointsX = { cx, cx - 6, cx - 4, cx + 4, cx + 6, cx };
        int[] pointsY = { cy - 10, cy - 2, cy + 8, cy + 8, cy - 2, cy - 10 };

        for (int i = 0; i < pointsX.Length - 1; i++)
        {
            DrawLine(image, pointsX[i], pointsY[i], pointsX[i + 1], pointsY[i + 1], outlineColor);
        }

        FillPolygon(image, pointsX, pointsY, baseColor);

        image.SetPixel(cx - 2, cy - 4, Colors.White);
    }

    private static void DrawTreatShape(Image image, Color baseColor, Color outlineColor)
    {
        int cx = RenderSize / 2;
        int cy = RenderSize / 2;

        DrawFilledCircle(image, cx, cy, 7, outlineColor, baseColor);

        for (int i = 0; i < 3; i++)
        {
            int x = cx - 3 + i * 3;
            int y = cy - 2;
            if (x >= 0 && x < RenderSize && y >= 0 && y < RenderSize)
                image.SetPixel(x, y, Colors.White);
        }
    }

    private static void DrawTotemShape(Image image, Color baseColor, Color outlineColor)
    {
        int cx = RenderSize / 2;
        int cy = RenderSize / 2;

        for (int y = cy - 10; y <= cy + 10; y++)
        {
            for (int x = cx - 4; x <= cx + 4; x++)
            {
                if (x < 0 || x >= RenderSize || y < 0 || y >= RenderSize) continue;
                bool isEdge = x == cx - 4 || x == cx + 4 || y == cy - 10 || y == cy + 10;
                image.SetPixel(x, y, isEdge ? outlineColor : baseColor);
            }
        }

        image.SetPixel(cx - 2, cy - 4, Colors.White);
        image.SetPixel(cx + 2, cy - 4, Colors.White);
        image.SetPixel(cx, cy + 2, outlineColor);
        image.SetPixel(cx - 1, cy + 3, outlineColor);
        image.SetPixel(cx + 1, cy + 3, outlineColor);
    }

    private static void DrawEquipmentShape(Image image, Color baseColor, Color outlineColor)
    {
        int cx = RenderSize / 2;
        int cy = RenderSize / 2;

        for (int y = cy - 6; y <= cy + 6; y++)
        {
            for (int x = cx - 8; x <= cx + 8; x++)
            {
                if (x < 0 || x >= RenderSize || y < 0 || y >= RenderSize) continue;
                bool isEdge = Mathf.Abs(x - cx) == 8 || Mathf.Abs(y - cy) == 6;
                image.SetPixel(x, y, isEdge ? outlineColor : baseColor);
            }
        }

        image.SetPixel(cx - 4, cy, Colors.White);
        image.SetPixel(cx + 4, cy, Colors.White);
    }

    private static void DrawExclusiveShape(Image image, Color baseColor, Color outlineColor)
    {
        int cx = RenderSize / 2;
        int cy = RenderSize / 2;

        for (int i = 0; i < 8; i++)
        {
            float angle = (float)(i * 2 * Math.PI / 8);
            int x = cx + (int)(9 * Math.Cos(angle));
            int y = cy + (int)(9 * Math.Sin(angle));
            DrawFilledCircle(image, x, y, 2, outlineColor, baseColor);
        }

        DrawFilledCircle(image, cx, cy, 5, outlineColor, new Color(baseColor.R * 1.3f, baseColor.G * 1.3f, baseColor.B * 1.3f));

        image.SetPixel(cx - 2, cy - 2, Colors.White);
        image.SetPixel(cx + 2, cy - 2, Colors.White);
    }

    private static void DrawDefaultShape(Image image, Color baseColor, Color outlineColor)
    {
        DrawFilledCircle(image, RenderSize / 2, RenderSize / 2, 8, outlineColor, baseColor);
    }

    private static void DrawFilledCircle(Image image, int cx, int cy, int radius, Color outlineColor, Color fillColor)
    {
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= RenderSize || y < 0 || y >= RenderSize) continue;
                int dx = x - cx;
                int dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= radius)
                {
                    if (dist > radius - 1.2f)
                        image.SetPixel(x, y, outlineColor);
                    else
                        image.SetPixel(x, y, fillColor);
                }
            }
        }
    }

    private static void DrawLine(Image image, int x1, int y1, int x2, int y2, Color color)
    {
        int dx = Math.Abs(x2 - x1);
        int dy = Math.Abs(y2 - y1);
        int sx = x1 < x2 ? 1 : -1;
        int sy = y1 < y2 ? 1 : -1;
        int err = dx - dy;

        int x = x1, y = y1;
        while (true)
        {
            if (x >= 0 && x < RenderSize && y >= 0 && y < RenderSize)
                image.SetPixel(x, y, color);
            if (x == x2 && y == y2) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x += sx; }
            if (e2 < dx) { err += dx; y += sy; }
        }
    }

    private static void FillPolygon(Image image, int[] pointsX, int[] pointsY, Color color)
    {
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (int y in pointsY) { minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }

        for (int y = minY; y <= maxY; y++)
        {
            List<int> intersections = new();
            for (int i = 0; i < pointsX.Length - 1; i++)
            {
                int x1 = pointsX[i], y1 = pointsY[i];
                int x2 = pointsX[i + 1], y2 = pointsY[i + 1];
                if ((y1 <= y && y2 > y) || (y2 <= y && y1 > y))
                {
                    int x = x1 + (y - y1) * (x2 - x1) / (y2 - y1);
                    intersections.Add(x);
                }
            }
            intersections.Sort();
            for (int i = 0; i < intersections.Count - 1; i += 2)
            {
                for (int x = intersections[i]; x < intersections[i + 1]; x++)
                {
                    if (x >= 0 && x < RenderSize && y >= 0 && y < RenderSize)
                        image.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void ApplyRarityOverlay(Image image, Color rarityColor)
    {
        if (rarityColor == Colors.Transparent) return;
        for (int y = 0; y < RenderSize; y++)
        {
            for (int x = 0; x < RenderSize; x++)
            {
                Color pixel = image.GetPixel(x, y);
                if (pixel.A > 0.01f)
                {
                    float blend = 0.15f;
                    image.SetPixel(x, y, new Color(
                        pixel.R * (1 - blend) + rarityColor.R * blend,
                        pixel.G * (1 - blend) + rarityColor.G * blend,
                        pixel.B * (1 - blend) + rarityColor.B * blend,
                        pixel.A
                    ));
                }
            }
        }
    }

    private static void DrawHighlight(Image image, Color baseColor)
    {
        int cx = RenderSize / 2;
        int cy = RenderSize / 2 - 3;
        image.SetPixel(cx - 1, cy, new Color(1, 1, 1, 0.6f));
        image.SetPixel(cx, cy - 1, new Color(1, 1, 1, 0.4f));
    }
}
