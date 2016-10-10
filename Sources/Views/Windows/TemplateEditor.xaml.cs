using System;
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

        private static readonly string PollQuestionFormat = typeof ( PollQuestion ).FullName;

        #endregion


        #region Auto-properties

        public ObservableCollection<ConnectionCurve> Connections { get; } = new ObservableCollection<ConnectionCurve>();

        #endregion


        #region Initialization

        public TemplateEditor( TemplateEditorViewModel viewModel ) : base( viewModel )
        {
            InitializeComponent();

            viewModel = DataContext as TemplateEditorViewModel;
            if ( viewModel != null )
                viewModel.QuestionTemplateConnections.CollectionChanged += QuestionsChartsMap_CollectionChanged;

            DataContextChanged += TemplateEditor_DataContextChanged;
        }

        #endregion


        #region Event handlers

        private void TemplateEditor_DataContextChanged( object sender, DependencyPropertyChangedEventArgs args )
        {
            var viewModel = args.OldValue as TemplateEditorViewModel;
            if ( viewModel != null )
                viewModel.QuestionTemplateConnections.CollectionChanged -= QuestionsChartsMap_CollectionChanged;

            viewModel = args.NewValue as TemplateEditorViewModel;
            if ( viewModel != null )
                viewModel.QuestionTemplateConnections.CollectionChanged += QuestionsChartsMap_CollectionChanged;
        }

        private void QuestionsChartsMap_CollectionChanged( object sender, NotifyCollectionChangedEventArgs args )
        {
            UpdateLayout();

            if ( args.Action == NotifyCollectionChangedAction.Add )
            {
                var newConnections =
                    from mapping in args.NewItems.OfType<QuestionChartMapping>()
                    select new ConnectionCurve( QuestionsList.FindItemContainer( mapping.Question ).FindChildOfType<HotspotContainer>(),
                                                ChartsList.FindItemContainer( mapping.Template ).FindChildOfType<HotspotContainer>(),
                                                ConnectionsList );

                foreach ( var connection in newConnections )
                    Connections.Add( connection );
            }
            else if ( args.Action == NotifyCollectionChangedAction.Remove )
            {
                var oldQuestionViews = args.OldItems.OfType<QuestionChartMapping>()
                                           .Select( mapping => QuestionsList.FindItemContainer( mapping.Question ).FindChildOfType<HotspotContainer>() )
                                           .ToHashSet();

                var connections = Connections.Where( connection => oldQuestionViews.Contains( connection.Left ) ).ToList();
                connections.ForEach( connection => Connections.Remove( connection ) );
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
            if ( !e.Data.GetDataPresent( PollQuestionFormat ) )
                e.Effects = DragDropEffects.None;
            else
                e.Effects = DragDropEffects.Copy;
        }

        /// <summary>
        ///     Handles the drop operation of a poll question to an empty space.
        /// </summary>
        private void ChartsPanel_Drop( object sender, DragEventArgs e )
        {
            if ( !e.Data.GetDataPresent( PollQuestionFormat ) )
                return;

            var pollQuestion = (PollQuestion) e.Data.GetData( PollQuestionFormat );
            ViewModel?.CreateNewChartCommand?.Execute( pollQuestion );
        }

        private void MergedChart_DragEnter( object sender, DragEventArgs e )
        {
            e.Handled = true;

            if ( !e.Data.GetDataPresent( PollQuestionFormat ) )
                return;

            var chart = e.Source as TemplateChart;
            if ( chart != null )
                chart.BorderBrush = Brushes.Black;
        }

        private void MergedChart_DragLeave( object sender, DragEventArgs e )
        {
            e.Handled = true;

            var chart = e.Source as TemplateChart;
            if ( chart != null )
                chart.BorderBrush = Brushes.Transparent;
        }

        private void MergedChart_DragOver( object sender, DragEventArgs e )
        {
            e.Handled = true;

            if ( !e.Data.GetDataPresent( PollQuestionFormat ) )
                return;

            var pollQuestion = (PollQuestion) e.Data.GetData( PollQuestionFormat );
            if ( ((e.Source as TemplateChart)?.DataContext as TemplateViewModel)?.AcceptsQuestion( pollQuestion ) == true )
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

            if ( !e.Data.GetDataPresent( PollQuestionFormat ) )
                return;

            var pollQuestion = (PollQuestion) e.Data.GetData( PollQuestionFormat );

            var mergedChartViewModel = (e.Source as TemplateChart)?.DataContext as TemplateViewModel;
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

        #endregion


        #region Nested type: ConnectionCurve

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


        private void MergedChart_DoubleClick( object sender, MouseButtonEventArgs e )
        {
            var templateViewModel = (sender as TemplateChart)?.DataContext as TemplateViewModel;

            if ( templateViewModel != null )
            {
                ViewModel?.EditChartTemplate( templateViewModel );
            }
        }
    }
}
