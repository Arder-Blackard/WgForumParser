﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace ForumParserWPF.Views.Behaviors
{
    /// <summary>
    ///     Static class used to attach to wpf control
    /// </summary>
    public static class GridViewColumnResize
    {
        #region Constants

        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.RegisterAttached( "Width", typeof (string), typeof (GridViewColumnResize),
                                                 new PropertyMetadata( OnSetWidthCallback ) );

        public static readonly DependencyProperty GridViewColumnResizeBehaviorProperty =
            DependencyProperty.RegisterAttached( "GridViewColumnResizeBehavior",
                                                 typeof (GridViewColumnResizeBehavior), typeof (GridViewColumnResize),
                                                 null );

        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached( "Enabled", typeof (bool), typeof (GridViewColumnResize),
                                                 new PropertyMetadata( OnSetEnabledCallback ) );

        public static readonly DependencyProperty ListViewResizeBehaviorProperty =
            DependencyProperty.RegisterAttached( "ListViewResizeBehaviorProperty",
                                                 typeof (ListViewResizeBehavior), typeof (GridViewColumnResize), null );

        #endregion


        #region Public methods

        public static string GetWidth( DependencyObject obj )
        {
            return (string) obj.GetValue( WidthProperty );
        }

        public static void SetWidth( DependencyObject obj, string value )
        {
            obj.SetValue( WidthProperty, value );
        }

        public static bool GetEnabled( DependencyObject obj )
        {
            return (bool) obj.GetValue( EnabledProperty );
        }

        public static void SetEnabled( DependencyObject obj, bool value )
        {
            obj.SetValue( EnabledProperty, value );
        }

        #endregion


        #region Nested type: GridViewColumnResizeBehavior

        /// <summary>
        ///     GridViewColumn class that gets attached to the GridViewColumn control
        /// </summary>
        public class GridViewColumnResizeBehavior
        {
            #region Fields

            private readonly GridViewColumn _element;

            #endregion


            #region Auto-properties

            public string Width { get; set; }

            #endregion


            #region Properties

            public bool IsStatic
            {
                get { return StaticWidth >= 0; }
            }

            public double StaticWidth
            {
                get
                {
                    double result;
                    return double.TryParse( Width, out result ) ? result : -1;
                }
            }

            public double Percentage
            {
                get
                {
                    if ( !IsStatic )
                        return Mulitplier*100;
                    return 0;
                }
            }

            public double Mulitplier
            {
                get
                {
                    if ( Width == "*" || Width == "1*" )
                        return 1;
                    if ( Width.EndsWith( "*" ) )
                    {
                        double perc;
                        if ( double.TryParse( Width.Substring( 0, Width.Length - 1 ), out perc ) )
                            return perc;
                    }
                    return 1;
                }
            }

            #endregion


            #region Initialization

            public GridViewColumnResizeBehavior( GridViewColumn element )
            {
                _element = element;
            }

            #endregion


            #region Public methods

            public void SetWidth( double allowedSpace, double totalPercentage )
            {
                if ( IsStatic )
                    _element.Width = StaticWidth;
                else
                {
                    var width = allowedSpace*(Percentage/totalPercentage);
                    if ( width >= 0 )
                        _element.Width = width;
                }
            }

            #endregion
        }

        #endregion


        #region Nested type: ListViewResizeBehavior

        /// <summary>
        ///     ListViewResizeBehavior class that gets attached to the ListView control
        /// </summary>
        public class ListViewResizeBehavior
        {
            #region Constants

            private const int Margin = 28;
            private const long RefreshTime = Timeout.Infinite;
            private const long Delay = 500;

            #endregion


            #region Fields

            private readonly ListView _element;
            private readonly Timer _timer;

            #endregion


            #region Auto-properties

            public bool Enabled { get; set; }

            #endregion


            #region Events and invocation

            private void OnLoaded( object sender, RoutedEventArgs e )
            {
                _element.SizeChanged += OnSizeChanged;
            }

            private void OnSizeChanged( object sender, SizeChangedEventArgs e )
            {
                if ( e.WidthChanged )
                {
                    _element.SizeChanged -= OnSizeChanged;
                    _timer.Change( Delay, RefreshTime );
                }
            }

            #endregion


            #region Initialization

            public ListViewResizeBehavior( ListView element )
            {
                if ( element == null )
                    throw new ArgumentNullException( "element" );
                _element = element;
                element.Loaded += OnLoaded;

                // Action for resizing and re-enable the size lookup
                // This stops the columns from constantly resizing to improve performance
                Action resizeAndEnableSize = () =>
                {
                    Resize();
                    _element.SizeChanged += OnSizeChanged;
                };
                _timer = new Timer( x => Application.Current.Dispatcher.BeginInvoke( resizeAndEnableSize ), null, Delay,
                                    RefreshTime );
            }

            #endregion


            #region Non-public methods

            private void Resize()
            {
                if ( Enabled )
                {
                    var totalWidth = _element.ActualWidth;
                    var gv = _element.View as GridView;
                    if ( gv != null )
                    {
                        var allowedSpace = totalWidth - GetAllocatedSpace( gv );
                        allowedSpace = allowedSpace - Margin;
                        var totalPercentage = GridViewColumnResizeBehaviors( gv ).Sum( x => x.Percentage );
                        foreach ( var behavior in GridViewColumnResizeBehaviors( gv ) )
                            behavior.SetWidth( allowedSpace, totalPercentage );
                    }
                }
            }

            private static IEnumerable<GridViewColumnResizeBehavior> GridViewColumnResizeBehaviors( GridView gv )
            {
                foreach ( var t in gv.Columns )
                {
                    var gridViewColumnResizeBehavior =
                        t.GetValue( GridViewColumnResizeBehaviorProperty ) as GridViewColumnResizeBehavior;
                    if ( gridViewColumnResizeBehavior != null )
                        yield return gridViewColumnResizeBehavior;
                }
            }

            private static double GetAllocatedSpace( GridView gv )
            {
                double totalWidth = 0;
                foreach ( var t in gv.Columns )
                {
                    var gridViewColumnResizeBehavior =
                        t.GetValue( GridViewColumnResizeBehaviorProperty ) as GridViewColumnResizeBehavior;
                    if ( gridViewColumnResizeBehavior != null )
                    {
                        if ( gridViewColumnResizeBehavior.IsStatic )
                            totalWidth += gridViewColumnResizeBehavior.StaticWidth;
                    }
                    else
                        totalWidth += t.ActualWidth;
                }
                return totalWidth;
            }

            #endregion
        }

        #endregion


        #region CallBacks

        private static void OnSetWidthCallback( DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e )
        {
            var element = dependencyObject as GridViewColumn;
            if ( element != null )
            {
                var behavior = GetOrCreateBehavior( element );
                behavior.Width = e.NewValue as string;
            }
        }

        private static void OnSetEnabledCallback( DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e )
        {
            var element = dependencyObject as ListView;
            if ( element != null )
            {
                var behavior = GetOrCreateBehavior( element );
                behavior.Enabled = (bool) e.NewValue;
            }
        }

        private static ListViewResizeBehavior GetOrCreateBehavior( ListView element )
        {
            var behavior = element.GetValue( GridViewColumnResizeBehaviorProperty ) as ListViewResizeBehavior;
            if ( behavior == null )
            {
                behavior = new ListViewResizeBehavior( element );
                element.SetValue( ListViewResizeBehaviorProperty, behavior );
            }

            return behavior;
        }

        private static GridViewColumnResizeBehavior GetOrCreateBehavior( GridViewColumn element )
        {
            var behavior = element.GetValue( GridViewColumnResizeBehaviorProperty ) as GridViewColumnResizeBehavior;
            if ( behavior == null )
            {
                behavior = new GridViewColumnResizeBehavior( element );
                element.SetValue( GridViewColumnResizeBehaviorProperty, behavior );
            }

            return behavior;
        }

        #endregion
    }
}