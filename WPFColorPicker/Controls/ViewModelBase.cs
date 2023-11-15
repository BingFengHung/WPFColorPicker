using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFColorPicker
{
    class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Set<T>(ref T target, T value, [CallerMemberName] string propertyName = null)
        {
            target = value;
            NotifyPropertyChanged(propertyName);
        }
    }
}
