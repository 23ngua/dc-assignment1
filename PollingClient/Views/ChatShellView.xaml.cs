using System;
using System.Windows;
using System.Windows.Controls;

namespace PollingClient.Views
{
    public partial class ChatShellView : UserControl
    {
        public ChatShellView()
        {
            InitializeComponent();
        }

        // Populates the sidebar.
        public void SetChannels(System.Collections.Generic.List<string> channels)
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

        private void ChannelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChannelListBox.SelectedItem == null)
            {
                PlaceholderText.Visibility = Visibility.Visible;
                ChannelContentPanel.Visibility = Visibility.Collapsed;
                return;
            }

            string channelName = ChannelListBox.SelectedItem.ToString();

            CurrentChannelTextBlock.Text = channelName;

            PlaceholderText.Visibility = Visibility.Collapsed;
            ChannelContentPanel.Visibility = Visibility.Visible;
        }

        // TODO: Placeholder actions

        private void CreateChannelButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO
            MessageBox.Show("Create channel is not implemented yet.");
        }

        private void LeaveChannelButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO
            MessageBox.Show("Leave channel is not implemented yet.");
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO
            MessageBox.Show("Sending messages is not implemented yet.");
        }

        private void ShareFileButton_Click(object sender, RoutedEventArgs e)
        {
            // TOODALOO
            MessageBox.Show("Sharing files is not implemented yet.");
        }
    }
}
