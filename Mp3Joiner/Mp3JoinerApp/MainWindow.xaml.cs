using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Mp3Joiner;

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
            var joiner = new Joiner();
            joiner.JoinTrivial(InputPaths, OutputPath);
        }

        private void InputPathListViewPreviewDrop(object sender, DragEventArgs args)
        {
            var added = false;
            foreach (var ss in args.Data.GetFormats().Select(f => args.Data.GetData(f)).OfType<string[]>())
            {
                foreach (var s in ss.Where(File.Exists))
                {
                    InputPaths.Add(s);
                    added = true;
                }
                if (added)
                {
                    break;
                }
            }
            args.Handled = true;
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
    }
}
