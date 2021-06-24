using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using JavaToCSharp;
using Microsoft.Win32;

namespace JavaToCSharpGui.ViewModels
{
    public class ShellViewModel : Screen, IShell
    {
        private readonly ObservableCollection<string> _usings = new ObservableCollection<string>(new JavaConversionOptions().Usings);
        private string _addUsingInput;
        private string _javaText;
        private string _csharpText;
        private string _openPath;
        private string _savePath;
        private string _copiedText;
        private string _conversionState;
        private bool _includeUsings = true;
        private bool _includeNamespace = true;
        private bool _useDebugAssertForAsserts = false;

        public ShellViewModel()
        {
            base.DisplayName = "Java to C# Converter";

            _includeUsings = Properties.Settings.Default.UseUsingsPreference;
            _includeNamespace = Properties.Settings.Default.UseNamespacePreference;
            _useDebugAssertForAsserts = Properties.Settings.Default.UseDebugAssertPreference;
        }

        public ObservableCollection<string> Usings
        {
            get { return _usings; }
        }

        public string AddUsingInput
        {
            get { return _addUsingInput; }
            set
            {
                _addUsingInput = value;
                NotifyOfPropertyChange(() => AddUsingInput);
            }
        }

        public string JavaText
        {
            get { return _javaText; }
            set
            {
                _javaText = value;
                NotifyOfPropertyChange(() => JavaText);
            }
        }

        public string CSharpText
        {
            get { return _csharpText; }
            set
            {
                _csharpText = value;
                NotifyOfPropertyChange(() => CSharpText);
            }
        }

        public string OpenPath
        {
            get { return _openPath; }
            set
            {
                _openPath = value;
                NotifyOfPropertyChange(() => OpenPath);
            }
        }

        public string CopiedText
        {
            get { return _copiedText; }
            set
            {
                _copiedText = value;
                NotifyOfPropertyChange(() => CopiedText);
            }
        }

        public string ConversionStateLabel
        {
            get { return _conversionState; }
            set
            {
                _conversionState = value;
                NotifyOfPropertyChange(() => ConversionStateLabel);
            }
        }

        public bool IncludeUsings
        {
            get { return _includeUsings; }
            set
            {
                _includeUsings = value;
                NotifyOfPropertyChange(() => IncludeUsings);
                Properties.Settings.Default.UseUsingsPreference = value;
                Properties.Settings.Default.Save();
            }
        }

        public bool IncludeNamespace
        {
            get { return _includeNamespace; }
            set
            {
                _includeNamespace = value;
                NotifyOfPropertyChange(() => IncludeNamespace);
                Properties.Settings.Default.UseNamespacePreference = value;
                Properties.Settings.Default.Save();
            }
        }

        public bool UseDebugAssertForAsserts
        {
            get { return _useDebugAssertForAsserts; }
            set
            {
                _useDebugAssertForAsserts = value;
                NotifyOfPropertyChange(() => UseDebugAssertForAsserts);
                Properties.Settings.Default.UseDebugAssertPreference = value;
                Properties.Settings.Default.Save();

                if (value && !_usings.Contains("System.Diagnostics"))
                {
                    _addUsingInput = "System.Diagnostics";
                    AddUsing();
                }
            }
        }

        public void AddUsing()
        {
            _usings.Add(_addUsingInput);
            AddUsingInput = string.Empty;
        }

        public void RemoveUsing(string value)
        {
            _usings.Remove(value);
        }

        public void Convert()
        {
            var options = new JavaConversionOptions();
            options.ClearUsings();

            foreach (var ns in _usings)
            {
                options.AddUsing(ns);
            }

            options.IncludeUsings = _includeUsings;
            options.IncludeNamespace = _includeNamespace;
            options.UseDebugAssertForAsserts = _useDebugAssertForAsserts;

            options.WarningEncountered += options_WarningEncountered;
            options.StateChanged += options_StateChanged;

            Task.Run(() =>
            {
                try
                {
                    var csharp = JavaToCSharpConverter.ConvertText(JavaText, options);

                    Dispatcher.CurrentDispatcher.Invoke(() => this.CSharpText = csharp);
                }
                catch (Exception ex)
                {
                    Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        MessageBox.Show("There was an error converting the text to C#: " + ex.Message);
                    });
                }
            });
        }

        void options_StateChanged(object sender, ConversionStateChangedEventArgs e)
        {
            switch (e.NewState)
            {
                case ConversionState.Starting:
                    ConversionStateLabel = "Starting...";
                    break;
                case ConversionState.ParsingJavaAst:
                    ConversionStateLabel = "Parsing Java code...";
                    break;
                case ConversionState.BuildingCSharpAst:
                    ConversionStateLabel = "Building C# AST...";
                    break;
                case ConversionState.Done:
                    ConversionStateLabel = "Done!";
                    break;
                default:
                    break;
            }
        }

        void options_WarningEncountered(object sender, ConversionWarningEventArgs e)
        {
            MessageBox.Show("Java Line " + e.JavaLineNumber + ": " + e.Message, "Warning Encountered");
        }

        public void OpenFileDialog()
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Java Files (*.java)|*.java";
            ofd.Title = "Open Java File";

            var result = ofd.ShowDialog();

            if (result.GetValueOrDefault())
            {
                OpenPath = ofd.FileName;
                JavaText = File.ReadAllText(ofd.FileName);
            }
        }

        public void CopyOutput()
        {
            Clipboard.SetText(CSharpText);

            CopiedText = "Copied!";

            Task.Run(async () =>
            {
                await Task.Delay(5000);
                CopiedText = null;
            });
        }

        public void ForkMeOnGitHub()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "http://www.github.com/paulirwin/javatocsharp",
                UseShellExecute = true
            });
        }
    }
}