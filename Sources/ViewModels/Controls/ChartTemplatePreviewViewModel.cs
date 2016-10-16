using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using ForumParser.Models;
using WpfCommon.ViewModels.Base;

namespace ForumParser.ViewModels.Controls
{
    /// <summary>
    ///     Describes groupped charts template.
    /// </summary>
    public class ChartTemplatePreviewViewModel : SimpleViewModelBase
    {
        #region Fields

        private ICollection<DataPointsGroup> _columnGroups;
        private ICollection<string> _gridLines;
        private double _maxValue;
        private double _width;
        private double _height;

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

        public ChartTemplatePreviewViewModel( ChartTemplate template, IEnumerable<KeyValuePair<TemplateQuestion, PollQuestion>> questions )
        {
            Template = template;
            Width = template.Width;
            Height = template.Height;

            foreach ( var question in questions )
                Series.Add( new QuestionSeriesViewModel( question.Key, question.Value ) );

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
                    Template );

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
}