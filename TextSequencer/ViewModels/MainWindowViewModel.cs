using Prism.Mvvm;

namespace TextSequencer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly TextSequencerViewModel _textSequencer;

        public TextSequencerViewModel TextSequencer => _textSequencer;

        private string _title = "Wortklauber";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public MainWindowViewModel(TextSequencerViewModel textSequencer)
        {
            _textSequencer = textSequencer;
        }
    }
}
