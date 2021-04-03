using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Extensions
{
    [SuppressMessage("ReSharper", "VSTHRD003")]
    public static class EitherToActionResult
    {
        public static Task<IActionResult> ToActionResult<TL, TR>(this Task<Either<TL, TR>> either) => either.Map(Match);

        public static Task<IActionResult> ToActionResult(this Task<Either<Error, Task>> either) => either.Bind(Match);

        private static IActionResult Match<TL, TR>(this Either<TL, TR> either) =>
            either.Match<IActionResult>(
                Left: l => new BadRequestObjectResult(l),
                Right: r => new OkObjectResult(r));

        private static Task<IActionResult> Match(Either<Error, Task> either) =>
            either.Match<Task<IActionResult>>(
                async t =>
                {
                    await t;
                    return new OkResult();
                },
                e => Task.FromResult((IActionResult) new BadRequestObjectResult(e)));
    }
}
