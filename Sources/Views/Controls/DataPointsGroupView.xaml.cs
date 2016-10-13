using System.Windows.Controls;
using System.Windows.Input;
using ForumParser.ViewModels.Controls;

namespace ForumParser.Views.Controls
{
    /// <summary>
    /// Interaction logic for DataPointsGroupView.xaml
    /// </summary>
    public partial class DataPointsGroupView : UserControl
    {
        public DataPointsGroupView()
        {
            InitializeComponent();
        }

        private void DataPointText_MouseDoubleClick( object sender, MouseButtonEventArgs e )
        {
            var text = (sender as ContentControl)?.Content?.ToString();
            if (text != null)
                (DataContext as DataPointsGroup)?.SetTextCustomCommand?.Execute( text );

        }
    }
}
