using System;
using System.Windows;
using System.Windows.Controls;

namespace PollingClient.Views
{
    public partial class ChannelConversationView : UserControl
    {
        public event EventHandler LeaveRequested;

        public ChannelConversationView()
        {
            InitializeComponent();
        }

        public void SetChannelName(string channelName)
        {
            CurrentChannelTextBlock.Text = channelName;
        }

        private void LeaveChannelButton_Click(object sender, RoutedEventArgs e)
        {
            LeaveRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
