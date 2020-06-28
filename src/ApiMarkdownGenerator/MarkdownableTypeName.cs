using System;
using System.Linq;
using System.Text.RegularExpressions;
using static MarkdownGenerator.Constants;

namespace MarkdownGenerator
{
    public class MarkdownableTypeName
    {

        private readonly Type _type;
        private readonly bool _isGenericType;
        private readonly bool _isCursorLink;
        private readonly Type _genericType;
        private readonly Type[] _args;

        public MarkdownableTypeName(Type type)
        {
            _type = type;
            _isGenericType = _type?.IsGenericType == true;
            if (_isGenericType)
            {
                _genericType = _type.GetGenericTypeDefinition();
                _args = _type.GetGenericArguments();
            }
            _isCursorLink = _type != null && !_type.IsPrimitive && !_type.Namespace.StartsWith("System") && type.FullName != null;
        }

        public string AsHeader()
        {
            return _isGenericType ? GenericAsHeader() : NormalAsHeader();
        }

        public string AsLink()
        {
            return _isGenericType ? GenericAsLink() : NormalAsLink();
        }

        private string NormalAsHeader()
        {
            if (_type == null) return "{empty}";
            if (_type == typeof(void)) return "void";

            return _type.Name;
        }

        private string NormalAsLink()
        {
            if (_type == null) return "{empty}";
            if (_type == typeof(void)) return "void";

            if (!_isCursorLink)
            {
                return _type.Name;
            }
            return new MarkdownBuilder().CursorLink(_type.Name).ToString();
        }

        private string GenericAsHeader()
        {
            var typeName = Regex.Replace(_genericType.Name, @"`.+$", "");
            var arguments = _args.Select(c => c.Name);
            return $@"{typeName}{GtEscapedChar}{string.Join(",", arguments)}{LtEscapedChar}";
        }

        private string GenericAsLink()
        {
            var typeName = Regex.Replace(_genericType.Name, @"`.+$", "");
            var md = new MarkdownBuilder();
            if (_isCursorLink)
            {
                var link = $@"{typeName}{LtChar}{string.Join(",", _args.Select(c => c.Name))}{GtChar}";
                md.CursorLink(link);
            }
            else
            {
                md.Append(typeName);
                md.Append(LtChar);
                md.Append(string.Join(",", _args.Select(c => new MarkdownableTypeName(c).AsLink())));
                md.Append(GtChar);
            }
            return md.ToString();
        }
    }
}
