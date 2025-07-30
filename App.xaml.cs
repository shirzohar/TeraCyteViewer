using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TeraCyteViewer.Services;
using TeraCyteViewer.ViewModels;

namespace TeraCyteViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var services = new ServiceCollection();
            ConfigureServices(services);
            
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register services
            services.AddSingleton<IAuthService, AuthService>();
            services.AddTransient<IImageService, ImageService>();
            services.AddTransient<IResultService, ResultService>();
            
            // Register ViewModels
            services.AddTransient<MainViewModel>();
        }

        public ServiceProvider? ServiceProvider => _serviceProvider;
    }
}
