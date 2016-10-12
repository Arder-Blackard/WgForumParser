using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
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

        private ForumTopic _forumTopic;
        private ICollection<PollQuestionChartViewModel> _questions;
        private readonly IViewProvider _viewProvider;

        #endregion


        #region Auto-properties

        public ObservableCollection<ChartTemplateViewModel> Templates { get; } = new ObservableCollection<ChartTemplateViewModel>();

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

        public TemplateEditorViewModel( IViewProvider viewProvider )
        {
            _viewProvider = viewProvider;
            CreateNewChartCommand = new DelegateCommand<PollQuestion>( CreateNewChartCommandHandler );
            AddQuestionToTemplateCommand = new DelegateCommand<Tuple<ChartTemplateViewModel, PollQuestion>>( AddQuestionToTemplateCommandHandler );
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

            var newChartViewModel = new ChartTemplateViewModel( question.Answers.Count );
            Templates.Add( newChartViewModel );

            AddQuestionToTemplate( question, newChartViewModel );
        }

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


        public void EditChartTemplate( ChartTemplateViewModel templateViewModel )
        {
            var result = _viewProvider.Show<TemplatePropertiesEditorViewModel>( this, propertiesViewModel => propertiesViewModel.Template = templateViewModel );
        }

        public void InitEditor( ICollection<ChartTemplateViewModel> templates, ForumTopic forumTopic )
        {
            ForumTopic = forumTopic;
            Templates.Clear();
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
