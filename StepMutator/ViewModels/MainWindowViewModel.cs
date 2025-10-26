using Prism.Mvvm;
using StepMutator.Models;

namespace StepMutator.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Step Mutator";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        private Sequence _sequence = new Sequence(16);
        public Sequence Sequence
        {
            get { return _sequence; }
            set { SetProperty(ref _sequence, value); }
        }

        public MainWindowViewModel()
        {

        }
    }
}
