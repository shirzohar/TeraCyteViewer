# 🧬 TeraCyte Live Image Viewer

A real-time microscopy image viewer with AI inference overlay, built with .NET WPF and MVVM architecture.

## 📋 Overview

This application connects to the TeraCyte backend to fetch live microscope images and their corresponding AI analysis results. It displays images with inference overlays, numerical analysis metrics, and intensity histograms in real-time.

## ✨ Features

- **🔐 JWT Authentication** - Secure login with automatic token refresh
- **📡 Real-time Polling** - Continuously fetches new image data and results
- **🖼️ Image Display** - Shows microscope images with AI inference overlays
- **📊 Analysis Results** - Displays intensity average, focus score, and classification
- **📈 Live Histogram** - Visual representation of 256 intensity values
- **📚 History View** - Scrollable history of previously seen images
- **🎨 Color-coded UI** - Visual feedback for different data states
- **🎯 Smart Classification** - Color-coded results (Green for HEALTH, Red for ANOMALY)
- **📊 Hierarchical Dashboard** - Optimized layout with results prioritized
- **🛡️ Robust Error Handling** - Graceful handling of network issues and data delays
- **📝 Comprehensive Logging** - Detailed error tracking and user feedback
- **⚡ MVVM Architecture** - Clean separation of concerns

## 🛠️ Technology Stack

- **.NET 8.0** - Latest .NET framework
- **WPF (Windows Presentation Foundation)** - Modern UI framework
- **MVVM Pattern** - Model-View-ViewModel architecture
- **LiveCharts** - Real-time charting library
- **Newtonsoft.Json** - JSON serialization
- **HttpClient** - HTTP communication

## 🚀 Getting Started

### Prerequisites

- Windows 10/11
- .NET 8.0 SDK or Runtime
- Visual Studio 2022 (recommended) or VS Code

### Installation

1. **Clone the repository:**
   ```bash
   git clone <repository-url>
   cd TeraCyteViewer
   ```

2. **Build the project:**
   ```bash
   dotnet build
   ```

3. **Run the application:**
   ```bash
   dotnet run
   ```

### Configuration

The application uses the following default credentials:
- **Username:** `shir.zohar`
- **Password:** `biotech456`
- **API Base URL:** `https://teracyte-assignment-server-764836180308.us-central1.run.app`

## 🏗️ Architecture

### MVVM Structure

```
TeraCyteViewer/
├── Models/                 # Data models
│   ├── ImageData.cs       # Image data structure
│   ├── ResultData.cs      # Analysis results
│   ├── LoginRequest.cs    # Authentication request
│   └── LoginResponse.cs   # Authentication response
├── ViewModels/            # ViewModels (MVVM)
│   ├── BaseViewModel.cs   # Base class with INotifyPropertyChanged
│   ├── MainViewModel.cs   # Main application logic
│   └── RelayCommand.cs    # Command implementation
├── Services/              # Business logic services
│   ├── AuthService.cs     # JWT authentication
│   ├── ImageService.cs    # Image data fetching
│   ├── ResultService.cs   # Results data fetching
│   └── AuthState.cs       # Authentication state management
├── Views/                 # UI Views
│   ├── HistoryWindow.xaml # History view
│   └── HistoryWindow.xaml.cs
└── MainWindow.xaml        # Main application window
```

### Data Flow

1. **Authentication** → `AuthService` handles JWT login and refresh
2. **Data Polling** → `ImageService` and `ResultService` fetch data
3. **UI Updates** → `MainViewModel` updates properties via data binding
4. **User Interaction** → Commands trigger actions in ViewModel

## 🎮 Usage

### Main Controls

- **▶️ Start Monitoring** - Begin real-time data polling
- **⏹️ Stop Monitoring** - Stop data polling
- **🔄 Refresh** - Manually fetch latest data
- **📚 History** - View previously seen images

### Understanding the Display

The application features a **hierarchical dashboard layout** optimized for efficient data viewing:

#### **Top Section (Primary Content)**
- **🔬 Microscope Image** (40% width) - Shows the current image with AI detection overlays
- **📊 Analysis Results** (60% width) - Displays comprehensive analysis with scrollable content:
  - **🎯 Classification** - AI classification with color coding:
    - 🟢 **Green** for HEALTH/HEALTHY
    - 🔴 **Red** for ANOMALY
  - **📋 Image Details** - Quality and processing information
  - **🎯 Confidence Metrics** - Detection confidence and accuracy
  - **📡 System Status** - Connection and update status

#### **Bottom Section (Secondary Content)**
- **📈 Intensity Histogram** (60% width) - Distribution of pixel intensities with random colors
- **📊 Histogram Statistics** (40% width) - Detailed statistical analysis:
  - Total Values, Max/Min Values, Average, Median
  - Non-Zero Count, Standard Deviation

#### **Color Coding System**
- **HEALTH/HEALTHY** = 🟢 Green (positive)
- **ANOMALY** = 🔴 Red (negative)
- **Consistent across main view and history**

## 🔧 Development

### Project Structure

The application follows MVVM best practices:

- **Models** - Pure data structures
- **ViewModels** - Business logic and data binding
- **Views** - UI presentation only
- **Services** - External communication and data processing

### Key Components

#### MainViewModel
- Manages application state and data
- Handles authentication and polling
- Updates UI through data binding
- Processes image and result data

#### Services
- **AuthService** - JWT token management
- **ImageService** - Image data retrieval
- **ResultService** - Analysis results retrieval

#### Commands
- **StartMonitoringCommand** - Begin data polling
- **StopMonitoringCommand** - Stop data polling
- **RefreshCommand** - Manual data refresh
- **ShowHistoryCommand** - Open history window

## 🐛 Troubleshooting

### Common Issues

1. **Authentication Failed**
   - Check internet connection
   - Verify credentials are correct
   - Check if server is accessible
   - Application will automatically retry authentication

2. **No Data Received**
   - Ensure monitoring is started
   - Check network connectivity
   - Verify API endpoints are working
   - Application will continue polling with exponential backoff

3. **UI Not Updating**
   - Check data binding in XAML
   - Verify ViewModel properties are updating
   - Ensure commands are properly connected

4. **Network Timeouts**
   - Application automatically retries failed requests
   - Check firewall settings
   - Verify server availability

### Debug Information

The application provides real-time status messages:
- ✅ Success messages (green)
- ⚠️ Warning messages (orange)
- ❌ Error messages (red)
- 🔄 Info messages (blue)

### Error Recovery Features

- **Automatic Retry** - Failed requests are retried up to 3 times
- **Token Refresh** - Authentication tokens are automatically refreshed
- **Graceful Degradation** - Application continues running even with network issues
- **User Feedback** - Clear status messages for all error conditions

## 📈 Performance

- **Polling Interval** - 5 seconds between data fetches
- **Token Refresh** - Automatic refresh 1 minute before expiry
- **History Limit** - Maximum 50 items to prevent memory issues
- **Error Handling** - Exponential backoff for failed requests
- **UI Responsiveness** - Optimized layout with prioritized content areas
- **Memory Management** - Efficient image handling and data binding
- **Network Resilience** - Retry mechanism with 3 attempts and 2-second delays
- **Timeout Protection** - 15-second timeout for all HTTP requests


## 📄 License

This project is part of the TeraCyte assignment for Shir Zohar.

## 👨‍💻 Author

**Shir Zohar** - TeraCyte Assignment

---

*Built with ❤️ using .NET WPF and MVVM architecture*