using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VsErrorHandler
{
    public partial class ErrorMarginControl : UserControl
    {
        public static readonly DependencyProperty HeaderTextProperty = DependencyProperty.Register(
            "HeaderText",
            typeof(string),
            typeof(ErrorMarginControl));

        public static readonly DependencyProperty ErrorTextProperty = DependencyProperty.Register(
            "ErrorText",
            typeof(string),
            typeof(ErrorMarginControl));

        public static readonly DependencyProperty ErrorTextVisibilityProperty = DependencyProperty.Register(
            "ErrorTextVisibility",
            typeof(Visibility),
            typeof(ErrorMarginControl),
            new PropertyMetadata(Visibility.Collapsed));

        private Exception _exception;

        public string HeaderText
        {
            get { return (string)GetValue(HeaderTextProperty); }
            set { SetValue(HeaderTextProperty, value); }
        }

        public string ErrorText
        {
            get { return (string)GetValue(ErrorTextProperty); }
            set { SetValue(ErrorTextProperty, value); }
        }

        public Visibility ErrorTextVisibility
        {
            get { return (Visibility)GetValue(ErrorTextVisibilityProperty); }
            set { SetValue(ErrorTextVisibilityProperty, value); }
        }

        public Exception Exception
        {
            get { return _exception; }
            set
            {
                _exception = value;
                if (_exception != null)
                {
                    HeaderText = String.Format("Unhandled Exception of Type '{0}' Occurred", _exception.GetType().Name);
                    ErrorText = FormatErrorText(_exception);
                }
                else
                {
                    HeaderText = String.Empty;
                    ErrorText = String.Empty;
                }
            }
        }

        public event EventHandler IgnoreError;
        public event EventHandler IgnoreAllErrors;

        public ErrorMarginControl()
        {
            InitializeComponent();
        }

        private void OnShowClick(object sender, RoutedEventArgs e)
        {
            ErrorTextVisibility = Visibility.Visible;
        }

        private void OnIgnoreClick(object sender, RoutedEventArgs e)
        {
            ErrorTextVisibility = Visibility.Collapsed;
            if (IgnoreError != null)
            {
                IgnoreError(this, EventArgs.Empty);
            }
        }

        private void OnIgnoreAllClick(object sender, RoutedEventArgs e)
        {
            ErrorTextVisibility = Visibility.Collapsed;
            if (IgnoreAllErrors != null)
            {
                IgnoreAllErrors(this, EventArgs.Empty);
            }
        }

        private static string FormatErrorText(Exception exception)
        {
            var builder = new System.Text.StringBuilder();
            FormatErrorText(builder, exception);
            return builder.ToString();
        }

        private static void FormatErrorText(StringBuilder builder, Exception exception)
        {
            builder.AppendLine(String.Format("Exception: {0}", exception.GetType()));
            builder.AppendLine("Stack Trace:");
            builder.AppendLine(exception.StackTrace);
            if (exception.InnerException != null)
            {
                builder.AppendLine();
                FormatErrorText(builder, exception.InnerException);
            }
        }
    }
}
