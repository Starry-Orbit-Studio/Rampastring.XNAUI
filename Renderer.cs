using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.IO;
using System.Text;

using FontStashSharp;
using System.Linq;

namespace Rampastring.XNAUI
{
    public struct SpriteBatchSettings
    {
        public SpriteBatchSettings(SpriteSortMode ssm, BlendState bs, SamplerState ss)
        {
            SpriteSortMode = ssm;
            BlendState = bs;
            SamplerState = ss;
        }

        public SpriteSortMode SpriteSortMode { get; }
        public SamplerState SamplerState { get; }
        public BlendState BlendState { get; }
    }

    /// <summary>
    /// Provides static methods for drawing.
    /// </summary>
    public static class Renderer
    {
        private static SpriteBatch spriteBatch;

        private static Texture2D whitePixelTexture;

        private static readonly LinkedList<SpriteBatchSettings> settingStack = new LinkedList<SpriteBatchSettings>();

        internal static SpriteBatchSettings CurrentSettings;

        public static void Initialize(GraphicsDevice gd, ContentManager content, string contentPath)
        {
            spriteBatch = new SpriteBatch(gd);
            AssetLoader.FontManager.Initialize(Path.Combine(contentPath, "Fonts"));

            whitePixelTexture = AssetLoader.CreateTexture(Color.White, 1, 1);
        }

        [Obsolete]
        public static string DefaultFont => AssetLoader.FontManager.DefaultFontName;

        public static string GetStringWithLimitedWidth(string str, SpriteFontBase spriteFont, int maxWidth)
        {
            var sb = new StringBuilder(str);

            while (spriteFont.MeasureString(sb.ToString()).X > maxWidth)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }
        public static TextParseReturnValue FixText(string text, SpriteFontBase spriteFont, int width)
        {
            return TextParseReturnValue.FixText(spriteFont, width, text);
        }

        public static List<string> GetFixedTextLines(string text, SpriteFontBase spriteFont, int width, bool splitWords = true)
        {
            return TextParseReturnValue.GetFixedTextLines(spriteFont, width, text, splitWords);
        }

        /// <summary>
        /// Pushes new settings into the renderer's internal stack and applies them.
        /// A call to <see cref="PushSettings(SpriteBatchSettings)"/> should always
        /// be followed by <see cref="PopSettings"/> once drawing with the new settings is done.
        /// </summary>
        /// <param name="settings">The sprite batch settings.</param>
        public static void PushSettings(SpriteBatchSettings settings)
        {
            EndDraw();
            PushSettingsInternal();
            CurrentSettings = settings;
            BeginDrawInternal(CurrentSettings);
        }

        /// <summary>
        /// Pops previous settings from the renderer's internal stack and applies them.
        /// </summary>
        public static void PopSettings()
        {
            EndDraw();
            PopSettingsInternal();
            BeginDrawInternal(CurrentSettings);
        }

        /// <summary>
        /// Changes current rendering settings. This can be called between 
        /// <see cref="PushSettings(SpriteBatchSettings)"/> and <see cref="PopSettings"/>
        /// when you want to draw something with new settings, but there's no reason 
        /// to save those settings.
        /// </summary>
        /// <param name="settings">The sprite batch settings.</param>
        public static void ChangeSettings(SpriteBatchSettings settings)
        {
            EndDraw();
            CurrentSettings = settings;
            BeginDrawInternal(CurrentSettings);
        }

        /// <summary>
        /// Prepares the renderer for drawing a batch of sprites.
        /// </summary>
        public static void BeginDraw()
        {
            BeginDrawInternal(CurrentSettings);
        }

        /// <summary>
        /// Draws the currently queued batch of sprites.
        /// </summary>
        public static void EndDraw()
        {
            spriteBatch.End();
        }

        public static void PushRenderTarget(RenderTarget2D renderTarget) => RenderTargetStack.PushRenderTarget(renderTarget);

        public static void PopRenderTarget() => RenderTargetStack.PopRenderTarget();

        //BlendState blendState = new BlendState();
        //blendState.AlphaDestinationBlend = Blend.One;
        //blendState.ColorDestinationBlend = Blend.InverseSourceAlpha;
        //blendState.AlphaSourceBlend = Blend.SourceAlpha;
        //blendState.ColorSourceBlend = Blend.SourceAlpha;

        internal static void BeginDrawInternal(SpriteBatchSettings settings) =>
            BeginDrawInternal(settings.SpriteSortMode, settings.BlendState, settings.SamplerState);

        internal static void BeginDrawInternal(SpriteSortMode ssm, BlendState bs, SamplerState ss)
        {
#if XNA
            spriteBatch.Begin(ssm, bs, ss, DepthStencilState.Default, RasterizerState.CullNone);
#else
            spriteBatch.Begin(ssm, bs, ss,
                DepthStencilState.None, RasterizerState.CullCounterClockwise);
#endif
        }

        internal static void PushSettingsInternal()
        {
            settingStack.AddFirst(CurrentSettings);
        }

        internal static void PopSettingsInternal()
        {
            CurrentSettings = settingStack.First.Value;
            settingStack.RemoveFirst();
        }

        internal static void ClearStack()
        {
            settingStack.Clear();
        }

        #region Rendering code

        public static void DrawTexture(Texture2D texture, Rectangle rectangle, Color color)
        {
            spriteBatch.Draw(texture, rectangle, color);
        }

        public static void DrawTexture(Texture2D texture, Rectangle sourceRectangle, Rectangle destinationRectangle, Color color)
        {
            spriteBatch.Draw(texture, destinationRectangle, sourceRectangle, color);
        }

        public static void DrawTexture(Texture2D texture, Rectangle sourceRectangle, Vector2 location, float rotation, Vector2 origin, Vector2 scale, Color color)
        {
#if !XNA
            spriteBatch.Draw(texture, location, sourceRectangle, color, rotation, origin, scale, SpriteEffects.None, 0f);
#else
            spriteBatch.Draw(texture, location, sourceRectangle, color, rotation, origin, scale, SpriteEffects.None, 0f);
#endif
        }

        public static void DrawTexture(Texture2D texture, Vector2 location, float rotation, Vector2 origin, Vector2 scale, Color color)
        {
#if !XNA
            spriteBatch.Draw(texture, location, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
#else
            spriteBatch.Draw(texture, location, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
#endif
        }

        /// <summary>
        /// Draws a circle's perimiter.
        /// </summary>
        /// <param name="position">The center point of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="color">The color of the circle.</param>
        /// <param name="precision">Defines how smooth the circle's perimiter is. 
        /// Larger values make the circle smoother, but have a larger effect on performance.</param>
        /// <param name="thickness">The thickness of the perimiter.</param>
        public static void DrawCircle(Vector2 position, float radius, Color color, int precision = 8, int thickness = 1)
        {
            float angle = 0f;
            float increase = (float)Math.PI * 2f / precision;

            Vector2 point = position + RMath.VectorFromLengthAndAngle(radius, angle);

            for (int i = 0; i <= precision; i++)
            {
                Vector2 nextPoint = position + RMath.VectorFromLengthAndAngle(radius, angle);
                DrawLine(point, nextPoint, color, thickness);
                point = nextPoint;
                angle += increase;
            }
        }

        /// <summary>
        /// Draws a circle where the circle's perimeter is dotted with a texture.
        /// </summary>
        /// <param name="position">The center point of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="texture">The texture to dot the circle's perimiter with.</param>
        /// <param name="color">The remap color of the texture.</param>
        /// <param name="precision">How many times the texture is drawn on the perimiter.</param>
        /// <param name="scale">The scale of the drawn texture compared to the size of the texture itself.</param>
        public static void DrawCircleWithTexture(Vector2 position, float radius,
            Texture2D texture, Color color, int precision = 8, float scale = 1f)
        {
            float angle = 0f;
            float increase = (float)Math.PI * 2f / precision;

            Vector2 point = position + RMath.VectorFromLengthAndAngle(radius, angle);

            for (int i = 0; i <= precision; i++)
            {
                DrawTexture(texture, point, 0f,
                    new Vector2(texture.Width / 2f, texture.Height / 2f),
                    new Vector2(scale, scale), color);
                point = position + RMath.VectorFromLengthAndAngle(radius, angle);
                angle += increase;
            }
        }

        public static void DrawString(string text, SpriteFontBase font, Vector2 location, Color color, float scale = 1)
        {
            spriteBatch.DrawString(font, text, location, color, new Vector2(scale), 0f, Vector2.Zero, 0f);
        }
        public static void DrawStringWithShadow(string text, SpriteFontBase font, Vector2 location, Color color, float scale = 1, float shadowDistance = 1.0f)
        {
#if XNA
            spriteBatch.DrawString(font, text,
                new Vector2(location.X + shadowDistance, location.Y + shadowDistance),
                new Color(0, 0, 0, color.A));
#else
            spriteBatch.DrawString(font, text,
                new Vector2(location.X + shadowDistance, location.Y + shadowDistance),
                new Color((byte)0, (byte)0, (byte)0, color.A),
                new Vector2(scale), 0f,
                Vector2.Zero, 0f);
#endif

            spriteBatch.DrawString(font, text, location, color, new Vector2(scale), 0f, Vector2.Zero, 0f);
        }

        public static void DrawRectangle(Rectangle rect, Color color, int thickness = 1)
        {
            spriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y + thickness, thickness, rect.Height - thickness), color);
            spriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(whitePixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        }

        public static void FillRectangle(Rectangle rect, Color color)
        {
            spriteBatch.Draw(whitePixelTexture, rect, color);
        }
        public static Vector2 GetTextDimensions(string text, SpriteFontBase spriteFont)
        {
            return spriteFont.MeasureString(text);
        }

        public static void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1)
        {
            Vector2 line = end - start;
            if (thickness > 1)
            {
                Vector2 offset = RMath.VectorFromLengthAndAngle(thickness / 2, RMath.AngleFromVector(line) - (float)Math.PI / 2.0f);
                end += offset;
                start += offset;
            }
            spriteBatch.Draw(whitePixelTexture,
                new Rectangle((int)start.X, (int)start.Y, (int)line.Length(), thickness),
                null, color, (float)Math.Atan2(line.Y, line.X), new Vector2(0, 0), SpriteEffects.None, 0f);
        }

        #endregion
    }
}
