using System;
using System.Windows;
using TeraCyteViewer.ViewModels;
using System.Windows.Media.Animation;

namespace TeraCyteViewer
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
            
            // Subscribe to property changes to trigger animations
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Only trigger animations if visual cues are active
            if (!ViewModel.IsVisualCueActive)
                return;
                
            switch (e.PropertyName)
            {
                case nameof(ViewModel.ShowNewDataIndicator):
                    if (ViewModel.ShowNewDataIndicator)
                    {
                        TriggerNewDataAnimation();
                    }
                    break;
                case nameof(ViewModel.ShowImageNewIndicator):
                    if (ViewModel.ShowImageNewIndicator)
                    {
                        TriggerImageFadeInAnimation();
                    }
                    break;
                case nameof(ViewModel.ShowIntensityNewIndicator):
                case nameof(ViewModel.ShowFocusNewIndicator):
                case nameof(ViewModel.ShowClassificationNewIndicator):
                    if (ViewModel.ShowIntensityNewIndicator || ViewModel.ShowFocusNewIndicator || ViewModel.ShowClassificationNewIndicator)
                    {
                        TriggerResultsPulseAnimation();
                    }
                    break;
                case nameof(ViewModel.ShowHistogramNewIndicator):
                    if (ViewModel.ShowHistogramNewIndicator)
                    {
                        TriggerGlowAnimation();
                    }
                    break;
            }
        }

        private void TriggerNewDataAnimation()
        {
            var storyboard = FindResource("NewDataAnimation") as Storyboard;
            storyboard?.Stop();
            storyboard?.Begin();
        }

        private void TriggerImageFadeInAnimation()
        {
            var storyboard = FindResource("ImageFadeInAnimation") as Storyboard;
            storyboard?.Stop();
            storyboard?.Begin();
        }

        private void TriggerResultsPulseAnimation()
        {
            var storyboard = FindResource("ResultsPulseAnimation") as Storyboard;
            storyboard?.Stop();
            storyboard?.Begin();
        }

        private void TriggerGlowAnimation()
        {
            var storyboard = FindResource("GlowAnimation") as Storyboard;
            storyboard?.Stop();
            storyboard?.Begin();
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.StopMonitoring();
            base.OnClosed(e);
        }
    }
}
