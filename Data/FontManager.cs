using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using FontStashSharp;

using Rampastring.Tools;

namespace Rampastring.XNAUI.Data
{
    public sealed class FontManager
    {
        private Dictionary<string, FontSystem> _cache = new Dictionary<string, FontSystem>();
        private Dictionary<string, List<FileInfo>> _fonts = new Dictionary<string, List<FileInfo>>();
        private FontSystem _defaultFont;
        private string _defaultFontName;

        public int DefaultCharacter { get; set; }

        public FontSystem this[string key] => GetFont(key);

        public FontSystem GetFont(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Logger.Warn($"Key is empty. Use default font \"{DefaultFontName}\".");
                return DefaultFont;
            }

            if (_cache.TryGetValue(key, out var result))
                return result;
            if (_fonts.TryGetValue(key, out var file))
            {
                result = new FontSystem
                {
                    DefaultCharacter = DefaultCharacter
                };
                file.ForEach(x =>
                {
                    using (var fs = x.OpenRead())
                        result.AddFont(fs);
                });
                return _cache[key] = result;
            }

            Logger.Warn($"Can't find the font \"{key}\". Use default font \"{DefaultFontName}\".");
            return DefaultFont;

        }

        public FontSystem DefaultFont => _defaultFont ?? (_defaultFont = GetFont(DefaultFontName));
        public string DefaultFontName { get; set; }

        public void Initialize(string customFolderPath = default)
        {
            // System Fonts
            var fontFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            InitializeFromPath(fontFolderPath);

            // Windows User Fonts
            fontFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");
            if (string.IsNullOrEmpty(fontFolderPath) || !Directory.Exists(fontFolderPath))
                return;
            InitializeFromPath(fontFolderPath);

            // Custom Fonts
            fontFolderPath = customFolderPath;
            if (string.IsNullOrEmpty(fontFolderPath) || !Directory.Exists(fontFolderPath))
                return;
            InitializeFromPath(fontFolderPath);

        }

        private void InitializeFromPath(string path)
        {
            var files = Directory.GetFiles(path, "*.ttf");
            for (int i = 0; i < files.Length; i++)
            {
                var fontCol = new PrivateFontCollection();
                fontCol.AddFontFile(files[i]);
                var font = fontCol.Families.Last();
                var name = font.Name;

                Logger.Debug($"Loading Font \"{name}\" from \"{files[i]}\"");
                var file = new FileInfo(files[i]);
                if (_fonts.TryGetValue(name, out var list))
                    list.Add(file);

                _fonts[name] = new List<FileInfo>() { file };
            }
        }
    }
}
