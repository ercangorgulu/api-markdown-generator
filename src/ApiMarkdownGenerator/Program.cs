using System.IO;
using System.Linq;
using System.Reflection;

namespace MarkdownGenerator
{
    internal class Program
    {
        // put dll & xml on same diretory.
        private static void Main(string[] args)
        {
            //output path for documentation
            var dest = "md";
            //namespace regex
            var namespaceMatch = "*.Controllers";
            //class regex
            var classMatch = ".*Controller";
            //dll path which will be documented
            var dllPath = @"C:\example\example.dll";

            var assembly = Assembly.LoadFrom(dllPath);
            var commentsLookup = MarkdownGenerator.GetCommentsLookup(assembly);
            var referencedModelContainer = new ReferencedModelContainer(commentsLookup);
            var types = MarkdownGenerator.Load(assembly, namespaceMatch, classMatch, referencedModelContainer, commentsLookup);

            // Home Markdown Builder
            var homeBuilder = new MarkdownBuilder();
            homeBuilder.Header(1, "References");
            homeBuilder.AppendLine();

            foreach (var g in types.GroupBy(x => x.Namespace).OrderBy(x => x.Key))
            {
                if (!Directory.Exists(dest)) Directory.CreateDirectory(dest);

                homeBuilder.HeaderWithLink(2, g.Key, g.Key);
                homeBuilder.AppendLine();

                var mb = new MarkdownBuilder();
                mb.Header(1, "Usage Documentation");
                mb.Header(2, "Controllers and Actions");
                foreach (var item in g.OrderBy(x => x.Name))
                {
                    homeBuilder.ListLink(MarkdownBuilder.MarkdownCodeQuote(item.BeautifyName), g.Key + "#" + item.BeautifyName.Replace("<", "").Replace(">", "").Replace(",", "").Replace(" ", "-").ToLower());

                    mb.Append(item.ToString());
                }
                mb.Header(2, "Referenced Types");
                mb.Append(referencedModelContainer.ToString());
                var result = Markdig.Markdown.Normalize(mb.ToString());
                File.WriteAllText(Path.Combine(dest, g.Key + ".md"), result);
                homeBuilder.AppendLine();
            }

            // Gen Home
            File.WriteAllText(Path.Combine(dest, "Home.md"), homeBuilder.ToString());
        }
    }
}
