using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;
using Rampastring.XNAUI.XNAControls;


namespace Rampastring.XNAUI
{
    public static class UIConfigurationsExtension
    {
        public static bool TryGet(this XNAControl control, string property, out string value)
            => UIConfigurations.Instance?.TryGet(control, property, out value) ?? (value = string.Empty) is null;

        private static Dictionary<string, object> _cache = new Dictionary<string, object>();
        public static bool TryGet<T>(this XNAControl control, string property, out T value)
        {
            List<string> parentNames = new List<string>();
            XNAControl ctrl = control;
            do
            {
                parentNames.Add(ctrl.Name);
                ctrl = ctrl.Parent;
            } while (ctrl != null);

            parentNames.Reverse();

            var key = string.Join(".", string.Join(".", parentNames.ToArray()), property);

            if (_cache.TryGetValue(key, out var obj))
            {
                value = (T)obj;
                return true;
            }

            if (control.TryGet(property, out var szValue))
            {
                bool success;
                (value, success) = UIConfigurations.ConvertValue<T>(szValue);

                Debug.Assert(value != null);

                if (success)
                {
                    _cache.Add(key, value);
                    return true;
                }
            }
            value = default;
            return false;
        }


        public static string Get(this XNAControl control, string property, string @default = default)
            => control.TryGet(property, out var result) ? result : @default;

        public static T Get<T>(this XNAControl control, string property, T @default = default)
            => control.TryGet(property, out T result) ? result : @default;
    }

    public delegate bool UIConfigurationConverter(string value, out object @out);
    public sealed class UIConfigurations
    {
        private Dictionary<string, string> _configurations;

        public UIConfigurations(Dictionary<string, string> configurations)
        {
            _configurations = configurations;
        }

        public bool TryGet(XNAControl control, string property, out string value)
        {
            var infos = GetParentInfo(control);
            var allPaths = GeneratePath(infos);
            var properties = allPaths.Select(x => string.Join(".", x, property).Replace("..", "."));
            var key = properties.FirstOrDefault(_configurations.ContainsKey);

            if (string.IsNullOrEmpty(key))
            {
                Logger.Warn("Faild to load property: " + properties.First());
                value = string.Empty;
                return false;
            }

            value = _configurations[key];
            Logger.Debug($"Success to load property: {properties.First()}({key}): {value}");
            return true;
        }

        public static UIConfigurations Instance { get; set; }

        public static Dictionary<Type, UIConfigurationConverter> Converter { get; } = new Dictionary<Type, UIConfigurationConverter>()
        {
            { typeof(bool), BooleanConverter },
            { typeof(sbyte), Int8Converter },
            { typeof(short), Int16Converter },
            { typeof(int), Int32Converter },
            { typeof(long), Int64Converter },
            { typeof(byte), UInt8Converter },
            { typeof(ushort), UInt16Converter },
            { typeof(uint), UInt32Converter },
            { typeof(ulong), UInt64Converter },
            { typeof(float), SingleConverter },
            { typeof(double), DoubleConverter },
            { typeof(decimal), DecimalConverter },
            { typeof(Color), ColorConverter },
            { typeof(Point), PointConverter },
            { typeof(Rectangle), RectangleConverter },
            { typeof(Texture2D), Texture2DConverter },
            { typeof(EnhancedSoundEffect), EnhancedSoundEffectConverter },
        };

        public static (T, bool success) ConvertValue<T>(string value, T @default = default)
        {
            var success = Converter.TryGetValue(typeof(T), out var func);
            if (!success)
            {
                Logger.Debug($"Convert Faild: Unknown type \"{typeof(T).FullName}\".");
                return (@default, false);
            }
            success = func(value, out var obj);
            if (!success)
            {
                Logger.Debug($"Convert Faild: Cannot convert value \"{value}\" to type \"{typeof(T).FullName}\". ");
                return (@default, false);
            }

            return ((T)obj, true);
        }

        #region Converters
        private static bool BooleanConverter(string value, out object @out)
        {
            var result = bool.TryParse(value, out var tmp);
            @out = tmp;
            return result;
        }
        private static bool Int8Converter(string value, out object @out)
        {
            var result = sbyte.TryParse(value, out var tmp);
            @out = tmp;
            return result;
        }
        private static bool Int16Converter(string value, out object @out)
        {
            var result = short.TryParse(value, out var tmp);
            @out = tmp;
            return result;
        }
        private static bool Int32Converter(string value, out object @out)
        {
            var result = int.TryParse(value, out var tmp);
            @out = tmp;
            return result;
        }
        private static bool Int64Converter(string value, out object @out)
        {
            var result = long.TryParse(value, out var tmp);
            @out = tmp;
            return result;
        }
        private static bool UInt8Converter(string value, out object @out)
        {
            var result = byte.TryParse(value, out var tmp);
            @out = tmp;
            return result;
        }
        private static bool UInt16Converter(string value, out object @out)
        {
            var result = ushort.TryParse(value, out var tmp);
            @out = tmp;
            return result;
        }
        private static bool UInt32Converter(string value, out object @out)
        {
            var result = uint.TryParse(value, out var tmp);
            @out = tmp;
            return result;
        }
        private static bool UInt64Converter(string value, out object @out)
        {
            var result = ulong.TryParse(value, out var tmp);
            @out = tmp;
            return result;
        }
        private static bool SingleConverter(string value, out object @out)
        {
            var result = float.TryParse(value, out var tmp);
            @out = tmp;
            return result;
        }
        private static bool DoubleConverter(string value, out object @out)
        {
            var result = double.TryParse(value, out var tmp);
            @out = tmp;
            return result;
        }
        private static bool DecimalConverter(string value, out object @out)
        {
            var result = decimal.TryParse(value, out var tmp);
            @out = tmp;
            return result;
        }
        private static bool ColorConverter(string colorString, out object @out)
        {
            try
            {
                if (colorString.StartsWith("#"))
                {
                    int color = Convert.ToInt32(colorString.TrimStart('#'), 16);
                    int a = byte.MaxValue;
                    int r = 0;
                    int g = 0;
                    int b = 0;
                    if (colorString.Length > 6)
                    {
                        if (colorString.Length > 8)
                            a = (int)((color & 0xFF000000) >> 24);

                        r = (color & 0x00FF0000) >> 16;
                        g = (color & 0x0000FF00) >> 8;
                        b = (color & 0x000000FF) >> 0;
                    }
                    else if (colorString.Length > 3)
                    {
                        if (colorString.Length > 4)
                            a = (color & 0xF000) >> 16;

                        r = ((color & 0x0F00) >> 8) * 0x11;
                        g = ((color & 0x00F0) >> 4) * 0x11;
                        b = ((color & 0x000F) >> 0) * 0x11;
                    }
                    else if (colorString.Length > 2)
                    {
                        r = g = b = (color & 0x000000FF) >> 0;
                    }
                    else if (colorString.Length > 1)
                    {
                        r = g = b = ((color & 0x000F) >> 0) * 0x11;
                    }

                    @out = new Color(r, g, b, a);
                }
                else
                {
                    string[] colorArray = colorString.Split(',').Select(x => x.Trim()).ToArray();

                    int alpha = byte.MaxValue;
                    if (colorArray.Length == 4)
                        if (byte.TryParse(colorArray[3], out var tmp))
                            alpha = tmp;
                        else if (float.TryParse(colorArray[3], out var tmp2))
                            alpha = (int)(byte.MaxValue * tmp2);


                    @out = new Color(
                        byte.Parse(colorArray[0]),
                        byte.Parse(colorArray[1]),
                        byte.Parse(colorArray[2]),
                        alpha);
                }
                return true;
            }
            catch
            {
                Logger.Log($"{nameof(ColorConverter)}: Failed to convert {colorString} to a valid color!");
                @out = null;
                return false;
            }
        }
        private static bool PointConverter(string value, out object @out)
        {
            var tmp = value.TrimStart('(').TrimEnd(')').Split(',').Select(i => i.Trim()).ToArray();
            if (tmp.Length == 2 &&
                int.TryParse(tmp[0], out var x) &&
                int.TryParse(tmp[1], out var y))
            {
                @out = new Point(x, y);
                return true;
            }

            @out = null;
            return false;
        }
        private static bool RectangleConverter(string value, out object @out)
        {
            var tmp = value.TrimStart('(').TrimEnd(')').Split(',').Select(i => i.Trim()).ToArray();
            if (tmp.Length == 4 &&
                int.TryParse(tmp[0], out var x) &&
                int.TryParse(tmp[1], out var y) &&
                int.TryParse(tmp[2], out var w) &&
                int.TryParse(tmp[3], out var h))
            {
                @out = new Rectangle(x, y, w, h);
                return true;
            }

            @out = null;
            return false;
        }
        private static bool Texture2DConverter(string value, out object @out)
        {
            if ((value.StartsWith("#") || value.Contains(',')) &&
               ColorConverter(value, out var color))
            {
                @out = AssetLoader.CreateTexture((Color)color, 1, 1);
                return true;
            }

            @out = AssetLoader.LoadTexture(value);
            return @out != null;
        }
        private static bool EnhancedSoundEffectConverter(string value, out object @out)
        {
            @out = EnhancedSoundEffect.GetOrCreate(value);
            return @out != null;
        }

        #endregion


        private static (string[] names, Type[] types) GetParentInfo(XNAControl control)
        {
            List<string> parentNames = new List<string>();
            List<Type> parentTypes = new List<Type>();

            XNAControl ctrl = control;
            do
            {
                parentNames.Add(ctrl.Name);
                parentTypes.Add(ctrl.GetType());
                ctrl = ctrl.Parent;
            } while (ctrl != null);

            parentNames.Reverse();
            parentTypes.Reverse();
            return (parentNames.ToArray(), parentTypes.ToArray());
        }
        private static string[] GeneratePath(string[] parentNames, Type[] parentTypes)
        {
            HashSet<string> paths = new HashSet<string>();
            for (int i = 0; i <= parentNames.Length; i++)
            {
                Queue<string> tmp;
                if (i == 0)
                {
                    tmp = new Queue<string>(parentNames);
                    while (tmp.Any())
                    {
                        paths.Add(string.Join(".", tmp));
                        _ = tmp.Dequeue();
                    }
                }
                else
                {
                    var type = parentTypes[i - 1];
                    do
                    {
                        parentNames[i - 1] = type.Name;
                        tmp = new Queue<string>(parentNames);

                        while (tmp.Any())
                        {
                            paths.Add(string.Join(".", tmp));
                            _ = tmp.Dequeue();
                        }

                        type = type.BaseType;
                    } while (typeof(XNAControl).IsAssignableFrom(type));
                }

            }
            paths.Add(string.Empty);

            return paths.ToArray();
        }
        private static string[] GeneratePath((string[] names, Type[] types) tuple) => GeneratePath(tuple.names, tuple.types);
    }
}
