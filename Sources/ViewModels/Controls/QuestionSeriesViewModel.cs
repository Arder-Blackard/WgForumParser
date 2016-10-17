using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ForumParser.Models;
using WpfCommon.Commands;
using WpfCommon.ViewModels.Base;

namespace ForumParser.ViewModels.Controls
{
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

        private readonly int _groupIndex;

        #endregion


        #region Auto-properties

        /// <summary>
        ///     Columns group columns.
        /// </summary>
        public ICollection<DataPoint> DataPoints { get; }

        public bool IsTextConflicted { get; set; }


        #region Commands

        public ICommand SetTextCustomCommand { get; }

        #endregion


        private ChartTemplate ChartTemplate { get; }

        #endregion


        #region Properties

        public string Text
        {
            get { return ChartTemplate.CustomAnswers[_groupIndex]; }
            set
            {
                ChartTemplate.CustomAnswers[_groupIndex] = value;
                OnPropertyChanged();
            }
        }

        #endregion


        #region Initialization

        public DataPointsGroup( int groupIndex, IEnumerable<DataPoint> dataPoints, ChartTemplate chartTemplate )
        {
            _groupIndex = groupIndex;
            ChartTemplate = chartTemplate;

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

        public string Label => $"{Value} ({Value/MaxValue:P1})";

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
