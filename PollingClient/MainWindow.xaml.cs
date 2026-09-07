using PollingClient.Views;
using ChatServerTier;
using ChatResult;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using System.Windows;

namespace PollingClient
{
    public partial class MainWindow : Window
    {
        private readonly SignInView signInView = new SignInView();
        private readonly ChatShellView chatShellView = new ChatShellView();

        private ChatServerConnection serverConnection;
        private string currentUserID;
        private string currentChannelName;

        // Poll Settings
        private Thread pollingThread;
        private volatile bool pollingActive;
        private const int PollingInterval = 1000;

        public MainWindow()
        {
            InitializeComponent();

            // Declare the event handlers to the corresponding method calls
            signInView.SignInRequested += SignInView_SignInRequested;

            chatShellView.JoinRequested += ChatShellView_JoinRequested;
            chatShellView.CreateChannelRequested += ChatShellView_CreateChannelRequested;
            chatShellView.LeaveRequested += ChatShellView_LeaveRequested;
            chatShellView.SendMessageRequested += ChatShellView_SendMessageRequested;
            chatShellView.ShareFileRequested += ChatShellView_ShareFileRequested;
            chatShellView.DownloadFileRequested += ChatShellView_DownloadFileRequested;
            chatShellView.SignOutRequested += ChatShellView_SignOutRequested;

            MainContent.Content = signInView;
            Closed += MainWindow_Closed;
        }

        /* --- SIGN IN --- */
        private void SignInView_SignInRequested(object sender, string userID)
        {
            try
            {
                if (serverConnection == null)
                {
                    serverConnection = new ChatServerConnection();
                }

                SignInResult result = serverConnection.Service.SignIn(userID);

                if (!result.Success)
                {
                    signInView.ShowStatus(result.Message);
                    return;
                }

                currentUserID = userID.Trim();

                List<string> channels = serverConnection.Service.GetChannelList();
                chatShellView.SetChannels(channels);
                chatShellView.ShowChannelListStatus(channels.Count == 0 ? "No channels currently exist." : "");

                MainContent.Content = chatShellView;
                StartPolling();
            }
            catch (EndpointNotFoundException)
            {
                signInView.ShowStatus("Could not connect to the chat server. Please make sure the server is running.");
            }
            catch (CommunicationException)
            {
                signInView.ShowStatus("Communication with the chat server failed.");
            }
            catch (Exception)
            {
                signInView.ShowStatus("An unexpected error occurred while signing in.");
            }
        }

        /* --- CHANNELS --- */

        private void ChatShellView_JoinRequested(object sender, string channelName)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.JoinChannel(currentUserID, channelName);
                chatShellView.ShowChannelListStatus(result.Message);

                if (result.Success)
                {
                    currentChannelName = channelName;
                    chatShellView.ShowChannelContent(channelName);
                }
                else
                {
                    chatShellView.HideChannelContent();
                }
            }
            catch (CommunicationException)
            {
                chatShellView.ShowChannelListStatus("An unexpected error occurred while joining the channel.");
            }
        }

        private void ChatShellView_CreateChannelRequested(object sender, string channelName)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.CreateChannel(currentUserID, channelName);
                chatShellView.ShowChannelListStatus(result.Message);

                if (result.Success)
                {
                    chatShellView.ClearNewChannelName();
                    chatShellView.SetChannels(serverConnection.Service.GetChannelList());
                }
            }
            catch (CommunicationException)
            {
                chatShellView.ShowChannelListStatus("Communication with the chat server failed.");
            }
            catch (Exception)
            {
                chatShellView.ShowChannelListStatus("An unexpected error occurred while creating the channel.");
            }
        }

        private void ChatShellView_LeaveRequested(object sender, EventArgs e)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.LeaveChannel(currentUserID);

                if (result.Success)
                {
                    currentChannelName = null;
                    chatShellView.ClearConversation();
                    chatShellView.HideChannelContent();
                    chatShellView.SetChannels(serverConnection.Service.GetChannelList());
                    chatShellView.ShowChannelListStatus(result.Message);
                }
            }
            catch (CommunicationException)
            {
                chatShellView.ShowChannelListStatus("Communication with the chat server failed.");
            }
            catch (Exception)
            {
                chatShellView.ShowChannelListStatus("An unexpected error occurred while leaving the channel.");
            }
        }

        private void ChatShellView_SendMessageRequested(object sender, string message)
        {
            try
            {
                serverConnection.Service.SendMessage(currentChannelName, message);
                chatShellView.ClearMessageBox();
            }
            catch (CommunicationException)
            {
                chatShellView.ShowSharedFileStatus("Communication with the chat server failed.");
            }
        }

        /* --- FILES --- */

        private void ChatShellView_ShareFileRequested(object sender, ShareFileEventArgs e)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.ShareFile(currentUserID, currentChannelName, e.FileName, e.FileBytes);
                chatShellView.ShowSharedFileStatus(result.Message);
            }
            catch (CommunicationException)
            {
                chatShellView.ShowSharedFileStatus("Communication with the chat server failed.");
            }
            catch (Exception)
            {
                chatShellView.ShowSharedFileStatus("An unexpected error occurred while sharing the file.");
            }
        }

        private void ChatShellView_DownloadFileRequested(object sender, Guid fileID)
        {
            try
            {
                FileDownloadResult result = serverConnection.Service.DownloadFile(currentUserID, fileID);

                if (!result.Success)
                {
                    chatShellView.ShowSharedFileStatus(result.Message);
                    return;
                }

                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), result.FileName);
                System.IO.File.WriteAllBytes(tempPath, result.FileBytes);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            catch (CommunicationException)
            {
                chatShellView.ShowSharedFileStatus("Communication with the chat server failed.");
            }
            catch (Exception)
            {
                chatShellView.ShowSharedFileStatus("An unexpected error occurred while opening the file.");
            }
        }

        /* --- MESSAGES --- */

        private void ChatShellView_SignOutRequested(object sender, EventArgs e)
        {
            SignOut();
            currentChannelName = null;
            currentUserID = null;

            chatShellView.ClearConversation();
            chatShellView.HideChannelContent();
            MainContent.Content = signInView;
        }

        private void SignOut()
        {
            pollingActive = false;
            try
            {
                if (serverConnection != null && !string.IsNullOrWhiteSpace(currentUserID))
                {
                    serverConnection.Service.SignOut(currentUserID);
                }
            }
            catch (Exception)
            {
                // Ignore errors during shutdown
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e) => SignOut();

        /* --- POLLING --- */

        private void StartPolling()
        {
            if (pollingThread != null && pollingThread.IsAlive) return;

            pollingActive = true;
            pollingThread = new Thread(PollingLoop) { IsBackground = true };
            pollingThread.Start();
        }

        private void PollingLoop()
        {
            ChatServerConnection pollingConnection = null;

            while (pollingActive)
            {
                try
                {
                    if (pollingConnection == null)
                    {
                        pollingConnection = new ChatServerConnection();
                    }

                    List<string> channels = pollingConnection.Service.GetChannelList();

                    Dispatcher.Invoke(() =>
                    {
                        if (currentChannelName == null)
                        {
                            chatShellView.SetChannels(channels);
                        }
                    });

                    string channelToPoll = currentChannelName;

                    if (channelToPoll != null)
                    {
                        List<SharedFileInformation> files = pollingConnection.Service.GetSharedFiles(channelToPoll);
                        string message = pollingConnection.Service.GetNewestMessage(channelToPoll);

                        Dispatcher.Invoke(() =>
                        {
                            if (currentChannelName == channelToPoll)
                            {
                                chatShellView.SetSharedFiles(files);

                                if (!string.IsNullOrEmpty(message))
                                {
                                    chatShellView.AppendMessage(message);
                                }
                            }
                        });
                    }
                }
                catch (CommunicationException)
                {
                    pollingConnection = null;
                }
                catch (Exception)
                {
                    pollingConnection = null;
                }

                Thread.Sleep(PollingInterval);
            }
        }
    }
}