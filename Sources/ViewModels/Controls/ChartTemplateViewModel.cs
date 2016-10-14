using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using ForumParser.Models;
using WpfCommon.Commands;
using WpfCommon.ViewModels.Base;

namespace ForumParser.ViewModels.Controls
{
    /// <summary>
    ///     Describes groupped charts template.
    /// </summary>
    public class ChartTemplateViewModel : SimpleViewModelBase
    {
        #region Fields

        private ICollection<DataPointsGroup> _columnGroups;
        private ICollection<string> _gridLines;
        private double _maxValue;

        #endregion


        #region Auto-properties

        public ObservableCollection<QuestionSeriesViewModel> Series { get; } = new ObservableCollection<QuestionSeriesViewModel>();

        public ChartTemplate Template { get; }

        #endregion


        #region Properties

        /// <summary>
        ///     Chart width.
        /// </summary>
        public double Width
        {
            get { return Template.Width; }
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if ( Template.Width == value )
                    return;
                Template.Width = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Chart height.
        /// </summary>
        public double Height
        {
            get { return Template.Height; }
            set
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if ( Template.Height == value )
                    return;
                Template.Height = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Chart grid lines labels.
        /// </summary>
        public ICollection<string> GridLines
        {
            get { return _gridLines; }
            set { SetValue( ref _gridLines, value ); }
        }

        /// <summary>
        ///     Each columns group are the columns for a single answer.
        /// </summary>
        public ICollection<DataPointsGroup> ColumnGroups
        {
            get { return _columnGroups; }
            private set { SetValue( ref _columnGroups, value ); }
        }

        /// <summary>
        ///     Number of answers in each question of the group.
        /// </summary>
        public int AnswersCount => Template.CustomAnswers.Count;

        /// <summary>
        ///     Chart maximal value.
        /// </summary>
        public double MaxValue
        {
            get { return _maxValue; }
            set { SetValue( ref _maxValue, value ); }
        }

        #endregion


        #region Initialization

        public ChartTemplateViewModel( int answersCount )
        {
            Template = new ChartTemplate { Width = 480, Height = 360, CustomAnswers = new string[answersCount] };
        }

        public ChartTemplateViewModel( ChartTemplate template, IEnumerable<KeyValuePair<TemplateQuestion, PollQuestion>> questions )
        {
            Template = template;

            foreach ( var question in questions )
                Series.Add( new QuestionSeriesViewModel( question.Key, question.Value ) );

            RebuildChart();
        }

        #endregion


        #region Public methods

        /// <summary>
        ///     Adds a question to the template.
        /// </summary>
        /// <param name="question">THe question to add.</param>
        /// <exception cref="ArgumentException">The question answers count doen't match that of the group.</exception>
        public void AddQuestion( PollQuestion question )
        {
            if ( question.Answers.Count != Template.CustomAnswers.Count )
                throw new ArgumentException( $"Cannot add question with {question.Answers.Count} answers to a group with {AnswersCount} answers" );

            if ( Series.Any( series => series.TemplateQuestion.QuestionText == question.Text ) )
                return;

            var templateQuestion = new TemplateQuestion( question );
            Template.Questions.Add( templateQuestion );

            Series.Add( new QuestionSeriesViewModel( templateQuestion, question ) );
            RebuildChart();
        }

        /// <summary>
        ///     Checks whether the template accepts the proposed question.
        /// </summary>
        /// <param name="question">The question to check.</param>
        /// <returns>True if the question can be added to the group.</returns>
        public bool AcceptsQuestion( PollQuestion question )
        {
            return question.Answers.Count == AnswersCount && Series.All( s => s.TemplateQuestion.QuestionText != question.Text );
        }

        /// <summary>
        ///     Removes the <paramref name="question" /> from the template.
        /// </summary>
        /// <param name="question">The question to be removed.</param>
        public void RemoveQuestion( PollQuestion question )
        {
            var seriesIndex =
                Series.Select( ( series, index ) => new { Index = index, series.TemplateQuestion.QuestionText } ).FirstOrDefault( a => a.QuestionText == question.Text )?.Index;

            if ( seriesIndex != null )
                Series.RemoveAt( (int) seriesIndex );

            if ( Series.Count > 0 )
                RebuildChart();
        }

        #endregion


        #region Non-public methods

        /// <summary>
        ///     Recalculates the values required to display the chart.
        /// </summary>
        private void RebuildChart()
        {
            if ( Series.Count == 0 )
                return;

            RebuildGrid();

            var list = new List<DataPointsGroup>();

            for ( var answerIndex = 0; answerIndex < AnswersCount; answerIndex ++ )
            {
                var dataPointsGroup = new DataPointsGroup(
                    answerIndex,
                    Series.Select( s => s.PollQuestion.Answers[answerIndex] )
                          .Select( a => new DataPoint( a.Text, a.Count, MaxValue ) )
                          .ToList(),
                    this );

                list.Add( dataPointsGroup );
            }

            ColumnGroups = list;
        }

        /// <summary>
        ///     Recalculates the grid values.
        /// </summary>
        private void RebuildGrid()
        {
            double maxValue = Series.SelectMany( s => s.PollQuestion.Answers ).Max( a => a.Count );
            double gridStep;

            CalculateGridParameters( ref maxValue, out gridStep );

            MaxValue = maxValue;
            GridLines = Enumerable.Range( 1, (int) (maxValue/gridStep) )
                                  .Select( value => (value*gridStep).ToString( CultureInfo.InvariantCulture ) )
                                  .ToArray();
        }

        /// <summary>
        ///     Calculates and returns the grid parameters.
        /// </summary>
        /// <param name="maxValue"></param>
        /// <param name="gridStep"></param>
        private static void CalculateGridParameters( ref double maxValue, out double gridStep )
        {
            var exp = Math.Floor( Math.Log10( maxValue ) );
            gridStep = Math.Pow( 10, exp );
            maxValue = Math.Ceiling( maxValue/gridStep )*gridStep;
        }

        #endregion
    }

    /// <summary>
    ///     Represents data series generated from a question answers.
    /// </summary>
    public class QuestionSeriesViewModel : SimpleViewModelBase
    {
        #region Auto-properties

        /// <summary>
        ///     Reference to the question template.
        /// </summary>
        public TemplateQuestion TemplateQuestion { get; }

        public PollQuestion PollQuestion { get; }

        #endregion


        #region Properties

        /// <summary>
        ///     Question display text.
        /// </summary>
        public string CustomText
        {
            get { return TemplateQuestion.CustomText; }
            set
            {
                if ( TemplateQuestion.CustomText == value )
                    return;
                TemplateQuestion.CustomText = value;
                OnPropertyChanged();
            }
        }

        #endregion


        #region Initialization

        public QuestionSeriesViewModel( TemplateQuestion templateQuestion, PollQuestion pollQuestion )
        {
            TemplateQuestion = templateQuestion;
            PollQuestion = pollQuestion;
        }

        #endregion
    }

    /// <summary>
    ///     Represents a single answer columns group.
    /// </summary>
    public class DataPointsGroup : SimpleViewModelBase
    {
        #region Fields

        private ChartTemplateViewModel ChartViewModel { get; }

        private int _groupIndex;

        #endregion


        #region Auto-properties

        /// <summary>
        ///     Columns group columns.
        /// </summary>
        public ICollection<DataPoint> DataPoints { get; }

        public bool IsTextConflicted { get; set; }

        #endregion


        #region Properties

        public string Text
        {
            get { return ChartViewModel.Template.CustomAnswers[_groupIndex]; }
            set
            {
                ChartViewModel.Template.CustomAnswers[_groupIndex] = value;
                OnPropertyChanged();
            }
        }

        #endregion


        #region Commands

        public ICommand SetTextCustomCommand { get; }

        #endregion


        #region Initialization

        public DataPointsGroup( int groupIndex, IEnumerable<DataPoint> dataPoints, ChartTemplateViewModel chartViewModel )
        {
            _groupIndex = groupIndex;
            ChartViewModel = chartViewModel;

            DataPoints = dataPoints.ToArray();

            var firstDataPointText = DataPoints.First().Text;

            IsTextConflicted = DataPoints.Skip( 1 ).Any( dataPoint => dataPoint.Text != firstDataPointText );
            Text = IsTextConflicted ? "???" : firstDataPointText;

            SetTextCustomCommand = new DelegateCommand<string>( SetTextOverride );
        }

        #endregion


        #region Non-public methods

        private void SetTextOverride( string text )
        {
            Text = text;
            IsTextConflicted = false;
        }

        #endregion
    }

    /// <summary>
    ///     A single chart column.
    /// </summary>
    public class DataPoint
    {
        #region Auto-properties

        /// <summary>
        ///     The chart max value.
        /// </summary>
        public double MaxValue { get; }

        /// <summary>
        ///     Data point value.
        /// </summary>
        public double Value { get; }

        /// <summary>
        ///     Data point text.
        /// </summary>
        public string Text { get; }

        #endregion


        #region Initialization

        /// <summary>Initializes a new instance of the <see cref="T:System.Object" /> class.</summary>
        public DataPoint( string text, double value, double maxValue )
        {
            Text = text;
            Value = value;
            MaxValue = maxValue;
        }

        #endregion
    }
}
