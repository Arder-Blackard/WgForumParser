using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using ForumParser.Models;
using WpfCommon.ViewModels.Base;

namespace ForumParser.ViewModels.Controls
{
    public abstract class ChartTemplateViewModel : SimpleViewModelBase
    {
        private ICollection<DataPointsGroup> _columnGroups;
        private ICollection<string> _gridLines;
        private double _maxValue;

        public ChartTemplateViewModel( ChartTemplate template, IEnumerable<KeyValuePair<TemplateQuestion, PollQuestion>> questions = null)
        {
            Template = template;

            foreach ( var question in questions ?? Enumerable.Empty<KeyValuePair<TemplateQuestion, PollQuestion>>() )
                Series.Add( new QuestionSeriesViewModel( question.Key, question.Value ) );

            RebuildChart();
        }

        public ObservableCollection<QuestionSeriesViewModel> Series { get; } = new ObservableCollection<QuestionSeriesViewModel>();
        public ChartTemplate Template { get; }

        /// <summary>
        ///     Chart width.
        /// </summary>
        public abstract double Width { get; set; }

        /// <summary>
        ///     Chart height.
        /// </summary>
        public abstract double Height { get; set; }

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

        /// <summary>
        ///     Recalculates the values required to display the chart.
        /// </summary>
        protected void RebuildChart()
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

            MaxValue = maxValue * 1.1;
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
    }
}