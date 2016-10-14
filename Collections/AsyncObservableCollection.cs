using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;

namespace ForumParser.Collections
{
    public class AsyncObservableCollection<T> : ObservableCollection<T>
    {
        #region Fields

        private readonly object _syncLock = new object();

        #endregion


        #region Events and invocation

        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        protected override void OnCollectionChanged( NotifyCollectionChangedEventArgs e )
        {
            using ( BlockReentrancy() )
            {
                var eh = CollectionChanged;
                if ( eh == null )
                    return;

                var dispatcher = (from NotifyCollectionChangedEventHandler nh in eh.GetInvocationList()
                                  let dpo = nh.Target as DispatcherObject
                                  where dpo != null
                                  select dpo.Dispatcher).FirstOrDefault();

                if ( dispatcher != null && dispatcher.CheckAccess() == false )
                    dispatcher.Invoke( DispatcherPriority.DataBind, (Action) (() => OnCollectionChanged( e )) );
                else
                {
                    foreach ( var @delegate in eh.GetInvocationList() )
                    {
                        var nh = (NotifyCollectionChangedEventHandler) @delegate;
                        nh.Invoke( this, e );
                    }
                }
            }
        }

        #endregion


        #region Initialization

        public AsyncObservableCollection()
        {
            EnableCollectionSynchronization( this, _syncLock );
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Collections.ObjectModel.ObservableCollection`1" /> class that contains elements
        ///     copied from the specified list.
        /// </summary>
        /// <param name="list">The list from which the elements are copied.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="list" /> parameter cannot be null.</exception>
        public AsyncObservableCollection( List<T> list ) : base( list )
        {
            EnableCollectionSynchronization( this, _syncLock );
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="T:System.Collections.ObjectModel.ObservableCollection`1" /> class that contains elements
        ///     copied from the specified collection.
        /// </summary>
        /// <param name="collection">The collection from which the elements are copied.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collection" /> parameter cannot be null.</exception>
        public AsyncObservableCollection( IEnumerable<T> collection ) : base( collection )
        {
            EnableCollectionSynchronization( this, _syncLock );
        }

        #endregion


        #region Non-public methods

        private static void EnableCollectionSynchronization( IEnumerable collection, object lockObject )
        {
            var method = typeof ( BindingOperations ).GetMethod( "EnableCollectionSynchronization", new[] { typeof ( IEnumerable<> ), typeof ( object ) } );
            // It's .NET 4.5
            method?.Invoke( null, new[] { collection, lockObject } );
        }

        #endregion
    }
}
