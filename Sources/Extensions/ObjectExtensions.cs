﻿using System.ArrayExtensions;
using System.Collections.Generic;
using System.Reflection;

namespace System
{
    /// <summary>
    ///     Source code is released under the MIT license.
    ///     The MIT License(MIT)
    ///     Copyright(c) 2014 Burtsev Alexey
    ///     Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
    ///     documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
    ///     rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
    ///     permit persons to whom the Software is furnished to do so, subject to the following conditions:
    ///     The above copyright notice and this permission notice shall be included in all copies or substantial portions of
    ///     the Software.
    ///     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
    ///     THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    ///     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    ///     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    ///     SOFTWARE.
    /// </summary>
    public static class ObjectExtensions
    {
        #region Constants

        private static readonly MethodInfo CloneMethod = typeof (object).GetMethod( "MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance );

        #endregion


        #region Public methods

        public static bool IsPrimitive( this Type type )
        {
            if ( type == typeof (string) )
                return true;
            return type.IsValueType & type.IsPrimitive;
        }

        public static object Copy( this object originalObject )
        {
            return InternalCopy( originalObject, new Dictionary<object, object>( new ReferenceEqualityComparer() ) );
        }

        public static T Copy<T>( this T original )
        {
            return (T) Copy( (object) original );
        }

        #endregion


        #region Non-public methods

        private static object InternalCopy( object originalObject, IDictionary<object, object> visited )
        {
            if ( originalObject == null )
                return null;
            var typeToReflect = originalObject.GetType();
            if ( IsPrimitive( typeToReflect ) )
                return originalObject;
            if ( visited.ContainsKey( originalObject ) )
                return visited[originalObject];
            if ( typeof (Delegate).IsAssignableFrom( typeToReflect ) )
                return null;
            var cloneObject = CloneMethod.Invoke( originalObject, null );
            if ( typeToReflect.IsArray )
            {
                var arrayType = typeToReflect.GetElementType();
                if ( IsPrimitive( arrayType ) == false )
                {
                    var clonedArray = (Array) cloneObject;
                    clonedArray.ForEach( ( array, indices ) => array.SetValue( InternalCopy( clonedArray.GetValue( indices ), visited ), indices ) );
                }
            }
            visited.Add( originalObject, cloneObject );
            CopyFields( originalObject, visited, cloneObject, typeToReflect );
            RecursiveCopyBaseTypePrivateFields( originalObject, visited, cloneObject, typeToReflect );
            return cloneObject;
        }

        private static void RecursiveCopyBaseTypePrivateFields( object originalObject,
                                                                IDictionary<object, object> visited,
                                                                object cloneObject,
                                                                Type typeToReflect )
        {
            if ( typeToReflect.BaseType != null )
            {
                RecursiveCopyBaseTypePrivateFields( originalObject, visited, cloneObject, typeToReflect.BaseType );
                CopyFields( originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate );
            }
        }

        private static void CopyFields( object originalObject,
                                        IDictionary<object, object> visited,
                                        object cloneObject,
                                        Type typeToReflect,
                                        BindingFlags bindingFlags =
                                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy,
                                        Func<FieldInfo, bool> filter = null )
        {
            foreach ( var fieldInfo in typeToReflect.GetFields( bindingFlags ) )
            {
                if ( filter != null && filter( fieldInfo ) == false )
                    continue;
                if ( IsPrimitive( fieldInfo.FieldType ) )
                    continue;
                var originalFieldValue = fieldInfo.GetValue( originalObject );
                var clonedFieldValue = InternalCopy( originalFieldValue, visited );
                fieldInfo.SetValue( cloneObject, clonedFieldValue );
            }
        }

        #endregion
    }

    public class ReferenceEqualityComparer : EqualityComparer<object>
    {
        #region Public methods

        public override bool Equals( object x, object y )
        {
            return ReferenceEquals( x, y );
        }

        public override int GetHashCode( object obj )
        {
            if ( obj == null )
                return 0;
            return obj.GetHashCode();
        }

        #endregion
    }

    namespace ArrayExtensions
    {
        public static class ArrayExtensions
        {
            #region Public methods

            public static void ForEach( this Array array, Action<Array, int[]> action )
            {
                if ( array.LongLength == 0 )
                    return;
                var walker = new ArrayTraverse( array );
                do
                    action( array, walker.Position ); while ( walker.Step() );
            }

            #endregion
        }

        internal class ArrayTraverse
        {
            #region Fields

            private readonly int[] maxLengths;
            public int[] Position;

            #endregion


            #region Initialization

            public ArrayTraverse( Array array )
            {
                maxLengths = new int[array.Rank];
                for ( var i = 0; i < array.Rank; ++i )
                    maxLengths[i] = array.GetLength( i ) - 1;
                Position = new int[array.Rank];
            }

            #endregion


            #region Public methods

            public bool Step()
            {
                for ( var i = 0; i < Position.Length; ++i )
                {
                    if ( Position[i] < maxLengths[i] )
                    {
                        Position[i]++;
                        for ( var j = 0; j < i; j++ )
                            Position[j] = 0;
                        return true;
                    }
                }
                return false;
            }

            #endregion
        }
    }
}
