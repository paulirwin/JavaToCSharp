using Caliburn.Micro;
using JavaToCSharp;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

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
    }
}