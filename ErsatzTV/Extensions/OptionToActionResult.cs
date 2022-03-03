using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Extensions;

[SuppressMessage("ReSharper", "VSTHRD003")]
public static class OptionToActionResult
{
    public static IActionResult ToActionResult<T>(this Option<T> option) =>
        option.Match<IActionResult>(
            t => new OkObjectResult(t),
            () => new NotFoundResult());

    public static Task<IActionResult> ToActionResult<T>(this Task<Option<T>> option) => option.Map(ToActionResult);
}