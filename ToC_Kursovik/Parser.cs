using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace ToC_Kursovik
{
    public class Parser
    {
        private readonly List<Token> tokens;
        private readonly List<Error> errors;

        public Parser(List<Token> tokens, List<Error> errors)
        {
            this.tokens = tokens;
            this.errors = errors;
        }

        public List<Error> Parse()
        {
            List<Error> errors = new List<Error>();
            foreach (List<Token> line in SplitTokensIntoLines(tokens))
            {
                RecursiveParser recursiveParser = new RecursiveParser(line);
                errors.AddRange(recursiveParser.Parse());
            }
            errors.AddRange(errors);
            return errors.OrderBy(e => e.Column).ToList();
        }


        private List<List<Token>> SplitTokensIntoLines(List<Token> tokens)
        {
            List<List<Token>> lines = new List<List<Token>>();
            List<Token> currentLine = new List<Token>();

            foreach (Token token in tokens)
            {
                currentLine.Add(token);

                if (token.Type == TokenType.CLOSE_BRACKET)
                {
                    lines.Add(currentLine);
                    currentLine = new List<Token>();
                }
            }

            if (currentLine.Count > 0)
            {
                lines.Add(currentLine);
            }

            return lines;
        }
    }


    public class RecursiveParser
    {
        private readonly List<Token> tokens;


        public RecursiveParser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        public List<Error> Parse()
        {
            int currentPosition = 0;
            List<Error> errors = new List<Error>();
            return Start(currentPosition, errors);
        }

        private List<Error> Start(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }
            //currentPosition = SkipInvalid(currentPosition, TokenType.REPEAT, errors);
            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return Start(currentPosition + 1, errors);
            if (!Match(currentPosition, TokenType.REPEAT, errors))
            {
                return GetMinErrorList(
                        SpaceAfterRepeat(currentPosition, CreateErrorList(currentPosition, TokenType.REPEAT, ErrorType.PUSH, errors)),
                        SpaceAfterRepeat(currentPosition + 1, CreateErrorList(currentPosition, TokenType.REPEAT, ErrorType.REPLACE, errors)),
                        Start(currentPosition + 1, CreateErrorList(currentPosition, TokenType.REPEAT, ErrorType.DELETE, errors))
                );
            }
            return SpaceAfterRepeat(currentPosition + 1, errors);
        }

        private List<Error> SpaceAfterRepeat(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }
            //currentPosition = SkipInvalid(currentPosition, TokenType.WHITESPACE, errors);
            if (!Match(currentPosition, TokenType.WHITESPACE, errors))
            {
                return GetMinErrorList(
                        NumberAfterRepeat(currentPosition + 1, CreateErrorList(currentPosition, TokenType.WHITESPACE, ErrorType.REPLACE, errors)),
                        SpaceAfterRepeat(currentPosition + 1, CreateErrorList(currentPosition, TokenType.WHITESPACE, ErrorType.DELETE, errors)),
                        NumberAfterRepeat(currentPosition, CreateErrorList(currentPosition, TokenType.WHITESPACE, ErrorType.PUSH, errors))
                );
            }
            return NumberAfterRepeat(currentPosition + 1, errors);
        }

        private List<Error> NumberAfterRepeat(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }
            //currentPosition = SkipInvalid(currentPosition, TokenType.NUMBER, errors);
            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return NumberAfterRepeat(currentPosition + 1, errors);
            if (!Match(currentPosition, TokenType.NUMBER, errors))
            {
                return GetMinErrorList(
                        OpenBracket(currentPosition, CreateErrorList(currentPosition, TokenType.NUMBER, ErrorType.PUSH, errors)),
                        OpenBracket(currentPosition + 1, CreateErrorList(currentPosition, TokenType.NUMBER, ErrorType.REPLACE, errors)),
                        NumberAfterRepeat(currentPosition + 1, CreateErrorList(currentPosition, TokenType.NUMBER, ErrorType.DELETE, errors))
                );
            }
            return OpenBracket(currentPosition + 1, errors);
        }

        private List<Error> OpenBracket(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }
            //currentPosition = SkipInvalid(currentPosition, TokenType.OPEN_BRACKET, errors);

            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return OpenBracket(currentPosition + 1, errors);

            if (!Match(currentPosition, TokenType.OPEN_BRACKET, errors))
            {
                return GetMinErrorList(
                        Command(currentPosition, CreateErrorList(currentPosition, TokenType.OPEN_BRACKET, ErrorType.PUSH, errors)),
                        Command(currentPosition + 1, CreateErrorList(currentPosition, TokenType.OPEN_BRACKET, ErrorType.REPLACE, errors)),
                        OpenBracket(currentPosition + 1, CreateErrorList(currentPosition, TokenType.OPEN_BRACKET, ErrorType.DELETE, errors))
                );
            }
            return Command(currentPosition + 1, errors);
        }

        private List<Error> Command(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }
            //currentPosition = SkipInvalid(currentPosition, TokenType.OPEN_BRACKET, errors);

            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return Command(currentPosition + 1, errors);

            if (!Match(currentPosition, TokenType.COMMAND, errors))
            {
                return GetMinErrorList(
                        SpaceAfterCommand(currentPosition, CreateErrorList(currentPosition, TokenType.COMMAND, ErrorType.PUSH, errors)),
                        SpaceAfterCommand(currentPosition + 1, CreateErrorList(currentPosition, TokenType.COMMAND, ErrorType.REPLACE, errors)),
                        Command(currentPosition + 1, CreateErrorList(currentPosition, TokenType.COMMAND, ErrorType.DELETE, errors))
                );
            }
            return SpaceAfterCommand(currentPosition + 1, errors);
        }

        private List<Error> SpaceAfterCommand(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }
            //currentPosition = SkipInvalid(currentPosition, TokenType.WHITESPACE, errors);
            if (!Match(currentPosition, TokenType.WHITESPACE, errors))
            {
                return GetMinErrorList(
                        NumberAfterCommand(currentPosition + 1, CreateErrorList(currentPosition, TokenType.WHITESPACE, ErrorType.REPLACE, errors)),
                        SpaceAfterCommand(currentPosition + 1, CreateErrorList(currentPosition, TokenType.WHITESPACE, ErrorType.DELETE, errors)),
                        NumberAfterCommand(currentPosition, CreateErrorList(currentPosition, TokenType.WHITESPACE, ErrorType.PUSH, errors))
                );
            }
            return NumberAfterCommand(currentPosition + 1, errors);
        }

        private List<Error> NumberAfterCommand(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }
            //currentPosition = SkipInvalid(currentPosition, TokenType.NUMBER, errors);
            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return NumberAfterCommand(currentPosition + 1, errors);
            if (!Match(currentPosition, TokenType.NUMBER, errors))
            {
                return GetMinErrorList(
                        CloseBracketOrCommand(currentPosition, CreateErrorList(currentPosition, TokenType.NUMBER, ErrorType.PUSH, errors)),
                        CloseBracketOrCommand(currentPosition + 1, CreateErrorList(currentPosition, TokenType.NUMBER, ErrorType.REPLACE, errors)),
                        NumberAfterCommand(currentPosition + 1, CreateErrorList(currentPosition, TokenType.NUMBER, ErrorType.DELETE, errors))
                );
            }
            return CloseBracketOrCommand(currentPosition + 1, errors);
        }

        private List<Error> CloseBracketOrCommand(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return errors;
            }
            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return CloseBracketOrCommand(currentPosition + 1, errors);
            if (Match(currentPosition, TokenType.COMMAND, errors))
            {
                return SpaceAfterCommand(currentPosition + 1, errors);
            }
            else if (Match(currentPosition, TokenType.CLOSE_BRACKET, errors))
            {
                return End(currentPosition + 1, errors);
            }
            //if (!Match(currentPosition, TokenType.COMMAND, errors))
            //{
            //    return GetMinErrorList(
            //            SpaceAfterCommand(currentPosition, CreateErrorList(currentPosition, TokenType.COMMAND, ErrorType.PUSH, errors)),
            //            SpaceAfterCommand(currentPosition + 1, CreateErrorList(currentPosition, TokenType.COMMAND, ErrorType.REPLACE, errors)),
            //            CloseBracketOrCommand(currentPosition + 1, CreateErrorList(currentPosition, TokenType.COMMAND, ErrorType.DELETE, errors))
            //    );
            //}
            //else if (!Match(currentPosition, TokenType.CLOSE_BRACKET, errors))
            //{
            //    return GetMinErrorList(
            //            End(currentPosition, CreateErrorList(currentPosition, TokenType.CLOSE_BRACKET, ErrorType.PUSH, errors)),
            //            End(currentPosition + 1, CreateErrorList(currentPosition, TokenType.CLOSE_BRACKET, ErrorType.REPLACE, errors)),
            //            CloseBracketOrCommand(currentPosition + 1, CreateErrorList(currentPosition, TokenType.CLOSE_BRACKET, ErrorType.DELETE, errors))
            //    );
            //}
            return End(currentPosition + 1, errors);
        }

        private List<Error> End(int currentPosition, List<Error> errors)
        {

            if (IsAtEnd(currentPosition))
            {
                errors.Add(
                        new Error(
                                CreateErrorMessage(currentPosition, TokenType.CLOSE_BRACKET, ErrorType.PUSH),
                                GetToken(currentPosition - 1).Line,
                                GetToken(currentPosition - 1).Column
                        )
                );
                return errors;
            }
            if (Match(currentPosition, TokenType.WHITESPACE, errors))
                return End(currentPosition + 1, errors);
            if (!Match(currentPosition, TokenType.CLOSE_BRACKET, errors))
            {
                AddError(currentPosition, TokenType.CLOSE_BRACKET, ErrorType.DELETE, errors);
                End(currentPosition + 1, errors);
            }
            return errors;
        }


        private bool Match(int currentPosition, TokenType expectedTokentype, List<Error> errors)
        {
            return Check(currentPosition, expectedTokentype, errors);
        }

        private bool Check(int currentPosition, TokenType expectedTokenType, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                return false;
            }
            else
            {
                return GetToken(currentPosition).Type == expectedTokenType;
            }
        }

        private int SkipInvalid(int currentPosition, TokenType expectedTokentype, List<Error> errors)
        {
            while (!IsAtEnd(currentPosition) && GetToken(currentPosition).Type == TokenType.INVALID)
            {
                AddError(currentPosition, expectedTokentype, ErrorType.DELETE, errors);
                currentPosition++;
            }
            return currentPosition;
        }

        private List<Error> CreateErrorList(
                int currentPosition,
                TokenType expectedTokenType,
                ErrorType errorType,
                List<Error> errorEntities
        )
        {
            List<Error> errors = new List<Error>(errorEntities);
            AddError(currentPosition, expectedTokenType, errorType, errors);
            return errors;
        }

        private List<Token> GetSubList(List<Token> tokens, int startPosition, int endPosition)
        {
            List<Token> subTokens = new List<Token>();
            for (int i = startPosition; i < endPosition; i++)
            {
                subTokens.Add(tokens[i]);
            }
            return subTokens;
        }


        private List<Error> GetMinErrorList(List<Error> e1, List<Error> e2, List<Error> e3)
        {
            if (e1.Count <= e2.Count && e1.Count <= e3.Count)
            {
                return e1;
            }
            else if (e2.Count <= e1.Count && e2.Count <= e3.Count)
            {
                return e2;
            }
            else
            {
                return e3;
            }
        }


        private Token GetToken(int currentPosition)
        {
            return tokens[currentPosition];
        }


        private bool IsAtEnd(int currentPosition)
        {
            return currentPosition >= tokens.Count;
        }

        private void AddError(int currentPosition, TokenType expectedTokentype, ErrorType errorType, List<Error> errors)
        {
            errors.Add(
                    new Error(
                            CreateErrorMessage(currentPosition, expectedTokentype, errorType),
                            GetToken(currentPosition).Line,
                            GetToken(currentPosition).Column
                    )
            );
        }

        private string CreateErrorMessage(int currentPosition, TokenType expectedTokenType, ErrorType errorType)
        {
            var actualToken = GetToken(currentPosition);

            return errorType switch
            {
                ErrorType.DELETE or ErrorType.DELETE_END =>
                    $"{errorType.GetDescription()}: '{GetTokenTypeDescription(actualToken.Type)}'",

                ErrorType.PUSH =>
                    $"{errorType.GetDescription()}: '{GetTokenTypeDescription(expectedTokenType)}'",

                ErrorType.REPLACE =>
                    $"Ожидалось: '{GetTokenTypeDescription(expectedTokenType)}' Фактически: '{GetTokenTypeDescription(actualToken.Type)}'",

                _ => string.Empty
            };
        }

        private string GetTokenTypeDescription(TokenType type)
        {
            return type switch
            {
                TokenType.REPEAT => "repeat",
                TokenType.COMMAND => "команда",
                TokenType.NUMBER => "число",
                TokenType.OPEN_BRACKET => "[",
                TokenType.CLOSE_BRACKET => "]",
                TokenType.UNKNOWN_WORD => "неизвестное слово",
                TokenType.INVALID => "невалидный токен",
                TokenType.WHITESPACE => "пробел",
                _ => type.ToString()
            };
        }
    }
}
