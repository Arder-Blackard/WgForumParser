using System.Collections.Generic;
using ForumParser.Models;

namespace ForumParser.ViewModels.Controls
{
    /// <summary>
    ///     Describes groupped charts template.
    /// </summary>
    public class PreviewChartTemplateViewModel : ChartTemplateViewModel
    {
        #region Fields

        private double _width;
        private double _height;

        #endregion


        #region Auto-properties


        #endregion


        #region Properties

        /// <summary>
        ///     Chart width.
        /// </summary>
        public override double Width
        {
            get { return _width; }
            set { SetValue( ref _width, value ); }
        }

        /// <summary>
        ///     Chart height.
        /// </summary>
        public override double Height
        {
            get { return _height; }
            set { SetValue( ref _height, value ); }
        }


        #endregion


        #region Initialization

        public PreviewChartTemplateViewModel( ChartTemplate template, IEnumerable<KeyValuePair<TemplateQuestion, PollQuestion>> questions ) : base(template, questions)
        {
            _width = template.Width;
            _height = template.Height;
        }

        #endregion

    }
}