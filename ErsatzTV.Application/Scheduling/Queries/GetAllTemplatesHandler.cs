using ErsatzTV.Core.Domain.Scheduling;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Scheduling;

public class GetAllTemplatesHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAllTemplates, List<TemplateViewModel>>
{
    public async Task<List<TemplateViewModel>> Handle(GetAllTemplates request, CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Template> templates = await dbContext.Templates
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        List<TemplateGroup> templateGroups = await dbContext.TemplateGroups
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var unusedTemplateGroups = templateGroups.ToList();

        // match templates to template groups
        foreach (var template in templates)
        {
            var maybeTemplateGroup = templateGroups.FirstOrDefault(bg => bg.Id == template.TemplateGroupId);
            if (maybeTemplateGroup != null)
            {
                unusedTemplateGroups.Remove(maybeTemplateGroup);
                template.TemplateGroup = maybeTemplateGroup;
            }
        }

        // create dummy templates for any groups that have no templates yet
        foreach (var unusedGroup in unusedTemplateGroups)
        {
            var dummyTemplate = new Template
            {
                Id = unusedGroup.Id * -1,
                TemplateGroupId = unusedGroup.Id,
                TemplateGroup = unusedGroup,
                Name = "(none)"
            };

            templates.Add(dummyTemplate);
        }

        return templates.Map(Mapper.ProjectToViewModel)
            .OrderBy(b => b.GroupName)
            .ThenBy(b => b.Name)
            .ToList();
    }
}
