using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Scheduling.YamlScheduling.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ErsatzTV.Core.Scheduling.YamlScheduling.Handlers;

public class YamlPlayoutGraphicsOnHandler(IGraphicsElementRepository graphicsElementRepository) : IYamlPlayoutHandler
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    private readonly Dictionary<string, Option<GraphicsElement>> _graphicsElementCache = new();

    public bool Reset => false;

    public async Task<bool> Handle(
        YamlPlayoutContext context,
        YamlPlayoutInstruction instruction,
        PlayoutBuildMode mode,
        Func<string, Task> executeSequence,
        ILogger<SequentialPlayoutBuilder> logger,
        CancellationToken cancellationToken)
    {
        if (instruction is not YamlPlayoutGraphicsOnInstruction graphicsOn)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(graphicsOn.GraphicsOn))
        {
            return false;
        }

        foreach (GraphicsElement ge in await GetGraphicsElementByPath(graphicsOn.GraphicsOn, cancellationToken))
        {
            string variables = null;
            if (graphicsOn.Variables.Count > 0)
            {
                variables = JsonConvert.SerializeObject(graphicsOn.Variables, JsonSettings);
            }

            context.SetGraphicsElement(ge.Id, variables);
        }

        return true;
    }

    private async Task<Option<GraphicsElement>> GetGraphicsElementByPath(
        string path,
        CancellationToken cancellationToken)
    {
        if (_graphicsElementCache.TryGetValue(path, out Option<GraphicsElement> cachedGraphicsElement))
        {
            foreach (GraphicsElement graphicsElement in cachedGraphicsElement)
            {
                return graphicsElement;
            }
        }
        else
        {
            Option<GraphicsElement> maybeGraphicsElement =
                await graphicsElementRepository.GetGraphicsElementByPath(path, cancellationToken);
            _graphicsElementCache.Add(path, maybeGraphicsElement);
            foreach (GraphicsElement graphicsElement in maybeGraphicsElement)
            {
                return graphicsElement;
            }
        }

        return Option<GraphicsElement>.None;
    }
}
