using System;
using System.Windows;
using TeraCyteViewer.ViewModels;

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
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.StopMonitoring();
            base.OnClosed(e);
        }
    }
}
