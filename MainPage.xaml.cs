using System.Text;
namespace VSProjectTextExport
{
    public partial class MainPage : ContentPage
    {
        private string _outputFilePath = string.Empty;
        private string _selectedPath = string.Empty;
        public MainPage()
        {
            InitializeComponent();
            createReportButton.IsEnabled = false;
            openTextFileButton.IsVisible = false;
            openFolderButton.IsVisible = false;
        }
        private string ListAllFilesAndDirectories(string spath, bool isMarkdownFormat)
        {
            try
            {
                string rootPath = spath;
                string header;
                var firstRelativeFolder = new DirectoryInfo(rootPath).Name;
                var allFileTypes = GetSelectedFileTypes();
                var fileListing = new List<string>();
                
                if (isMarkdownFormat)
                {
                    // Markdown format
                    header = "# Project Structure" + Environment.NewLine + Environment.NewLine;
                    // Add the root folder to the listing 
                    fileListing.Add("## " + firstRelativeFolder);
                }
                else
                {
                    // Plain text format
                    header = "PROJECT STRUCTURE:" + Environment.NewLine + Environment.NewLine;
                    // Add the root folder to the listing 
                    fileListing.Add(firstRelativeFolder);
                }
                
                try
                {
                    var allDirectories = Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories)
                                    .Where(dir => {
                                        try {
                                            return !dir.Split(Path.DirectorySeparatorChar).Any(part => part.StartsWith(".") || part.Equals("bin") || part.Equals("obj") || part.Equals("Platforms") || part.Equals("Resources"));
                                        } catch {
                                            // Skip this directory if there's an error in filtering
                                            return false;
                                        }
                                    })
                                    .Where(dir => {
                                        try {
                                            // Check if we actually have access to the directory
                                            var _ = Directory.GetFiles(dir);
                                            return true;
                                        } catch {
                                            return false;
                                        }
                                    })
                                    .OrderBy(dir => dir);

                    //Process each file and folder        
                    foreach (var dir in allDirectories)
                    {
                        try
                        {
                            var relativeDirPath = Path.GetRelativePath(rootPath, dir);
                            
                            if (isMarkdownFormat)
                            {
                                fileListing.Add("### Directory: " + relativeDirPath);
                            }
                            else
                            {
                                fileListing.Add("Directory: " + relativeDirPath);
                            }
                            
                            // Collect all files in the current directory 
                            List<string> files = new List<string>();
                            foreach (var ext in allFileTypes)
                            {
                                try
                                {
                                    files.AddRange(Directory.EnumerateFiles(dir, ext, SearchOption.TopDirectoryOnly));
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    // Access denied, skip this file type
                                    continue;
                                }
                            }
                            
                            var allFilesInDir = files.Select(file => Path.GetFileName(file))
                                .OrderBy(fileName => fileName);
                            
                            // Add the sorted file names to the list  
                            foreach (var fileName in allFilesInDir)
                            {
                                if (isMarkdownFormat)
                                {
                                    fileListing.Add("- " + fileName);
                                }
                                else
                                {
                                    fileListing.Add("  " + fileName);
                                }
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Access to directory denied, skip and continue
                            continue;
                        }
                        catch (Exception)
                        {
                            // Skip other errors
                            continue;
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    if (isMarkdownFormat)
                    {
                        fileListing.Add("*No permission to access subdirectories.*");
                    }
                    else
                    {
                        fileListing.Add("No permission to access subdirectories.");
                    }
                }
                
                // Collect all files in the root directory with error handling  
                try
                {
                    List<string> rootFiles = new List<string>();
                    foreach (var ext in allFileTypes)
                    {
                        try
                        {
                            rootFiles.AddRange(Directory.EnumerateFiles(rootPath, ext, SearchOption.TopDirectoryOnly));
                        }
                        catch
                        {
                            // Ignore file access errors
                        }
                    }
                    
                    var allFilesInRoot = rootFiles.Select(file => Path.GetFileName(file))
                        .OrderBy(fileName => fileName);
                    
                    // Add the sorted file names to the list 
                    foreach (var fileName in allFilesInRoot)
                    {
                        if (isMarkdownFormat)
                        {
                            fileListing.Add("- " + fileName);
                        }
                        else
                        {
                            fileListing.Add("  " + fileName);
                        }
                    }
                }
                catch (Exception)
                {
                    if (isMarkdownFormat)
                    {
                        fileListing.Add("*Error accessing files in the main directory.*");
                    }
                    else
                    {
                        fileListing.Add("Error accessing files in the main directory.");
                    }
                }
                
                var resultContent = new List<string>();
                foreach (var file in fileListing)
                {
                    resultContent.Add(file);
                }
                var filesListingString = header + string.Join(Environment.NewLine, resultContent) + Environment.NewLine + Environment.NewLine;
                var singleStr = filesListingString;
                Console.WriteLine(singleStr); 
                return singleStr;
            }
            catch (Exception ex)
            {
                if (isMarkdownFormat)
                {
                    return $"## Error\n\nError listing files and folders: {ex.Message}";
                }
                else
                {
                    return $"ERROR\n\nError listing files and folders: {ex.Message}";
                }
            }
        }
        private async void OnPickFileClicked(object sender, EventArgs e)
        {
            try
            {
                FileResult? result = null;
                
                // Simplify the macOS implementation with fewer special options
                if (DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
                {
                    try
                    {
                        // Use simple FilePicker without options
                        result = await FilePicker.PickAsync();
                    }
                    catch (Exception pickerEx)
                    {
                        // Show detailed error message for macOS
                        await DisplayAlert("FilePicker Error", 
                            $"Details: {pickerEx.Message}\n\n" +
                            $"StackTrace: {pickerEx.StackTrace}", "OK");
                        return;
                    }
                }
                else
                {
                    // For other platforms, use the usual filters
                    var pickOptions = new PickOptions
                    {
                        PickerTitle = "Please choose a .sln file",
                        FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                            { DevicePlatform.iOS, new[] { "public.text" } },
                            { DevicePlatform.Android, new[] { "application/octet-stream" } },
                            { DevicePlatform.WinUI, new[] { ".sln" } }
                        })
                    };
                    
                    result = await FilePicker.Default.PickAsync(pickOptions);
                }

                if (result != null)
                {
                    // Verify the picked file is a .sln file
                    if (Path.GetExtension(result.FullPath).ToLowerInvariant() != ".sln")
                    {
                        await DisplayAlert("Invalid File", "Please select a .sln file.", "OK");
                        return;
                    }

                    // Use Path.GetDirectoryName properly
                    _selectedPath = Path.GetDirectoryName(result.FullPath) ?? string.Empty;
                    
                    // Check if we got a valid path
                    if (string.IsNullOrEmpty(_selectedPath))
                    {
                        await DisplayAlert("Error", "Could not determine directory path.", "OK");
                        return;
                    }
                    
                    PathLabel.Text = ListAllFilesAndDirectories(_selectedPath, false);
                    createReportButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error has occurred: {ex.Message}", "OK");
            }
        }
        private async void OnCreateReportClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedPath))
            { 
                await DisplayAlert("Error", "Please choose a folder first.", "OK"); 
                return; 
            }
            
            try
            {
                // Determine the name of the first relative folder for the output file  
                var firstRelativeFolder = new DirectoryInfo(_selectedPath).Name;
                
                // Get output format selection
                bool isMarkdownFormat = markdownRadioButton.IsChecked;
                string fileExtension = isMarkdownFormat ? ".md" : ".txt";
                
                // Choose a location that has guaranteed write permissions
                string outputPath;
                if (DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
                {
                    // On macOS, first try to save in the project directory
                    outputPath = Path.Combine(_selectedPath, firstRelativeFolder + fileExtension);
                    
                    try
                    {
                        // Test if we can write to this location
                        using (var testWrite = File.Create(outputPath))
                        {
                            // File could be created, close it
                        }
                        // Delete the test file
                        File.Delete(outputPath);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Fall back to Documents folder if we can't write to the project directory
                        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        outputPath = Path.Combine(documentsPath, firstRelativeFolder + fileExtension);
                        
                        // Inform the user
                        await DisplayAlert("Information", 
                            $"Due to macOS permissions, the output file will be saved in the Documents folder: {outputPath}", "OK");
                    }
                }
                else
                {
                    // For other platforms, save in the project directory
                    outputPath = Path.Combine(_selectedPath, firstRelativeFolder + fileExtension);
                }
                
                var allFileTypes = GetSelectedFileTypes();
                
                List<string> accessibleDirectories = new List<string>();
                List<string> allFiles = new List<string>();
                
                try
                {
                    // Careful handling of directory listing
                    var allDirectories = Directory.EnumerateDirectories(_selectedPath, "*", SearchOption.AllDirectories)
                        .Where(dir => {
                            try {
                                // Filter directories
                                bool shouldInclude = !dir.Split(Path.DirectorySeparatorChar)
                                    .Any(part => part.StartsWith(".") || part.Equals("bin") || part.Equals("obj") || 
                                        part.Equals("Platforms") || part.Equals("Resources"));
                                
                                // Check access
                                if (shouldInclude)
                                {
                                    Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
                                }
                                
                                return shouldInclude;
                            }
                            catch {
                                // Skip on errors
                                return false;
                            }
                        })
                        .OrderBy(dir => dir);
                        
                    accessibleDirectories = allDirectories.ToList();
                        
                    // Collect files from the root directory
                    try
                    {
                        var slnFilesInRoot = SafeEnumerateFiles(_selectedPath, "*.sln");
                        var csprojFilesInRoot = SafeEnumerateFiles(_selectedPath, "*.csproj");
                        
                        List<string> otherFilesInRoot = new List<string>();
                        foreach (var ext in allFileTypes.Where(ext => ext != "*.sln" && ext != "*.csproj"))
                        {
                            try
                            {
                                otherFilesInRoot.AddRange(Directory.EnumerateFiles(_selectedPath, ext, SearchOption.TopDirectoryOnly));
                            }
                            catch { /* Ignore and continue */ }
                        }
                        
                        allFiles.AddRange(slnFilesInRoot);
                        allFiles.AddRange(csprojFilesInRoot);
                        allFiles.AddRange(otherFilesInRoot.OrderBy(f => f));
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Warning", $"Problems accessing the root directory: {ex.Message}", "OK");
                    }
                    
                    // Collect files from subdirectories
                    foreach (var dir in accessibleDirectories)
                    {
                        foreach (var ext in allFileTypes)
                        {
                            try
                            {
                                var files = Directory.EnumerateFiles(dir, ext, SearchOption.TopDirectoryOnly);
                                allFiles.AddRange(files);
                            }
                            catch { /* Ignore and continue */ }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Warning", $"Problem searching directories: {ex.Message}", "OK");
                }
                
                // Create report with the files we can access
                var contentResults = new List<string>();
                var failedFiles = new List<string>();
                
                foreach (var filePath in allFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileName(filePath);
                        var relativePath = Path.GetRelativePath(_selectedPath, filePath);
                        string contents = File.ReadAllText(filePath);
                        
                        if (isMarkdownFormat)
                        {
                            // Get language for syntax highlighting based on file extension
                            string fileExt = Path.GetExtension(filePath).ToLowerInvariant();
                            string language = DetermineCodeLanguage(fileExt);
                            
                            contentResults.Add(
                                $"## File: {fileName}" + Environment.NewLine + 
                                $"**Path:** `{firstRelativeFolder}{Path.DirectorySeparatorChar}{relativePath}`" + 
                                Environment.NewLine + Environment.NewLine + 
                                $"```{language}" + Environment.NewLine + 
                                contents + Environment.NewLine + 
                                "```" + Environment.NewLine + Environment.NewLine
                            );
                        }
                        else
                        {
                            // Original text format
                            contentResults.Add(
                                $"// File: {fileName}" + Environment.NewLine + 
                                $"// Path: {firstRelativeFolder}{Path.DirectorySeparatorChar}{relativePath}" + 
                                Environment.NewLine + Environment.NewLine + 
                                contents + Environment.NewLine + 
                                Environment.NewLine + 
                                "// " + new string('-', 80) + Environment.NewLine
                            );
                        }
                    }
                    catch
                    {
                        failedFiles.Add(filePath);
                    }
                }
                
                // Combine everything based on format
                string finalContent;
                
                if (isMarkdownFormat)
                {
                    var directorySummary = ListAllFilesAndDirectories(_selectedPath, isMarkdownFormat);
                    var fileContents = string.Join(Environment.NewLine, contentResults);
                    finalContent = directorySummary + Environment.NewLine + "# File Contents" + Environment.NewLine + Environment.NewLine + fileContents;
                    
                    if (failedFiles.Any())
                    {
                        finalContent += Environment.NewLine + Environment.NewLine +
                            "## Files with Access Issues" + Environment.NewLine + Environment.NewLine +
                            "The following files could not be read due to permission issues:" + 
                            Environment.NewLine +
                            string.Join(Environment.NewLine, failedFiles.Select(f => "- " + f));
                    }
                }
                else
                {
                    // Original text format
                    var directorySummary = ListAllFilesAndDirectories(_selectedPath, isMarkdownFormat);
                    var fileContents = string.Join(Environment.NewLine, contentResults);
                    finalContent = "PROJECT STRUCTURE:" + Environment.NewLine + Environment.NewLine + 
                                  directorySummary + Environment.NewLine + 
                                  "FILE CONTENTS:" + Environment.NewLine + Environment.NewLine + 
                                  fileContents;
                    
                    if (failedFiles.Any())
                    {
                        finalContent += Environment.NewLine + Environment.NewLine +
                            "FILES WITH ACCESS ISSUES:" + Environment.NewLine + Environment.NewLine +
                            "The following files could not be read due to permission issues:" + 
                            Environment.NewLine +
                            string.Join(Environment.NewLine, failedFiles.Select(f => "- " + f));
                    }
                }
                
                // Save the file
                File.WriteAllText(outputPath, finalContent, Encoding.UTF8);
                
                OutputPathLabel.Text = outputPath;
                _outputFilePath = outputPath;
                
                openTextFileButton.IsVisible = true;
                openFolderButton.IsVisible = true;

            }
            catch (Exception ex) 
            { 
                await DisplayAlert("Error", $"An error has occurred: {ex.Message}", "OK"); 
            }
        }
        
        // Helper method to determine the language for syntax highlighting
        private string DetermineCodeLanguage(string fileExtension)
        {
            return fileExtension switch
            {
                ".cs" => "csharp",
                ".xaml" => "xml",
                ".csproj" => "xml",
                ".sln" => "plaintext",
                ".resx" => "xml",
                ".json" => "json",
                ".css" => "css",
                ".html" => "html",
                ".razor" => "razor",
                ".cshtml" => "cshtml",
                ".js" => "javascript",
                _ => "plaintext"
            };
        }

        // Helper method to safely enumerate files with error handling
        private IEnumerable<string> SafeEnumerateFiles(string directory, string searchPattern)
        {
            try
            {
                return Directory.EnumerateFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
            }
            catch (Exception)
            {
                // Return empty collection on any error
                return Enumerable.Empty<string>();
            }
        }

        private async void OnOpenTextFileClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_outputFilePath) && File.Exists(_outputFilePath))
            {
                try
                {
                    if (DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
                    {
                        // On macOS we use the special file:// prefix
                        string macOsPath = "file://" + _outputFilePath;
                        await Launcher.Default.OpenAsync(new Uri(macOsPath));
                    }
                    else
                    {
                        // Standard method for other platforms
                        await Launcher.Default.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(_outputFilePath) });
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error opening file", 
                        $"The file could not be opened: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "File not found.", "OK");
            }
        }

        private async void OnOpenFolderClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_selectedPath) && Directory.Exists(_selectedPath))
            {
                try
                {
                    if (DeviceInfo.Platform == DevicePlatform.macOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
                    {
                        // On macOS we use the special file:// prefix with trailing slash
                        string macOsPath = "file://" + _selectedPath;
                        // Make sure the path ends with a slash
                        if (!macOsPath.EndsWith("/"))
                            macOsPath += "/";
                        
                        await Launcher.Default.OpenAsync(new Uri(macOsPath));
                    }
                    else
                    {
                        // Standard method for other platforms
                        await Launcher.Default.OpenAsync(new Uri(_selectedPath));
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error opening folder", 
                        $"The folder could not be opened: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error", "Folder not found.", "OK");
            }
        }

        private IEnumerable<string> GetSelectedFileTypes()
        {
            var selectedFileTypes = new List<string>(); 
            if (csCheckBox.IsChecked) selectedFileTypes.Add("*.cs"); 
            if (xamlCheckBox.IsChecked) selectedFileTypes.Add("*.xaml");
            if (slnCheckBox.IsChecked) selectedFileTypes.Add("*.sln");
            if (csprojCheckBox.IsChecked) selectedFileTypes.Add("*.csproj");
            if (resxCheckBox.IsChecked) selectedFileTypes.Add("*.resx");
            if (jsonCheckBox.IsChecked) selectedFileTypes.Add("*.json");
            if (cssCheckBox.IsChecked) selectedFileTypes.Add("*.css");
            if (htmlCheckBox.IsChecked) selectedFileTypes.Add("*.html");
            if (razorCheckBox.IsChecked) selectedFileTypes.Add("*.razor");
            if (cshtmlCheckBox.IsChecked) selectedFileTypes.Add("*.cshtml");
            if (jsCheckBox.IsChecked) selectedFileTypes.Add("*.js");
            return selectedFileTypes;
        }
    }
}