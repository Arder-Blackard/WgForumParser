using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommonLib.Extensions;
using ForumParser.Models;
using ForumParser.ViewModels.Controls;
using WpfCommon.Commands;
using WpfCommon.Services;
using WpfCommon.ViewModels.Base;

namespace ForumParser.ViewModels.Windows
{
    public class TemplateEditorViewModel : SimpleWindowViewModelBase
    {
        #region Fields

        private readonly IViewProvider _viewProvider;

        private ObservableCollection<PollQuestionChartViewModel> _questions;
        private ObservableCollection<ChartTemplateViewModel> _templates = new ObservableCollection<ChartTemplateViewModel>();
        private ObservableCollection<QuestionChartMapping> _questionTemplateConnections = new ObservableCollection<QuestionChartMapping>();

        #endregion


        #region Auto-properties

        public double UnifiedWidth { get; set; }

        public double UnifiedHeight { get; set; }

        #endregion


        #region Properties

        public ObservableCollection<ChartTemplateViewModel> Templates
        {
            get { return _templates; }
            private set { SetValue( ref _templates, value ); }
        }

        public ObservableCollection<QuestionChartMapping> QuestionTemplateConnections
        {
            get { return _questionTemplateConnections; }
            private set { SetValue( ref _questionTemplateConnections, value ); }
        }

        public ObservableCollection<PollQuestionChartViewModel> Questions
        {
            get { return _questions; }
            private set { SetValue( ref _questions, value ); }
        }

        public string ViewTitle => "Редактор шаблонов";

        #endregion


        #region Commands

        public ICommand AddQuestionToTemplateCommand { get; }
        public ICommand CreateNewChartCommand { get; }
        public ICommand RemoveQuestionFromTemplateCommand { get; }
        public ICommand SetEqualSizeCommand { get; }

        private void AddQuestionToTemplateCommandHandler( Tuple<ChartTemplateViewModel, PollQuestion> param )
        {
            var targetChart = param.Item1;
            var question = param.Item2;

            if ( question == null )
                throw new ArgumentNullException( nameof( question ) );
            if ( targetChart == null )
                throw new ArgumentNullException( nameof( targetChart ) );

            AddQuestionToTemplate( question, targetChart );
        }

        private void CreateNewChartCommandHandler( PollQuestion question )
        {
            if ( question == null )
                throw new ArgumentNullException( nameof( question ) );

            var newChartViewModel = new ChartTemplateViewModel( question.Answers.Count );
            Templates.Add( newChartViewModel );

            AddQuestionToTemplate( question, newChartViewModel );
        }

        private void RemoveQuestionFromTemplateCommandHandler( PollQuestion question )
        {
            RemoveQuestionFromTemplate( question );
        }

        private void SetEqualSizeCommandHandler()
        {
            foreach ( var template in Templates )
            {
                template.Width = UnifiedWidth;
                template.Height = UnifiedHeight;
            }
        }

        #endregion


        #region Initialization

        public TemplateEditorViewModel( IViewProvider viewProvider )
        {
            _viewProvider = viewProvider;
            CreateNewChartCommand = new DelegateCommand<PollQuestion>( CreateNewChartCommandHandler );
            AddQuestionToTemplateCommand = new DelegateCommand<Tuple<ChartTemplateViewModel, PollQuestion>>( AddQuestionToTemplateCommandHandler );
            RemoveQuestionFromTemplateCommand = new DelegateCommand<PollQuestion>( RemoveQuestionFromTemplateCommandHandler );
            SetEqualSizeCommand = new DelegateCommand( SetEqualSizeCommandHandler );
        }

        #endregion


        #region Public methods

        public void EditChartTemplate( ChartTemplateViewModel templateViewModel )
        {
            _viewProvider.Show<TemplatePropertiesEditorViewModel>( this, propertiesViewModel => propertiesViewModel.Template = templateViewModel );
        }

        public void LoadEditor( ICollection<ChartTemplate> templates, IList<PollQuestion> pollQuestions )
        {
            Questions = new ObservableCollection<PollQuestionChartViewModel>( pollQuestions.Select( question => new PollQuestionChartViewModel( question ) ) );

            //  Find templates matching the forum topic questions

            Templates.Clear();

            var questionsMap = Questions.ToDictionary( question => question.Text, StringComparer.OrdinalIgnoreCase );

            foreach ( var template in templates )
            {
                var matchingQuestions = new List<KeyValuePair<TemplateQuestion, PollQuestion>>();

                var matches = template.Questions.Select( question => new { TemplateQuestion = question, PollQuestion = FindMatchingPollQuestion( question, questionsMap ) } )
                                      .Where( pair => pair.PollQuestion != null );

                foreach (var pair in )

                if ( matchingQuestions.Any() )
                {
                    var templateViewModel = new ChartTemplateViewModel( matchingQuestions );
                    Templates.Add( templateViewModel );

                    foreach ( var questionViewModel in matchingQuestions )
                        QuestionTemplateConnections.Add( new QuestionChartMapping( questionViewModel, templateViewModel ) );
                }
            }
        }

        #endregion


        #region Non-public methods

        private void AddQuestionToTemplate( PollQuestion question, ChartTemplateViewModel targetTemplate )
        {
            var questionViewModel = Questions.FirstOrDefault( viewModel => viewModel.Question == question );
            if ( questionViewModel == null )
                throw new ArgumentException( @"The question does not belong to the questions list.", nameof( question ) );

            //  Remove existing mapping
            var existingMapping = QuestionTemplateConnections.FirstOrDefault( mapping => mapping.Question == questionViewModel );
            if ( existingMapping != null )
            {
                QuestionTemplateConnections.Remove( existingMapping );

                var existingChart = existingMapping.Template;
                existingChart.RemoveQuestion( question );
                if ( existingChart.Series.Count == 0 )
                    Templates.Remove( existingChart );
            }

            //  Add new mapping
            targetTemplate.AddQuestion( question );
            QuestionTemplateConnections.Add( new QuestionChartMapping( questionViewModel, targetTemplate ) );
        }

        private void RemoveQuestionFromTemplate( PollQuestion question )
        {
            var questionViewModel = Questions.FirstOrDefault( viewModel => viewModel.Question == question );
            if ( questionViewModel == null )
                throw new ArgumentException( @"The question does not belong to the questions list.", nameof( question ) );

            //  Remove existing mapping
            var existingMapping = QuestionTemplateConnections.FirstOrDefault( mapping => mapping.Question == questionViewModel );
            if ( existingMapping != null )
            {
                QuestionTemplateConnections.Remove( existingMapping );

                var existingTemplate = existingMapping.Template;
                existingTemplate.RemoveQuestion( question );
                if ( existingTemplate.Series.Count == 0 )
                    Templates.Remove( existingTemplate );
            }
        }

        private static PollQuestionChartViewModel FindMatchingPollQuestion( TemplateQuestion templateQuestion, Dictionary<string, PollQuestionChartViewModel> questionsMap )
        {
            var pollQuestion = questionsMap.GetOrDefault( templateQuestion.QuestionText );

            //  Determine cases when match is unsuccessful
            if ( pollQuestion == null )
                return null;

            if ( pollQuestion.Answers.Count != templateQuestion.Answers.Count )
                return null;

            if ( pollQuestion.Answers
                             .Zip( templateQuestion.Answers, ( pollAnswer, templateAnswerText ) => pollAnswer.Text.Equals( templateAnswerText, StringComparison.OrdinalIgnoreCase ) )
                             .All( match => match ) )
                return null;

            //  The matching poll question has been found
            return pollQuestion;
        }

        #endregion
    }

    public class QuestionChartMapping
    {
        #region Auto-properties

        public PollQuestionChartViewModel Question { get; }
        public ChartTemplateViewModel Template { get; }

        #endregion


        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        public QuestionChartMapping( PollQuestionChartViewModel question, ChartTemplateViewModel template )
        {
            Question = question;
            Template = template;
        }

        #endregion
    }
}
