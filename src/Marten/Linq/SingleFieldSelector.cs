﻿using System;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Marten.Schema;
using Marten.Services;

namespace Marten.Linq
{
    public class SingleFieldSelector<T> : BasicSelector, ISelector<T>
    {
        public SingleFieldSelector(IQueryableDocument mapping, MemberInfo[] members)
            : base(mapping.FieldFor(members).SqlLocator)
        {
        }

        public SingleFieldSelector(IQueryableDocument mapping, MemberInfo[] members, bool distinct)
            : base(distinct, mapping.FieldFor(members).SqlLocator)
        {
        }

        public SingleFieldSelector(bool distinct, string field) : base(distinct, field)
        {
        }

        public T Resolve(DbDataReader reader, IIdentityMap map, QueryStatistics stats)
        {
            var raw = reader[0];
            return raw == DBNull.Value ? default(T) : (T) raw;
        }

        public Task<T> ResolveAsync(DbDataReader reader, IIdentityMap map, QueryStatistics stats, CancellationToken token)
        {
            return reader.GetFieldValueAsync<T>(0, token);
        }
    }
}