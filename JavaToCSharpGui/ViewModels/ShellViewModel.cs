using Caliburn.Micro;
using JavaToCSharp;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace JavaToCSharpGui
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
        private bool _includeUsings = true;
        private bool _includeNamespace = true;
        private bool _useDebugAssertForAsserts = false;

        public ShellViewModel()
        {
            base.DisplayName = "Java to C# Converter";
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

        public bool IncludeUsings
        {
            get { return _includeUsings; }
            set
            {
                _includeUsings = value;
                NotifyOfPropertyChange(() => IncludeUsings);
            }
        }

        public bool IncludeNamespace
        {
            get { return _includeNamespace; }
            set
            {
                _includeNamespace = value;
                NotifyOfPropertyChange(() => IncludeNamespace);
            }
        }

        public bool UseDebugAssertForAsserts
        {
            get { return _useDebugAssertForAsserts; }
            set
            {
                _useDebugAssertForAsserts = value;
                NotifyOfPropertyChange(() => UseDebugAssertForAsserts);

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

            try
            {
                CSharpText = JavaToCSharpConverter.ConvertText(JavaText, options);
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error converting the text to C#: " + ex.Message);
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
            UrlLauncher.UrlLauncher.LaunchUrl("http://www.github.com/paulirwin/javatocsharp");
        }
    }
}