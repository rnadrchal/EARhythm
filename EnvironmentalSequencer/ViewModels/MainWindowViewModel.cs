using Prism.Mvvm;

namespace EnvironmentalSequencer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Environment";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel()
        {

        }
    }
}
