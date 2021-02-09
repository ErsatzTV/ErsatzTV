using System.IO;
using ErsatzTV.ViewModels;
using FluentValidation;

namespace ErsatzTV.Validators
{
    public class LocalMediaSourceEditViewModelValidator : AbstractValidator<LocalMediaSourceEditViewModel>
    {
        public LocalMediaSourceEditViewModelValidator()
        {
            RuleFor(x => x.Folder).NotEmpty();
            RuleFor(x => x.Folder).Must(Directory.Exists).WithMessage("Folder must exist on filesystem");
        }
    }
}
