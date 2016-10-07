using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using ForumParser.Models;
using WpfCommon.ViewModels.Base;
using Enumerable = System.Linq.Enumerable;

namespace ForumParser.ViewModels.Controls
{
    public class TemplateViewModel : SimpleViewModelBase
    {
        #region Fields

        public ObservableCollection<QuestionDataSeries> Series { get; } = new ObservableCollection<QuestionDataSeries>();
        private int _answersCount;
        private ICollection<DataPointsGroup> _columnGroups;
        private double _maxValue;
        private ICollection<string> _gridLines;
        private double _width = 480;
        private double _height = 360;

        #endregion


        #region Auto-properties

        #endregion


        #region Properties

        public double Width
        {
            get { return _width; }
            set { SetValue( ref _width, value ); }
        }

        public double Height
        {
            get { return _height; }
            set { SetValue( ref _height, value ); }
        }

        public ICollection<string> GridLines
        {
            get { return _gridLines; }
            set { SetValue( ref _gridLines, value ); }
        }

        public ICollection<DataPointsGroup> ColumnGroups
        {
            get { return _columnGroups; }
            private set { SetValue( ref _columnGroups, value ); }
        }

        public int AnswersCount
        {
            get { return _answersCount; }
            set { SetValue( ref _answersCount, value ); }
        }

        public double MaxValue
        {
            get { return _maxValue; }
            set { SetValue( ref _maxValue, value ); }
        }

        #endregion


        #region Initialization

        public TemplateViewModel( int answersCount )
        {
            AnswersCount = answersCount;
        }

        #endregion


        #region Public methods

        public void AddQuestion( PollQuestion question )
        {
            if ( question.Answers.Count != AnswersCount )
                throw new ArgumentException( $"Cannot add question with {question.Answers.Count} answers to a group with {AnswersCount} answers" );

            if ( Series.Any( series => series.Question == question ) )
                return;

            Series.Add( new QuestionDataSeries( question ) );
            RebuildChart();
        }

        public bool AcceptsQuestion( PollQuestion question )
        {
            return question.Answers.Count == AnswersCount && Series.All( s => s.Question != question );
        }

        public void RemoveQuestion( PollQuestion question )
        {
            var seriesIndex = Series.Select( ( series, index ) => new { Index = index, question = series.Question } ).FirstOrDefault( a => a.question == question )?.Index;

            if ( seriesIndex != null )
                Series.RemoveAt( (int) seriesIndex );

            if (Series.Count > 0)
                RebuildChart();
        }

        #endregion


        #region Non-public methods

        private void RebuildChart()
        {
            if ( Series.Count == 0 )
                return;

            RebuildGrid();

            ColumnGroups =
                (from answerIndex in Enumerable.Range( 0, AnswersCount )
                 let dataPoints = from s in Series
                                  let answer = s.Question.Answers[answerIndex]
                                  select new DataPoint( answer.Text, answer.Count, MaxValue )
                 select new DataPointsGroup( dataPoints )
                ).ToList();
        }

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

        private static void CalculateGridParameters( ref double maxValue, out double gridStep )
        {
            var exp = Math.Floor( Math.Log10( maxValue ) );
            gridStep = Math.Pow( 10, exp );
            maxValue = Math.Ceiling( maxValue/gridStep )*gridStep;
        }

        #endregion
    }

    public class QuestionDataSeries : SimpleViewModelBase
    {
        #region Auto-properties

        public string Text { get; set; }
        public PollQuestion Question { get; }

        #endregion


        #region Initialization

        public QuestionDataSeries( PollQuestion question )
        {
            Question = question;
            Text = question.Text;
        }

        #endregion
    }

    public class DataPointsGroup : SimpleViewModelBase
    {
        #region Auto-properties

        public ICollection<DataPoint> DataPoints { get; }

        #endregion


        #region Initialization

        public DataPointsGroup( IEnumerable<DataPoint> dataPoints )
        {
            DataPoints = dataPoints.ToArray();
        }

        #endregion
    }

    public class DataPoint
    {
        #region Auto-properties

        public double MaxValue { get; }
        public double Value { get; }

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
