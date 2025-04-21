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
        { TokenType.INVALID,      @"[^a-zA-Z0-9\s\[\]]+" },
        { TokenType.UNKNOWN_WORD, @"\b\w+\b" },
    };

        private static readonly Regex combinedRegex;

        public List<Error> Errors { get; } = new();

        static Lexer()
        {
            string combined = string.Join("|",
                tokenPatterns.Select(kvp => $"(?<{kvp.Key}>{kvp.Value})"));
            combinedRegex = new Regex(combined, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            var buffer = "";
            int bufferLine = 1;
            int bufferColumn = 1;

            int line = 1;
            int column = 1;

            var matches = combinedRegex.Matches(input);

            foreach (Match match in matches)
            {
                TokenType matchedType = TokenType.INVALID;
                string value = match.Value;

                foreach (var type in tokenPatterns.Keys)
                {
                    if (match.Groups[type.ToString()].Success)
                    {
                        matchedType = type;
                        break;
                    }
                }

                int newlines = value.Count(c => c == '\n');
                int currentLine = line;
                int currentColumn = column;

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

                if (matchedType == TokenType.INVALID)
                {
                    Errors.Add(new Error($"Невалидная последовательность символов: {match.Value}", currentLine, currentColumn));
                    continue;
                }

                //if (matchedType == TokenType.WHITESPACE)
                //{
                //    FlushBuffer();
                //    continue;
                //}

                if (matchedType == TokenType.UNKNOWN_WORD)
                {
                    if (string.IsNullOrEmpty(buffer))
                    {
                        buffer = value;
                        bufferLine = currentLine;
                        bufferColumn = currentColumn;
                    }
                    else
                    {
                        buffer += value;
                    }
                }
                else
                {
                    FlushBuffer();
                    tokens.Add(new Token(matchedType, value, match.Index, currentLine, currentColumn));
                }
            }

            FlushBuffer();

            return tokens;

            void FlushBuffer()
            {
                if (!string.IsNullOrEmpty(buffer))
                {
                    /*---------------*/
                    //string[] knownKeywords = { "repeat", "right", "left", "forward", "back" };

                    //foreach (var keyword in knownKeywords)
                    //{
                    //    var keywordRegex = new Regex($"^({keyword})(\\d+)$");
                    //    var match = keywordRegex.Match(buffer);
                    //    if (match.Success)
                    //    {
                    //        tokens.Add(new Token(GetTokenType(keyword), match.Groups[1].Value, -1, bufferLine, bufferColumn));
                    //        tokens.Add(new Token(TokenType.NUMBER, match.Groups[2].Value, -1, bufferLine, bufferColumn + match.Groups[1].Value.Length));
                    //        buffer = "";
                    //        return;
                    //    }
                    //}
                    /*-----------------------*/
                    // Повторный прогон склеенной строки
                    var recheckMatches = combinedRegex.Matches(buffer);

                    foreach (Match match in recheckMatches)
                    {
                        foreach (var type in tokenPatterns.Keys)
                        {
                            if (type == TokenType.INVALID || type == TokenType.WHITESPACE)
                                continue;

                            if (match.Groups[type.ToString()].Success)
                            {
                                tokens.Add(new Token(type, match.Value, -1, bufferLine, bufferColumn));
                                break;
                            }
                        }
                    }

                    buffer = "";
                }

            }

        }
        private TokenType GetTokenType(string keyword)
        {
            return keyword switch
            {
                "repeat" => TokenType.REPEAT,
                "right" or "left" or "forward" or "back" => TokenType.COMMAND,
                _ => TokenType.UNKNOWN_WORD
            };
        }
    }


    //public class Lexer
    //{
    //    private static readonly Dictionary<TokenType, string> tokenPatterns = new()
    //{
    //    { TokenType.REPEAT,       @"\brepeat\b" },
    //    { TokenType.COMMAND,      @"\b(forward|right|back|left)\b" },

    //    { TokenType.NUMBER,       @"\b\d+(\.\d+)?\b" },
    //    { TokenType.OPEN_BRACKET,  @"\[" },
    //    { TokenType.CLOSE_BRACKET, @"\]" },


    //    { TokenType.WHITESPACE,   @"\s+" },
    //    { TokenType.INVALID, @"[^a-zA-Z0-9\s\[\]]+" },
    //    { TokenType.UNKNOWN_WORD, @"\b\w+\b" },
    //};

    //    private static readonly Regex combinedRegex;

    //    private List<Token> Errors = new List<Token>();
    //    static Lexer()
    //    {
    //        string combined = string.Join("|",
    //            tokenPatterns.Select(kvp => $"(?<{kvp.Key}>{kvp.Value})"));
    //        combinedRegex = new Regex(combined, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    //    }



    //    public List<Token> Tokenize(string input)
    //    {
    //        var tokens = new List<Token>();

    //        int line = 1;
    //        int column = 1;
    //        int globalIndex = 0;

    //        var matches = combinedRegex.Matches(input);

    //        foreach (Match match in matches)
    //        {
    //            TokenType matchedType;
    //            foreach (TokenType type in tokenPatterns.Keys)
    //            {
    //                if (match.Groups[type.ToString()].Success)
    //                {
    //                    matchedType = type;
    //                    string value = match.Value;
    //                    tokens.Add(new Token(matchedType, value, match.Index, line, column));

    //                    // Обновляем позицию
    //                    int newlines = value.Count(c => c == '\n');

    //                    if (newlines == 0)
    //                    {
    //                        column += value.Length;
    //                    }
    //                    else
    //                    {
    //                        line += newlines;
    //                        int lastNewline = value.LastIndexOf('\n');
    //                        column = value.Length - lastNewline;
    //                    }

    //                    globalIndex += value.Length;
    //                    break;
    //                }
    //            }
    //        }

    //        return tokens;
    //    }
    //}
}
