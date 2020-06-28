using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarkdownGenerator
{
    public class MarkdownableType
    {
        private readonly Type _type;
        private readonly ILookup<string, XmlDocumentComment> _commentLookup;
        private readonly Beautifier _beautifier;
        private readonly MarkdownableTypeName _markdownableTypeName;
        private readonly ReferencedModelContainer _referencedModelContainer;

        public string Namespace => _type.Namespace;
        public string Name => _type.Name;
        public string BeautifyName => _markdownableTypeName.AsHeader();

        public MarkdownableType(Type type, ILookup<string, XmlDocumentComment> commentLookup, ReferencedModelContainer referencedModelContainer)
        {
            _type = type;
            _commentLookup = commentLookup;
            _markdownableTypeName = new MarkdownableTypeName(_type);
            _beautifier = new Beautifier(_commentLookup, referencedModelContainer);
            _referencedModelContainer = referencedModelContainer;
        }

        private MethodInfo[] GetMethods()
        {
            return _type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any() && !x.IsPrivate)
                .ToArray();
        }

        private PropertyInfo[] GetProperties()
        {
            return _type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.SetProperty)
                .Where(x =>
                {
                    try
                    {
                        return !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any();
                    }
                    catch
                    {
                        return true;
                    }
                })
                .Where(y =>
                {
                    var get = y.GetGetMethod(true);
                    var set = y.GetSetMethod(true);
                    if (get != null && set != null)
                    {
                        return !(get.IsPrivate && set.IsPrivate);
                    }
                    else if (get != null)
                    {
                        return !get.IsPrivate;
                    }
                    else if (set != null)
                    {
                        return !set.IsPrivate;
                    }
                    else
                    {
                        return false;
                    }
                })
                .ToArray();
        }

        private FieldInfo[] GetFields()
        {
            return _type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.GetField | BindingFlags.SetField)
                .Where(x => !x.IsSpecialName && !x.GetCustomAttributes<ObsoleteAttribute>().Any() && !x.IsPrivate)
                .ToArray();
        }

        private void BuildTable<T>(MarkdownBuilder mb, string label, T[] array, IEnumerable<XmlDocumentComment> docs, Func<T, string> type, Func<T, string> name, Func<T, string> finalName)
        {
            if (array.Any())
            {
                mb.AppendLine(label);
                mb.AppendLine();

                string[] head = (this._type.IsEnum)
                    ? new[] { "Value", "Name", "Summary" }
                    : new[] { "Type", "Name", "Summary" };

                IEnumerable<T> seq = array;
                if (!this._type.IsEnum)
                {
                    seq = array.OrderBy(x => name(x));
                }

                var data = seq.Select(item2 =>
                {
                    var summary = docs.FirstOrDefault(x => x.MemberName == name(item2) || x.MemberName.StartsWith(name(item2) + "`"))?.Summary ?? "";
                    var typeText = type(item2);
                    return new[] { typeText.Contains('[') ? typeText : MarkdownBuilder.MarkdownCodeQuote(typeText), finalName(item2), summary };
                });

                mb.Table(head, data);
            }
        }

        public override string ToString()
        {
            var mb = new MarkdownBuilder();

            var isController = _type.Name.EndsWith("Controller");

            mb.Header(isController ? 3 : 4, _markdownableTypeName.AsHeader());
            mb.AppendLine();

            var desc = _commentLookup[_type.FullName].FirstOrDefault(x => x.MemberType == MemberType.Type)?.Summary ?? "";
            if (desc != "")
            {
                mb.AppendLine(desc);
            }

            mb.AppendLine();

            if (isController)
            {
                foreach (var method in GetMethods())
                {
                    var methodInfo = _beautifier.ToMarkdownMethodInfo(method, _type, _type.Name.Substring(0, _type.Name.Length - "Controller".Length));
                    mb.AppendLine(methodInfo);
                }
            }
            else if (_type.IsEnum)
            {
                var underlyingEnumType = Enum.GetUnderlyingType(_type);

                var enums = Enum.GetNames(_type)
                    .Select(x => new { Name = x, Value = (Convert.ChangeType(Enum.Parse(_type, x), underlyingEnumType)) })
                    .OrderBy(x => x.Value)
                    .ToArray();

                BuildTable(mb, "Enum", enums, _commentLookup[_type.FullName], x => x.Value.ToString(), x => x.Name, x => x.Name);
            }
            else
            {
                BuildTable(mb, "Fields", GetFields(), _commentLookup[_type.FullName], x => new MarkdownableTypeName(x.FieldType).AsLink(), x => x.Name, x => x.Name);
                BuildTable(mb, "Properties", GetProperties(), _commentLookup[_type.FullName], x => new MarkdownableTypeName(x.PropertyType).AsLink(), x => x.Name, x => x.Name);
            }

            return mb.ToString();
        }
    }
}