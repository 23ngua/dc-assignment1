using PollingClient.Views;
using System.Windows;

namespace PollingClient
{
    public partial class MainWindow : Window
    {
        private readonly SignInView signInView = new SignInView();
        private readonly ChatShellView chatShellView = new ChatShellView();

        public MainWindow()
        {
            InitializeComponent();

            signInView.SignInRequested += (s, userId) =>
            {
                // TODO: replace with real channel names from the server
                chatShellView.SetChannels(new System.Collections.Generic.List<string> { "General" , "Chat 1" });

                MainContent.Content = chatShellView;
            };

            MainContent.Content = signInView;
        }
    }
}
