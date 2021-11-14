using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Color = Microsoft.Xna.Framework.Color;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using System.Linq;

namespace Rampastring.XNAUI
{
    /// <summary>
    /// A static class that provides easy-to-use methods
    /// for loading and generating assets such as textures and sounds.
    /// </summary>
    public static class AssetLoader
    {
        /// <summary>
        /// A list of filesystem paths that assets are attempted to load from.
        /// </summary>
        public static List<string> AssetSearchPaths;

        private static GraphicsDevice graphicsDevice;
        private static ContentManager contentManager;

        private static List<WeakReference<Texture2D>> textureCache;
        private static List<WeakReference<SoundEffect>> soundCache;

        private static bool _initialized = false;

        public static bool IsInitialized => _initialized;

        /// <summary>
        /// Initializes the AssetLoader.
        /// </summary>
        /// <param name="gd">The graphics device.</param>
        /// <param name="content">The game content manager.</param>
        public static void Initialize(GraphicsDevice gd, ContentManager content)
        {
            if (_initialized)
                throw new InvalidOperationException("AssetLoader is already initialized.");
            _initialized = true;

            graphicsDevice = gd;
            AssetSearchPaths = new List<string>();
            textureCache = new List<WeakReference<Texture2D>>();
            soundCache = new List<WeakReference<SoundEffect>>();
            contentManager = content;
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
            return CreateDummyTexture();
        }

        private static Texture2D LoadTextureInternal(string name)
        {
            try
            {
                foreach (string searchPath in AssetSearchPaths)
                {
                    Logger.Debug($"Search \"{name}\" in \"{searchPath}\"");
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

        /// <summary>
        /// Creates and returns a 100x100 pink square.
        /// </summary>
        private static Texture2D CreateDummyTexture()
        {
            return CreateTexture(new Color(255, 54, 244), 100, 100);
        }

        /// <summary>
        /// Checks if a specified asset file exists.
        /// </summary>
        /// <param name="name">The name of the asset file.</param>
        /// <returns></returns>
        public static bool AssetExists(string name)
        {
            Logger.Debug($"Try Find \"{name}\"");
            foreach (string searchPath in AssetSearchPaths)
            {
                if (File.Exists(Path.Combine(searchPath, name)))
                    return true;
            }

            return false;
        }

        internal static string GetAssetPath(string name)
        {
            Logger.Debug($"Try Find \"{name}\"");
            foreach (string searchPath in AssetSearchPaths)
            {
                var path = Path.Combine(searchPath, name);
                if (File.Exists(path))
                    return path;
            }

            return string.Empty;
        }

        /// <summary>
        /// Creates a one-colored texture.
        /// </summary>
        /// <param name="color">The color of the texture.</param>
        /// <param name="width">The width of the texture in pixels.</param>
        /// <param name="height">The height of the texture in pixels.</param>
        /// <returns>A texture.</returns>
        public static Texture2D CreateTexture(Color color, int width, int height)
        {
            Texture2D texture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);

            Color[] colorArray = new Color[width * height];

            for (int i = 0; i < colorArray.Length; i++)
                colorArray[i] = color;

            texture.SetData(colorArray);

            return texture;
        }

        /// <summary>
        /// Creates a texture from a <see cref="System.Drawing.Image"/>.
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
        /// Loads a sound effect with the given name.
        /// </summary>
        /// <param name="name">The name of the sound effect.</param>
        /// <returns>The loaded sound effect, or null if the sound effect isn't found.</returns>
        public static SoundEffect LoadSound(string name)
        {
            Logger.Debug($"Try Load \"{name}\"");
            SoundEffect cachedSound = null;

            if (soundCache.Any(x => x.TryGetTarget(out cachedSound) && cachedSound.Name == name))
            {
                Logger.Debug($"Find \"{name}\" from cache");
                return cachedSound;
            }

            foreach (string searchPath in AssetSearchPaths)
            {
                if (File.Exists(Path.Combine(searchPath, name)))
                {
                    using (FileStream fs = File.OpenRead(Path.Combine(searchPath, name)))
                    {
                        SoundEffect se = SoundEffect.FromStream(fs);
                        se.Name = name;
                        soundCache.Add(new WeakReference<SoundEffect>(se));
                        return se;
                    }
                }
            }

            Logger.Log("AssetLoader.LoadSound: Sound not found! " + name);

            return null;
        }

        /// <summary>
        /// Loads a <see cref="Song"/> with the specified name.
        /// </summary>
        /// <param name="name">The name of the song.</param>
        /// <returns>The loaded song, or null if loading the song fails.</returns>
        public static Song LoadSong(string name)
        {
            try
            {
                return contentManager.Load<Song>(name);
            }
            catch (Exception ex)
            {
                Logger.Log("Loading song " + name + " failed! Message: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Support Format:<br />
        /// - R, G, B (All values must be between 0 and 255.)<br />
        /// - R, G, B, A (All values must be between 0 and 255.)<br />
        /// - #RRGGBB<br />
        /// - #AARRGGBB<br />
        /// Creates a color based on a color string in the form string.<br />
        /// Returns a given default color if parsing the given string fails.
        /// </summary>
        /// <param name="colorString">The color string.</param>
        /// <param name="defaultColor">The default color to return if parsing the string fails.</param>
        /// <returns>A XNA Color struct.</returns>
        /// <returns>A XNA Color struct based on the given string.</returns>
        public static Color GetColorFromString(string colorString, Color? defaultColor = null)
        {
            try
            {
                if (colorString.StartsWith("#"))
                {
                    int color = Convert.ToInt32(colorString.TrimStart('#'), 16);
                    return new Color(
                        alpha: (color & 0xFF000000) >> 24,
                            r: (color & 0x00FF0000) >> 16,
                            g: (color & 0x0000FF00) >> 8,
                            b: (color & 0x000000FF) >> 0);
                }
                else
                {
                    string[] colorArray = colorString.Split(',').Select(x => x.Trim()).ToArray();

                    int alpha = 255;
                    if (colorArray.Length == 4)
                        alpha = byte.Parse(colorArray[3]);

                    return new Color(
                        byte.Parse(colorArray[0]),
                        byte.Parse(colorArray[1]),
                        byte.Parse(colorArray[2]),
                        alpha);
                }
            }
            catch
            {
                if (defaultColor.HasValue)
                    return defaultColor.Value;
                else
                    throw new FormatException("AssetLoader.GetColorFromString: Failed to convert " + colorString + " to a valid color!");
            }
        }

        /// <summary>
        /// Creates a color based on a color string in the form "R,G,B,A" or "#AARRGGBB" or "#RRGGBB".<br />
        /// If colorString is "R,G,B,A". All values must be between 0 and 255.
        /// </summary>
        /// <param name="colorString">"R,G,B,A" or "#AARRGGBB" or "#RRGGBB"</param>
        /// <returns>A XNA Color struct.</returns>
        [Obsolete("Use GetColorFromString", true)]
        public static Color GetRGBAColorFromString(string colorString) => throw new NotSupportedException();
    }
}
