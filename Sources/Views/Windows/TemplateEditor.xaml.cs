using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommonLib.Extensions;
using ForumParser.Models;
using ForumParser.ViewModels.Controls;
using ForumParser.ViewModels.Windows;
using ForumParser.Views.Controls;
using ForumParser.Views.Extensions;
using WpfCommon.ViewModels.Base;
using WpfCommon.Views.Base;

namespace ForumParser.Views.Windows
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class TemplateEditor : WindowBase<TemplateEditorViewModel>
    {
        #region Constants

        private static readonly string DragAndDropPollQuestionFormat = typeof ( PollQuestion ).FullName;

        #endregion


        #region Auto-properties

        public ObservableCollection<ConnectionCurve> Connections { get; } = new ObservableCollection<ConnectionCurve>();

        #endregion


        #region Initialization

        public TemplateEditor( TemplateEditorViewModel viewModel ) : base( viewModel )
        {
            InitializeComponent();
        }

        #endregion


        #region Event handlers

        private void QuestionsChartsMap_CollectionChanged( object sender, NotifyCollectionChangedEventArgs args )
        {
            UpdateLayout();

            if ( args.Action == NotifyCollectionChangedAction.Add )
            {
                var newMappings = args.NewItems.OfType<QuestionChartMapping>();
                AddConnectionCurves( newMappings );
            }
            else if ( args.Action == NotifyCollectionChangedAction.Remove )
            {
                var oldMappings = args.OldItems.OfType<QuestionChartMapping>();
                RemoveConnectionCurves( oldMappings );
            }
        }

        private void PollQuestionChart_MouseMove( object sender, MouseEventArgs e )
        {
            if ( e.LeftButton != MouseButtonState.Pressed )
                return;

            var pollQuestionChart = e.Source as PollQuestionChart;
            var pollQuestion = (pollQuestionChart?.DataContext as PollQuestionChartViewModel)?.Question;
            if ( pollQuestion == null )
                return;

            DragDrop.DoDragDrop( pollQuestionChart, pollQuestion, DragDropEffects.Copy );
        }

        private void ChartsPanel_DragEnter( object sender, DragEventArgs e )
        {
        }

        private void ChartsPanel_DragOver( object sender, DragEventArgs e )
        {
            if ( !e.Data.GetDataPresent( DragAndDropPollQuestionFormat ) )
                e.Effects = DragDropEffects.None;
            else
                e.Effects = DragDropEffects.Copy;
        }

        /// <summary>
        ///     Handles the drop operation of a poll question to an empty space.
        /// </summary>
        private void ChartsPanel_Drop( object sender, DragEventArgs e )
        {
            if ( !e.Data.GetDataPresent( DragAndDropPollQuestionFormat ) )
                return;

            var pollQuestion = (PollQuestion) e.Data.GetData( DragAndDropPollQuestionFormat );
            ViewModel?.CreateNewChartCommand?.Execute( pollQuestion );
        }

        private void MergedChart_DragEnter( object sender, DragEventArgs e )
        {
            e.Handled = true;

            if ( !e.Data.GetDataPresent( DragAndDropPollQuestionFormat ) )
                return;

            var chart = e.Source as ChartTemplateView;
            if ( chart != null )
                chart.BorderBrush = Brushes.Black;
        }

        private void MergedChart_DragLeave( object sender, DragEventArgs e )
        {
            e.Handled = true;

            var chart = e.Source as ChartTemplateView;
            if ( chart != null )
                chart.BorderBrush = Brushes.Transparent;
        }

        private void MergedChart_DragOver( object sender, DragEventArgs e )
        {
            e.Handled = true;

            if ( !e.Data.GetDataPresent( DragAndDropPollQuestionFormat ) )
                return;

            var pollQuestion = (PollQuestion) e.Data.GetData( DragAndDropPollQuestionFormat );
            if ( ((e.Source as ChartTemplateView)?.DataContext as ChartTemplateViewModel)?.AcceptsQuestion( pollQuestion ) == true )
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        /// <summary>
        ///     Handles the drop operation of a poll question to an existing Template.
        /// </summary>
        private void MergedChart_Drop( object sender, DragEventArgs e )
        {
            e.Handled = true;

            if ( !e.Data.GetDataPresent( DragAndDropPollQuestionFormat ) )
                return;

            var pollQuestion = (PollQuestion) e.Data.GetData( DragAndDropPollQuestionFormat );

            var mergedChartViewModel = (e.Source as ChartTemplateView)?.DataContext as ChartTemplateViewModel;
            if ( mergedChartViewModel?.AcceptsQuestion( pollQuestion ) != true )
                return;

            ViewModel?.AddQuestionToTemplateCommand?.Execute( Tuple.Create( mergedChartViewModel, pollQuestion ) );
        }

        private void ConnectionBreaker_Click( object sender, RoutedEventArgs e )
        {
            var question = (((e.Source as Button)?.DataContext as ConnectionCurve)?.Left.DataContext as PollQuestionChartViewModel)?.Question;
            if ( question != null )
                ViewModel?.RemoveQuestionFromTemplateCommand?.Execute( question );
        }

        private void ChartsList_LayoutUpdated( object sender, EventArgs e )
        {
            ConnectionsList.UpdateLayout();
        }

        private void MergedChart_DoubleClick( object sender, MouseButtonEventArgs e )
        {
            var templateViewModel = (sender as ChartTemplateView)?.DataContext as ChartTemplateViewModel;

            if ( templateViewModel != null )
                ViewModel?.EditChartTemplate( templateViewModel );
        }

        private void TemplateEditor_Loaded( object sender, RoutedEventArgs e )
        {
            var questionTemplateConnections = (DataContext as TemplateEditorViewModel)?.QuestionTemplateConnections;
            if ( questionTemplateConnections != null )
            {
                AddConnectionCurves( questionTemplateConnections );
                questionTemplateConnections.CollectionChanged += QuestionsChartsMap_CollectionChanged;
            }
        }

        #endregion


        #region Non-public methods

        private void RemoveConnectionCurves( IEnumerable<QuestionChartMapping> oldMappings )
        {
            var oldQuestionViews = oldMappings
                .Select( mapping => QuestionsList.FindItemContainer( mapping.Question ).FindChildOfType<HotspotContainer>() )
                .ToHashSet();

            var connections = Connections.Where( connection => oldQuestionViews.Contains( connection.Left ) ).ToList();
            connections.ForEach( connection => Connections.Remove( connection ) );
        }

        private void AddConnectionCurves( IEnumerable<QuestionChartMapping> newMappings )
        {
            var newConnections =
                newMappings.Select( mapping => new ConnectionCurve( QuestionsList.FindItemContainer( mapping.Question ).FindChildOfType<HotspotContainer>(),
                                                                    ChartsList.FindItemContainer( mapping.Template ).FindChildOfType<HotspotContainer>(),
                                                                    ConnectionsList ) );

            foreach ( var connection in newConnections )
                Connections.Add( connection );
        }

        #endregion


        #region Nested type: ConnectionCurve

        /// <summary>
        ///     Represents a connection curve between two blocks.
        /// </summary>
        public class ConnectionCurve : SimpleViewModelBase
        {
            #region Fields

            private readonly UIElement _parent;

            #endregion


            #region Auto-properties

            public HotspotContainer Left { get; }
            public HotspotContainer Right { get; }

            #endregion


            #region Properties

            public Point Point0 => GetPoint( 2, Left );

            public Point Point1 => GetPoint( 50, Left );

            public Point Point2 => GetPoint( 50, Right );

            public Point Point3 => GetPoint( 94, Right );

            #endregion


            #region Initialization

            /// <summary>
            ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
            /// </summary>
            public ConnectionCurve( HotspotContainer left, HotspotContainer right, UIElement parent )
            {
                _parent = parent;
                Left = left;
                Left.ParentLayoutUpdated += UpdateLeftPoints;

                Right = right;
                Right.ParentLayoutUpdated += UpdateRightPoints;
            }

            #endregion


            #region Public methods

            public Point GetPoint( int x, HotspotContainer target )
            {
                return new Point( x, target.TranslatePoint( target.HotspotLocation, _parent ).Y );
            }

            #endregion


            #region Non-public methods

            private void UpdateRightPoints( object sender, EventArgs e )
            {
                OnPropertyChanged( nameof( Point2 ) );
                OnPropertyChanged( nameof( Point3 ) );
            }

            private void UpdateLeftPoints( object sender, EventArgs args )
            {
                OnPropertyChanged( nameof( Point0 ) );
                OnPropertyChanged( nameof( Point1 ) );
            }

            #endregion
        }

        #endregion
    }
}
