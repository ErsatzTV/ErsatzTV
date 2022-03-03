using ErsatzTV.Core;
using ErsatzTV.Core.Emby;
using ErsatzTV.Core.Interfaces.Emby;
using Newtonsoft.Json;

namespace ErsatzTV.Infrastructure.Emby;

public class EmbySecretStore : IEmbySecretStore
{
    public Task<Unit> DeleteAll() => SaveSecrets(new EmbySecrets());

    public Task<EmbySecrets> ReadSecrets() =>
        File.ReadAllTextAsync(FileSystemLayout.EmbySecretsPath)
            .Map(JsonConvert.DeserializeObject<EmbySecrets>)
            .Map(s => Optional(s).IfNone(new EmbySecrets()));

    public Task<Unit> SaveSecrets(EmbySecrets embySecrets) =>
        Some(JsonConvert.SerializeObject(embySecrets)).Match(
            s => File.WriteAllTextAsync(FileSystemLayout.EmbySecretsPath, s).ToUnit(),
            Task.FromResult(Unit.Default));
}