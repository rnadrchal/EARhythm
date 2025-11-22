using FourierSequencer.Models;
using Prism.Mvvm;

namespace FourierSequencer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Fourier";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private readonly FourierSequencerModel _sequencer;
        public FourierSequencerModel Sequencer => _sequencer;   

        public MainWindowViewModel(FourierSequencerModel sequencer)
        {
            _sequencer = sequencer;
        }
    }
}
