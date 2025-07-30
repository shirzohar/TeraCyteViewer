# 🧬 TeraCyte Live Image Viewer

A real-time microscope image analysis application built with .NET 8.0 WPF, featuring live data polling, JWT authentication, and advanced image analysis capabilities.

## ✨ Features

### 🔐 **Authentication & Security**
- **JWT Token Management**: Secure login with automatic token refresh
- **Encrypted Storage**: Tokens and history encrypted using Windows DPAPI
- **Session Persistence**: Automatic login restoration between sessions
- **User Information**: Display current user details and role
- **Secure Logout**: Complete session cleanup with option to re-login

### 📊 **Real-Time Monitoring**
- **Live Data Polling**: Continuous monitoring of microscope images
- **Smart Change Detection**: Only updates when new images are detected
- **Automatic Reconnection**: Handles network interruptions gracefully
- **Status Indicators**: Real-time connection and processing status

### 🔬 **Image Analysis**
- **Classification Results**: Automatic cell health classification
- **Focus Scoring**: Image quality assessment
- **Intensity Analysis**: Detailed pixel intensity measurements
- **Histogram Visualization**: Live charts with statistical analysis
- **Dynamic Metrics**: Real-time calculation of processing metrics

### 📚 **History Management**
- **Persistent Storage**: History saved between sessions and logins
- **Image Gallery**: Visual history of all analyzed images
- **Individual Deletion**: Remove specific images from history
- **Bulk Operations**: Clear all history with confirmation
- **Encrypted Storage**: History data protected with user-specific encryption

### 🎨 **User Interface**
- **Modern Design**: Clean, professional interface with animations
- **Responsive Layout**: Adapts to different screen sizes
- **Visual Feedback**: Animated indicators for new data
- **Status Updates**: Real-time progress and error reporting
- **Accessibility**: High contrast and clear visual hierarchy

## 🚀 Quick Start

### Prerequisites
- **.NET 8.0 Runtime** or later
- **Windows 10/11** (for DPAPI encryption)

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

## 🔐 Security Features

### Secure Token Storage
- **Encrypted Storage**: Tokens are encrypted using Windows Data Protection API (DPAPI)
- **User-Specific**: Encryption is tied to the current Windows user account
- **Automatic Persistence**: Tokens are automatically saved and restored between sessions
- **Secure Cleanup**: Tokens are properly cleared on logout

### Authentication Flow
1. **Initial Login**: Authenticate with username/password
2. **Token Storage**: Encrypt and store tokens locally
3. **Session Restoration**: Automatically restore valid tokens on app restart
4. **Token Refresh**: Automatically refresh expired tokens
5. **Secure Logout**: Clear all stored tokens and authentication state

### Data Protection
- **History Encryption**: All history data is encrypted using DPAPI
- **User Isolation**: Data is tied to the current Windows user
- **Secure Deletion**: Proper cleanup of sensitive data
- **No Plain Text**: No sensitive data stored in plain text

## 🏗️ Architecture

### MVVM Structure

```
TeraCyteViewer/
├── Models/                 # Data models
│   ├── ImageData.cs       # Image data structure
│   ├── ResultData.cs      # Analysis results
│   ├── LoginRequest.cs    # Authentication request
│   ├── LoginResponse.cs   # Authentication response
│   └── UserInfo.cs        # User information model
├── ViewModels/            # ViewModels (MVVM)
│   ├── BaseViewModel.cs   # Base class with INotifyPropertyChanged
│   ├── MainViewModel.cs   # Main application logic
│   └── RelayCommand.cs    # Command implementation
├── Services/              # Business logic services
│   ├── AuthService.cs     # JWT authentication & secure storage
│   ├── ImageService.cs    # Image data fetching
│   ├── ResultService.cs   # Results data fetching
│   └── AuthState.cs       # Authentication state management
├── Views/                 # UI Views
│   ├── HistoryWindow.xaml # History view with management
│   └── HistoryWindow.xaml.cs
├── Helpers/               # Utility classes
│   └── InverseBooleanToVisibilityConverter.cs
└── MainWindow.xaml        # Main application window
```

### Data Flow

1. **Authentication** → `AuthService` handles JWT login and refresh
2. **Data Polling** → `ImageService` and `ResultService` fetch data
3. **UI Updates** → `MainViewModel` updates properties via data binding
4. **User Interaction** → Commands trigger actions in ViewModel
5. **Data Persistence** → Secure storage of tokens and history

## 🎮 Usage

### Main Controls

- **🔑 Login** - Authenticate with the TeraCyte server
- **▶️ Start Monitoring** - Begin real-time data polling
- **⏹️ Stop Monitoring** - Stop data polling
- **🔄 Refresh** - Manually fetch latest data
- **📚 History** - View and manage previously seen images
- **🚪 Logout** - Securely end session and clear tokens

### Authentication States

**Not Authenticated:**
- Login button visible
- Monitoring controls disabled
- "Please login to start monitoring" message

**Authenticated:**
- User info displayed
- History and Logout buttons visible
- Full monitoring capabilities available

### History Management

**Viewing History:**
- Click "📚 History" to open history window
- Browse through all analyzed images
- View classification results and timestamps

**Managing History:**
- Delete individual images with confirmation
- Clear all history with bulk operation
- History persists between sessions and logins

## 📡 API Endpoints

The application uses the following TeraCyte API endpoints:

- **POST /api/auth/login** - User authentication
- **POST /api/auth/refresh** - Token refresh
- **GET /api/auth/me** - Current user information
- **GET /api/image** - Latest microscope image
- **GET /api/results** - Analysis results for current image

## 🔧 Technical Details

### Dependencies
- **.NET 8.0** - Modern .NET framework
- **WPF** - Windows Presentation Foundation
- **LiveCharts** - Real-time data visualization
- **Newtonsoft.Json** - JSON serialization
- **HttpClient** - HTTP communication

### Security Implementation
- **DPAPI Encryption** - Windows Data Protection API
- **JWT Tokens** - JSON Web Token authentication
- **User-Specific Keys** - Encryption tied to Windows user
- **Secure Storage** - Local encrypted file storage

### Performance Features
- **Async Operations** - Non-blocking UI operations
- **Smart Polling** - Efficient data fetching
- **Memory Management** - Automatic cleanup of old data
- **Error Recovery** - Robust error handling and retry logic

## 🐛 Troubleshooting

### Common Issues

**Authentication Failed:**
- Verify internet connection
- Check username/password
- Ensure server is accessible

**History Not Loading:**
- Check file permissions in `%LocalAppData%\TeraCyteViewer\`
- Verify Windows user account hasn't changed
- Clear stored data if corrupted

**Images Not Updating:**
- Check network connectivity
- Verify authentication status
- Restart monitoring if needed

### Log Files
Application logs are stored in:
```
logs/teracyte.log
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

*Built with ❤️ using .NET WPF and MVVM architecture*