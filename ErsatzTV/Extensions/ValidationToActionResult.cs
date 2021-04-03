using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using ErsatzTV.Core;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Extensions
{
    [SuppressMessage("ReSharper", "VSTHRD003")]
    public static class ValidationToActionResult
    {
        public static IActionResult ToActionResult<T>(this Validation<BaseError, T> validation) =>
            validation.Match<IActionResult>(
                t => new OkObjectResult(t),
                e => new BadRequestObjectResult(e));

        public static Task<IActionResult> ToActionResult<T>(this Task<Validation<BaseError, T>> validation) =>
            validation.Map(ToActionResult);

        public static Task<IActionResult> ToActionResult(this Task<Validation<BaseError, Task>> validation) =>
            validation.Bind(ToActionResult);

        private static Task<IActionResult> ToActionResult(Validation<BaseError, Task> validation) =>
            validation.MatchAsync<IActionResult>(
                async t =>
                {
                    await t;
                    return new OkResult();
                },
                e => new BadRequestObjectResult(e));
    }
}
