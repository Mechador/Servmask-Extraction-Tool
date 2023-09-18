using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ServTool.ViewModels
{

    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty] private string? _fileText;

        [RelayCommand]
        private async Task ListFile(CancellationToken token)
        {
            ErrorMessages?.Clear();
            FileText = null;
            try
            {
                var file = await DoOpenFilePickerAsync();
                if (file is null) return;

                    using
                    var readStream = await file.OpenReadAsync();
                    using
                    var reader = new BinaryReader(readStream);
                                    
                    var backupSize = reader.BaseStream.Length;
                    var curPosition = reader.BaseStream.Position;
                do
                {
                    const int arrayLength = 4377;

                    var header = reader.ReadChars(arrayLength); //array of header chars
                    char[] bFileName = new char[255]; //array chars that represents name of the file
                    Array.Copy(header, 0, bFileName, 0, 255); //create array only with filename
                    string filename = new string(bFileName).Replace("\0", string.Empty); //string with the name

                    var test = new string(header);
                    var trimFileName = test.Remove(0, 255);

                    //Size of the file content
                    var bFileSize = trimFileName.Substring(0, 14).Replace("\0", string.Empty); // Get filesize and trim null-bytes
                    int size = Convert.ToInt32(bFileSize);
                    var path = trimFileName.Remove(0, 26).Replace("\0", string.Empty);

                    string fullPath = System.IO.Path.Combine(path, filename);

                    string result = fullPath + "\n";

                    var currentPosition = reader.BaseStream.Position + size;
                    reader.BaseStream.Seek(currentPosition, SeekOrigin.Begin);

                    FileText += fullPath + "\r\n";
                }
                while (curPosition < backupSize);
                 
                reader.Dispose();

            }
            catch (Exception e)
            {
                ErrorMessages?.Add(e.Message);
            }

            
        }

        [RelayCommand]
        private async Task ExtractionFile(CancellationToken token)
        {
            ErrorMessages?.Clear();
            FileText = null;
            try
            {
                var file = await DoExtractPickerAsync();
                if (file is null) return;

                var backupName = file.Name;

                //Getting a path of the opened backup
                file.TryGetUri(out Uri? uri);
                var dirOpenedFile = uri.AbsolutePath;
               

                using
                var readStream = await file.OpenReadAsync();
                using
                var reader = new BinaryReader(readStream);

                var backupSize = reader.BaseStream.Length;
                var curPosition = reader.BaseStream.Position;

                do
                {
                    const int arrayLength = 4377;

                    var header = reader.ReadChars(arrayLength); //array of header chars
                    char[] bFileName = new char[255]; //array chars that represents name of the file
                    Array.Copy(header, 0, bFileName, 0, 255); //create array only with filename
                    string filename = new string(bFileName).Replace("\0", string.Empty); //string with the name

                    var test = new string(header);
                    var trimFileName = test.Remove(0, 255);

                    //Size of the file content
                    var bFileSize = trimFileName.Substring(0, 14).Replace("\0", string.Empty); // Get filesize and trim null-bytes
                    int contentSize = Convert.ToInt32(bFileSize);
                    var path = trimFileName.Remove(0, 26).Replace("\0", string.Empty);

                    //Getting a file content
                    var content = reader.ReadBytes(contentSize);

                    //Increase by filesize and set a current position
                    var currentPosition = reader.BaseStream.Position;
                    reader.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
                    var curDirName = backupName + "\\";

                    string path1 = System.IO.Path.ChangeExtension(uri.AbsolutePath, null);
                    string dirPath = System.IO.Path.Combine(path1, path);
                    string fullPath = System.IO.Path.Combine(path1, path, filename);
                    string result = fullPath + "\n";
                    FileText += result ;
                    try
                        {

                            // Try to create the directory.
                            DirectoryInfo di = Directory.CreateDirectory(dirPath);
                        
                            // Create a new stream to write to the file
                            var Writer = new BinaryWriter(File.OpenWrite(fullPath));

                            // Writer raw data                
                            Writer.Write(content);
                            Writer.Flush();
                            Writer.Close();
                        }
                        catch (Exception e)
                         {
                                Console.WriteLine("The process failed: {0}", e.ToString());
                         }
                        finally
                        {
                                //Console.WriteLine(path);
                        }
    

                    }
                while (curPosition < backupSize);

                reader.Dispose();
 
            }
            catch (Exception e)
            {
                ErrorMessages?.Add(e.Message);
            }
            
        }

        [RelayCommand]
        private async Task SaveFile()
        {
            ErrorMessages?.Clear();
            try
            {
                var file = await DoSaveFilePickerAsync();
                if (file is null) return;

                // Limit the text file to 1MB so that the demo won't lag.
                if (FileText?.Length <= 1024 * 1024 * 1000)
                {
                    var stream = new MemoryStream(Encoding.Default.GetBytes(FileText));
                    await using var writeStream = await file.OpenWriteAsync();
                    await stream.CopyToAsync(writeStream);
                }
                else
                {
                    throw new Exception("File exceeded 1000MB limit.");
                }
            }
            catch (Exception e)
            {
                ErrorMessages?.Add(e.Message);
            }
        }

        private async Task<IStorageFile?> DoOpenFilePickerAsync()
        {
            // For learning purposes, we opted to directly get the reference
            // for StorageProvider APIs here inside the ViewModel. 

            // For your real-world apps, you should follow the MVVM principles
            // by making service classes and locating them with DI/IoC.

            // See IoCFileOps project for an example of how to accomplish this.
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Open backup File",
                AllowMultiple = false,
            });

            return files?.Count >= 1 ? files[0] : null;
        }

        private async Task<IStorageFile?> DoSaveFilePickerAsync()
        {
            // For learning purposes, we opted to directly get the reference
            // for StorageProvider APIs here inside the ViewModel. 

            // For your real-world apps, you should follow the MVVM principles
            // by making service classes and locating them with DI/IoC.

            // See DepInject project for a sample of how to accomplish this.
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            return await provider.SaveFilePickerAsync(new FilePickerSaveOptions()
            {
                Title = "Save Text File"
            });
        }

        private async Task<IStorageFile?> DoExtractPickerAsync()
        {
            // For learning purposes, we opted to directly get the reference
            // for StorageProvider APIs here inside the ViewModel. 

            // For your real-world apps, you should follow the MVVM principles
            // by making service classes and locating them with DI/IoC.

            // See IoCFileOps project for an example of how to accomplish this.
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
                desktop.MainWindow?.StorageProvider is not { } provider)
                throw new NullReferenceException("Missing StorageProvider instance.");

            var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                Title = "Extract File",
                AllowMultiple = false
            });

            return files?.Count >= 1 ? files[0] : null;
            
        }


    }
}