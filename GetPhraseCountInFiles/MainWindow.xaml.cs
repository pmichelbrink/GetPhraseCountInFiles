using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GetPhraseCountInFiles
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> Files { get; set; } = new ObservableCollection<string>();
        private double _fileCount = 0;
        string _logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GetPhraseCountInFiles.log");
        private double _phraseCount = 0;
        CancellationTokenSource _tokenSource = new CancellationTokenSource();

        CancellationToken _cancelToken;
        public MainWindow()
        {
            InitializeComponent();

            txtFileCount.Dispatcher.BeginInvoke((Action)(() => txtFileCount.Text = _fileCount.ToString()));
            txtPhraseCount.Dispatcher.BeginInvoke((Action)(() => txtPhraseCount.Text = _phraseCount.ToString()));

            _cancelToken = _tokenSource.Token;
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtSearchPhrase.Text) || string.IsNullOrEmpty(txtSearchPath.Text))
                return;

            Files.Clear();
            string searchPhrase = txtSearchPhrase.Text;
            _fileCount = 0;
            _phraseCount = 0;
            txtFileCount.Text = _fileCount.ToString();
            txtPhraseCount.Text = _phraseCount.ToString();

            File.WriteAllText(_logPath, $"Search started at {DateTime.Now}" + Environment.NewLine);

            DirectoryInfo searchFolder = new DirectoryInfo(txtSearchPath.Text);

            var task = Task.Run(() =>
            {
                SearchFolder(searchFolder, searchPhrase);

                string finishedMsg = $"Search finished at {DateTime.Now}";
                File.AppendAllText(_logPath, finishedMsg);
                MessageBox.Show(finishedMsg);
            }, _tokenSource.Token);

            try
            {
                await task;
            }
            catch (OperationCanceledException ex)
            {
                MessageBox.Show("Searching canceled.");
                _tokenSource = new CancellationTokenSource();
                _cancelToken = _tokenSource.Token;
            }
        }

        private void SearchFolder(DirectoryInfo searchFolder, string searchPhrase)
        {
            if (!searchFolder.Exists)
                return;

            searchFolder.GetFiles().ToList().ForEach(file =>
            {
                try
                {
                    string fleText = File.ReadAllText(file.FullName);               
                    double phraseCount = new Regex(Regex.Escape(searchPhrase)).Matches(fleText).Count;

                    if (phraseCount > 0)
                    {
                        txtPhraseCount.Dispatcher.BeginInvoke((Action)(() => Files.Add(file.FullName)));
                        
                        //txtHistory.Dispatcher.BeginInvoke((Action)(() => txtHistory.Text = $"Search phrase found in {file.FullName}{Environment.NewLine}{txtHistory.Text}"));
                        _phraseCount += phraseCount;
                        txtPhraseCount.Dispatcher.BeginInvoke((Action)(() => txtPhraseCount.Text = _phraseCount.ToString()));
                    }

                    ++_fileCount;
                    txtFileCount.Dispatcher.BeginInvoke((Action)(() => txtFileCount.Text = _fileCount.ToString()));
                }
                catch (Exception ex)
                {
                    File.AppendAllText(_logPath, ex.Message);
                }
            });

            searchFolder.GetDirectories().ToList().ForEach(folder =>
            {
                SearchFolder(folder, searchPhrase);
            });
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            _tokenSource.Cancel();
        }
        private void btnPathBrowse_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                //Title = Properties.Resources.SystmFolderLocation,
                //InitialDirectory = currentSystmFolderPath
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                txtSearchPath.Text = dialog.FileName;
        }

        private void lstFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb == null)
                return;

            string selectedFile = lb.SelectedItem as string;

            var p = new Process();
            p.StartInfo = new ProcessStartInfo(selectedFile)
            {
                UseShellExecute = true
            };
            p.Start();
        }

        private void GetPhraseCountInFilesWindows_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //btnSearch.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                //e.Handled = true;
            }
        }
    }
    public class FileModel
    {
        public int MyProperty { get; set; }
    }
}
