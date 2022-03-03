using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ErsatzTV.Core;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using LanguageExt;
using MediatR;

namespace ErsatzTV.Application.Streaming;

public class GetLastPtsDurationHandler : IRequestHandler<GetLastPtsDuration, Either<BaseError, PtsAndDuration>>
{
    private readonly IConfigElementRepository _configElementRepository;

    public GetLastPtsDurationHandler(IConfigElementRepository configElementRepository)
    {
        _configElementRepository = configElementRepository;
    }

    public async Task<Either<BaseError, PtsAndDuration>> Handle(
        GetLastPtsDuration request,
        CancellationToken cancellationToken)
    {
        Validation<BaseError, RequestParameters> validation = await Validate(request);
        return await validation.Match(
            Handle,
            error => Task.FromResult<Either<BaseError, PtsAndDuration>>(error.Join()));
    }

    private async Task<Validation<BaseError, RequestParameters>> Validate(GetLastPtsDuration request) =>
        await ValidateFFprobePath()
            .MapT(
                ffprobePath => new RequestParameters(
                    request.FileName,
                    ffprobePath));

    private async Task<Either<BaseError, PtsAndDuration>> Handle(RequestParameters parameters)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = parameters.FFprobePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("0");
        startInfo.ArgumentList.Add("-show_entries");
        startInfo.ArgumentList.Add("packet=pts,duration");
        startInfo.ArgumentList.Add("-of");
        startInfo.ArgumentList.Add("compact=p=0:nk=1");
        startInfo.ArgumentList.Add("-read_intervals");
        startInfo.ArgumentList.Add("-999999");
        startInfo.ArgumentList.Add(parameters.FileName);

        var probe = new Process
        {
            StartInfo = startInfo
        };

        probe.Start();
        return await probe.StandardOutput.ReadToEndAsync().MapAsync<string, Either<BaseError, PtsAndDuration>>(
            async output =>
            {
                await probe.WaitForExitAsync();
                return probe.ExitCode == 0
                    ? PtsAndDuration.From(output.Split("\n").Filter(s => !string.IsNullOrWhiteSpace(s)).Last().Trim())
                    : BaseError.New($"FFprobe at {parameters.FFprobePath} exited with code {probe.ExitCode}");
            });
    }

    private Task<Validation<BaseError, string>> ValidateFFprobePath() =>
        _configElementRepository.GetValue<string>(ConfigElementKey.FFprobePath)
            .FilterT(File.Exists)
            .Map(
                ffprobePath =>
                    ffprobePath.ToValidation<BaseError>("FFprobe path does not exist on the file system"));

    private record RequestParameters(string FileName, string FFprobePath);
}
