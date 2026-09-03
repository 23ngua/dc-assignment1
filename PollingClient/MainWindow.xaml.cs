using ChatShared;
using ChatShared.FileSharing;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
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

        // Background thread used to repeatedly request updates from the server
        private Thread pollingThread;

        // Controls whether the background polling loop should continue running
        private volatile bool pollingActive;

        // Delay between polling requests in milliseconds
        private const int PollingIntervalMs = 1000;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Runs continuously on background polling thread
        private void PollingLoop()
        {
            // Give the polling thread its own WCF connection
            ChatServerConnection pollingConnection = null;

            // Continue until the client tells the polling thread to stop
            while (pollingActive)
            {
                try
                {
                    // Create the polling connection the first time it is needed
                    if (pollingConnection == null)
                    {
                        pollingConnection = new ChatServerConnection();
                    }

                    // Ask the server for the latest channel list
                    List<ChannelInfo> channels = pollingConnection.Service.GetChannels();

                    // WPF controls must only be changed on the UI thread
                    Dispatcher.Invoke(() =>
                    {
                        // Only refresh this lsit while the channel-list veiw is visible
                        if (ChannelListView.Visibility == Visibility.Visible)
                        {
                            // Remember the currently selected channel if there is no one
                            string selectedChannel = ChannelListBox.SelectedItem as string;

                            // Replace the old channel snapshot
                            ChannelListBox.Items.Clear();

                            // Display every current channel returned by the server
                            foreach (ChannelInfo channel in channels)
                            {
                                ChannelListBox.Items.Add(channel.Name);
                            }

                            // Restore the previous selection if that channel still exists
                            if (selectedChannel != null && ChannelListBox.Items.Contains(selectedChannel))
                            {
                                ChannelListBox.SelectedItem = selectedChannel;
                            }
                        }
                    });
                }
                catch (CommunicationException)
                {
                    // Discard a failed WCF connection so the next poll can reconnect
                    pollingConnection = null;
                }
                catch (Exception)
                {
                    // Keep the polling thread alive after an unexpected polling error
                    pollingConnection = null;
                }

                // Wait before asking the server for another update
                Thread.Sleep(PollingIntervalMs);
            }
        }

        // Starts the background polling thread
        private void StartPolling()
        {
            // Do not start another polling thread if one is already running
            if (pollingThread != null && pollingThread.IsAlive)
            {
                return;
            }

            // Tell the polling loop that it should continue running
            pollingActive = true;

            // Create a new thread that will execute PollingLoop()
            pollingThread = new Thread(PollingLoop);

            // Allow the application to close even if this thread is still running
            pollingThread.IsBackground = true;

            // Begin executing the polling loop in the background
            pollingThread.Start();
        }

        // Runs when the main client window has been closed
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // Tell the background polling loop to stop running
            pollingActive = false;

            try
            {
                // Only attempt sign-out if this client actually signed in
                if (serverConnection != null && !string.IsNullOrWhiteSpace(currentUserId))
                {
                    // Ask the server to release this user's session and membership
                    serverConnection.Service.SignOut(currentUserId);
                }
            }
            catch (CommunicationException)
            {
                // Ignore shutdown communication errors because the window is already closing
            }
            catch (Exception)
            {
                // Prevent an unexpected shutdown error from blocking application exit
            }
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

                    // Start background polling now that the client is signed in
                    StartPolling();
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

        // FILE SHARING METHODS
        // Runs when the user clicks the Share File button
        private void ShareFileButton_Click(object sender, RoutedEventArgs e)
        {
            // Let the user pick a file, restricted to the allowed types up front
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Allowed files (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.txt)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.txt"
            };

            // Do nothing further if the user cancelled the dialog
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                // Read the chosen file's bytes from disk
                byte[] content = File.ReadAllBytes(dialog.FileName);
                string fileName = System.IO.Path.GetFileName(dialog.FileName);

                // Check the size on the client first
                if (content.Length > FileSharingRules.MaxFileSizeBytes)
                {
                    FileStatusTextBlock.Text = "That file is too large. The maximum size is 2 MB.";
                    return;
                }

                // Send the file to the server to be shared into the current channel
                FileUploadResult result = serverConnection.Service.UploadFile(
                    currentUserId, currentChannelName, fileName, content);

                // Show the server's response either way
                FileStatusTextBlock.Text = result.Message;

                // Refresh the list immediately so the sender sees their own file appear right away
                if (result.Success)
                {
                    RefreshSharedFiles(serverConnection);
                }
            }
            catch (CommunicationException)
            {
                FileStatusTextBlock.Text = "Communication with the chat server failed.";
            }
            catch (Exception)
            {
                FileStatusTextBlock.Text = "An unexpected error occurred while sharing the file.";
            }
        }
    }
}
