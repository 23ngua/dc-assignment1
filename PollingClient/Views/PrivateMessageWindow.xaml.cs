using DataLibrary;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace PollingClient.Views
{
    public partial class PrivateMessageWindow : Window
    {
        public string PartnerUserID { get; }

        public event EventHandler<string> SendPrivateMessageRequested; // raised when the user wants to send a reply from window

        public PrivateMessageWindow(string partnerUserID)
        {
            InitializeComponent();

            PartnerUserID = partnerUserID;
            HeaderTextBlock.Text = "Private conversation with " + partnerUserID;
            Title = "Private message - " + partnerUserID;
        }

        public void SetMessages(List<ChatMessage> messages)
        {
            MessagesListBox.Items.Clear();
            foreach (ChatMessage msg in messages)
            {
                MessagesListBox.Items.Add(msg.SenderId + ": " + msg.Text);
            }

            if (MessagesListBox.Items.Count > 0)
            {
                MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
            }
        }

        public void ClearMessageBox()
        {
            ReplyTextBox.Clear();
            StatusTextBlock.Text = "";
        }

        public void ShowStatus(string message)
        {
            StatusTextBlock.Text = message;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendReply();
        }

        private void ReplyTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendReply();
            }
        }

        private void SendReply()
        {
            string message = ReplyTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            SendPrivateMessageRequested?.Invoke(this, message);
        }
    }
}