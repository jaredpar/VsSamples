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

namespace TestErrorMargin
{
    /// <summary>
    /// Interaction logic for TestErrorMarginControl.xaml
    /// </summary>
    public partial class TestErrorMarginControl : UserControl
    {
        internal event EventHandler ThrowError;

        public TestErrorMarginControl()
        {
            InitializeComponent();
        }

        private void CanActivate(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void DoActivate(object sender, ExecutedRoutedEventArgs e)
        {
            if (ThrowError != null)
            {
                ThrowError(this, EventArgs.Empty);
            }
        }
    }
}
