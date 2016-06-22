﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ForumParserWPF.Models;
using WpfCommon.Commands;
using WpfCommon.ViewModels.Base;

namespace ForumParserWPF.ViewModels.Windows
{
    public class TemplateEditorViewModel : SimpleWindowViewModelBase
    {
        #region Fields

        private ForumTopic _forumTopic;
        private ICollection<PollQuestionChartViewModel> _questions;

        #endregion


        #region Auto-properties

        public ObservableCollection<TemplateViewModel> Templates { get; } = new ObservableCollection<TemplateViewModel>();

        public ObservableCollection<QuestionChartMapping> QuestionTemplateConnections { get; } = new ObservableCollection<QuestionChartMapping>();

        #endregion


        #region Properties

        public ICollection<PollQuestionChartViewModel> Questions
        {
            get { return _questions; }
            set { SetValue( ref _questions, value ); }
        }

        public string ViewTitle => "Редактор шаблонов";

        public ForumTopic ForumTopic
        {
            get { return _forumTopic; }
            set
            {
                if ( !SetValue( ref _forumTopic, value ) )
                    return;
                InitQuestions();
            }
        }

        #endregion


        #region Initialization

        public TemplateEditorViewModel()
        {
            CreateNewChartCommand = new DelegateCommand<PollQuestion>( CreateNewChartCommandHandler );
            AddQuestionToTemplateCommand = new DelegateCommand<Tuple<TemplateViewModel, PollQuestion>>( AddQuestionToTemplateCommandHandler );
            RemoveQuestionFromTemplateCommand = new DelegateCommand<PollQuestion>( RemoveQuestionFromTemplateCommandHandler );
            SetEqualSizeCommand = new DelegateCommand( SetEqualSizeCommandHandler );
        }

        #endregion


        #region Non-public methods

        private void SetEqualSizeCommandHandler()
        {
            foreach ( var template in Templates )
            {
                template.Width = UnifiedWidth;
                template.Height = UnifiedHeight;
            }
        }

        private void AddQuestionToTemplate( PollQuestion question, TemplateViewModel targetTemplate )
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

        private void InitQuestions()
        {
            Questions = _forumTopic.Poll.Questions.Select( question => new PollQuestionChartViewModel( question ) ).ToArray();
        }

        #endregion


        #region Commands

        public ICommand AddQuestionToTemplateCommand { get; }
        public ICommand RemoveQuestionFromTemplateCommand { get; }
        public ICommand CreateNewChartCommand { get; }
        public ICommand SetEqualSizeCommand { get; }

        public double UnifiedWidth { get; set; }

        public double UnifiedHeight { get; set; }

        private void CreateNewChartCommandHandler( PollQuestion question )
        {
            if ( question == null )
                throw new ArgumentNullException( nameof( question ) );

            var newChartViewModel = new TemplateViewModel( question.Answers.Count );
            Templates.Add( newChartViewModel );

            AddQuestionToTemplate( question, newChartViewModel );
        }

        private void AddQuestionToTemplateCommandHandler( Tuple<TemplateViewModel, PollQuestion> param )
        {
            var targetChart = param.Item1;
            var question = param.Item2;

            if ( question == null )
                throw new ArgumentNullException( nameof( question ) );
            if ( targetChart == null )
                throw new ArgumentNullException( nameof( targetChart ) );

            AddQuestionToTemplate( question, targetChart );
        }

        private void RemoveQuestionFromTemplateCommandHandler( PollQuestion question )
        {
            RemoveQuestionFromTemplate( question );
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

        #endregion
    }

    public class QuestionChartMapping
    {
        #region Auto-properties

        public PollQuestionChartViewModel Question { get; }
        public TemplateViewModel Template { get; }

        #endregion


        #region Initialization

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        public QuestionChartMapping( PollQuestionChartViewModel question, TemplateViewModel template )
        {
            Question = question;
            Template = template;
        }

        #endregion
    }
}