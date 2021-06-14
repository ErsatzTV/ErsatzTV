namespace ErsatzTV.Application.FFmpegProfiles.Commands
{
    public record CreateFFmpegProfileResult(int FFmpegProfileId) : EntityIdResult(FFmpegProfileId);
}
