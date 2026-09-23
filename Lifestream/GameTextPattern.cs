using Lumina.Text.ReadOnly;
using System.Text;
using System.Text.RegularExpressions;

namespace Lifestream;

internal sealed class GameTextPattern(params ReadOnlySeString[] rows)
{
    internal bool Matches(string text)
    {
        var actual = Normalize(text);
        foreach(var row in rows)
        {
            var pattern = new StringBuilder(@"\A");
            var meaningful = false;
            foreach(var payload in row)
            {
                if(payload.Type == ReadOnlySePayloadType.Text)
                {
                    var literal = Encoding.UTF8.GetString(payload.Body.Span);
                    meaningful |= literal.Any(char.IsLetter);
                    pattern.Append(Regex.Escape(Normalize(literal)));
                }
                else
                    pattern.Append(".*");
            }
            // A template must contain meaningful fixed text.
            if(meaningful && Regex.IsMatch(actual, pattern.Append(@"\z").ToString(), RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)) return true;
        }
        return false;
    }

    private static string Normalize(string value) => new(value
        .Where(c => !char.IsWhiteSpace(c) && c != '\u00ad').Select(char.ToUpperInvariant).ToArray());
}
