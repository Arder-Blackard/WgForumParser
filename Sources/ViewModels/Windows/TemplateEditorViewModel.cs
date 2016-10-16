﻿using System;
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
        private AsyncObservableCollection<ChartTemplateViewModel> _editorTemplates = new AsyncObservableCollection<ChartTemplateViewModel>();
        private AsyncObservableCollection<QuestionChartMapping> _questionTemplateConnections = new AsyncObservableCollection<QuestionChartMapping>();

        #endregion


        #region Auto-properties

        public double UnifiedWidth { get; set; }

        public double UnifiedHeight { get; set; }

        #endregion


        #region Properties

        public AsyncObservableCollection<ChartTemplateViewModel> EditorTemplates
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

            var questionsMap = Questions.ToDictionary( question => question.Text, StringComparer.OrdinalIgnoreCase );

            foreach ( var template in templates )
            {
                //  Find questions matching the template
                var matches = template.Questions
                                      .Select( question => new { TemplateQuestion = question, PollQuestionViewModel = FindMatchingPollQuestion( question, questionsMap ) } )
                                      .Where( pair => pair.PollQuestionViewModel != null )
                                      .ToList();

                if ( matches.Any() )
                {
                    //  Add template view model
                    var matchingQuestions = matches.Select( match => new KeyValuePair<TemplateQuestion, PollQuestion>( match.TemplateQuestion, match.PollQuestionViewModel.Question ) );
                    var templateViewModel = new ChartTemplateViewModel( template.Copy(), matchingQuestions );
                    EditorTemplates.Add( templateViewModel );

                    //  Add questions-template connections 
                    foreach ( var match in matches )
                        QuestionTemplateConnections.Add( new QuestionChartMapping( match.PollQuestionViewModel, templateViewModel ) );
                }
            }

            TemplatesChanged = false;
        }

        /// <summary>
        ///     Opens editor for a single chart template represented by the <paramref name="templateViewModel"/>.
        /// </summary>
        /// <param name="templateViewModel">The view model representing a chart template.</param>
        public void EditChartTemplate( ChartTemplateViewModel templateViewModel )
        {
            _viewProvider.Show<TemplatePropertiesEditorViewModel>( this, propertiesViewModel => propertiesViewModel.Template = templateViewModel );
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
