using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ErsatzTV.Filters;

public class V2ApiActionFilter : ActionFilterAttribute
{
    private static readonly Lazy<bool> UseV2Ui =
        new(() => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ETV_V2_UI"))); 
    
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!UseV2Ui.Value)
        {
            context.Result = new NotFoundResult();
        }
    }
}
