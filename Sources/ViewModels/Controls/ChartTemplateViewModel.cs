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

        private int _answersCount;
        private ICollection<DataPointsGroup> _columnGroups;
        private ICollection<string> _gridLines;
        private double _height = 360;
        private double _maxValue;
        private double _width = 480;

        #endregion


        #region Auto-properties

        public ObservableCollection<QuestionDataSeries> Series { get; } = new ObservableCollection<QuestionDataSeries>();

        public ChartTemplate Template { get; }

        #endregion


        #region Properties

        /// <summary>
        ///     Chart width.
        /// </summary>
        public double Width
        {
            get { return _width; }
            set { SetValue( ref _width, value ); }
        }

        /// <summary>
        ///     Chart height.
        /// </summary>
        public double Height
        {
            get { return _height; }
            set { SetValue( ref _height, value ); }
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
        public int AnswersCount
        {
            get { return _answersCount; }
            set { SetValue( ref _answersCount, value ); }
        }

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
            AnswersCount = answersCount;
            Template = new ChartTemplate { Width = 480, Height = 360, };
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
            if ( question.Answers.Count != AnswersCount )
                throw new ArgumentException( $"Cannot add question with {question.Answers.Count} answers to a group with {AnswersCount} answers" );

            if ( Series.Any( series => series.Question == question ) )
                return;

            Series.Add( new QuestionDataSeries( question ) );
            RebuildChart();
        }

        /// <summary>
        ///     Checks whether the template accepts the proposed question.
        /// </summary>
        /// <param name="question">The question to check.</param>
        /// <returns>True if the question can be added to the group.</returns>
        public bool AcceptsQuestion( PollQuestion question )
        {
            return question.Answers.Count == AnswersCount && Series.All( s => s.Question != question );
        }

        /// <summary>
        ///     Removes the <paramref name="question" /> from the template.
        /// </summary>
        /// <param name="question">The question to be removed.</param>
        public void RemoveQuestion( PollQuestion question )
        {
            var seriesIndex =
                Series.Select( ( series, index ) => new { Index = index, question = series.Question } ).FirstOrDefault( a => a.question == question )?.Index;

            if ( seriesIndex != null )
                Series.RemoveAt( (int) seriesIndex );

            if ( Series.Count > 0 )
                RebuildChart();
        }

        public ChartTemplate GetTemplate()
        {
            var template = new ChartTemplate();

            return template;
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

            ColumnGroups = (from answerIndex in Enumerable.Range( 0, AnswersCount )
                            select new DataPointsGroup( from s in Series
                                                        let answer = s.Question.Answers[answerIndex]
                                                        select new DataPoint( answer.Text, answer.Count, MaxValue ) )
                           ).ToList();
        }

        /// <summary>
        ///     Recalculates the grid values.
        /// </summary>
        private void RebuildGrid()
        {
            double maxValue = Series.SelectMany( s => s.Question.Answers ).Max( a => a.Count );
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
    public class QuestionDataSeries : SimpleViewModelBase
    {
        #region Fields

        private string _text;

        #endregion


        #region Auto-properties

        /// <summary>
        ///     Reference to the poll question.
        /// </summary>
        public PollQuestion Question { get; }

        #endregion


        #region Properties

        /// <summary>
        ///     Question display text.
        /// </summary>
        public string Text
        {
            get { return _text; }
            set { SetValue( ref _text, value ); }
        }

        #endregion


        #region Initialization

        public QuestionDataSeries( PollQuestion question )
        {
            Question = question;
            Text = question.Text;
        }

        #endregion
    }

    /// <summary>
    ///     Represents a single answer columns group.
    /// </summary>
    public class DataPointsGroup : SimpleViewModelBase
    {
        private string _text;


        #region Auto-properties

        /// <summary>
        ///     Columns group columns.
        /// </summary>
        public ICollection<DataPoint> DataPoints { get; }

        public string Text
        {
            get { return _text; }
            set { SetValue( ref _text, value ); }
        }

        public bool IsTextConflicted { get; set; }

        #endregion


        #region Commands

        public ICommand SetTextOverrideCommand { get; }

        #endregion


        #region Initialization

        public DataPointsGroup( IEnumerable<DataPoint> dataPoints )
        {
            DataPoints = dataPoints.ToArray();

            var firstDataPointText = DataPoints.First().Text;

            IsTextConflicted = DataPoints.Skip( 1 ).Any( dataPoint => dataPoint.Text != firstDataPointText );
            Text = IsTextConflicted ? "???" : firstDataPointText;

            SetTextOverrideCommand = new DelegateCommand<string>( SetTextOverride );
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
