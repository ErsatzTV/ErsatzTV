using ErsatzTV.Core;
using ErsatzTV.Core.Interfaces.Jellyfin;
using ErsatzTV.Core.Jellyfin;
using Newtonsoft.Json;

namespace ErsatzTV.Infrastructure.Jellyfin;

public class JellyfinSecretStore : IJellyfinSecretStore
{
    public Task<Unit> DeleteAll() => SaveSecrets(new JellyfinSecrets());

    public Task<JellyfinSecrets> ReadSecrets() =>
        File.ReadAllTextAsync(FileSystemLayout.JellyfinSecretsPath)
            .Map(JsonConvert.DeserializeObject<JellyfinSecrets>)
            .Map(s => Optional(s).IfNone(new JellyfinSecrets()));

    public Task<Unit> SaveSecrets(JellyfinSecrets jellyfinSecrets) =>
        Some(JsonConvert.SerializeObject(jellyfinSecrets)).Match(
            s => File.WriteAllTextAsync(FileSystemLayout.JellyfinSecretsPath, s).ToUnit(),
            Task.FromResult(Unit.Default));
}