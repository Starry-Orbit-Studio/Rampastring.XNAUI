using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;

using Color = Microsoft.Xna.Framework.Color;

namespace Rampastring.XNAUI.Data
{
    public sealed class Texture2DManager
    {
        /// <summary>
        /// A list of filesystem paths that assets are attempted to load from.
        /// </summary>
        public static List<string> ImageSearchPaths;
        private static GraphicsDevice graphicsDevice;
        private static List<WeakReference<Texture2D>> textureCache;
        /// <summary>
        /// Creates a one-colored texture.
        /// </summary>
        /// <param name="color">The color of the texture.</param>
        /// <param name="width">The width of the texture in pixels.</param>
        /// <param name="height">The height of the texture in pixels.</param>
        /// <returns>A texture.</returns>
        public static Texture2D CreateTexture(Color color, int width, int height)
        {
            var key = $"{width}:{height}#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

            Logger.Debug($"Try Load \"{key}\"");

            Texture2D texture = null;

            if (textureCache.Any(x => x.TryGetTarget(out texture) && texture.Name == key))
            {
                Logger.Debug($"Find \"{key}\" from cache");
                return texture;
            }

            Logger.Debug($"Create \"{key}\"");

            texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color) { Name = key };

            Color[] colorArray = new Color[width * height];

            for (int i = 0; i < colorArray.Length; i++)
                colorArray[i] = color;

            texture.SetData(colorArray);

            textureCache.Add(new WeakReference<Texture2D>(texture));
            return texture;
        }

        /// <summary>
        /// Loads a texture with the specific name. If the texture isn't found from any
        /// asset search path, returns a dummy texture.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture if it was found and could be loaded, otherwise a dummy texture.</returns>
        public static Texture2D LoadTexture(string name)
        {
            Logger.Debug($"Try Load \"{name}\"");
            Texture2D texture = null;

            if (textureCache.Any(x => x.TryGetTarget(out texture) && texture.Name == name))
            {
                Logger.Debug($"Find \"{name}\" from cache");
                return texture;
            }

            return LoadTextureUncached(name);
        }

        /// <summary>
        /// Loads a texture with the specific name. Does not look at textures in 
        /// the texture cache, and doesn't add loaded textures to the texture cache.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture if it was found and could be loaded, otherwise a dummy texture.</returns>
        public static Texture2D LoadTextureUncached(string name)
        {
            Logger.Debug($"Try Load \"{name}\" Uncached");
            var texture = LoadTextureInternal(name);
            if (texture != null)
            {
                textureCache.Add(new WeakReference<Texture2D>(texture));
                return texture;
            }

            Logger.Debug($"WARN: Cannot Find \"{name}\"!");
            return DummyTexture;
        }

        /// <summary>
        /// Creates a texture from a <see cref="Image"/>.
        /// Returns null if creating the texture fails.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <returns>The created texture, or null if creating the texture fails.</returns>
        public static Texture2D TextureFromImage(Image image)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Seek(0, SeekOrigin.Begin);
                    Texture2D texture = Texture2D.FromStream(graphicsDevice, stream);
                    PremultiplyAlpha(texture);
                    return texture;
                }
            }
            catch (Exception ex)
            {
                Logger.Log("AssetLoader.TextureFromImage: failed to create texture! Message: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Creates and returns a 100x100 pink square.
        /// </summary>
        public static Texture2D DummyTexture { get; } = CreateTexture(new Color(255, 54, 244), 100, 100);

        private static Texture2D LoadTextureInternal(string name)
        {
            try
            {
                foreach (string searchPath in ImageSearchPaths)
                {
                    //Logger.Debug($"Search \"{name}\" in \"{searchPath}\"");
                    if (File.Exists(Path.Combine(searchPath, name)))
                    {
                        Logger.Debug($"Find \"{name}\" from \"{searchPath}\"");
                        using (FileStream fs = File.OpenRead(Path.Combine(searchPath, name)))
{
                            Texture2D texture = Texture2D.FromStream(graphicsDevice, fs);
                            texture.Name = name;
                            PremultiplyAlpha(texture);

                            return texture;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("AssetLoader.LoadTextureInternal: loading texture " + name + " failed! Message: " + ex.Message);
            }

            return null;
        }
        private static void PremultiplyAlpha(Texture2D texture)
        {
            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                data[i].R = (byte)(data[i].R * data[i].A / 255);
                data[i].G = (byte)(data[i].G * data[i].A / 255);
                data[i].B = (byte)(data[i].B * data[i].A / 255);
            }

            texture.SetData(data);
        }
    }
}
