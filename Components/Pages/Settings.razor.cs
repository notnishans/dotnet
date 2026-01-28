using System.ComponentModel.DataAnnotations;
using JournalApp.Services;
using JournalApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace JournalApp.Components.Pages
{
    /// <summary>
    /// Settings page for theme, security, and export
    /// Demonstrates form handling and settings management
    /// </summary>
    public partial class Settings
    {
        [Inject] public JournalService JournalService { get; set; } = default!;
        [Inject] public NavigationManager Navigation { get; set; } = default!;
        [Inject] public AuthenticationStateService AuthStateService { get; set; } = default!;
        [Inject] public AuthenticationService AuthenticationService { get; set; } = default!;
        [Inject] public SettingsService SettingsService { get; set; } = default!;
        [Inject] public IDialogService DialogService { get; set; } = default!;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

        private AppSettings? settings;
        private PasswordModel passwordModel = new();
        private UpdateProfileDto profileModel = new();
        private SetJournalPinDto setPinModel = new();
        private ChangePinDto changePinModel = new();
        private User? currentUser;
        private string message = "";
        private bool isSuccess = false;
        private string _selectedTheme = "Dark";
        private string selectedTheme 
        { 
            get => _selectedTheme; 
            set 
            { 
                if (_selectedTheme != value)
                {
                    _selectedTheme = value;
                    OnThemePillClicked(value);
                }
            } 
        }

        private async void OnThemePillClicked(string theme)
        {
            await UpdateTheme(theme);
        }
        
        // Export settings
        private DateTime? exportStartDate = DateTime.Today.AddMonths(-1);
        private DateTime? exportEndDate = DateTime.Today;

        protected override async Task OnInitializedAsync()
        {
            var userId = AuthStateService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                Navigation.NavigateTo("/login");
                return;
            }

            settings = await SettingsService.GetSettingsAsync();
            selectedTheme = settings.Theme;
            
            // Apply the saved theme
            await JSRuntime.InvokeVoidAsync("themeManager.applyTheme", selectedTheme);

            // Load current user profile
            currentUser = AuthStateService.CurrentUser;
            if (currentUser != null)
            {
                profileModel.Username = currentUser.Username;
                profileModel.Email = currentUser.Email;
            }
        }

        private async Task UpdateProfile()
        {
            var userId = AuthStateService.GetCurrentUserId();
            if (!userId.HasValue) return;

            var result = await AuthenticationService.UpdateProfileAsync(userId.Value, profileModel);
            
            if (result.IsSuccess)
            {
                // Update the current user in state
                if (result.User != null)
                {
                    AuthStateService.SetCurrentUser(result.User);
                    currentUser = result.User;
                }
                
                message = "Profile updated.";
                isSuccess = true;
                Snackbar.Add(message, Severity.Info, options =>
                {
                    options.SnackbarVariant = Variant.Filled;
                    options.Icon = Icons.Material.Filled.CheckCircle;
                });
            }
            else
            {
                message = result.Message;
                isSuccess = false;
            }
        }

        private async Task SetPin()
        {
            var userId = AuthStateService.GetCurrentUserId();
            if (!userId.HasValue) return;

            var result = await AuthenticationService.SetJournalPinAsync(userId.Value, setPinModel);
            
            if (result.IsSuccess)
            {
                // Update the current user in state
                if (result.User != null)
                {
                    AuthStateService.SetCurrentUser(result.User);
                    currentUser = result.User;
                }
                
                message = result.Message;
                isSuccess = true;
                setPinModel = new();
            }
            else
            {
                message = result.Message;
                isSuccess = false;
            }
        }

        private async Task ChangePin()
        {
            var userId = AuthStateService.GetCurrentUserId();
            if (!userId.HasValue) return;

            var result = await AuthenticationService.ChangePinAsync(userId.Value, changePinModel);
            
            if (result.IsSuccess)
            {
                message = result.Message;
                isSuccess = true;
                changePinModel = new();
            }
            else
            {
                message = result.Message;
                isSuccess = false;
            }
        }

        private async Task UpdateTheme(string theme)
        {
            var success = await SettingsService.UpdateThemeAsync(theme);
            if (success)
            {
                // Apply theme instantly using JavaScript helper if needed
                await JSRuntime.InvokeVoidAsync("themeManager.applyTheme", theme);
                Snackbar.Add($"Appearance set to {theme} mode.", Severity.Info, options =>
                {
                    options.SnackbarVariant = Variant.Filled;
                    options.Icon = Icons.Material.Filled.Palette;
                });
            }
        }


        private async Task ExportToPdf()
        {
            try
            {
                // Validate date range
                if (exportStartDate > exportEndDate)
                {
                    message = "Start date must be before end date.";
                    isSuccess = false;
                    return;
                }

                // Ensure user is loaded
                if (currentUser == null)
                {
                    message = "User not found. Please log in again.";
                    isSuccess = false;
                    return;
                }

                // Generate filename with timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"JournalExport_{timestamp}.html";
                
                // Use Documents folder for system
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var filePath = Path.Combine(documentsPath, "JournalApp", fileName);
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!exportStartDate.HasValue || !exportEndDate.HasValue)
                {
                    message = "✨ Please select both a start and end date for your archive.";
                    isSuccess = false;
                    return;
                }

                // Export using the service (now creates HTML)
                var exportService = new ExportService(JournalService);
                var result = await exportService.ExportToPdfAsync(
                    exportStartDate.Value, 
                    exportEndDate.Value, 
                    filePath, 
                    currentUser?.Id ?? 0);

                // Check if export was successful
                if (result.StartsWith("Successfully"))
                {
                    message = $"{result}\n\n📁 File saved to:\n{filePath}\n\n✨ Open this HTML file in your browser and use File → Print → Save as PDF to create your PDF!\n\nYou can find it in File Explorer → Documents → JournalApp";
                    isSuccess = true;
                    
                    // Try to open the HTML file
                    try
                    {
                        await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync(
                            new Microsoft.Maui.ApplicationModel.OpenFileRequest
                            {
                                File = new Microsoft.Maui.Storage.ReadOnlyFile(filePath)
                            });
                    }
                    catch
                    {
                        // Silently ignore if launcher fails - the file is still saved
                    }
                }
                else
                {
                    message = result;
                    isSuccess = false;
                }
            }
            catch (Exception ex)
            {
                message = $"Error exporting: {ex.Message}";
                isSuccess = false;
            }
        }
    }

    /// <summary>
    /// Model for password form - demonstrates data annotations
    /// </summary>
    public class PasswordModel
    {
        public string CurrentPassword { get; set; } = "";
        
        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "Password must be between 4 and 100 characters")]
        public string NewPassword { get; set; } = "";
        
        [Required(ErrorMessage = "Please confirm your password")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = "";
    }
}
