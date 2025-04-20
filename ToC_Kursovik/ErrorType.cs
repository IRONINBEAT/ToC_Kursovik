using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToC_Kursovik
{
    public enum ErrorType
    {
        REPLACE,
        DELETE,
        PUSH,
        DELETE_END
    }

    public static class ErrorTypeExtensions
    {
        public static string GetDescription(this ErrorType errorType)
        {
            return errorType switch
            {
                ErrorType.REPLACE => "Заменить",
                ErrorType.DELETE => "Удалить",
                ErrorType.PUSH => "Вставить",
                ErrorType.DELETE_END => "Удалить",
                _ => throw new ArgumentOutOfRangeException(nameof(errorType), errorType, null)
            };
        }
    }
}
