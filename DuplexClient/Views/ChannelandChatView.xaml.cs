using DataLibrary;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DuplexClient.Views
{
    // Class that carries the name and raw bytes of a file selected for sharing.
    public class ShareFileEventArgs : EventArgs
    {
        public string FileName { get; }
        public byte[] FileBytes { get; }

        public ShareFileEventArgs(string fileName, byte[] fileBytes)
        {
            FileName = fileName;
            FileBytes = fileBytes;
        }
    }

    /*
     * ChatShellView - Holds channel list (leftside) and conversation panel (rightside)
     * 
     */

    public partial class ChatShellView : UserControl
    {
        // FIELDS
        public event EventHandler SignOutRequested;
        public event EventHandler<string> JoinRequested;
        public event EventHandler<string> CreateChannelRequested;
        public event EventHandler LeaveRequested;
        public event EventHandler<string> SendMessageRequested;
        public event EventHandler<ShareFileEventArgs> ShareFileRequested;
        public event EventHandler<Guid> DownloadFileRequested;
        public event EventHandler<string> PrivateConversationRequested;

        private string joiningChannelName;

        public ChatShellView()
        {
            InitializeComponent();
        }

        // CONSTRUCTOR
        public void SetChannels(List<string> channels)
        {
            string selected = ChannelListBox.SelectedItem as string;

            ChannelListBox.Items.Clear();
            foreach (string channel in channels)
            {
                ChannelListBox.Items.Add(channel);
            }

            if (selected != null && ChannelListBox.Items.Contains(selected))
            {
                ChannelListBox.SelectedItem = selected;
            }
        }

        // METHODS
        public void ShowChannelListStatus(string message) => ChannelListStatusTextBlock.Text = message;

        public void ClearNewChannelName() => NewChannelNameTextBox.Clear();

        public void ShowChannelContent(string channelName)
        {
            CurrentChannelTextBlock.Text = channelName;
            PlaceholderText.Visibility = Visibility.Collapsed;
            ChannelContentPanel.Visibility = Visibility.Visible;
        }

        public void HideChannelContent()
        {
            joiningChannelName = null;
            ChannelListBox.SelectedItem = null;
            PlaceholderText.Visibility = Visibility.Visible;
            ChannelContentPanel.Visibility = Visibility.Collapsed;
        }

        public void ClearConversation()
        {
            MessagesListBox.Items.Clear();
            MembersListBox.Items.Clear();
            SharedFilesListBox.Items.Clear();
            MessageTextBox.Clear();
            SharedFilesStatusTextBlock.Text = "";
        }

        public void AppendMessage(string message)
        {
            MessagesListBox.Items.Add(message);
            if (MessagesListBox.Items.Count > 0)
            {
                MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
            }
        }

        public void SetMembers(List<string> members)
        {
            MembersListBox.Items.Clear();
            foreach (string member in members)
            {
                MembersListBox.Items.Add(member);
            }
        }

        public void SetSharedFiles(List<SharedFileInformation> files)
        {
            object selected = SharedFilesListBox.SelectedItem;

            SharedFilesListBox.Items.Clear();
            foreach (SharedFileInformation file in files)
            {
                SharedFilesListBox.Items.Add(file);
            }

            if (selected != null && SharedFilesListBox.Items.Contains(selected))
            {
                SharedFilesListBox.SelectedItem = selected;
            }
        }

        public void ShowSharedFileStatus(string message) => SharedFilesStatusTextBlock.Text = message;

        public void ClearMessageBox() => MessageTextBox.Clear();

        private void ChannelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChannelListBox.SelectedItem == null) return;

            string channelName = ChannelListBox.SelectedItem.ToString();
            if (channelName == joiningChannelName) return; // Break when already in channel

            joiningChannelName = channelName;
            LeaveRequested?.Invoke(this, EventArgs.Empty);
            JoinRequested?.Invoke(this, channelName);
        }

        private void SignOutButton_Click(object sender, RoutedEventArgs e) =>
            SignOutRequested?.Invoke(this, EventArgs.Empty);

        private void CreateChannelButton_Click(object sender, RoutedEventArgs e)
        {
            string channelName = NewChannelNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(channelName))
            {
                ChannelListStatusTextBlock.Text = "Please enter a channel name.";
                return;
            }
            CreateChannelRequested?.Invoke(this, channelName);
        }

        private void LeaveChannelButton_Click(object sender, RoutedEventArgs e)
        {
            joiningChannelName = null;
            LeaveRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e) => SendMessage();

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) SendMessage();
        }

        private void SendMessage()
        {
            string message = MessageTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(message)) return;
            SendMessageRequested?.Invoke(this, message);
        }

        private void ShareFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Supported files (*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.txt)|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.txt"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(dialog.FileName);

                if (fileBytes.Length > 2 * 1024 * 1024)
                {
                    SharedFilesStatusTextBlock.Text = "File exceeds the 2 MB limit.";
                    return;
                }

                string fileName = System.IO.Path.GetFileName(dialog.FileName);
                ShareFileRequested?.Invoke(this, new ShareFileEventArgs(fileName, fileBytes));
            }
            catch (Exception)
            {
                SharedFilesStatusTextBlock.Text = "Could not read the selected file.";
            }
        }

        private void SharedFilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SharedFilesListBox.SelectedItem is DataLibrary.SharedFileInformation selectedFile)
            {
                DownloadFileRequested?.Invoke(this, selectedFile.FileID);
            }
        }

        private void MembersListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MembersListBox.SelectedItem is string member)
            {
                PrivateConversationRequested?.Invoke(this, member);
            }
        }
    }
}