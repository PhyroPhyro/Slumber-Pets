using Godot;
using System;
using System.Collections.Generic;

public static class PetSpriteGenerator
{
    private const int SpriteSize = 32;
    private const int Scale = 2;
    private const int RenderSize = SpriteSize * Scale;

    public static Texture2D GenerateSprite(PetSpriteState state)
    {
        var image = Image.CreateEmpty(RenderSize, RenderSize, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        var bodyColor = GetBodyColor(state);
        var outlineColor = GetOutlineColor(state);
        var eyeColor = GetEyeColor(state);
        var accentColor = GetAccentColor(state);

        DrawPetBody(image, bodyColor, outlineColor, state);
        DrawPetEyes(image, eyeColor, state);
        DrawPetMouth(image, outlineColor, state);
        DrawPetAccessories(image, accentColor, state);

        return ImageTexture.CreateFromImage(image);
    }

    private static Color GetBodyColor(PetSpriteState state)
    {
        switch (state)
        {
            case PetSpriteState.Idle: return new Color(0.7f, 0.7f, 0.85f);
            case PetSpriteState.Happy: return new Color(0.95f, 0.85f, 0.4f);
            case PetSpriteState.Sad: return new Color(0.5f, 0.5f, 0.7f);
            case PetSpriteState.Sleeping: return new Color(0.35f, 0.35f, 0.55f);
            case PetSpriteState.Eating: return new Color(0.85f, 0.7f, 0.4f);
            case PetSpriteState.Playing: return new Color(0.95f, 0.6f, 0.35f);
            case PetSpriteState.Walking: return new Color(0.6f, 0.85f, 0.6f);
            case PetSpriteState.Traveling: return new Color(0.75f, 0.5f, 0.75f);
            default: return Colors.White;
        }
    }

    private static Color GetOutlineColor(PetSpriteState state)
    {
        var body = GetBodyColor(state);
        return new Color(body.R * 0.6f, body.G * 0.6f, body.B * 0.6f);
    }

    private static Color GetEyeColor(PetSpriteState state)
    {
        switch (state)
        {
            case PetSpriteState.Sleeping: return new Color(0.2f, 0.2f, 0.3f);
            case PetSpriteState.Happy: return new Color(0.2f, 0.2f, 0.2f);
            default: return new Color(0.15f, 0.15f, 0.15f);
        }
    }

    private static Color GetAccentColor(PetSpriteState state)
    {
        switch (state)
        {
            case PetSpriteState.Eating: return new Color(0.9f, 0.3f, 0.3f);
            case PetSpriteState.Playing: return new Color(0.3f, 0.8f, 0.9f);
            case PetSpriteState.Traveling: return new Color(0.9f, 0.9f, 0.3f);
            default: return GetBodyColor(state);
        }
    }

    private static void DrawPetBody(Image image, Color bodyColor, Color outlineColor, PetSpriteState state)
    {
        int centerX = RenderSize / 2;
        int centerY = RenderSize / 2;
        int radius = 10;

        int yOffset = 0;
        if (state == PetSpriteState.Sleeping) yOffset = 2;
        if (state == PetSpriteState.Walking) yOffset = -1;

        for (int y = 0; y < RenderSize; y++)
        {
            for (int x = 0; x < RenderSize; x++)
            {
                int dx = x - centerX;
                int dy = (y - centerY) + yOffset;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= radius)
                {
                    if (dist > radius - 1.5f)
                    {
                        image.SetPixel(x, y, outlineColor);
                    }
                    else
                    {
                        float shade = 1.0f - (dist / radius) * 0.2f;
                        image.SetPixel(x, y, new Color(bodyColor.R * shade, bodyColor.G * shade, bodyColor.B * shade));
                    }
                }
            }
        }

        DrawEars(image, outlineColor, bodyColor, state);
    }

    private static void DrawEars(Image image, Color outlineColor, Color bodyColor, PetSpriteState state)
    {
        int centerX = RenderSize / 2;
        int topY = RenderSize / 2 - 10;

        if (state == PetSpriteState.Sleeping)
        {
            DrawFilledCircle(image, centerX - 8, topY - 2, 4, outlineColor, bodyColor);
            DrawFilledCircle(image, centerX + 8, topY - 2, 4, outlineColor, bodyColor);
        }
        else if (state == PetSpriteState.Happy || state == PetSpriteState.Playing)
        {
            DrawTriangle(image, centerX - 10, topY - 6, centerX - 14, topY + 4, centerX - 6, topY + 4, outlineColor, bodyColor);
            DrawTriangle(image, centerX + 10, topY - 6, centerX + 14, topY + 4, centerX + 6, topY + 4, outlineColor, bodyColor);
        }
        else
        {
            DrawFilledCircle(image, centerX - 9, topY - 3, 5, outlineColor, bodyColor);
            DrawFilledCircle(image, centerX + 9, topY - 3, 5, outlineColor, bodyColor);
        }
    }

    private static void DrawPetEyes(Image image, Color eyeColor, PetSpriteState state)
    {
        int centerX = RenderSize / 2;
        int eyeY = RenderSize / 2 - 3;

        if (state == PetSpriteState.Sleeping)
        {
            DrawClosedEye(image, centerX - 5, eyeY, eyeColor);
            DrawClosedEye(image, centerX + 5, eyeY, eyeColor);
        }
        else if (state == PetSpriteState.Happy)
        {
            DrawHappyEye(image, centerX - 5, eyeY, eyeColor);
            DrawHappyEye(image, centerX + 5, eyeY, eyeColor);
        }
        else if (state == PetSpriteState.Sad)
        {
            DrawSadEye(image, centerX - 5, eyeY, eyeColor, true);
            DrawSadEye(image, centerX + 5, eyeY, eyeColor, false);
        }
        else
        {
            DrawFilledCircle(image, centerX - 5, eyeY, 2, eyeColor, eyeColor);
            DrawFilledCircle(image, centerX + 5, eyeY, 2, eyeColor, eyeColor);
            image.SetPixel(centerX - 4, eyeY - 1, Colors.White);
            image.SetPixel(centerX + 6, eyeY - 1, Colors.White);
        }
    }

    private static void DrawPetMouth(Image image, Color outlineColor, PetSpriteState state)
    {
        int centerX = RenderSize / 2;
        int mouthY = RenderSize / 2 + 4;

        if (state == PetSpriteState.Happy || state == PetSpriteState.Playing)
        {
            DrawSmile(image, centerX, mouthY, outlineColor);
        }
        else if (state == PetSpriteState.Sad)
        {
            DrawFrown(image, centerX, mouthY + 2, outlineColor);
        }
        else if (state == PetSpriteState.Eating)
        {
            DrawOpenMouth(image, centerX, mouthY, outlineColor);
        }
        else if (state == PetSpriteState.Sleeping)
        {
            DrawSleepingMouth(image, centerX, mouthY, outlineColor);
        }
        else
        {
            DrawNeutralMouth(image, centerX, mouthY, outlineColor);
        }
    }

    private static void DrawPetAccessories(Image image, Color accentColor, PetSpriteState state)
    {
        int centerX = RenderSize / 2;

        if (state == PetSpriteState.Eating)
        {
            DrawFoodBowl(image, centerX + 12, RenderSize / 2 + 8, accentColor);
        }
        else if (state == PetSpriteState.Playing)
        {
            DrawBall(image, centerX - 14, RenderSize / 2 + 10, accentColor);
        }
        else if (state == PetSpriteState.Traveling)
        {
            DrawSuitcase(image, centerX, RenderSize / 2 + 12, accentColor);
        }
        else if (state == PetSpriteState.Walking)
        {
            DrawFootprints(image, accentColor);
        }
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

    private static void DrawTriangle(Image image, int x1, int y1, int x2, int y2, int x3, int y3, Color outlineColor, Color fillColor)
    {
        int minY = Mathf.Min(y1, Mathf.Min(y2, y3));
        int maxY = Mathf.Max(y1, Mathf.Max(y2, y3));
        int minX = Mathf.Min(x1, Mathf.Min(x2, x3));
        int maxX = Mathf.Max(x1, Mathf.Max(x2, x3));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (x < 0 || x >= RenderSize || y < 0 || y >= RenderSize) continue;
                if (PointInTriangle(x, y, x1, y1, x2, y2, x3, y3))
                {
                    image.SetPixel(x, y, fillColor);
                }
            }
        }
    }

    private static bool PointInTriangle(int px, int py, int x1, int y1, int x2, int y2, int x3, int y3)
    {
        float d1 = Sign(px, py, x1, y1, x2, y2);
        float d2 = Sign(px, py, x2, y2, x3, y3);
        float d3 = Sign(px, py, x3, y3, x1, y1);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private static float Sign(int px, int py, int x1, int y1, int x2, int y2)
    {
        return (px - x2) * (y1 - y2) - (x1 - x2) * (py - y2);
    }

    private static void DrawClosedEye(Image image, int cx, int cy, Color color)
    {
        image.SetPixel(cx - 1, cy, color);
        image.SetPixel(cx, cy + 1, color);
        image.SetPixel(cx + 1, cy, color);
    }

    private static void DrawHappyEye(Image image, int cx, int cy, Color color)
    {
        image.SetPixel(cx - 1, cy, color);
        image.SetPixel(cx, cy - 1, color);
        image.SetPixel(cx + 1, cy, color);
    }

    private static void DrawSadEye(Image image, int cx, int cy, Color color, bool isLeft)
    {
        image.SetPixel(cx, cy, color);
        if (isLeft)
        {
            image.SetPixel(cx - 1, cy - 1, color);
        }
        else
        {
            image.SetPixel(cx + 1, cy - 1, color);
        }
    }

    private static void DrawSmile(Image image, int cx, int cy, Color color)
    {
        image.SetPixel(cx - 3, cy - 1, color);
        image.SetPixel(cx - 2, cy + 1, color);
        image.SetPixel(cx - 1, cy + 2, color);
        image.SetPixel(cx, cy + 2, color);
        image.SetPixel(cx + 1, cy + 2, color);
        image.SetPixel(cx + 2, cy + 1, color);
        image.SetPixel(cx + 3, cy - 1, color);
    }

    private static void DrawFrown(Image image, int cx, int cy, Color color)
    {
        image.SetPixel(cx - 3, cy + 1, color);
        image.SetPixel(cx - 2, cy - 1, color);
        image.SetPixel(cx - 1, cy - 2, color);
        image.SetPixel(cx, cy - 2, color);
        image.SetPixel(cx + 1, cy - 2, color);
        image.SetPixel(cx + 2, cy - 1, color);
        image.SetPixel(cx + 3, cy + 1, color);
    }

    private static void DrawOpenMouth(Image image, int cx, int cy, Color color)
    {
        image.SetPixel(cx - 2, cy, color);
        image.SetPixel(cx - 1, cy, color);
        image.SetPixel(cx, cy, color);
        image.SetPixel(cx + 1, cy, color);
        image.SetPixel(cx + 2, cy, color);
        image.SetPixel(cx - 1, cy + 1, color);
        image.SetPixel(cx, cy + 1, color);
        image.SetPixel(cx + 1, cy + 1, color);
    }

    private static void DrawSleepingMouth(Image image, int cx, int cy, Color color)
    {
        image.SetPixel(cx - 1, cy, color);
        image.SetPixel(cx, cy, color);
        image.SetPixel(cx + 1, cy, color);
    }

    private static void DrawNeutralMouth(Image image, int cx, int cy, Color color)
    {
        image.SetPixel(cx - 2, cy, color);
        image.SetPixel(cx - 1, cy, color);
        image.SetPixel(cx, cy, color);
        image.SetPixel(cx + 1, cy, color);
        image.SetPixel(cx + 2, cy, color);
    }

    private static void DrawFoodBowl(Image image, int cx, int cy, Color color)
    {
        for (int x = cx - 4; x <= cx + 4; x++)
        {
            if (x >= 0 && x < RenderSize && cy >= 0 && cy < RenderSize)
                image.SetPixel(x, cy, color);
            if (x >= 0 && x < RenderSize && cy + 1 >= 0 && cy + 1 < RenderSize)
                image.SetPixel(x, cy + 1, color);
        }
        image.SetPixel(cx - 4, cy - 1, color);
        image.SetPixel(cx + 4, cy - 1, color);
    }

    private static void DrawBall(Image image, int cx, int cy, Color color)
    {
        DrawFilledCircle(image, cx, cy, 4, color, new Color(color.R * 0.8f, color.G * 0.8f, color.B * 0.8f));
        image.SetPixel(cx - 1, cy - 1, Colors.White);
    }

    private static void DrawSuitcase(Image image, int cx, int cy, Color color)
    {
        for (int y = cy - 3; y <= cy + 3; y++)
        {
            for (int x = cx - 6; x <= cx + 6; x++)
            {
                if (x >= 0 && x < RenderSize && y >= 0 && y < RenderSize)
                {
                    if (x == cx - 6 || x == cx + 6 || y == cy - 3 || y == cy + 3)
                        image.SetPixel(x, y, new Color(color.R * 0.7f, color.G * 0.7f, color.B * 0.7f));
                    else
                        image.SetPixel(x, y, color);
                }
            }
        }
        image.SetPixel(cx, cy - 4, color);
        image.SetPixel(cx, cy - 5, color);
    }

    private static void DrawFootprints(Image image, Color color)
    {
        DrawFilledCircle(image, 8, RenderSize - 6, 2, color, color);
        DrawFilledCircle(image, RenderSize - 8, RenderSize - 10, 2, color, color);
    }
}
