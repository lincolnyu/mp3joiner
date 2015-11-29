using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Mp3Joiner;
using System.Reflection;
using System.Windows.Input;
using System;

namespace Mp3JoinerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _outputPath;

        public MainWindow()
        {
            InitializeComponent();

            SetTitle();

            DataContext = this;
        }

        public ObservableCollection<string> InputPaths { get; private set; } = new ObservableCollection<string>();

        public string OutputPath
        {
            get { return _outputPath; }
            set
            {
                if (_outputPath != value)
                {
                    _outputPath = value;
                    NotifyPropertyChanged("OutputPath");
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private void OutputPathBrowseButtonOnClick(object sender, RoutedEventArgs args)
        {
            var ofd = new OpenFileDialog();
            ofd.InitialDirectory = Path.GetDirectoryName(OutputPath);
            if (ofd.ShowDialog() == true)
            {
                OutputPath = ofd.FileName;
            }
        }

        private void StartButtonOnClick(object sender, RoutedEventArgs args)
        {
            Start();
        }

        private void InputPathListViewPreviewDrop(object sender, DragEventArgs args)
        {
            var added = false;
            foreach (var ss in args.Data.GetFormats().Select(f => args.Data.GetData(f)).OfType<string[]>())
            {
                foreach (var s in ss)
                {
                    if (File.Exists(s) && Path.GetExtension(s).ToLower() == ".mp3")
                    {
                        InputPaths.Add(s);
                        added = added || true;
                    }
                    else if (Directory.Exists(s))
                    {
                        added = added || AddMp3InFolder(s);
                    }
                }
                if (added)
                {
                    break;
                }
            }
            args.Handled = true;
        }

        private bool AddMp3InFolder(string s)
        {
            var dir = new DirectoryInfo(s);
            var result = false;
            foreach (var f in dir.GetFiles().Where(x=>x.Extension.ToLower()==".mp3"))
            {
                InputPaths.Add(f.FullName);
                result = true;
            }
            return result;
        }

        private void OutputPathViewPreviewDrop(object sender, DragEventArgs args)
        {
            var paths = args.Data.GetFormats().Select(f => args.Data.GetData(f)).OfType<string[]>().FirstOrDefault();
            if (paths != null)
            {
                var path = paths.FirstOrDefault();
                if (path != null)
                {
                    OutputPath = path;
                }
            }
        }

        private void MoveUpButtonClick(object sender, RoutedEventArgs e)
        {
            if (InputPathsList.SelectedItems == null)
            {
                return;
            }
            var indices = new List<int>();
            var items = new List<string>();
            foreach (var item in InputPathsList.SelectedItems.OfType<string>())
            {
                var index = InputPaths.IndexOf(item);
                items.Add(item);
                indices.Add(index);
            }
            foreach (var index in indices)
            {
                MoveUp(index);
            }
            InputPathsList.SelectedItems.Clear();
            foreach (var item in items)
            {
                InputPathsList.SelectedItems.Add(item);
            }
        }

        private void MoveDownButtonClick(object sender, RoutedEventArgs e)
        {
            if (InputPathsList.SelectedItems == null)
            {
                return;
            }

            var indices = new List<int>();
            var items = new List<string>();
            foreach (var item in InputPathsList.SelectedItems.OfType<string>())
            {
                var index = InputPaths.IndexOf(item);
                items.Add(item);
                indices.Add(index);
            }
            indices.Reverse();
            foreach (var index in indices)
            {
                MoveDown(index);
            }
            InputPathsList.SelectedItems.Clear();
            foreach (var item in items)
            {
                InputPathsList.SelectedItems.Add(item);
            }
        }

        private void MoveUp(int index)
        {
            var a = InputPaths[index];
            var b = InputPaths[index - 1];
            InputPaths[index] = b;
            InputPaths[index - 1] = a;
        }

        private void MoveDown(int index)
        {
            var a = InputPaths[index];
            var b = InputPaths[index + 1];
            InputPaths[index] = b;
            InputPaths[index + 1] = a;
        }

        private void AscendingButtonClick(object sender, RoutedEventArgs e)
        {
            var objs = InputPaths.OrderBy(x => x).ToList();
            InputPaths.Clear();
            foreach (var obj in objs)
            {
                InputPaths.Add(obj);
            }
        }

        private void DescendingButtonClick(object sender, RoutedEventArgs e)
        {
            var objs = InputPaths.OrderByDescending(x => x).ToList();
            InputPaths.Clear();
            foreach (var obj in objs)
            {
                InputPaths.Add(obj);
            }
        }

        private void DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            var delList = new List<string>();
            foreach (var item in InputPathsList.SelectedItems.OfType<string>())
            {
                delList.Add(item);
            }
            foreach (var del in delList)
            {
                InputPaths.Remove(del);
            }
        }
        private void OutputPathTextPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Start();
                e.Handled = true;
            }
        }
        
        private void WindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Start();
                e.Handled = true;
            }
        }

        private void Start()
        {
            if (InputPaths.Count > 0 && OutputPath != null)
            {
                if (File.Exists(OutputPath))
                {
                    if (MessageBox.Show(Strings.OverwritingFileWarning, Strings.AppName) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
                var joiner = new Joiner();
                joiner.Join(InputPaths, OutputPath);
            }
        }

        /// <summary>
        ///  Sets app title as per app name and version
        /// </summary>
        /// <remarks>
        ///  References:
        ///  1. http://stackoverflow.com/questions/22527830/how-to-get-the-publish-version-of-a-wpf-application
        /// </remarks>
        private void SetTitle()
        {
            try
            {
                var ver = System.Deployment.Application.ApplicationDeployment.CurrentDeployment.
                    CurrentVersion;
                Title = string.Format("{0} (Ver {1}.{2} Beta)", Strings.AppName, ver.Major, ver.Minor);
            }
            catch (System.Deployment.Application.InvalidDeploymentException)
            {
                var ver = Assembly.GetExecutingAssembly().GetName().Version;
                Title = string.Format("{0} (Asm Ver {1}.{2} Beta)", Strings.AppName, ver.Major, ver.Minor);
            }
        }
    }
}
