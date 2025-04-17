using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ToC_Kursovik
{
    public class Lexer
    {
        private static readonly Dictionary<TokenType, string> tokenPatterns = new()
    {
        { TokenType.REPEAT,       @"\brepeat\b" },
        { TokenType.COMMAND,      @"\b(forward|right|back|left)\b" },

        { TokenType.NUMBER,       @"\b\d+(\.\d+)?\b" },
        { TokenType.OPEN_BRACKET,  @"\[" },
        { TokenType.CLOSE_BRACKET, @"\]" },
        

        { TokenType.WHITESPACE,   @"\s+" },
        { TokenType.INVALID, @"[^a-zA-Z0-9\s\[\]]+" },
        { TokenType.UNKNOWN_WORD, @"\b\w+\b" },
    };

        private static readonly Regex combinedRegex;

        static Lexer()
        {
            string combined = string.Join("|",
                tokenPatterns.Select(kvp => $"(?<{kvp.Key}>{kvp.Value})"));
            combinedRegex = new Regex(combined, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();

            int line = 1;
            int column = 1;
            int globalIndex = 0;

            var matches = combinedRegex.Matches(input);

            foreach (Match match in matches)
            {
                TokenType matchedType;
                foreach (TokenType type in tokenPatterns.Keys)
                {
                    if (match.Groups[type.ToString()].Success)
                    {
                        matchedType = type;
                        string value = match.Value;
                        tokens.Add(new Token(matchedType, value, match.Index, line, column));

                        // Обновляем позицию
                        int newlines = value.Count(c => c == '\n');

                        if (newlines == 0)
                        {
                            column += value.Length;
                        }
                        else
                        {
                            line += newlines;
                            int lastNewline = value.LastIndexOf('\n');
                            column = value.Length - lastNewline;
                        }

                        globalIndex += value.Length;
                        break;
                    }
                }
            }

            return tokens;
        }
    }
}
