using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommonLib.Extensions;
using ForumParser.Collections;
using ForumParser.Models;
using ForumParser.ViewModels.Controls;
using WpfCommon.Commands;
using WpfCommon.Services;
using WpfCommon.ViewModels.Base;
using WpfCommon.ViewModels.Dialogs;

namespace ForumParser.ViewModels.Windows
{
    public class TemplateEditorViewModel : SimpleViewModelBase, IWindowViewModel, IViewModel
    {
        #region Fields

        private readonly IViewProvider _viewProvider;

        private AsyncObservableCollection<PollQuestionChartViewModel> _questions;
        private AsyncObservableCollection<EditableChartTemplateViewModel> _editorTemplates = new AsyncObservableCollection<EditableChartTemplateViewModel>();
        private AsyncObservableCollection<QuestionChartMapping> _questionTemplateConnections = new AsyncObservableCollection<QuestionChartMapping>();
        private readonly TemplateMatcher _templateMatcher;

        #endregion


        #region Auto-properties

        public double UnifiedWidth { get; set; }

        public double UnifiedHeight { get; set; }

        #endregion


        #region Properties

        public AsyncObservableCollection<EditableChartTemplateViewModel> EditorTemplates
        {
            get { return _editorTemplates; }
            private set { SetValue( ref _editorTemplates, value ); }
        }

        public AsyncObservableCollection<QuestionChartMapping> QuestionTemplateConnections
        {
            get { return _questionTemplateConnections; }
            private set { SetValue( ref _questionTemplateConnections, value ); }
        }

        public AsyncObservableCollection<PollQuestionChartViewModel> Questions
        {
            get { return _questions; }
            private set { SetValue( ref _questions, value ); }
        }

        public bool TemplatesChanged { get; private set; }

        public string ViewTitle => "Редактор шаблонов";

        #endregion


        #region Commands

        public ICommand AddQuestionToTemplateCommand { get; }
        public ICommand CreateNewChartCommand { get; }
        public ICommand RemoveQuestionFromTemplateCommand { get; }
        public ICommand SetEqualSizeCommand { get; }

        private void AddQuestionToTemplateCommandHandler( Tuple<EditableChartTemplateViewModel, PollQuestion> param )
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

            var newChartViewModel = new EditableChartTemplateViewModel( question.Answers.Count );
            EditorTemplates.Add( newChartViewModel );

            AddQuestionToTemplate( question, newChartViewModel );
        }

        private void RemoveQuestionFromTemplateCommandHandler( PollQuestion question )
        {
            RemoveQuestionFromTemplate( question );
        }

        private void SetEqualSizeCommandHandler()
        {
            foreach ( var template in EditorTemplates )
            {
                template.Width = UnifiedWidth;
                template.Height = UnifiedHeight;
            }
        }

        #endregion


        #region Initialization

        public TemplateEditorViewModel( IViewProvider viewProvider, TemplateMatcher templateMatcher )
        {
            _viewProvider = viewProvider;
            _templateMatcher = templateMatcher;
            CreateNewChartCommand = new DelegateCommand<PollQuestion>( CreateNewChartCommandHandler );
            AddQuestionToTemplateCommand = new DelegateCommand<Tuple<EditableChartTemplateViewModel, PollQuestion>>( AddQuestionToTemplateCommandHandler );
            RemoveQuestionFromTemplateCommand = new DelegateCommand<PollQuestion>( RemoveQuestionFromTemplateCommandHandler );
            SetEqualSizeCommand = new DelegateCommand( SetEqualSizeCommandHandler );
        }

        #endregion


        #region Public methods

        /// <summary>
        ///     Initializes the chart templates editor.
        /// </summary>
        /// <param name="templates">The chart templates.</param>
        /// <param name="pollQuestions">The poll questions to match for the templates.</param>
        public void LoadEditor( ICollection<ChartTemplate> templates, IList<PollQuestion> pollQuestions )
        {
            // Setup questions view models
            Questions = new AsyncObservableCollection<PollQuestionChartViewModel>( pollQuestions.Select( question => new PollQuestionChartViewModel( question ) ) );

            EditorTemplates.Clear();

            var templateMatches = _templateMatcher.MatchTemplates( templates, pollQuestions);
            var questionsViewModelsMap = Questions.ToDictionary( viewModel => viewModel.Question );

            foreach ( var templateMatch in templateMatches )
            {
                var templateCopy = templateMatch.Key.Copy();
                templateCopy.Questions = templateMatch.Value.Select( pair => pair.Key ).ToList();
                var templateViewModel = new EditableChartTemplateViewModel( templateCopy, templateMatch.Value );
                EditorTemplates.Add( templateViewModel );
                foreach ( var pair in templateMatch.Value )
                {
                    QuestionTemplateConnections.Add( new QuestionChartMapping( questionsViewModelsMap[pair.Value], templateViewModel ) );
                }
            }

            TemplatesChanged = false;
        }

        /// <summary>
        ///     Opens editor for a single chart template represented by the <paramref name="templateViewModel"/>.
        /// </summary>
        /// <param name="templateViewModel">The view model representing a chart template.</param>
        public void EditChartTemplate( EditableChartTemplateViewModel templateViewModel )
        {
            _viewProvider.Show<TemplatePropertiesEditorViewModel>( this, propertiesViewModel => propertiesViewModel.Template = templateViewModel );
        }

        #endregion


        #region Non-public methods

        private void AddQuestionToTemplate( PollQuestion question, EditableChartTemplateViewModel targetTemplate )
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
                    EditorTemplates.Remove( existingChart );
            }

            //  Add new mapping
            targetTemplate.AddQuestion( question );
            QuestionTemplateConnections.Add( new QuestionChartMapping( questionViewModel, targetTemplate ) );

            TemplatesChanged = true;
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
                    EditorTemplates.Remove( existingTemplate );
            }

            TemplatesChanged = true;
        }

        private static PollQuestionChartViewModel FindMatchingPollQuestion( TemplateQuestion templateQuestion, Dictionary<string, PollQuestionChartViewModel> questionsMap )
        {
            var pollQuestion = questionsMap.GetOrDefault( templateQuestion.QuestionText );

            //  Determine cases when match is unsuccessful
            if ( pollQuestion == null )
                return null;

            if ( pollQuestion.Answers.Count != templateQuestion.Answers.Count )
                return null;

            if ( !pollQuestion.Answers
                              .Zip( templateQuestion.Answers, ( pollAnswer, templateAnswerText ) => pollAnswer.Text.Equals( templateAnswerText, StringComparison.OrdinalIgnoreCase ) )
                              .All( match => match ) )
                return null;

            //  The matching poll question has been found
            return pollQuestion;
        }

        #endregion


        public void OnViewLoaded()
        {
        }

        public bool OnViewClosing()
        {
            return true;
        }

        public bool ConfirmTemplatesEdit()
        {
            return true;
        }

        public bool CancelTemplatesEdit()
        {
            return _viewProvider.ShowMessageBox( this, "Отменить сделанные изменения?", "Подтверждение выхода", MessageBoxButton.OKCancel, MessageBoxImage.Question ) == true;
        }
    }

    public class QuestionChartMapping
    {
        #region Auto-properties

        public PollQuestionChartViewModel Question { get; }
        public EditableChartTemplateViewModel Template { get; }

        #endregion


        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        public QuestionChartMapping( PollQuestionChartViewModel question, EditableChartTemplateViewModel template )
        {
            Question = question;
            Template = template;
        }

        #endregion
    }
}
