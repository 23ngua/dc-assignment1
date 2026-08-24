using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ChatShared;
using System.ServiceModel;

namespace PollingClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Stores the WCF connection used to communicate with chat server
        private ChatServerConnection serverConnection;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Runs when user clicks Sign In button
        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            // Read user ID entered into text box
            string userId = UserIdTextBox.Text;

            try
            {
                // Create server connection the first time it is needed
                if (serverConnection == null)
                {
                    serverConnection = new ChatServerConnection();
                }

                // Send sign-in request to WCF server
                SignInResult result = serverConnection.Service.SignIn(userId);

                // Display message returned by server
                StatusTextBlock.Text = result.Message;

                // Prevent this client from signing in again after success
                if (result.Success)
                {
                    UserIdTextBox.IsEnabled = false;
                    SignInButton.IsEnabled = false;
                }
            }
            catch (EndpointNotFoundException)
            {
                // The client could not find running chat server
                StatusTextBlock.Text = "Could not connect to the chat server. Please make sure the server is running.";
            }
            catch (CommunicationException)
            {
                // A WCF communication problem occurred
                StatusTextBlock.Text = "Communication with the chat server failed.";
            }
            catch (Exception)
            {
                // Prevent an unexpected error from crashing client
                StatusTextBlock.Text = "An unexpected error occurred while signing in.";
            }
        }
    }
}
