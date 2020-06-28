using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MarkdownGenerator
{
    public static class MarkdownGenerator
    {
        public static ILookup<string, XmlDocumentComment> GetCommentsLookup(Assembly assembly)
        {
            var xmlFile = $"{Path.GetDirectoryName(assembly.Location)}\\{Path.GetFileNameWithoutExtension(assembly.Location)}.xml";
            var comments = !File.Exists(xmlFile)
                ? Array.Empty<XmlDocumentComment>()
                : VSDocParser.ParseXmlComment(XDocument.Parse(File.ReadAllText(xmlFile)));

            return comments.ToLookup(x => x.ClassName);
        }

        public static MarkdownableType[] Load(
            Assembly assembly,
            string namespaceMatch,
            string classMatch,
            ReferencedModelContainer referencedModelContainer,
            ILookup<string, XmlDocumentComment> commentsLookup = null)
        {
            commentsLookup ??= GetCommentsLookup(assembly);

            var namespaceRegex =
                !string.IsNullOrEmpty(namespaceMatch) ? new Regex(namespaceMatch) : null;

            var classRegex =
                !string.IsNullOrEmpty(classMatch) ? new Regex(classMatch) : null;

            var markdownableTypes = new[] { assembly }
                .SelectMany(x =>
                {
                    try
                    {
                        return x.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        return ex.Types.Where(t => t != null);
                    }
                    catch
                    {
                        return Type.EmptyTypes;
                    }
                })
                .Where(x => x != null)
                .Where(x => x.IsPublic && !typeof(Delegate).IsAssignableFrom(x) && !x.GetCustomAttributes<ObsoleteAttribute>().Any())
                .Where(x => IsRequiredNamespace(x, namespaceRegex))
                .Where(x => IsRequiredClass(x, classRegex))
                .Select(x => new MarkdownableType(x, commentsLookup, referencedModelContainer))
                .ToArray();

            return markdownableTypes;
        }

        private static bool IsRequiredNamespace(Type type, Regex regex)
        {
            if (regex == null)
            {
                return true;
            }
            return regex.IsMatch(type.Namespace ?? string.Empty);
        }

        private static bool IsRequiredClass(Type type, Regex regex)
        {
            if (regex == null)
            {
                return true;
            }
            return regex.IsMatch(type.Name ?? string.Empty);
        }
    }
}