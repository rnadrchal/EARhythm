using Prism.Mvvm;

namespace ChemSequencer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Boltzmann";
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
