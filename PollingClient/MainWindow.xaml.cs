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
using System.ServiceModel.Channels;
using System.Security.Policy;

namespace PollingClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // This stores the WCF connection used to communicate with chat server
        private ChatServerConnection serverConnection;


        // This stores the user ID accepted by server
        private string currentUserId;

        // This stores the channel the client is currently inside
        private string currentChannelName;

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

                // Continue only when sign-in was successful 
                if (result.Success)
                {
                    // Remember the ID accepted for this client
                    currentUserId = userId.Trim();

                    // Ask server for all currently available channels
                    List<ChannelInfo> channels = serverConnection.Service.GetChannels();

                    // Remove any old entries before displaying latest list
                    ChannelListBox.Items.Clear();

                    // Add each server channel to channel list
                    foreach (ChannelInfo channel in channels)
                    {
                        ChannelListBox.Items.Add(channel.Name);
                    }

                    // Give feedback if server currently has no channels
                    if (channels.Count == 0)
                    {
                        ChannelListStatusTextBlock.Text = "No channels currently exist.";
                    }
                    else
                    {
                        ChannelListStatusTextBlock.Text = "";
                    }

                    // Hide sign-in view now that login has succeeded
                    SignInView.Visibility = Visibility.Collapsed;

                    // Show the channel-list view
                    ChannelListView.Visibility = Visibility.Visible;
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

        // Runs when the user clicks the Join Channel button
        private void JoinChannelButton_Click(object sender, RoutedEventArgs e)
        {
            // Make sure user selected a channel first
            if (ChannelListBox.SelectedItem == null)
            {
                ChannelListStatusTextBlock.Text = "Please select a channel to join.";

                return;
            }

            // Get selected channel name from ListBox
            string channelName = ChannelListBox.SelectedItem.ToString();

            try
            {
                // Ask server to join user to selected channel
                ChannelActionResult result = serverConnection.Service.JoinChannel(currentUserId, channelName);

                // Display server's result to user
                ChannelListStatusTextBlock.Text = result.Message;

                // Enter the conversation view only when the server accepts the join
                if (result.Success)
                {
                    // Remember which channel the client successfully joined
                    currentChannelName = channelName;

                    // Show current channel name at top of conversation view
                    CurrentChannelTextBlock.Text = currentChannelName;

                    // Clear any previous channel-list message
                    ChannelListStatusTextBlock.Text = "";

                    // Hide the channel-list view
                    ChannelListView.Visibility = Visibility.Collapsed;

                    // Show channel conversation view
                    ChannelConversationView.Visibility = Visibility.Visible;
                }
            }
            catch (CommunicationException)
            {
                // Handle a WCF communication failure without crashing client
                ChannelListStatusTextBlock.Text = "An unexpected error occurred while joining the channel.";
            }
        }

        // Runs when the user clicks the Leave Channel button
        private void LeaveChannelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ask the server to remove this user from their current channel
                ChannelActionResult result = serverConnection.Service.LeaveChannel(currentUserId);

                // Only return to channel list if server accepted the leave
                if (result.Success)
                {
                    // The client is no longer inside a channel
                    currentChannelName = null;

                    // Clear conversation data from previous channel
                    MessagesListBox.Items.Clear();
                    MembersListBox.Items.Clear();
                    SharedFilesListBox.Items.Clear();
                    MessageTextBox.Clear();

                    // Get a fresh channel list from server
                    List<ChannelInfo> channels = serverConnection.Service.GetChannels();

                    // Replace old channel-list contents
                    ChannelListBox.Items.Clear();

                    foreach (ChannelInfo channel in channels)
                    {
                        ChannelListBox.Items.Add(channel.Name);
                    }

                    // Display successful leave message
                    ChannelListStatusTextBlock.Text = result.Message;

                    // Hide conversation view
                    ChannelConversationView.Visibility = Visibility.Collapsed;

                    // Return to channel-list view
                    ChannelListView.Visibility = Visibility.Visible;
                }
            }
            catch (CommunicationException)
            {
                // Handle a WCF communication problem without crashing
                ChannelListStatusTextBlock.Text = "Communication with the chat server failed.";
            }
            catch (Exception)
            {
                // Handle any unexpected problem cleanly
                ChannelListStatusTextBlock.Text = "An unexpected error occurred while leaving the channel.";
            }
        }

        // Runs when the user clicks the Create Channel button
        private void CreateChannelButton_Click(object sender, RoutedEventArgs e)
        {
            // Read and clean channel name entered by user
            string channelName = NewChannelNameTextBox.Text.Trim();

            // Give immediate feedback when no name was entered
            if (string.IsNullOrWhiteSpace(channelName))
            {
                ChannelListStatusTextBlock.Text = "Please enter a channel name.";

                return;
            }

            try
            {
                // Ask the server to create the new channel
                ChannelActionResult result = serverConnection.Service.CreateChannel(currentUserId, channelName);

                // Display the server's response
                ChannelListStatusTextBlock.Text = result.Message;

                // Refresh the channel list only when creation succeeds
                if (result.Success)
                {
                    // Clear the channel-name input after successful creation
                    NewChannelNameTextBox.Clear();

                    // Ask server for latest authoritative channel lsit
                    List<ChannelInfo> channels = serverConnection.Service.GetChannels();

                    // Remove old list before rebuilding it
                    ChannelListBox.Items.Clear();

                    // Display every current server channel
                    foreach (ChannelInfo channel in channels)
                    {
                        ChannelListBox.Items.Add(channel.Name);
                    }
                }
            }
            catch (CommunicationException)
            {
                // Handle a WCF communication failure without crashing
                ChannelListStatusTextBlock.Text = "Communication with the chat server failed.";
            }
            catch (Exception)
            {
                // Handle any unexpected problem cleanly
                ChannelListStatusTextBlock.Text = "An unexpected error occurred while creating the channel.";
            }
        }
    }
}
