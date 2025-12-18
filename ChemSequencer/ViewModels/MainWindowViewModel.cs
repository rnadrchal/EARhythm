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
        private readonly MoleculePlayer _sequencePlayer;

        public MoleculePlayer SequencePlayer => _sequencePlayer;

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
            _sequencePlayer = new MoleculePlayer();

            FindMoleculeCommand = new DelegateCommand(async () => FindMolecule());
        }

        private void FindMolecule()
        {
            _dialogService.ShowDialog("SelectMolecule", callback: r =>
            {
                if (r.Result == ButtonResult.OK)
                {
                    Molecule = r.Parameters["Molecule"] as MoleculeModel;
                    _sequencePlayer.UpdateSequence(Molecule);
                }
            });

        }

    }
}
