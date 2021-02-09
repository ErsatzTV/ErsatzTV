using System.Threading.Tasks;
using LanguageExt;

namespace ErsatzTV.Core
{
    public static class LanguageExtensions
    {
        public static Either<BaseError, TR> ToEither<TR>(this Validation<BaseError, TR> validation) =>
            validation.ToEither().MapLeft(errors => errors.Join());

        public static Task<Either<BaseError, TR>> ToEitherAsync<TR>(this Validation<BaseError, Task<TR>> validation) =>
            validation.ToEither()
                .MapLeft(errors => errors.Join())
                .MapAsync<BaseError, Task<TR>, TR>(e => e);
    }
}
