using System.Windows.Input;
using Prism.Mvvm;

namespace ImageSequencer.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Image Sequencer";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public ImageViewer ImageViewer { get; }

        public MainWindowViewModel()
        {
            ImageViewer = new ImageViewer();
        }
    }
}
