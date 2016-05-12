﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Chloe.Mapper
{
    public class ObjectActivator : IObjectActivator
    {
        int? _checkNullOrdinal;
        Func<IDataReader, ReaderOrdinalEnumerator, ObjectActivatorEnumerator, object> _instanceCreator;
        List<int> _readerOrdinals;
        List<IObjectActivator> _objectActivators;
        List<IValueSetter> _memberSetters;

        ReaderOrdinalEnumerator _readerOrdinalEnumerator;
        ObjectActivatorEnumerator _objectActivatorEnumerator;
        public ObjectActivator(Func<IDataReader, ReaderOrdinalEnumerator, ObjectActivatorEnumerator, object> instanceCreator, List<int> readerOrdinals, List<IObjectActivator> objectActivators, List<IValueSetter> memberSetters, int? checkNullOrdinal)
        {
            this._instanceCreator = instanceCreator;
            this._readerOrdinals = readerOrdinals;
            this._objectActivators = objectActivators;
            this._memberSetters = memberSetters;
            this._checkNullOrdinal = checkNullOrdinal;

            this._readerOrdinalEnumerator = new ReaderOrdinalEnumerator(readerOrdinals);
            this._objectActivatorEnumerator = new ObjectActivatorEnumerator(objectActivators);
        }

        public object CreateInstance(IDataReader reader)
        {
            if (this._checkNullOrdinal != null)
            {
                if (reader.IsDBNull(this._checkNullOrdinal.Value))
                    return null;
            }

            this._readerOrdinalEnumerator.Reset();
            this._objectActivatorEnumerator.Reset();

            object obj = null;
            try
            {
                obj = this._instanceCreator(reader, this._readerOrdinalEnumerator, this._objectActivatorEnumerator);
            }
            catch (Exception ex)
            {
                if (this._readerOrdinalEnumerator.CurrentOrdinal >= 0)
                {
                    throw new DataException(AppendThrowMsg(reader, this._readerOrdinalEnumerator.CurrentOrdinal), ex);
                }

                throw;
            }

            IValueSetter memberSetter = null;
            try
            {
                for (int i = 0; i < this._memberSetters.Count; i++)
                {
                    memberSetter = this._memberSetters[i];
                    memberSetter.SetValue(obj, reader);
                }
            }
            catch (Exception ex)
            {

                MappingMemberBinder binder = memberSetter as MappingMemberBinder;
                if (binder != null)
                {
                    throw new DataException(AppendThrowMsg(reader, binder.Ordinal), ex);
                }

                throw;
            }

            return obj;
        }

        static string AppendThrowMsg(IDataReader reader, int ordinal)
        {
            string msg = string.Format("Error: {0}({1},{2},{3})", reader.GetName(ordinal), ordinal.ToString(), reader.GetDataTypeName(ordinal), reader.GetFieldType(ordinal).FullName);
            return msg;
        }
    }
}
