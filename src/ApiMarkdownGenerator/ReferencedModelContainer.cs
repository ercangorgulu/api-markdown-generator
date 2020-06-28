using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkdownGenerator
{
    public class ReferencedModelContainer
    {
        private readonly HashSet<Type> _types = new HashSet<Type>();
        private readonly ILookup<string, XmlDocumentComment> _commentLookup;

        public ReferencedModelContainer(ILookup<string, XmlDocumentComment> commentLookup)
        {
            _commentLookup = commentLookup;
        }

        public bool AddType(Type type)
        {
            if (type == null) return false;
            if (type == typeof(void)) return false;

            if (!type.IsGenericType)
            {
                if (!IsCursorLink(type)) return false;
                return _types.Add(type);
            }

            var added = false;

            if (IsCursorLink(type))
            {
                added = _types.Add(type);
            }
            foreach (var subType in type.GetGenericArguments())
            {
                added |= AddType(subType);
            }
            return added;
        }

        public override string ToString()
        {
            FinalizeTypes();

            var sb = new StringBuilder();

            foreach (var type in _types.OrderBy(c => c.Name))
            {
                sb.AppendLine(new MarkdownableType(type, _commentLookup, this).ToString());
            }

            return sb.ToString();
        }

        private void FinalizeTypes()
        {
            bool added;

            do
            {
                added = false;
                foreach (var type in _types.ToList())
                {
                    foreach (var property in type.GetProperties())
                    {
                        if (property.PropertyType.IsPrimitive || property.PropertyType.Namespace.StartsWith("System"))
                        {
                            continue;
                        }

                        added |= AddType(property.PropertyType);
                    }
                }
            }
            while (added);
        }

        private bool IsCursorLink(Type type)
        {
            if (type.IsPrimitive || type.Namespace.StartsWith("System") || type.FullName == null)
            {
                return false;
            }
            return true;
        }
    }
}
