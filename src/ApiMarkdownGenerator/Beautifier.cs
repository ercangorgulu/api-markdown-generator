using Microsoft.AspNetCore.Mvc;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarkdownGenerator
{
    public class Beautifier
    {
        private readonly ILookup<string, XmlDocumentComment> _commentLookup;
        private readonly ReferencedModelContainer _referencedModelContainer;

        public Beautifier(ILookup<string, XmlDocumentComment> commentLookup, ReferencedModelContainer referencedModelContainer)
        {
            _commentLookup = commentLookup;
            _referencedModelContainer = referencedModelContainer;
        }

        public string ToMarkdownMethodInfo(MethodInfo methodInfo, Type type, string controller)
        {
            var isExtension = methodInfo.GetCustomAttributes<System.Runtime.CompilerServices.ExtensionAttribute>(false).Any();

            var seq = methodInfo.GetParameters().Select(x =>
            {
                var suffix = x.HasDefaultValue ? (" = " + (x.DefaultValue ?? $"null")) : "";
                _referencedModelContainer.AddType(x.ParameterType);
                return $"{new MarkdownableTypeName(x.ParameterType).AsLink()} {x.Name}{suffix}";
            });

            var mb = new MarkdownBuilder();

            var coreAttributes = methodInfo.GetCustomAttributes<ProducesResponseTypeAttribute>();
            var swaggerAttributes = methodInfo.GetCustomAttributes<SwaggerResponseAttribute>();

            //TODO get this from route also
            mb.Header(4, $"{controller}/{methodInfo.Name}");
            mb.AppendLine($"- {methodInfo.Name}({(isExtension ? "this " : "")}{string.Join(", ", seq)})").AppendLine();

            var documentation = _commentLookup[type.FullName]
                .FirstOrDefault(c => c.MemberType == MemberType.Method && c.MemberName == methodInfo.Name)
                ?.Summary;
            if (documentation != null)
            {
                mb.AppendLine(documentation);
                mb.AppendLine();
            }

            var headers = new[] {
                "StatusCode",
                "ReturnType",
                "Description"
            };

            var actionDescriptions = new List<string[]>();

            if (coreAttributes.Any())
            {
                foreach (var attribute in coreAttributes)
                {
                    actionDescriptions.Add(new[] {
                        attribute.StatusCode.ToString(),
                        new MarkdownableTypeName(attribute.Type).AsLink(),
                        ""
                    });
                    if (attribute.Type != null)
                    {
                        _referencedModelContainer.AddType(attribute.Type);
                    }
                }
            }
            else if (swaggerAttributes.Any())
            {
                foreach (var attribute in swaggerAttributes)
                {
                    actionDescriptions.Add(new[] {
                        attribute.StatusCode.ToString(),
                        new MarkdownableTypeName(attribute.Type).AsLink(),
                        attribute.Description
                    });
                    if (attribute.Type != null)
                    {
                        _referencedModelContainer.AddType(attribute.Type);
                    }
                }
            }

            mb.Table(headers, actionDescriptions);

            return mb.ToString();
        }
    }
}
