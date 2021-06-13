namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public record UpdateFFmpegProfileResult(int FFmpegProfileId) : EntityIdResult(FFmpegProfileId);
}
