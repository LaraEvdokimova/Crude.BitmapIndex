﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Crude.BitmapIndex.Implementations.Bitmap;
using Crude.BitmapIndex.Implementations.BitmapIndexes;
using static Crude.BitmapIndex.Helpers.PropertyNameHelper;

namespace Crude.BitmapIndex.Implementations.Builder
{
    public class BitmapIndexBuilder<T>
    {
        private readonly Dictionary<string, Predicate<T>> _keys;
        private Func<int, IBitmap> _bitMapFactory;
        private IEnumerable<T> _data;

        public BitmapIndexBuilder()
        {
            _keys = new Dictionary<string, Predicate<T>>();
        }

        public BitmapIndexBuilder<T> WithBitMap(Func<int, IBitmap> constructor)
        {
            _bitMapFactory = constructor;
            return this;
        }


        public BitmapIndexBuilder<T> IndexFor<TV>(T @object, Expression<Func<T, TV>> expression)
        {
            var name = From(expression);
            var func = expression.Compile();
            var value = func.Invoke(@object);

            bool Selector(T obj)
            {
                return func.Invoke(obj).Equals(value);
            }

            _keys.Add($"{name}.{value}", Selector);
            return this;
        }


        public BitmapIndexBuilder<T> IndexFor(string key, Predicate<T> predicate)
        {
            _keys.Add(key, predicate);
            return this;
        }


        public BitmapIndexBuilder<T> IndexForClass<TV, TSelected>(T @object,
            Expression<Func<T, TV>> expressionClass,
            Expression<Func<TV, TSelected>> expressionProperty)
        {
            var classPropName = From(expressionClass);
            var nestedPropName = From(expressionProperty);

            var funcClass = expressionClass.Compile();
            var classPropValue = funcClass.Invoke(@object);
            var funcProp = expressionProperty.Compile();
            var nestedPropValue = funcProp.Invoke(classPropValue);

            bool Selector(T obj)
            {
                var c = funcClass.Invoke(obj);
                var p = funcProp.Invoke(c);
                return p.Equals(nestedPropValue);
            }

            _keys.Add($"{classPropName}.{nestedPropName}.{nestedPropValue.ToString()}", Selector);

            return this;
        }


        public BitmapIndexBuilder<T> IndexForArrayOfClass<TValue, TSelectValue>(T @object,
            Expression<Func<T, IEnumerable<TValue>>> expression,
            Func<IEnumerable<TValue>, IEnumerable<TSelectValue>> valueSelector)
        {
            var propertyName = From(expression);
            var func = expression.Compile();
            var values = func.Invoke(@object);
            foreach (var value in valueSelector(values))
            {
                bool Selector(T obj)
                {
                    var val = func.Invoke(obj);
                    var selectedValues = valueSelector(val);
                    return selectedValues.Contains(value);
                }

                _keys.Add($"{propertyName}.{value.ToString()}", Selector);
            }

            return this;
        }


        public BitmapIndexBuilder<T> IndexForArray<TValue>(T @object, Expression<Func<T, IEnumerable<TValue>>> expression)
        {
            var propertyName = From(expression);
            var func = expression.Compile();
            var values = func.Invoke(@object);
            foreach (var value in values)
            {
                bool Selector(T obj)
                {
                    return func.Invoke(obj).Contains(value);
                }

                _keys.Add($"{propertyName}.{value.ToString()}", Selector);
            }

            return this;
        }


        public BitmapIndexBuilder<T> ForData(IEnumerable<T> data)
        {
            _data = data;
            return this;
        }


        public DefaultBitmapIndex<T> Build()
        {
            if (_data is null) throw new NullReferenceException("Data is not set");

            return new DefaultBitmapIndex<T>(_keys, _data, _bitMapFactory ?? (i => new BitmapDefault(i)) );
        }


        public IEnumerable<string> Keys()
        {
            return _keys.Keys;
        }


        public override string ToString()
        {
            var keysStr = string.Join("\n", _keys.Keys);
            return keysStr;
        }
    }
}