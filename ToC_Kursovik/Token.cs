using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToC_Kursovik
{
    public enum TokenType
    {
        REPEAT,
        COMMAND,

        NUMBER,
        OPEN_BRACKET,    // [
        CLOSE_BRACKET,   // ]

        UNKNOWN_WORD,
        INVALID,
        WHITESPACE
    }
    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int GlobalPosition { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, string value, int globalPosition, int line, int column)
        {
            Type = type;
            Value = value;
            GlobalPosition = globalPosition;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Type}: '{Value}' at Line {Line}, Column {Column}";
        }
    }
}
