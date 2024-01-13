using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ErsatzTV.ViewModels;

public class TemplateItemEditViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    
    public int Id { get; set; }
    public int BlockId { get; set; }
    public string BlockName { get; set; }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
