using JournalApp.Services;
using JournalApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using System.Text;
using System.Text.RegularExpressions;

namespace JournalApp.Components.Pages
{
    /// <summary>
    /// Create/Edit Entry page
    /// Demonstrates: EditForm, Data Validation, Two-way Data Binding, Event Handling
    /// Following all PDF concepts from Lecture 9
    /// </summary>
    public partial class CreateEditEntry
    {
        private ElementReference contentEditorRef;
        
        [Inject]
        private AuthenticationService AuthenticationService { get; set; } = default!;

        [Parameter]
        public int? EntryId { get; set; }

        private JournalEntry entry = new()
        {
            EntryDate = DateTime.Today,
            PrimaryMood = "",
            PrimaryMoodCategory = MoodCategory.Neutral
        };

        private string secondaryMood1Temp = "";
        private string secondaryMood2Temp = "";
        private string message = "";
        private bool isSuccess = false;
        private bool showPreview = false;
        private bool IsEditMode => EntryId.HasValue;
        
        // Enhanced editor features
        private bool showEmojiPicker = false;
        private string selectedEmojiCategory = "Smileys";
        private bool isFullscreen = false;
        private string autoSaveStatus = "";
        private string autoSaveStatusClass = "";
        private Timer? autoSaveTimer;
        private Stack<string> undoStack = new();
        private Stack<string> redoStack = new();
        private string lastSavedContent = "";
        private string newTag = "";
        private bool isProtected = false;
        
        // Emoji categories
        private Dictionary<string, List<string>> emojiCategories = new()
        {
            { "Smileys", new List<string> { "😊", "😀", "😁", "😂", "🤣", "😃", "😄", "😅", "😆", "😉", "😍", "🥰", "😘", "😗", "😙", "😚", "🙂", "🤗", "🤩", "🤔", "😐", "😑", "😶", "🙄", "😏", "😣", "😥", "😮", "🤐", "😯", "😪", "😫", "🥱", "😴", "😌", "😛", "😜", "😝", "🤤" } },
            { "Emotions", new List<string> { "🤯", "😳", "🥵", "🥶", "😱", "😨", "😰", "😢", "😭", "😤", "😠", "😡", "🤬", "😈", "💀", "☠️", "💩", "🤡", "👻", "👽", "🤖", "😺", "😸", "😹", "😻", "😼", "😽", "🙀", "😿", "😾" } },
            { "Gestures", new List<string> { "👍", "👎", "👌", "✌️", "🤞", "🤟", "🤘", "🤙", "👈", "👉", "👆", "👇", "☝️", "✋", "🤚", "🖐️", "🖖", "👋", "🤝", "👏", "🙌", "👐", "🤲", "🤝", "🙏", "✍️", "💪", "🦾", "🦿", "🦵", "🦶" } },
            { "Objects", new List<string> { "📱", "💻", "⌨️", "🖥️", "🖨️", "🖱️", "🖲️", "🕹️", "📀", "💿", "💾", "💽", "📹", "📷", "📸", "📞", "☎️", "📟", "📠", "📺", "📻", "🎙️", "🎚️", "🎛️", "🧭", "⏰", "⏱️", "⏲️", "⏳", "📡", "🔋", "🔌", "💡", "🔦", "🕯️", "🪔", "🧯" } },
            { "Nature", new List<string> { "🌸", "🌺", "🌻", "🌷", "🌹", "🥀", "💐", "🌼", "🌿", "☘️", "🍀", "🪴", "🌱", "🌲", "🌳", "🌴", "🌵", "🌾", "🌍", "🌎", "🌏", "🌑", "🌒", "🌓", "🌔", "🌕", "🌖", "🌗", "🌘", "🌙", "🌚", "🌛", "🌜", "⭐", "🌟", "✨", "☀️", "🌤️", "⛅", "🌥️", "☁️", "🌦️", "🌧️", "⛈️", "🌩️", "🌨️", "❄️", "☃️", "⛄" } },
            { "Food", new List<string> { "🍏", "🍎", "🍐", "🍊", "🍋", "🍌", "🍉", "🍇", "🍓", "🫐", "🍈", "🍒", "🍑", "🥭", "🍍", "🥥", "🥝", "🍅", "🍆", "🥑", "🥦", "🥬", "🥒", "🌶️", "🫑", "🌽", "🥕", "🧄", "🧅", "🥔", "🍠", "🥐", "🥖", "🍞", "🥨", "🧀", "🥚", "🍳", "🥞", "🧇", "🥓", "🥩", "🍗", "🍖", "🦴", "🌭", "🍔", "🍟", "🍕", "🥪", "🥙", "🌮", "🌯", "🫔", "🥗", "🥘", "🫕", "🥫", "🍝", "🍜", "🍲", "🍛", "🍣", "🍱", "🥟", "🦪", "🍤", "🍙", "🍚", "🍘", "🍥", "🥠", "🥮", "🍢", "🍡", "🍧", "🍨", "🍦", "🥧", "🧁", "🍰", "🎂", "🍮", "🍭", "🍬", "🍫", "🍿", "🍩", "🍪", "🌰", "🥜", "🍯" } },
            { "Travel", new List<string> { "✈️", "🚀", "🛸", "🚁", "🛩️", "🛫", "🛬", "🪂", "🚂", "🚃", "🚄", "🚅", "🚆", "🚇", "🚈", "🚉", "🚊", "🚝", "🚞", "🚋", "🚌", "🚍", "🚎", "🚐", "🚑", "🚒", "🚓", "🚔", "🚕", "🚖", "🚗", "🚘", "🚙", "🚚", "🚛", "🚜", "🏍️", "🛵", "🦽", "🦼", "🛹", "🛴", "🚲", "🛺", "⛵", "🚤", "🛶", "⛴️", "🛳️", "🚢", "⚓" } },
            { "Symbols", new List<string> { "❤️", "🧡", "💛", "💚", "💙", "💜", "🖤", "🤍", "🤎", "💔", "❣️", "💕", "💞", "💓", "💗", "💖", "💘", "💝", "✨", "⭐", "🌟", "💫", "✔️", "✅", "❌", "❎", "➕", "➖", "✖️", "➗", "♾️", "‼️", "⁉️", "❓", "❔", "❕", "❗", "⚠️", "🔱", "⚜️", "💯", "🔰", "🔟" } }
        };

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Check authentication
                if (!AuthState.IsAuthenticated)
                {
                    Navigation.NavigateTo("/login");
                    return;
                }

                var userId = AuthState.GetCurrentUserId();
                if (!userId.HasValue)
                {
                    Navigation.NavigateTo("/dashboard");
                    return;
                }

                if (IsEditMode && EntryId.HasValue)
                {
                    // Load existing entry for editing
                    var existingEntry = await JournalService.GetEntryByIdAsync(EntryId.Value, userId.Value);
                    if (existingEntry != null)
                    {
                        entry = existingEntry;
                        secondaryMood1Temp = entry.SecondaryMood1 ?? "";
                        secondaryMood2Temp = entry.SecondaryMood2 ?? "";
                        isProtected = !string.IsNullOrEmpty(entry.Pin);
                    }
                    else
                    {
                        message = "Reflection not found.";
                        isSuccess = false;
                    }
                }
                else
                {
                    // [BUSINESS RULE]: Check if they are trying to backdate
                    if (entry.EntryDate.Date < DateTime.Today)
                    {
                        Snackbar.Add("You can only capture reflections for today or the future. Past memories are already preserved.", Severity.Warning);
                        entry.EntryDate = DateTime.Today;
                    }

                    // [BUSINESS RULE]: Only one entry per day.
                    var todayEntry = await JournalService.GetEntryByDateAsync(entry.EntryDate, userId.Value);
                    if (todayEntry != null)
                    {
                        Snackbar.Add("A reflection already exists for this date. Redirecting you to refine it...", Severity.Info);
                        await Task.Delay(1000);
                        Navigation.NavigateTo($"/edit-entry/{todayEntry.Id}");
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                message = $"Error initializing create entry page: {ex.Message}";
                isSuccess = false;
                Console.WriteLine($"[ERROR] CreateEditEntry.OnInitializedAsync: {ex.Message}\n{ex.StackTrace}");
                Snackbar.Add("Error loading entry page. Please try again.", Severity.Error);
            }
        }

        // Update mood categories when selections change
        private void UpdatePrimaryMoodCategory()
        {
            if (!string.IsNullOrEmpty(entry.PrimaryMood))
            {
                entry.PrimaryMoodCategory = MoodDefinitions.GetMoodCategory(entry.PrimaryMood);
            }
        }

        private void UpdateSecondaryMood1Category()
        {
            if (!string.IsNullOrEmpty(entry.SecondaryMood1))
            {
                entry.SecondaryMood1Category = MoodDefinitions.GetMoodCategory(entry.SecondaryMood1);
            }
            else
            {
                entry.SecondaryMood1Category = null;
            }
        }

        private void UpdateSecondaryMood2Category()
        {
            if (!string.IsNullOrEmpty(entry.SecondaryMood2))
            {
                entry.SecondaryMood2Category = MoodDefinitions.GetMoodCategory(entry.SecondaryMood2);
            }
            else
            {
                entry.SecondaryMood2Category = null;
            }
        }

        // Event handler for primary mood change (kept for backwards compatibility)
        private void OnPrimaryMoodChanged(ChangeEventArgs e)
        {
            var selectedMood = e.Value?.ToString();
            if (!string.IsNullOrEmpty(selectedMood))
            {
                entry.PrimaryMood = selectedMood;
                entry.PrimaryMoodCategory = MoodDefinitions.GetMoodCategory(selectedMood);
            }
        }

        // Event handler for secondary mood 1 change (kept for backwards compatibility)
        private void OnSecondaryMood1Changed(ChangeEventArgs e)
        {
            var selectedMood = e.Value?.ToString();
            if (!string.IsNullOrEmpty(selectedMood))
            {
                entry.SecondaryMood1 = selectedMood;
                entry.SecondaryMood1Category = MoodDefinitions.GetMoodCategory(selectedMood);
            }
            else
            {
                entry.SecondaryMood1 = null;
                entry.SecondaryMood1Category = null;
            }
        }

        // Event handler for secondary mood 2 change
        private void OnSecondaryMood2Changed(ChangeEventArgs e)
        {
            var selectedMood = e.Value?.ToString();
            if (!string.IsNullOrEmpty(selectedMood))
            {
                entry.SecondaryMood2 = selectedMood;
                entry.SecondaryMood2Category = MoodDefinitions.GetMoodCategory(selectedMood);
            }
            else
            {
                entry.SecondaryMood2 = null;
                entry.SecondaryMood2Category = null;
            }
        }

        // Handle valid form submission
        private async Task HandleValidSubmit()
        {
            try
            {
                var userId = AuthState.GetCurrentUserId();
                if (!userId.HasValue)
                {
                    message = "User not authenticated. Please log in.";
                    isSuccess = false;
                    Navigation.NavigateTo("/login");
                    return;
                }

                // Ensure entry has required fields
                if (string.IsNullOrWhiteSpace(entry.Title))
                {
                    message = "Title is required.";
                    isSuccess = false;
                    return;
                }

                if (string.IsNullOrWhiteSpace(entry.Content))
                {
                    message = "Content cannot be empty.";
                    isSuccess = false;
                    return;
                }

                // [SECURITY]: If not protected, clear the PIN
                if (!isProtected)
                {
                    entry.Pin = null;
                }

                // Set entry properties
                entry.UserId = userId.Value;
                entry.WordCount = CalculateWordCount(entry.Content);
                
                // Update mood categories
                UpdatePrimaryMoodCategory();
                UpdateSecondaryMood1Category();
                UpdateSecondaryMood2Category();

                bool success;
                if (IsEditMode && EntryId.HasValue)
                {
                    entry.Id = EntryId.Value;
                    success = await JournalService.UpdateEntryAsync(entry);
                    if (success)
                    {
                        Snackbar.Add("Your reflection has been refined! ✨", Severity.Success);
                    }
                    else
                    {
                        Snackbar.Add("Failed to update your memory. Please try again.", Severity.Error);
                    }
                }
                else
                {
                    // [DEFENSIVE]: Last-second check to ensure DB user 1 exists
                    await JournalService.EntryExistsForDateAsync(DateTime.Today.AddDays(-1000), userId.Value);
                    
                    success = await JournalService.CreateEntryAsync(entry, userId.Value);
                    if (success)
                    {
                        message = "Reflection captured successfully! ✨";
                        Snackbar.Add(message, Severity.Success);
                    }
                    else
                    {
                        // Check if it really exists or if something else failed
                        var exists = await JournalService.EntryExistsForDateAsync(entry.EntryDate, userId.Value);
                        message = exists ? "A reflection already exists for this date. You can only capture one peak memory per day." : "Unable to preserve this memory. Please check your connection and try again.";
                        Snackbar.Add(message, exists ? Severity.Info : Severity.Error);
                    }
                }

                isSuccess = success;
                StateHasChanged();

                if (success)
                {
                    await Task.Delay(1500);
                    Navigation.NavigateTo("/entries");
                }
            }
            catch (Exception ex)
            {
                message = $"Error: {ex.Message}";
                isSuccess = false;
                Snackbar.Add(message, Severity.Error);
                Console.WriteLine($"[ERROR] HandleValidSubmit: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Handle invalid form submission
        private void HandleInvalidSubmit()
        {
            message = "Please correct the errors in the form.";
            isSuccess = false;
        }

        // Calculate word count - helper method
        private int CalculateWordCount(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, 
                StringSplitOptions.RemoveEmptyEntries);
            return words.Length;
        }
        
        // Calculate reading time based on average reading speed (200 words/min)
        private int CalculateReadingTime(string content)
        {
            var wordCount = CalculateWordCount(content);
            if (wordCount == 0) return 0;
            return Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
        }
        
        // Handle content changes for auto-save
        private async Task OnContentChanged(ChangeEventArgs e)
        {
            // Update undo stack
            if (!string.IsNullOrEmpty(entry.Content) && entry.Content != lastSavedContent)
            {
                undoStack.Push(lastSavedContent);
                redoStack.Clear();
                lastSavedContent = entry.Content;
            }
            
            // Trigger auto-save
            autoSaveStatus = "Saving...";
            autoSaveStatusClass = "saving";
            StateHasChanged();
            
            autoSaveTimer?.Dispose();
            autoSaveTimer = new Timer(async _ => await AutoSave(), null, 2000, Timeout.Infinite);
            await Task.CompletedTask;
        }
        
        // Auto-save functionality
        private async Task AutoSave()
        {
            try
            {
                if (IsEditMode)
                {
                    await JournalService.UpdateEntryAsync(entry);
                    autoSaveStatus = "✓ Saved";
                    autoSaveStatusClass = "saved";
                }
                else
                {
                    autoSaveStatus = "Draft";
                    autoSaveStatusClass = "draft";
                }
                
                await InvokeAsync(StateHasChanged);
                
                // Clear status after 3 seconds
                await Task.Delay(3000);
                autoSaveStatus = "";
                await InvokeAsync(StateHasChanged);
            }
            catch
            {
                autoSaveStatus = "✗ Save failed";
                autoSaveStatusClass = "error";
                await InvokeAsync(StateHasChanged);
            }
        }
        
        // Undo edit
        private void UndoEdit()
        {
            if (undoStack.Count > 0)
            {
                redoStack.Push(entry.Content);
                entry.Content = undoStack.Pop();
                StateHasChanged();
            }
        }
        
        // Redo edit
        private void RedoEdit()
        {
            if (redoStack.Count > 0)
            {
                undoStack.Push(entry.Content);
                entry.Content = redoStack.Pop();
                StateHasChanged();
            }
        }
        
        // Toggle fullscreen mode
        private async Task ToggleFullscreen()
        {
            isFullscreen = !isFullscreen;
            await JSRuntime.InvokeVoidAsync("eval", $"document.getElementById('contentEditor').parentElement.classList.toggle('fullscreen')");
        }
        
        // Insert emoji
        private async Task InsertEmoji(string emoji)
        {
            await JSRuntime.InvokeVoidAsync("textEditorHelper.replaceSelection", emoji);
            await Task.Delay(50);
            var updatedContent = await JSRuntime.InvokeAsync<string>("eval", "document.getElementById('contentEditor').value");
            entry.Content = updatedContent;
            showEmojiPicker = false;
            StateHasChanged();
        }

        // Add tag to entry
        private void AddTag(string tag)
        {
            if (string.IsNullOrEmpty(entry.Tags))
            {
                entry.Tags = tag;
            }
            else
            {
                var tags = entry.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                if (!tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
                {
                    tags.Add(tag);
                    entry.Tags = string.Join(", ", tags);
                }
            }
        }

        private void RemoveTag(string tag)
        {
            if (!string.IsNullOrEmpty(entry.Tags))
            {
                var tags = entry.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                tags.Remove(tag);
                entry.Tags = tags.Any() ? string.Join(", ", tags) : "";
            }
        }

        private void HandleTagKeyUp(KeyboardEventArgs e)
        {
            if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(newTag))
            {
                AddTag(newTag.Trim());
                newTag = "";
            }
        }

        private void HandleTagAddClick()
        {
            if (!string.IsNullOrWhiteSpace(newTag))
            {
                AddTag(newTag.Trim());
                newTag = "";
            }
        }

        // Delete entry
        private async Task DeleteEntry()
        {
            if (IsEditMode && EntryId.HasValue)
            {
                var confirmed = true; // In real app, use confirmation dialog
                if (confirmed)
                {
                    var success = await JournalService.DeleteEntryAsync(EntryId.Value);
                    if (success)
                    {
                        message = "Entry deleted successfully!";
                        isSuccess = true;
                        await Task.Delay(1000);
                        Navigation.NavigateTo("/entries");
                    }
                    else
                    {
                        message = "Failed to delete entry.";
                        isSuccess = false;
                    }
                }
            }
        }

        // Toggle preview
        private void TogglePreview()
        {
            showPreview = !showPreview;
        }

        // Apply formatting to selected text
        private async Task ApplyFormat(string format)
        {
            try
            {
                // Special handling for emoji picker
                if (format == "emoji")
                {
                    showEmojiPicker = !showEmojiPicker;
                    StateHasChanged();
                    return;
                }
                
                // Get current content from the textarea
                var currentContent = await JSRuntime.InvokeAsync<string>("eval", "document.getElementById('contentEditor').value");
                
                switch (format)
                {
                    case "bold":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.wrapSelection", "**");
                        break;
                    case "italic":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.wrapSelection", "*");
                        break;
                    case "underline":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.wrapSelection", "__");
                        break;
                    case "strikethrough":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.wrapSelection", "~~");
                        break;
                    case "code":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.wrapSelection", "`");
                        break;
                    case "highlight":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.wrapSelection", "==");
                        break;
                    case "h1":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.insertLine", "# ");
                        break;
                    case "h2":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.insertLine", "## ");
                        break;
                    case "h3":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.insertLine", "### ");
                        break;
                    case "bulletList":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.insertLine", "- ");
                        break;
                    case "numberedList":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.insertLine", "1. ");
                        break;
                    case "checkbox":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.insertLine", "- [ ] ");
                        break;
                    case "link":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.insertAtCursor", "[", "](url)");
                        break;
                    case "image":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.insertAtCursor", "![alt text](", "image-url)");
                        break;
                    case "quote":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.insertLine", "> ");
                        break;
                    case "codeblock":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.insertAtCursor", "```\n", "\n```");
                        break;
                    case "hr":
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.replaceSelection", "\n---\n");
                        break;
                    case "table":
                        var tableTemplate = "\n| Header 1 | Header 2 | Header 3 |\n|----------|----------|----------|\n| Cell 1   | Cell 2   | Cell 3   |\n| Cell 4   | Cell 5   | Cell 6   |\n";
                        await JSRuntime.InvokeVoidAsync("textEditorHelper.replaceSelection", tableTemplate);
                        break;
                    default:
                        message = "Unknown format";
                        isSuccess = false;
                        break;
                }
                
                // Update the C# property with the new content
                await Task.Delay(50); // Small delay to let JS update the DOM
                var updatedContent = await JSRuntime.InvokeAsync<string>("eval", "document.getElementById('contentEditor').value");
                entry.Content = updatedContent;
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Format error: {ex.Message}");
                message = $"Formatting failed: {ex.Message}";
                isSuccess = false;
            }
        }

        // Clear formatting
        private void ClearFormatting()
        {
            if (!string.IsNullOrEmpty(entry.Content))
            {
                entry.Content = Regex.Replace(entry.Content, @"\*\*|__|_|\*|#|>|\[|\]|\(|\)|-|\d+\.", "");
            }
        }

        // Render markdown to HTML for preview
        private string RenderMarkdown(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return "<p class='text-muted'>Nothing to preview...</p>";

            var html = markdown;

            // Escape HTML to prevent XSS
            html = html.Replace("<", "&lt;").Replace(">", "&gt;");

            // Code blocks (must come before inline code)
            html = Regex.Replace(html, @"```([^\n]*)\n(.*?)```", "<pre><code class='language-$1'>$2</code></pre>", RegexOptions.Singleline);

            // Headings
            html = Regex.Replace(html, @"^### (.+)$", "<h3>$1</h3>", RegexOptions.Multiline);
            html = Regex.Replace(html, @"^## (.+)$", "<h2>$1</h2>", RegexOptions.Multiline);
            html = Regex.Replace(html, @"^# (.+)$", "<h1>$1</h1>", RegexOptions.Multiline);

            // Horizontal rule
            html = Regex.Replace(html, @"^---+$", "<hr/>", RegexOptions.Multiline);

            // Bold
            html = Regex.Replace(html, @"\*\*(.+?)\*\*", "<strong>$1</strong>");

            // Italic
            html = Regex.Replace(html, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "<em>$1</em>");

            // Underline
            html = Regex.Replace(html, @"__(.+?)__", "<u>$1</u>");

            // Strikethrough
            html = Regex.Replace(html, @"~~(.+?)~~", "<del>$1</del>");

            // Highlight
            html = Regex.Replace(html, @"==(.+?)==", "<mark>$1</mark>");

            // Inline code
            html = Regex.Replace(html, @"`(.+?)`", "<code>$1</code>");

            // Links
            html = Regex.Replace(html, @"\[(.+?)\]\((.+?)\)", "<a href='$2' target='_blank' rel='noopener'>$1</a>");

            // Checkbox (unchecked)
            html = Regex.Replace(html, @"^- \[ \] (.+)$", "<div class='checkbox-item'><input type='checkbox' disabled> $1</div>", RegexOptions.Multiline);

            // Checkbox (checked)
            html = Regex.Replace(html, @"^- \[x\] (.+)$", "<div class='checkbox-item'><input type='checkbox' checked disabled> $1</div>", RegexOptions.Multiline);

            // Bullet lists (must come after checkboxes)
            var bulletMatches = Regex.Matches(html, @"^- (.+)$", RegexOptions.Multiline);
            if (bulletMatches.Count > 0)
            {
                var listItems = string.Join("", bulletMatches.Cast<Match>().Select(m => $"<li>{m.Groups[1].Value}</li>"));
                html = Regex.Replace(html, @"(?:^- .+$\n?)+", $"<ul>{listItems}</ul>", RegexOptions.Multiline);
            }

            // Numbered lists
            var numberedMatches = Regex.Matches(html, @"^\d+\. (.+)$", RegexOptions.Multiline);
            if (numberedMatches.Count > 0)
            {
                var listItems = string.Join("", numberedMatches.Cast<Match>().Select(m => $"<li>{m.Groups[1].Value}</li>"));
                html = Regex.Replace(html, @"(?:^\d+\. .+$\n?)+", $"<ol>{listItems}</ol>", RegexOptions.Multiline);
            }

            // Block quotes
            var quoteMatches = Regex.Matches(html, @"^&gt; (.+)$", RegexOptions.Multiline);
            if (quoteMatches.Count > 0)
            {
                foreach (Match match in quoteMatches)
                {
                    html = html.Replace(match.Value, $"<blockquote>{match.Groups[1].Value}</blockquote>");
                }
            }

            // Tables
            html = Regex.Replace(html, @"\|(.+?)\|\n\|[-:\| ]+\|\n((?:\|.+?\|\n?)*)", match =>
            {
                var headerRow = match.Groups[1].Value;
                var headers = headerRow.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var headerHtml = string.Join("", headers.Select(h => $"<th>{h}</th>"));

                var bodyRows = match.Groups[2].Value.Trim().Split('\n');
                var bodyHtml = string.Join("", bodyRows.Select(row =>
                {
                    var cells = row.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var cellHtml = string.Join("", cells.Select(c => $"<td>{c}</td>"));
                    return $"<tr>{cellHtml}</tr>";
                }));

                return $"<table class='markdown-table'><thead><tr>{headerHtml}</tr></thead><tbody>{bodyHtml}</tbody></table>";
            }, RegexOptions.Multiline);

            // Line breaks (preserve newlines)
            html = html.Replace("\n", "<br/>");

            // Clean up multiple <br/> tags from empty lines
            html = Regex.Replace(html, @"(<br/>){3,}", "<br/><br/>");

            return html;
        }

        // Cancel and navigate back
        private void Cancel()
        {
            Navigation.NavigateTo("/entries");
        }
    }
}
