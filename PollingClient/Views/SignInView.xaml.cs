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

        public void ShowStatus(string message)
        {
            StatusTextBlock.Text = message;
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            SignInRequested?.Invoke(this, UserIDTextBox.Text);
        }
        private void UserIDTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (UserIDTextBox.Text == "Enter your UserID")
            {
                UserIDTextBox.Clear();
            }
        }
    }
}