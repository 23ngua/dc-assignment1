using DataLibrary;
using ServerTier;
using PollingClient.Views;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Documents;

namespace PollingClient
{
    public partial class MainWindow : Window
    {
        private readonly SignInView signInView = new SignInView();
        private readonly ChannelAndChatView channelAndChatView = new ChannelAndChatView();
        private readonly Dictionary<string, PrivateMessageWindow> openPrivateWindows = new Dictionary<string, PrivateMessageWindow>();
        private readonly Dictionary<string, int> privateMessageSeenCounts = new Dictionary<string, int>();

        private ServerConnection serverConnection;
        private string currentUserID;
        private string currentChannelName;
        private volatile int lastMessageIndex = 0; // Tracks message index, so user can only see messages onwards, can be changed by multipe threads

        // Poll Settings
        private Thread pollingThread;
        private volatile bool pollingActive;
        private const int PollingInterval = 300;

        public MainWindow()
        {
            InitializeComponent();

            signInView.SignInRequested += SignInView_SignInRequested;

            channelAndChatView.JoinRequested += ChannelAndChatView_JoinRequested;
            channelAndChatView.CreateChannelRequested += ChannelAndChatView_CreateChannelRequested;
            channelAndChatView.LeaveRequested += ChannelAndChatView_LeaveRequested;
            channelAndChatView.SendMessageRequested += ChannelAndChatView_SendMessageRequested;
            channelAndChatView.ShareFileRequested += ChannelAndChatView_ShareFileRequested;
            channelAndChatView.DownloadFileRequested += ChannelAndChatView_DownloadFileRequested;
            channelAndChatView.SignOutRequested += ChannelAndChatView_SignOutRequested;
            channelAndChatView.PrivateConversationRequested += ChannelAndChatView_PrivateConversationRequested;

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
                    serverConnection = new ServerConnection();
                }

                SignInResult result = serverConnection.Service.SignIn(userID);

                if (!result.Success)
                {
                    signInView.ShowStatus(result.Message);
                    return;
                }

                currentUserID = userID.Trim();
                channelAndChatView.SetCurrentUser(currentUserID);

                List<string> channels = serverConnection.Service.GetChannelList();
                channelAndChatView.SetChannels(channels);
                channelAndChatView.ShowChannelListStatus(channels.Count == 0 ? "No channels currently exist." : "");

                MainContent.Content = channelAndChatView;
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

        private void ChannelAndChatView_JoinRequested(object sender, string channelName)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.JoinChannel(currentUserID, channelName);
                channelAndChatView.ShowChannelListStatus(result.Message);

                if (result.Success)
                {
                    currentChannelName = channelName;
                    lastMessageIndex = serverConnection.Service.GetMessageCount(channelName); // Get index for messages onwards

                    channelAndChatView.ClearConversation();
                    channelAndChatView.ShowChannelContent(channelName);
                }
                else
                {
                    channelAndChatView.HideChannelContent();
                }
            }
            catch (CommunicationException)
            {
                channelAndChatView.ShowChannelListStatus("An unexpected error occurred while joining the channel.");
            }
        }

        private void ChannelAndChatView_CreateChannelRequested(object sender, string channelName)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.CreateChannel(currentUserID, channelName);
                channelAndChatView.ShowChannelListStatus(result.Message);

                if (result.Success)
                {
                    channelAndChatView.ClearNewChannelName();
                    channelAndChatView.SetChannels(serverConnection.Service.GetChannelList());
                }
            }
            catch (CommunicationException)
            {
                channelAndChatView.ShowChannelListStatus("Communication with the chat server failed.");
            }
            catch (Exception)
            {
                channelAndChatView.ShowChannelListStatus("An unexpected error occurred while creating the channel.");
            }
        }

        private void ChannelAndChatView_LeaveRequested(object sender, EventArgs e)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.LeaveChannel(currentUserID);

                if (result.Success)
                {
                    currentChannelName = null;
                    lastMessageIndex = 0;
                    channelAndChatView.ClearConversation();
                    channelAndChatView.HideChannelContent();
                    channelAndChatView.SetChannels(serverConnection.Service.GetChannelList());
                    channelAndChatView.ShowChannelListStatus(result.Message);
                }
            }
            catch (CommunicationException)
            {
                channelAndChatView.ShowChannelListStatus("Communication with the chat server failed.");
            }
            catch (Exception)
            {
                channelAndChatView.ShowChannelListStatus("An unexpected error occurred while leaving the channel.");
            }
        }

        /* --- MESSAGES (Inc Private) --- */

        private void ChannelAndChatView_SendMessageRequested(object sender, string message)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.SendMessage(currentUserID, currentChannelName, message);

                if (result.Success)
                {
                    channelAndChatView.ClearMessageBox();
                }
                else
                {
                    channelAndChatView.ShowSharedFileStatus(result.Message);
                }
            }
            catch (CommunicationException)
            {
                channelAndChatView.ShowSharedFileStatus("Communication with the chat server failed.");
            }
        }

        private void ChannelAndChatView_PrivateConversationRequested(object sender, string partnerUserID)
        {
            ShowPrivateWindowFor(partnerUserID);
        }

        private void ShowPrivateWindowFor(string partnerUserID)
        {
            if (partnerUserID == currentUserID)
            {
                return;
            }

            if (openPrivateWindows.ContainsKey(partnerUserID))
            {
                openPrivateWindows[partnerUserID].Activate();
                return;
            }

            PrivateMessageWindow newWindow = new PrivateMessageWindow(partnerUserID);
            newWindow.SendPrivateMessageRequested += PrivateWindow_SendPrivateMessageRequested; // add to notification list
            newWindow.Closed += (s, e) => PrivateWindow_Closed(partnerUserID);//when 'Closed' fires, run lambda. s and e are the sender and event args

            openPrivateWindows.Add(partnerUserID, newWindow);

            RefreshPrivateWindowContent(partnerUserID, newWindow);

            newWindow.Show();
        }

        private void RefreshPrivateWindowContent(string partnerUserID, PrivateMessageWindow window)
        {
            try
            {
                List<ChatMessage> fullHistory = serverConnection.Service.GetPrivateMessages(currentUserID, partnerUserID);

                window.SetMessages(fullHistory); // clears and repopulates and scrolls to bottom

                privateMessageSeenCounts[partnerUserID] = fullHistory.Count;
            }
            catch (Exception)
            {
                // on fail, the next poll try open window
            }
        }

        private void PrivateWindow_Closed(string partnerUserID)
        {
            openPrivateWindows.Remove(partnerUserID);
        }

        private void PrivateWindow_SendPrivateMessageRequested(object sender, string message)
        {
            PrivateMessageWindow window = sender as PrivateMessageWindow;
            if (window == null) return;

            try
            {
                ChannelActionResult result = serverConnection.Service.SendPrivateMessage(currentUserID, window.PartnerUserID, message);

                if (result.Success)
                {
                    window.ClearMessageBox();
                }
                else
                {
                    window.ShowStatus(result.Message);
                }
            }
            catch (CommunicationException)
            {
                window.ShowStatus("Communication with the chat server failed.");
            }
        }

        private void CloseAllPrivateWindows()
        {
            List<PrivateMessageWindow> windows = new List<PrivateMessageWindow>(openPrivateWindows.Values);
            foreach (PrivateMessageWindow window in windows)
            {
                window.Close();
            }
            openPrivateWindows.Clear();
        }

        /* --- FILES --- */

        private void ChannelAndChatView_ShareFileRequested(object sender, ShareFileEventArgs e)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.ShareFile(currentUserID, currentChannelName, e.FileName, e.FileBytes);
                channelAndChatView.ShowSharedFileStatus(result.Message);
            }
            catch (CommunicationException)
            {
                channelAndChatView.ShowSharedFileStatus("Communication with the chat server failed.");
            }
            catch (Exception)
            {
                channelAndChatView.ShowSharedFileStatus("An unexpected error occurred while sharing the file.");
            }
        }

        private void ChannelAndChatView_DownloadFileRequested(object sender, Guid fileID)
        {
            try
            {
                FileDownloadResult result = serverConnection.Service.DownloadFile(currentUserID, fileID);

                if (!result.Success)
                {
                    channelAndChatView.ShowSharedFileStatus(result.Message);
                    return;
                }

                string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), result.FileName);
                System.IO.File.WriteAllBytes(tempPath, result.FileBytes);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
            catch (CommunicationException)
            {
                channelAndChatView.ShowSharedFileStatus("Communication with the chat server failed.");
            }
            catch (Exception)
            {
                channelAndChatView.ShowSharedFileStatus("An unexpected error occurred while opening the file.");
            }
        }

        private void ChannelAndChatView_SignOutRequested(object sender, EventArgs e)
        {
            SignOut();
            currentChannelName = null;
            currentUserID = null;
            lastMessageIndex = 0;

            CloseAllPrivateWindows();
            channelAndChatView.ClearConversation();
            channelAndChatView.HideChannelContent();
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
            ServerConnection pollingConnection = null;

            while (pollingActive)
            {
                try
                {
                    if (pollingConnection == null)
                    {
                        pollingConnection = new ServerConnection();
                    }

                    List<string> channels = pollingConnection.Service.GetChannelList();

                    Dispatcher.Invoke(() =>
                    {
                            channelAndChatView.SetChannels(channels); // Always update channel list 
                    });

                    string channelToPoll = currentChannelName;

                    if (channelToPoll != null)
                    {
                        List<string> members = pollingConnection.Service.GetMemberList(channelToPoll);
                        List<SharedFileInformation> files = pollingConnection.Service.GetSharedFiles(channelToPoll);
                        List<ChatMessage> newMessages = pollingConnection.Service.GetMessagesSince(channelToPoll, lastMessageIndex);

                        Dispatcher.Invoke(() =>
                        {
                            // Guard against the user switching/leaving channels mid-poll
                            if (currentChannelName == channelToPoll)
                            {
                                channelAndChatView.SetMembers(members);
                                channelAndChatView.SetSharedFiles(files);

                                foreach (ChatMessage msg in newMessages)
                                {
                                    channelAndChatView.AppendMessage($"{msg.SenderId}: {msg.Text}");
                                }
                            }
                        });

                        if (currentChannelName == channelToPoll)
                        {
                            lastMessageIndex += newMessages.Count;
                        }
                    }

                    /* --- PRIVATE MESSAGES --- */
                    List<string> partners = pollingConnection.Service.GetPrivateConversationPartners(currentUserID);

                    foreach (string partner in partners)
                    {
                        bool windowIsOpen = false;
                        Dispatcher.Invoke(() => { windowIsOpen = openPrivateWindows.ContainsKey(partner); });

                        if (windowIsOpen)
                        {
                            // window open = fresh content 
                            Dispatcher.Invoke(() =>
                            {
                                PrivateMessageWindow window = openPrivateWindows[partner];
                                RefreshPrivateWindowContent(partner, window);
                            });
                        }
                        else
                        {
                            // window not open = check if user hasnt seen any messages yet
                            int alreadySeenCount = 0;
                            Dispatcher.Invoke(() => { privateMessageSeenCounts.TryGetValue(partner, out alreadySeenCount); });

                            List<ChatMessage> conversation = pollingConnection.Service.GetPrivateMessages(currentUserID, partner);
                            int actualCount = conversation.Count;

                            if (actualCount > alreadySeenCount)
                            {
                                Dispatcher.Invoke(() => ShowPrivateWindowFor(partner));
                            }
                        }
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