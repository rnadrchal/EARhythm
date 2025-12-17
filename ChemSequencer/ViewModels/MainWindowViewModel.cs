using System.Threading.Tasks;
using System.Windows.Input;
using Egami.Chemistry.Model;
using Egami.Chemistry.PubChem;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Mvvm;

namespace ChemSequencer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly PubChemClient _pubChemClient;
        private readonly IDialogService _dialogService;


        private string _title = "Boltzmann";
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private MoleculeModel _molecule;
        public MoleculeModel Molecule
        {
            get => _molecule;
            set => SetProperty(ref _molecule, value);
        }

        public ICommand FindMoleculeCommand { get; }

        public MainWindowViewModel(PubChemClient pubChemClient, IDialogService dialogService)
        {
            _pubChemClient = pubChemClient;
            _dialogService = dialogService;

            FindMoleculeCommand = new DelegateCommand(async () => FindMolecule());
        }

        private void FindMolecule()
        {
            _dialogService.ShowDialog("SelectMolecule", callback: r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                    Molecule = r.Parameters["Molecule"] as Egami.Chemistry.Model.MoleculeModel;
                }
            });

        }

    }
}
