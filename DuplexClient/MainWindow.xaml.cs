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

using DataLibrary;
using DuplexClient.Views;
using System.ServiceModel;

namespace DuplexClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SignInView signInView = new SignInView();
        private readonly ChannelAndChatView channelAndChatView = new ChannelAndChatView();
        private readonly ClientUpdateHandler callbackHandler = new ClientUpdateHandler();
        private readonly Dictionary<string, PrivateMessageWindow> openPrivateWindows = new Dictionary<string, PrivateMessageWindow>();
        private DuplexServerConnection serverConnection;
        private string currentUserID;
        private string currentChannelName;

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

            callbackHandler.ChannelListUpdatedReceived += CallbackHandler_ChannelListUpdatedReceived;
            callbackHandler.ChannelMembersUpdatedReceived += CallbackHandler_ChannelMembersUpdatedReceived;
            callbackHandler.PublicMessageReceivedReceived += CallbackHandler_PublicMessageReceivedReceived;
            callbackHandler.SharedFilesUpdatedReceived += CallbackHandler_SharedFilesUpdatedReceived;
            callbackHandler.PrivateMessageReceivedReceived += CallbackHandler_PrivateMessageReceivedReceived;

            MainContent.Content = signInView;

            Closed += MainWindow_Closed;
        }

        private void SignInView_SignInRequested(object sender, string userID)
        {
            try
            {
                if (serverConnection == null)
                {
                    serverConnection = new DuplexServerConnection(callbackHandler);
                }

                SignInResult result = serverConnection.Service.SignIn(userID);

                if (!result.Success)
                {
                    signInView.ShowStatus(result.Message);
                    return;
                }

                currentUserID = userID.Trim();

                channelAndChatView.SetCurrentUser(currentUserID);

                // Initial state loaded once after sign-in
                List<string> channels = serverConnection.Service.GetChannelList();

                channelAndChatView.SetChannels(channels);
                channelAndChatView.ShowChannelListStatus(channels.Count == 0 ? "No channels currently exist." : "");

                MainContent.Content = channelAndChatView;
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

        private void CallbackHandler_ChannelListUpdatedReceived(object sender, List<string> channels)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                channelAndChatView.SetChannels(channels);

                channelAndChatView.ShowChannelListStatus(channels.Count == 0 ? "No channels currently exist." : "");
            }));
        }

        private void CallbackHandler_ChannelMembersUpdatedReceived(object sender, ChannelMembersUpdatedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (currentChannelName == e.ChannelName)
                {
                    channelAndChatView.SetMembers(e.Members);
                }
            }));
        }

        private void CallbackHandler_PublicMessageReceivedReceived(object sender, PublicMessageReceivedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (currentChannelName == e.ChannelName)
                {
                    channelAndChatView.AppendMessage($"{e.Message.SenderId}: {e.Message.Text}");
                }
            }));
        }

        private void CallbackHandler_SharedFilesUpdatedReceived(object sender, SharedFilesUpdatedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (currentChannelName == e.ChannelName)
                {
                    channelAndChatView.SetSharedFiles(e.Files);
                }
            }));
        }

        private void CallbackHandler_PrivateMessageReceivedReceived(object sender, PrivateMessageReceivedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                string partnerUserID = e.SenderID == currentUserID ? e.RecipientID : e.SenderID;

                if (!openPrivateWindows.ContainsKey(partnerUserID))
                {
                    ChannelAndChatView_PrivateConversationRequested(this, partnerUserID);
                    return;
                }

                PrivateMessageWindow privateWindow = openPrivateWindows[partnerUserID];

                List<ChatMessage> messages = serverConnection.Service.GetPrivateMessages(currentUserID, partnerUserID);

                privateWindow.SetMessages(messages);
            }));
        }

        private void ChannelAndChatView_PrivateConversationRequested(object sender, string partnerUserID)
        {
            if (string.IsNullOrWhiteSpace(partnerUserID)) { return; }

            if (partnerUserID == currentUserID) { return; }

            if (openPrivateWindows.ContainsKey(partnerUserID)) 
            { 
                openPrivateWindows[partnerUserID].Activate();
                return;
            }

            PrivateMessageWindow privateWindow = new PrivateMessageWindow(partnerUserID);

            privateWindow.SendPrivateMessageRequested += PrivateWindow_SendPrivateMessageRequested;

            privateWindow.Closed += (s, e) => { openPrivateWindows.Remove(partnerUserID); };

            openPrivateWindows[partnerUserID] = privateWindow;

            List<ChatMessage> messages = serverConnection.Service.GetPrivateMessages(currentUserID, partnerUserID);

            privateWindow.SetMessages(messages);
            privateWindow.Show();
        }

        private void PrivateWindow_SendPrivateMessageRequested(object sender, string message)
        {
            if (!(sender is PrivateMessageWindow privateWindow)) { return; }

            try
            {
                ChannelActionResult result = serverConnection.Service.SendPrivateMessage(currentUserID, privateWindow.PartnerUserID, message);

                if (!result.Success)
                {
                    privateWindow.ShowStatus(result.Message);
                    return;
                }

                privateWindow.ClearMessageBox();
            }
            catch (CommunicationException)
            {
                privateWindow.ShowStatus("Communication with the chat server failed.");
            }
            catch (Exception)
            {
                privateWindow.ShowStatus("An unexpected error occurred while sending the private message.");
            }
        }

        private void ChannelAndChatView_JoinRequested(object sender, string channelName)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.JoinChannel(currentUserID, channelName);

                if (!result.Success)
                {
                    channelAndChatView.ShowChannelListStatus(result.Message);
                    return;
                }

                currentChannelName = channelName;

                channelAndChatView.ClearConversation();
                channelAndChatView.ShowChannelContent(channelName);

                // Load initial state once after joining
                List<string> members = serverConnection.Service.GetMemberList(channelName);

                List<SharedFileInformation> files = serverConnection.Service.GetSharedFiles(channelName);

                channelAndChatView.SetMembers(members);
                channelAndChatView.SetSharedFiles(files);

                channelAndChatView.ShowChannelListStatus(result.Message);
            }
            catch (CommunicationException)
            {
                channelAndChatView.ShowChannelListStatus("Communication with the chat server failed.");
            }
            catch (Exception)
            {
                channelAndChatView.ShowChannelListStatus("An unexpected error occurred while joining the channel.");
            }
        }

        private void ChannelAndChatView_LeaveRequested(object sender, EventArgs e)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.LeaveChannel(currentUserID);

                if (!result.Success)
                {
                    channelAndChatView.ShowChannelListStatus(result.Message);
                    return;
                }

                currentChannelName = null;

                channelAndChatView.ClearConversation();
                channelAndChatView.HideChannelContent();
                channelAndChatView.SetMembers(new List<string>());
                channelAndChatView.SetSharedFiles(new List<SharedFileInformation>());

                channelAndChatView.ShowChannelListStatus(result.Message);
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

        private void ChannelAndChatView_CreateChannelRequested(object sender, string channelName)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.CreateChannel(currentUserID, channelName);

                channelAndChatView.ShowChannelListStatus(result.Message);

                if (result.Success)
                {
                    channelAndChatView.ClearNewChannelName();
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

        private void ChannelAndChatView_SendMessageRequested(object sender, string message)
        {
            try
            {
                ChannelActionResult result = serverConnection.Service.SendMessage(currentUserID, currentChannelName, message);

                if (!result.Success)
                {
                    channelAndChatView.ShowChannelListStatus(result.Message);
                    return;
                }

                channelAndChatView.ClearMessageBox();
                channelAndChatView.ShowChannelListStatus("");
            }
            catch (CommunicationException)
            {
                channelAndChatView.ShowChannelListStatus("Communication with the chat server failed.");
            }
            catch (Exception)
            {
                channelAndChatView.ShowChannelListStatus("An unexpected error occurred while sending the message.");
            }
        }

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
            try
            {
                if (!string.IsNullOrWhiteSpace(currentUserID))
                {
                    serverConnection.Service.SignOut(currentUserID);
                }
            }
            catch (CommunicationException) { /** connection may be already unavailable */ }

            finally
            {
                currentUserID = null;
                currentChannelName = null;

                foreach (PrivateMessageWindow window in openPrivateWindows.Values.ToList())
                {
                    window.Close();
                }

                openPrivateWindows.Clear();

                channelAndChatView.ClearConversation();
                channelAndChatView.HideChannelContent();

                MainContent.Content = signInView;
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(currentUserID))
                {
                    serverConnection?.Service.SignOut(currentUserID);
                }
            }
            catch { /** ignore shutdown communication errors */ }
        }
    }
}
