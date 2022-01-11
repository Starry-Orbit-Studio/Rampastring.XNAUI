using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;
using Rampastring.XNAUI.XNAControls;

namespace Rampastring.XNAUI
{
    public static class UIConfigurationsExtension
    {
        public static bool TryGet(this XNAControl control, string property, out object value)
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
            => control.TryGet(property, out var result) ? result.ToString() : @default;

        public static T Get<T>(this XNAControl control, string property, T @default = default)
            => control.TryGet(property, out T result) ? result : @default;
    }

    public delegate bool UIConfigurationConverter(object value, out object @out);

    public sealed class UIConfigurations
    {
        private UIConfigMappingNode _mappingNode;

        public UIConfigurations(UIConfigMappingNode mappingNode)
        {
            _mappingNode = mappingNode;
        }

        public bool TryGet(XNAControl control, string property, out object value)
        {
            bool success;
            (success, value) = TryGet(_mappingNode, control, property);
            return success;
        }


        public static UIConfigurations Instance { get; set; }

        public static Dictionary<Type, UIConfigurationConverter> Converter { get; } = new Dictionary<Type, UIConfigurationConverter>()
        {
            { typeof(string), StringConverter },
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

        public static (T, bool success) ConvertValue<T>(object value, T @default = default)
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
        private static bool BooleanConverter(object value, out object @out)
        {
            var result = bool.TryParse(value.ToString(), out var tmp);
            @out = tmp;
            return result;
        }
        private static bool Int8Converter(object value, out object @out)
        {
            var result = sbyte.TryParse(value.ToString(), out var tmp);
            @out = tmp;
            return result;
        }
        private static bool Int16Converter(object value, out object @out)
        {
            var result = short.TryParse(value.ToString(), out var tmp);
            @out = tmp;
            return result;
        }
        private static bool Int32Converter(object value, out object @out)
        {
            var result = int.TryParse(value.ToString(), out var tmp);
            @out = tmp;
            return result;
        }
        private static bool Int64Converter(object value, out object @out)
        {
            var result = long.TryParse(value.ToString(), out var tmp);
            @out = tmp;
            return result;
        }
        private static bool UInt8Converter(object value, out object @out)
        {
            var result = byte.TryParse(value.ToString(), out var tmp);
            @out = tmp;
            return result;
        }
        private static bool UInt16Converter(object value, out object @out)
        {
            var result = ushort.TryParse(value.ToString(), out var tmp);
            @out = tmp;
            return result;
        }
        private static bool UInt32Converter(object value, out object @out)
        {
            var result = uint.TryParse(value.ToString(), out var tmp);
            @out = tmp;
            return result;
        }
        private static bool UInt64Converter(object value, out object @out)
        {
            var result = ulong.TryParse(value.ToString(), out var tmp);
            @out = tmp;
            return result;
        }
        private static bool SingleConverter(object value, out object @out)
        {
            var result = float.TryParse(value.ToString(), out var tmp);
            @out = tmp;
            return result;
        }
        private static bool DoubleConverter(object value, out object @out)
        {
            var result = double.TryParse(value.ToString(), out var tmp);
            @out = tmp;
            return result;
        }
        private static bool DecimalConverter(object value, out object @out)
        {
            var result = decimal.TryParse(value.ToString(), out var tmp);
            @out = tmp;
            return result;
        }
        private static bool ColorConverter(object value, out object @out)
        {
            string colorString = value.ToString();
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
        private static bool PointConverter(object value, out object @out)
        {
            var tmp = value.ToString().TrimStart('(').TrimEnd(')').Split(',').Select(i => i.Trim()).ToArray();
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
        private static bool RectangleConverter(object value, out object @out)
        {
            var tmp = value.ToString().TrimStart('(').TrimEnd(')').Split(',').Select(i => i.Trim()).ToArray();
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
        private static bool Texture2DConverter(object value, out object @out)
        {
            string str = value.ToString();
            if ((str.StartsWith("#") || str.Contains(',')) &&
               ColorConverter(str, out var color))
            {
                @out = AssetLoader.CreateTexture((Color)color, 1, 1);
                return true;
            }

            @out = AssetLoader.LoadTexture(str);
            return @out != null;
        }
        private static bool EnhancedSoundEffectConverter(object value, out object @out)
        {
            @out = EnhancedSoundEffect.GetOrCreate(value.ToString());
            return @out != null;
        }
        private static bool StringConverter(object value, out object @out) => (@out = value.ToString()) is string;
        #endregion

        #region Private Methods

        private static (bool success, object result) TryGet(IUIConfigNode node, XNAControl control, string property)
        {
            //Logger.Debug($"[UIConf] ❓Try get control \"{control.Name} : {control.GetType().Name}\"'s property \"{property}\".");
            (List<string> uiNames, string[,] typeNames) = GetInfos(control);
            for (int i = uiNames.Count - 1; i >= 0; i--)
            {
                (ResultStatus status, object result) = TryGetProperty(node, uiNames.ToArray(), typeNames, property);
                if (status != ResultStatus.Fail)
                {
                    Logger.Debug($"[UIConf] ✔️Success to get control \"{control.Name} : {control.GetType().Name}\"'s property \"{property}\".");
                    return (true, result);
                }

                uiNames.RemoveAt(i);
            }
            Logger.Debug($"[UIConf] ❌Cannot get control \"{control.Name} : {control.GetType().Name}\"'s property \"{property}\".");
            return (false, null);
        }

        private static (List<string> uiNames, string[,] typeNames) GetInfos(XNAControl control)
        {
            List<string> uiNames = new List<string>();
            List<List<string>> typeNames = new List<List<string>>();
            while (control != null)
            {
                string uiName = control.Name;
                List<string> typeName = new List<string>();

                Type type = control.GetType();
                while (true)
                {
                    typeName.Add(type.Name);
                    if (type == typeof(XNAControl))
                        break;

                    type = type.BaseType;
                }

                uiNames.Add(uiName);
                typeNames.Add(typeName);
                do
                {
                    control = control.Parent;
                }
                while (control?.SkipUIPath ?? false);
            }
            string[,] table = new string[typeNames.Select(i => i.Count).Max(), typeNames.Count];
            for (int p = 0; p < typeNames.Count; p++)
            {
                for (int l = 0; l < typeNames[p].Count; l++)
                {
                    table[l, p] = typeNames[p][l];
                }
            }
            return (uiNames, table);
        }
        // TODO: 需要缓存
        private static (ResultStatus status, object result) TryGetProperty(IUIConfigNode node, string[] uiNames, string[,] typeNames, string propertyName)
        {
            // TODO: 数据没有被反转
            int mP = uiNames.Length;
            int mL = typeNames.GetLength(0);
            int p = -1;
            int l = 0;
            while (true)
            {
                if (p < mP && p > -1)
                {
                    var tmp = typeNames[l, p];
                    if (string.IsNullOrEmpty(tmp) || uiNames[p] == tmp)
                    {
                        p++;
                        goto End;
                    }

                    uiNames[p] = tmp;
                }

                p++;
                var (success, result) = TryGetNodeByFullName(node, uiNames);
                if (success)
                {
                    var (status, value) = TryGetPropertyValueFromNode(result, propertyName);
                    if (status != ResultStatus.Fail)
                    {
                        Logger.Debug($"[UIConf]   Loading path is \"{string.Join(".", uiNames.Reverse())}\" .");
                        return (status, value);
                    }
                }

            End:
                if (p >= mP)
                {
                    p = 0;
                    l++;
                }
                if (l >= mL)
                    return (ResultStatus.Fail, null);
            }
        }

        /// <summary>
        /// 尝试获取属性
        /// </summary>
        /// <param name="node"></param>
        /// <param name="property"></param>
        /// <returns>
        /// 当 type 为 <seealso cref="ResultStatus.Dictionary"/> 时 resule 是<seealso cref="Dictionary{string, IUIConfigNode}"/> <br />
        /// 当 type 为 <seealso cref="ResultStatus.List"/> 时 resule 是<seealso cref="List{IUIConfigNode}"/> <br />
        /// 当 type 为 <seealso cref="ResultStatus.Value"/> 时 resule 是<seealso cref="string"/> <br />
        /// 当 type 为 <seealso cref="ResultStatus.Fail"/> 时 resule 是<seealso cref="null"/>
        /// </returns>
        private static (ResultStatus type, object result) TryGetPropertyValueFromNode(IUIConfigNode node, string property)
        {
            if (node is UIConfigMappingNode map && map.TryGetValue(property, out node))
            {
                switch (node)
                {
                    case UIConfigMappingNode mappingNode:
                        return (ResultStatus.Dictionary, mappingNode.Children);
                    case UIConfigCollectionNode collectionNode:
                        return (ResultStatus.List, collectionNode.Children);
                    case UIConfigValueNode valueNode:
                        return (ResultStatus.Value, valueNode.Value);
                }
            }
            return (0, null);
        }

        /// <summary>
        /// 尝试用全名获取节点
        /// </summary>
        /// <param name="names">名称列表 正序</param>
        /// <param name="node">根节点</param>
        /// <returns></returns>
        private static (bool success, IUIConfigNode node) TryGetNodeByFullName(IUIConfigNode node, string[] names)
        {
            bool success = false;
            for (int i = names.Length - 1; i >= 0; i--)
            {
                if (!((UIConfigMappingNode)node).TryGetValue(names[i], out node))
                {
                    success = false;
                    break;
                }

                success = true;
            }
            return (success, node);
        }

        enum ResultStatus : byte
        {
            Fail,
            Value,
            List,
            Dictionary
        }
        #endregion
    }

    public interface IUIConfigNode
    {
    }

    public sealed class UIConfigMappingNode : IUIConfigNode, IDictionary<string, IUIConfigNode>
    {
        public IUIConfigNode this[string key] { get => ((IDictionary<string, IUIConfigNode>)Children)[key]; set => ((IDictionary<string, IUIConfigNode>)Children)[key] = value; }

        public Dictionary<string, IUIConfigNode> Children { get; set; } = new Dictionary<string, IUIConfigNode>();

        public ICollection<string> Keys => ((IDictionary<string, IUIConfigNode>)Children).Keys;

        public ICollection<IUIConfigNode> Values => ((IDictionary<string, IUIConfigNode>)Children).Values;

        public int Count => ((ICollection<KeyValuePair<string, IUIConfigNode>>)Children).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, IUIConfigNode>>)Children).IsReadOnly;

        public void Add(string key, IUIConfigNode value)
        {
            ((IDictionary<string, IUIConfigNode>)Children).Add(key, value);
        }

        public void Add(KeyValuePair<string, IUIConfigNode> item)
        {
            ((ICollection<KeyValuePair<string, IUIConfigNode>>)Children).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, IUIConfigNode>>)Children).Clear();
        }

        public bool Contains(KeyValuePair<string, IUIConfigNode> item)
        {
            return ((ICollection<KeyValuePair<string, IUIConfigNode>>)Children).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ((IDictionary<string, IUIConfigNode>)Children).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, IUIConfigNode>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, IUIConfigNode>>)Children).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, IUIConfigNode>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, IUIConfigNode>>)Children).GetEnumerator();
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, IUIConfigNode>)Children).Remove(key);
        }

        public bool Remove(KeyValuePair<string, IUIConfigNode> item)
        {
            return ((ICollection<KeyValuePair<string, IUIConfigNode>>)Children).Remove(item);
        }

        public bool TryGetValue(string key, out IUIConfigNode value)
        {
            if (key is null)
            {
                value = null;
                return false;
            }
            return ((IDictionary<string, IUIConfigNode>)Children).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Children).GetEnumerator();
        }
    }
    public sealed class UIConfigCollectionNode : IUIConfigNode, IList<IUIConfigNode>
    {
        public IUIConfigNode this[int index] { get => ((IList<IUIConfigNode>)Children)[index]; set => ((IList<IUIConfigNode>)Children)[index] = value; }

        public List<IUIConfigNode> Children { get; set; } = new List<IUIConfigNode>();

        public int Count => ((ICollection<IUIConfigNode>)Children).Count;

        public bool IsReadOnly => ((ICollection<IUIConfigNode>)Children).IsReadOnly;

        public void Add(IUIConfigNode item)
        {
            ((ICollection<IUIConfigNode>)Children).Add(item);
        }

        public void Clear()
        {
            ((ICollection<IUIConfigNode>)Children).Clear();
        }

        public bool Contains(IUIConfigNode item)
        {
            return ((ICollection<IUIConfigNode>)Children).Contains(item);
        }

        public void CopyTo(IUIConfigNode[] array, int arrayIndex)
        {
            ((ICollection<IUIConfigNode>)Children).CopyTo(array, arrayIndex);
        }

        public IEnumerator<IUIConfigNode> GetEnumerator()
        {
            return ((IEnumerable<IUIConfigNode>)Children).GetEnumerator();
        }

        public int IndexOf(IUIConfigNode item)
        {
            return ((IList<IUIConfigNode>)Children).IndexOf(item);
        }

        public void Insert(int index, IUIConfigNode item)
        {
            ((IList<IUIConfigNode>)Children).Insert(index, item);
        }

        public bool Remove(IUIConfigNode item)
        {
            return ((ICollection<IUIConfigNode>)Children).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<IUIConfigNode>)Children).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Children).GetEnumerator();
        }
    }
    public sealed class UIConfigValueNode : IUIConfigNode
    {
        public string Value { get; set; }
    }

}
