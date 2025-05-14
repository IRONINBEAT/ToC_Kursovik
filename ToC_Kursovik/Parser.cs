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

            List<Error> errors = [.. this.errors];
            foreach (List<Token> line in SplitTokens(tokens))
            {
                RecursiveDescentParser recursiveDescentParser = new RecursiveDescentParser(line);
                errors.AddRange(recursiveDescentParser.Parse());
            }
            

            return errors
                .OrderBy(e => e.Line)
                .ThenBy(e => e.Column)
                .ToList();
        }


        private List<List<Token>> SplitTokens(List<Token> tokens)
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


    public class RecursiveDescentParser
    {
        private readonly List<Token> tokens;


        public RecursiveDescentParser(List<Token> tokens)
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

            if (IsMatch(currentPosition, TokenType.WHITESPACE, errors))
                return Start(currentPosition + 1, errors);
            
            if (!IsMatch(currentPosition, TokenType.REPEAT, errors))
            {
                return GetMinListOfErrors(
                        SpaceAfterRepeat(currentPosition, CreateListOfErrors(currentPosition, TokenType.REPEAT, ErrorType.PUSH, errors)),
                        SpaceAfterRepeat(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.REPEAT, ErrorType.REPLACE, errors)),
                        Start(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.REPEAT, ErrorType.DELETE, errors))
                );
            }

            return SpaceAfterRepeat(currentPosition + 1, errors);
        }

        private List<Error> SpaceAfterRepeat(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                if (!IsMatch(currentPosition, TokenType.WHITESPACE, errors))
                {
                    AddError(currentPosition - 1, TokenType.WHITESPACE, ErrorType.PUSH, errors);
                }
                return errors;
            }

            if (!IsMatch(currentPosition, TokenType.WHITESPACE, errors))
            {
                return GetMinListOfErrors(
                        NumberAfterRepeat(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.WHITESPACE, ErrorType.REPLACE, errors)),
                        SpaceAfterRepeat(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.WHITESPACE, ErrorType.DELETE, errors)),
                        NumberAfterRepeat(currentPosition, CreateListOfErrors(currentPosition, TokenType.WHITESPACE, ErrorType.PUSH, errors))
                );
            }
            return NumberAfterRepeat(currentPosition + 1, errors);
        }

        private List<Error> NumberAfterRepeat(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                if (!IsMatch(currentPosition, TokenType.NUMBER, errors))
                {
                    AddError(currentPosition - 1, TokenType.NUMBER, ErrorType.PUSH, errors);
                }
                return errors;
            }

            if (IsMatch(currentPosition, TokenType.WHITESPACE, errors))
                return NumberAfterRepeat(currentPosition + 1, errors);
            if (!IsMatch(currentPosition, TokenType.NUMBER, errors))
            {
                return GetMinListOfErrors(
                        OpenBracket(currentPosition, CreateListOfErrors(currentPosition, TokenType.NUMBER, ErrorType.PUSH, errors)),
                        OpenBracket(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.NUMBER, ErrorType.REPLACE, errors)),
                        NumberAfterRepeat(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.NUMBER, ErrorType.DELETE, errors))
                );
            }
            return OpenBracket(currentPosition + 1, errors);
        }

        private List<Error> OpenBracket(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                if (!IsMatch(currentPosition, TokenType.OPEN_BRACKET, errors))
                {
                    AddError(currentPosition - 1, TokenType.OPEN_BRACKET, ErrorType.PUSH, errors);
                }
                return errors;
            }


            if (IsMatch(currentPosition, TokenType.WHITESPACE, errors))
                return OpenBracket(currentPosition + 1, errors);

            if (!IsMatch(currentPosition, TokenType.OPEN_BRACKET, errors))
            {
                return GetMinListOfErrors(
                        Command(currentPosition, CreateListOfErrors(currentPosition, TokenType.OPEN_BRACKET, ErrorType.PUSH, errors)),
                        Command(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.OPEN_BRACKET, ErrorType.REPLACE, errors)),
                        OpenBracket(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.OPEN_BRACKET, ErrorType.DELETE, errors))
                );
            }
            return Command(currentPosition + 1, errors);
        }

        private List<Error> Command(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                if (!IsMatch(currentPosition - 1, TokenType.CLOSE_BRACKET, errors))
                {
                    AddError(currentPosition - 1, TokenType.CLOSE_BRACKET, ErrorType.PUSH, errors);
                }
                return errors;
            }


            if (IsMatch(currentPosition, TokenType.WHITESPACE, errors))
                return Command(currentPosition + 1, errors);

            if (!IsMatch(currentPosition, TokenType.COMMAND, errors))
            {
                return GetMinListOfErrors(
                        SpaceAfterCommand(currentPosition, CreateListOfErrors(currentPosition, TokenType.COMMAND, ErrorType.PUSH, errors)),
                        SpaceAfterCommand(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.COMMAND, ErrorType.REPLACE, errors)),
                        Command(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.COMMAND, ErrorType.DELETE, errors))
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

            if (!IsMatch(currentPosition, TokenType.WHITESPACE, errors))
            {
                return GetMinListOfErrors(
                        NumberAfterCommand(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.WHITESPACE, ErrorType.REPLACE, errors)),
                        SpaceAfterCommand(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.WHITESPACE, ErrorType.DELETE, errors)),
                        NumberAfterCommand(currentPosition, CreateListOfErrors(currentPosition, TokenType.WHITESPACE, ErrorType.PUSH, errors))
                );
            }
            return NumberAfterCommand(currentPosition + 1, errors);
        }
        
        private List<Error> NumberAfterCommand(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                if (!IsMatch(currentPosition - 1, TokenType.CLOSE_BRACKET, errors))
                {
                    AddError(currentPosition - 1, TokenType.CLOSE_BRACKET, ErrorType.PUSH, errors);
                }
                
                return errors;
            }

            if (IsMatch(currentPosition, TokenType.WHITESPACE, errors))
                return NumberAfterCommand(currentPosition + 1, errors);
            if (!IsMatch(currentPosition, TokenType.NUMBER, errors))
            {
                return GetMinListOfErrors(
                        CloseBracketOrCommand(currentPosition, CreateListOfErrors(currentPosition, TokenType.NUMBER, ErrorType.PUSH, errors)),
                        CloseBracketOrCommand(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.NUMBER, ErrorType.REPLACE, errors)),
                        NumberAfterCommand(currentPosition + 1, CreateListOfErrors(currentPosition, TokenType.NUMBER, ErrorType.DELETE, errors))
                );
            }
            return CloseBracketOrCommand(currentPosition + 1, errors);
        }



        private List<Error> CloseBracketOrCommand(int currentPosition, List<Error> errors)
        {
            if (IsAtEnd(currentPosition))
            {
                if (!IsMatch(currentPosition - 1, TokenType.CLOSE_BRACKET, errors))
                {
                    AddError(currentPosition - 1, TokenType.CLOSE_BRACKET, ErrorType.PUSH, errors);
                }
                return errors;
            }

            if (IsMatch(currentPosition, TokenType.WHITESPACE, errors))
                return CloseBracketOrCommand(currentPosition + 1, errors);

            if (IsMatch(currentPosition, TokenType.COMMAND, errors))
                return SpaceAfterCommand(currentPosition + 1, errors);

            else if (IsMatch(currentPosition, TokenType.UNKNOWN_WORD, errors) || IsMatch(currentPosition, TokenType.NUMBER, errors) || IsMatch(currentPosition, TokenType.OPEN_BRACKET, errors))
            {
                if (IsMatch(currentPosition, TokenType.NUMBER, errors))

                {
                    AddError(currentPosition, TokenType.NUMBER, ErrorType.DELETE, errors);
                    return CloseBracketOrCommand(currentPosition + 1, errors);
                }
                else if (IsMatch(currentPosition, TokenType.OPEN_BRACKET, errors))

                {
                    AddError(currentPosition, TokenType.OPEN_BRACKET, ErrorType.DELETE, errors);
                    return CloseBracketOrCommand(currentPosition + 1, errors);
                }
                else
                {
                    AddError(currentPosition, TokenType.UNKNOWN_WORD, ErrorType.DELETE, errors);
                    return CloseBracketOrCommand(currentPosition + 1, errors);
                }
                
            }

            return End(currentPosition + 1, errors);

        }



        private List<Error> End(int currentPosition, List<Error> errors)
        {

            if (IsAtEnd(currentPosition))
            {
                if (!IsMatch(currentPosition - 1, TokenType.CLOSE_BRACKET, errors))
                {
                    AddError(currentPosition - 1, TokenType.CLOSE_BRACKET, ErrorType.PUSH, errors);
                }
                return errors;
            }
            if (IsMatch(currentPosition, TokenType.WHITESPACE, errors))
                return End(currentPosition + 1, errors);
            if (!IsMatch(currentPosition, TokenType.CLOSE_BRACKET, errors))
            {
                AddError(currentPosition, TokenType.CLOSE_BRACKET, ErrorType.DELETE, errors);
                End(currentPosition + 1, errors);
            }
            return errors;
        }


        private bool IsMatch(int currentPosition, TokenType expectedTokentype, List<Error> errors)
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

        private List<Error> CreateListOfErrors(
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

        private List<Error> GetMinListOfErrors(List<Error> e1, List<Error> e2, List<Error> e3)
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

        //private void AddError(int currentPosition, TokenType expectedTokentype, ErrorType errorType, List<Error> errors)
        //{
        //    if (currentPosition <= 0)
        //    {
        //        errors.Add(
        //            new Error(
        //                    CreateErrorMessage(currentPosition, expectedTokentype, errorType),
        //                    GetToken(currentPosition).Line,
        //                    GetToken(currentPosition).Column
        //            )
        //    );
        //    }
        //    else
        //    {
        //        errors.Add(
        //            new Error(
        //                    CreateErrorMessage(currentPosition, expectedTokentype, errorType),
        //                    GetToken(currentPosition - 1).Line,
        //                    GetToken(currentPosition).Column
        //            )
        //    );
        //    }

        //}

        private void AddError(int currentPosition, TokenType expectedTokenType, ErrorType errorType, List<Error> errors)
        {
            int line = 1;
            int column = 1;

            if (errorType == ErrorType.PUSH || errorType == ErrorType.REPLACE)
            {
                if (currentPosition == 0 && tokens.Count > 0)
                {
                    //var token = tokens[0];
                    //line = token.Line;
                    //column = token.Column;
                    var currToken = tokens[currentPosition];
                    line = currToken.Line;

                    column = currToken.Column + currToken.Value.Length;
                }



                else if (currentPosition > 0 && currentPosition <= tokens.Count)
                {
                //var currToken = tokens[currentPosition];

                    if (expectedTokenType == TokenType.CLOSE_BRACKET)
                    {
                        var prevToken = tokens[currentPosition - 1];
                        var currToken = tokens[currentPosition];
                        line = currToken.Line;

                        column = currToken.Column + currToken.Value.Length;

                    }
                    else
                    {
                        var prevToken = tokens[currentPosition - 1];
                        line = prevToken.Line;

                        column = prevToken.Column + 1;
                    }
                    

                    // Найдём начало строки (первый токен на этой строке)
                    //int lineStartIndex = currentPosition - 1; 
                    //while (lineStartIndex > 0 && tokens[lineStartIndex - 1].Line == line)
                    //    lineStartIndex--;

                    //// Теперь суммируем длины всех токенов на этой строке до текущего
                    //int columnOffset = 0;
                    //for (int i = lineStartIndex; i <= currentPosition; i++)
                    //{
                    //    columnOffset += tokens[i].Value.Length;
                    //}

                    //column = columnOffset + 1;
                }
                //else if (tokens.Count > 0)
                //{
                //    var last = tokens[^1];
                //    line = last.Line;
                //    column = last.Column + last.Value.Length;
                //}
            }
            else if (currentPosition < tokens.Count)
            {
                var token = tokens[currentPosition];
                line = token.Line;
                column = token.Column;
            }

            errors.Add(new Error(
                CreateErrorMessage(currentPosition, expectedTokenType, errorType),
                line,
                column
            ));
        }

        //private void AddError(int currentPosition, TokenType expectedTokenType, ErrorType errorType, List<Error> errors)
        //{
        //    int line = 1;
        //    int column = 1;

        //    if (errorType == ErrorType.PUSH)
        //    {
        //        if (currentPosition == 0 && tokens.Count > 0)
        //        {
        //            var token = tokens[0];
        //            line = token.Line;
        //            column = token.Column;
        //        }
        //        else if (currentPosition > 0 && currentPosition < tokens.Count)
        //        {
        //            var prevToken = tokens[currentPosition - 1];
        //            var nextToken = tokens[currentPosition];

        //            if (prevToken.Line == nextToken.Line)
        //            {
        //                line = nextToken.Line;
        //                column = nextToken.Column;
        //            }
        //            else
        //            {
        //                line = prevToken.Line;
        //                column = prevToken.Column + prevToken.Value.Length;
        //            }
        //        }
        //        else if (currentPosition == tokens.Count && tokens.Count > 0)
        //        {
        //            var last = tokens[^1];
        //            line = last.Line;
        //            column = last.Column + last.Value.Length;
        //        }
        //    }
        //    else if (currentPosition < tokens.Count)
        //    {
        //        var token = tokens[currentPosition];
        //        line = token.Line;
        //        column = token.Column;
        //    }

        //    errors.Add(new Error(
        //        CreateErrorMessage(currentPosition, expectedTokenType, errorType),
        //        line,
        //        column
        //    ));
        //}

        private string CreateErrorMessage(int currentPosition, TokenType expectedTokenType, ErrorType errorType)
        {
            var actualToken = GetToken(currentPosition);

            return errorType switch
            {
                ErrorType.DELETE or ErrorType.DELETE_END =>
                    $"{errorType.GetDescription()}: '{actualToken.Value}'",

                ErrorType.PUSH =>
                    $"{errorType.GetDescription()}: '{GetDescriptionOfTokenType(expectedTokenType)}'",

                ErrorType.REPLACE =>
                    $"Ожидалось: '{GetDescriptionOfTokenType(expectedTokenType)}' Фактически: '{actualToken.Value}'",

                _ => string.Empty
            };
        }

        private string GetDescriptionOfTokenType(TokenType type)
        {
            return type switch
            {
                TokenType.REPEAT => "repeat",
                TokenType.COMMAND => "forward | right | back | left",
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
