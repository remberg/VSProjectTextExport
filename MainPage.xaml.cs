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
        private string ListAllFilesAndDirectories(string spath)
        {
            string rootPath = spath;
            var header = "***********************************" + Environment.NewLine;
            var firstRelativeFolder = new DirectoryInfo(rootPath).Name;
            var allFileTypes = GetSelectedFileTypes();
            var fileListing = new List<string>();
            // Add the root folder to the listing 
            fileListing.Add(firstRelativeFolder);
            var allDirectories = Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories)
                            .Where(dir => !dir.Split(Path.DirectorySeparatorChar).Any(part => part.StartsWith(".") || part.Equals("bin") || part.Equals("obj") || part.Equals("Platforms") || part.Equals("Resources"))).OrderBy(dir => dir);

            //Process each file and folder        
            foreach (var dir in allDirectories)
            {
                var relativeDirPath = Path.GetRelativePath(rootPath, dir);
                fileListing.Add("└──Directory: " + relativeDirPath);
                // Collect all files in the current directory 
                var allFilesInDir = allFileTypes.SelectMany(ext => Directory.EnumerateFiles(dir, ext, SearchOption.TopDirectoryOnly)).Select(file => Path.GetFileName(file))
                  // Extract the file names  
                  .OrderBy(fileName => fileName);
                // Sort the file names  
                // Add the sorted file names to the list  
                foreach (var fileName in allFilesInDir)
                {
                    fileListing.Add("     └──File: " + fileName);
                }

            }
            // Collect all files in the root directory   
            var allFilesInRoot = allFileTypes.SelectMany(ext => Directory.EnumerateFiles(rootPath, ext, SearchOption.TopDirectoryOnly)).Select(file => Path.GetFileName(file))
                      // Extract the file names 
                      .OrderBy(fileName => fileName);
            // Sort the file names 
            // Add the sorted file names to the list 
            foreach (var fileName in allFilesInRoot)
            {
                fileListing.Add("File: " + fileName);
            }
            var resultContent = new List<string>(); foreach (var file in fileListing)
            {
                resultContent.Add(file);
            }
            var filesListingString = "Files and Folders in Project " + firstRelativeFolder + ":" + Environment.NewLine + string.Join(Environment.NewLine, resultContent) + Environment.NewLine + Environment.NewLine;
            var singleStr = filesListingString;
            Console.WriteLine(singleStr); return singleStr;
        }
        private async void OnPickFileClicked(object sender, EventArgs e)
        {
            try
            {
                var pickOptions = new PickOptions
                {
                    PickerTitle = "Please choose a .sln file",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                     {
                        { DevicePlatform.iOS, new[] { "sln" } },
                         { DevicePlatform.Android, new[] { "sln" } },
                          { DevicePlatform.WinUI, new[] { ".sln" } },
                           { DevicePlatform.MacCatalyst, new[] { "sln" } },
                            { DevicePlatform.macOS, new[] { "sln" } }, })
                };
                var result = await FilePicker.PickAsync(pickOptions);
                if (result != null)
                {
                    _selectedPath = Path.GetDirectoryName(result.FullPath) ?? string.Empty; PathLabel.Text = ListAllFilesAndDirectories(_selectedPath); createReportButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error has occurred: {ex.Message}", "OK");
            }
        }
        private async void OnCreateReportClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedPath)) { await DisplayAlert("Error", "Please choose a folder first.", "OK"); return; }
            try
            {
                var header = "**********************************************************************" + Environment.NewLine;
                // Determine the name of the first relative folder for the output file  
                var firstRelativeFolder = new DirectoryInfo(_selectedPath).Name;
                string outputPath = Path.Combine(_selectedPath + @"\" + firstRelativeFolder + ".txt");
                var allFileTypes = GetSelectedFileTypes();
                // Get all directories except those that start with '.', as well as 'bin' and 'obj' folders     
                var allDirectories = Directory.EnumerateDirectories(_selectedPath, "*", SearchOption.AllDirectories).Where(dir => !dir.Split(Path.DirectorySeparatorChar).Any(part => part.StartsWith(".") || part.Equals("bin") || part.Equals("obj") || part.Equals("Platforms") || part.Equals("Resources"))).OrderBy(dir => dir);
                // Search all permissible directories for the specified file types     
                var filesInDirectories = allDirectories.SelectMany(dir => allFileTypes.SelectMany(ext => Directory.EnumerateFiles(dir, ext, SearchOption.TopDirectoryOnly))).OrderBy(dir => dir);
                // Capture specific files in the root directory in the desired order  
                var slnFilesInRoot = Directory.EnumerateFiles(_selectedPath, "*.sln", SearchOption.TopDirectoryOnly);
                var csprojFilesInRoot = Directory.EnumerateFiles(_selectedPath, "*.csproj", SearchOption.TopDirectoryOnly);
                var otherFilesInRoot = allFileTypes.Where(ext => ext != "*.sln" && ext != "*.csproj").SelectMany(ext => Directory.EnumerateFiles(_selectedPath, ext, SearchOption.TopDirectoryOnly)).OrderBy(dir => dir);
                // Combine the files from the root directory in the specific order   
                var filesInRoot = slnFilesInRoot.Concat(csprojFilesInRoot).Concat(otherFilesInRoot);
                // Combine the files from the subdirectories and the root directory     
                var allFiles = filesInRoot.Concat(filesInDirectories);
                var resultContent = allFiles.Select(path => new
                {
                    Name = Path.GetFileName(path),
                    RelativePath = Path.GetRelativePath(_selectedPath, path),
                    Contents = File.ReadAllText(path)
                }).Select(info => header + "Filename: " + info.Name + Environment.NewLine + "Relative Path: " + firstRelativeFolder + "\\" + info.RelativePath + Environment.NewLine + header + info.Contents);
                var singleStr = ListAllFilesAndDirectories(_selectedPath);
                singleStr += string.Join(Environment.NewLine, resultContent);
                Console.WriteLine(singleStr);
                File.WriteAllText(outputPath, singleStr, Encoding.UTF8);
                OutputPathLabel.Text = outputPath;
                _outputFilePath = outputPath;
                openTextFileButton.IsVisible = true; openFolderButton.IsVisible = true;
            }
            catch (Exception ex) { await DisplayAlert("Error", $"An error has occurred: {ex.Message}", "OK"); }
        }
        private async void OnOpenTextFileClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_outputFilePath) && File.Exists(_outputFilePath))
            {
                await Launcher.Default.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(_outputFilePath) });
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
                await Launcher.Default.OpenAsync(new Uri(_selectedPath));
            }
            else
            {
                await DisplayAlert("Error", "Folder not found.", "OK");
            }
        }
        private IEnumerable<string> GetSelectedFileTypes()
        {
            var selectedFileTypes = new List<string>(); if (csCheckBox.IsChecked) selectedFileTypes.Add("*.cs"); if (xamlCheckBox.IsChecked) selectedFileTypes.Add("*.xaml");
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