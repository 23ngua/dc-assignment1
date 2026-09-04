using System;
using System.Windows;
using System.Windows.Controls;

namespace PollingClient.Views
{
    public partial class SignInView : UserControl
    {
        public event EventHandler<string> SignInRequested;

        public SignInView()
        {
            InitializeComponent();
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            SignInRequested?.Invoke(this, UserIdTextBox.Text);
        }
    }
}
