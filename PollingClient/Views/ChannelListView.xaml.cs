using System;
using System.Windows;
using System.Windows.Controls;

namespace PollingClient.Views
{
    public partial class ChannelListView : UserControl
    {
        public event EventHandler<string> JoinRequested;

        public ChannelListView()
        {
            InitializeComponent();
        }

        private void JoinChannelButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChannelListBox.SelectedItem != null)
            {
                JoinRequested?.Invoke(this, ChannelListBox.SelectedItem.ToString());
            }
        }
    }
}
