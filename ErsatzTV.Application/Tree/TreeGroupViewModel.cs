namespace ErsatzTV.Application.Tree;

public record TreeGroupViewModel(int Id, string Name, List<TreeItemViewModel> Children, bool IsSystem = false);
