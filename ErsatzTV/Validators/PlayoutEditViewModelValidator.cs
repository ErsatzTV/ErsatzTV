using ErsatzTV.ViewModels;
using FluentValidation;
using FluentValidation.Results;

namespace ErsatzTV.Validators;

public class PlayoutEditViewModelValidator : AbstractValidator<PlayoutEditViewModel>
{
    public PlayoutEditViewModelValidator()
    {
        RuleFor(p => p.Channel).NotNull();
        RuleFor(p => p.ProgramSchedule).NotNull().When(p => string.IsNullOrWhiteSpace(p.Kind));
        RuleFor(p => p.ExternalJsonFile).NotNull().When(p => p.Kind == PlayoutKind.ExternalJson);
    }

    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        ValidationResult result = await ValidateAsync(
            ValidationContext<PlayoutEditViewModel>.CreateWithOptions(
                (PlayoutEditViewModel)model,
                x => x.IncludeProperties(propertyName)));
        if (result.IsValid)
        {
            return [];
        }

        return result.Errors.Select(e => e.ErrorMessage);
    };
}
