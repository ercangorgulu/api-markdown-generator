using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarkdownGenerator
{
    public class MarkdownBuilder
    {
        protected readonly StringBuilder sb = new StringBuilder();

        public static string MarkdownCodeQuote(string code)
        {
            return "`" + code + "`";
        }

        public MarkdownBuilder Append(string text)
        {
            sb.Append(text);
            return this;
        }

        public MarkdownBuilder AppendLine()
        {
            sb.AppendLine();
            return this;
        }

        public MarkdownBuilder AppendLine(string text)
        {
            sb.AppendLine(text);
            return this;
        }

        public MarkdownBuilder Header(int level, string text)
        {
            for (int i = 0; i < level; i++)
            {
                sb.Append("#");
            }
            sb.Append(' ').AppendLine(text).AppendLine();
            return this;
        }

        public MarkdownBuilder HeaderWithCode(int level, string code)
        {
            sb.Append('#', level).Append(' ');
            CodeQuote(code);
            sb.AppendLine();
            return this;
        }

        public MarkdownBuilder HeaderWithLink(int level, string text, string url)
        {
            sb.Append('#', level).Append(' ');
            Link(text, url);
            sb.AppendLine();
            return this;
        }

        public MarkdownBuilder CursorLink(string header)
        {
            if (header == null)
            {
                sb.Append("{empty}");
                return this;
            }
            sb.Append("[`");
            sb.Append(header);
            sb.Append("`]");
            sb.Append("(#");
            sb.Append(header);
            sb.Append(")");
            return this;
        }

        public MarkdownBuilder Link(string text, string url)
        {
            sb.Append("[`");
            sb.Append(text);
            sb.Append("`]");
            sb.Append("(");
            sb.Append(url);
            sb.Append(")");
            return this;
        }

        public MarkdownBuilder Image(string altText, string imageUrl)
        {
            sb.Append("!");
            Link(altText, imageUrl);
            return this;
        }

        public MarkdownBuilder Code(string language, string code)
        {
            sb.Append("```");
            sb.AppendLine(language);
            sb.AppendLine(code);
            sb.AppendLine("```");
            return this;
        }

        public MarkdownBuilder CodeQuote(string code)
        {
            sb.Append("`");
            sb.Append(code);
            sb.Append("`");
            return this;
        }

        public MarkdownBuilder Table(string[] headers, IEnumerable<string[]> items)
        {
            if (!items.Any())
            {
                return this;
            }

            sb.Append("| ").Append(string.Join(" | ", headers)).AppendLine(" |");
            sb.Append("| ").Append(string.Join(" | ", headers.Select(c => "---"))).AppendLine(" |");

            foreach (var item in items)
            {
                sb.Append("| ").Append(string.Join(" | ", item)).AppendLine(" |");
            }
            return this;
        }

        public MarkdownBuilder List(string text) // nest zero
        {
            sb.Append("- ").AppendLine(text);
            return this;
        }

        public MarkdownBuilder ListLink(string text, string url) // nest zero
        {
            sb.Append("- ");
            Link(text, url);
            sb.AppendLine();
            return this;
        }

        public override string ToString()
        {
            return sb.ToString();
        }
    }
}
