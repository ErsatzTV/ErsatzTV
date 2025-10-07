namespace ErsatzTV.Services.Validators;

public interface IEnvironmentValidator
{
    Task<bool> Validate();
}
