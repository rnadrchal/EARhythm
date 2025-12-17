using Egami.Chemistry.PubChem;
using Prism.Mvvm;

namespace ChemSequencer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly PubChemClient _pubChemClient;
        private string _title = "Boltzmann";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel(PubChemClient pubChemClient)
        {
            _pubChemClient = pubChemClient;
        }
    }
}
