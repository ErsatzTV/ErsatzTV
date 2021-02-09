using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Extensions
{
    public static class ListToActionResult
    {
        public static Task<IActionResult> ToActionResult<T>(this Task<List<T>> list) =>
            list.Map<List<T>, IActionResult>(l => new OkObjectResult(l));
    }
}
