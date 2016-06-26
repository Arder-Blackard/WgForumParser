using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace ForumParser.Views.Behaviors
{
    public class AutoScroller : Behavior<ListBox>
    {
        #region Constants

        public static readonly DependencyProperty AutoScrollTriggerProperty = DependencyProperty.Register(
            "AutoScrollTrigger",
            typeof (object),
            typeof (AutoScroller),
            new PropertyMetadata( null, AutoScrollTriggerPropertyChanged ) );

        #endregion


        #region Auto-properties

        public bool AutoScroll { get; set; }

        #endregion


        #region Properties

        public object AutoScrollTrigger
        {
            get { return GetValue( AutoScrollTriggerProperty ); }
            set { SetValue( AutoScrollTriggerProperty, value ); }
        }

        #endregion


        #region Events and invocation

        private void OnCollectionChanged( object sender, EventArgs args )
        {
            Application.Current.Dispatcher.Invoke( () =>
            {
                if ( AutoScroll )
                    ScrollToEnd( AssociatedObject );
            } );
        }

        #endregion


        #region Non-public methods

        private static void ScrollToEnd( ListBox listView )
        {
            var count = listView.Items.Count;
            if ( count == 0 )
                return;
            var item = listView.Items[count - 1];
            listView.ScrollIntoView( item );
            listView.Items.MoveCurrentToLast();
        }

        private static void AutoScrollTriggerPropertyChanged( DependencyObject d, DependencyPropertyChangedEventArgs args )
        {
            var autoScroller = d as AutoScroller;
            if ( autoScroller == null )
                return;

            // must be attached to a ScrollViewer
            var listView = autoScroller.AssociatedObject;

            // check if we are attached to an ObservableCollection, in which case we
            // will subscribe to CollectionChanged so that we scroll when stuff is added/removed
            var ncol = args.NewValue as INotifyCollectionChanged;

            // new event handler
            if ( ncol != null )
                ncol.CollectionChanged += autoScroller.OnCollectionChanged;

            // remove old eventhandler
            var ocol = args.OldValue as INotifyCollectionChanged;
            if ( ocol != null )
                ocol.CollectionChanged -= autoScroller.OnCollectionChanged;

            // also scroll to bottom when the bound object itself changes
            if ( listView != null && autoScroller.AutoScroll )
                ScrollToEnd( listView );
        }

        #endregion
    }
}
