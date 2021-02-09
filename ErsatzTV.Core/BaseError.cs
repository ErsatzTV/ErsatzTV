using LanguageExt;

namespace ErsatzTV.Core
{
    public class BaseError : NewType<BaseError, string>
    {
        public BaseError(string value) : base(value)
        {
        }

        public static implicit operator BaseError(string str) => New(str);
    }

    public static class ErrorExtensions
    {
        public static BaseError Join(this Seq<BaseError> errors) => string.Join("; ", errors);
    }
}
