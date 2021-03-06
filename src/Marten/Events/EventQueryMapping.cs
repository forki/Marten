﻿using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Marten.Generation;
using Marten.Linq;
using Marten.Schema;
using Marten.Services;

namespace Marten.Events
{
    public class EventQueryMapping : DocumentMapping
    {
        public EventQueryMapping(StoreOptions storeOptions) : base(typeof(IEvent), storeOptions)
        {
            Selector = new EventSelector(storeOptions.Events, storeOptions.Serializer());
            DatabaseSchemaName = storeOptions.Events.DatabaseSchemaName;

            Table = new TableName(DatabaseSchemaName, "mt_events");

            duplicateField(x => x.Sequence, "seq_id");
            duplicateField(x => x.StreamId, "stream_id");
            duplicateField(x => x.Version, "version");
            duplicateField(x => x.Timestamp, "timestamp");
        }

        private DuplicatedField duplicateField(Expression<Func<IEvent, object>> property, string columnName)
        {
            var finder = new FindMembers();
            finder.Visit(property);

            return DuplicateField(finder.Members.ToArray(), columnName: columnName);
        }

        public ISelector<IEvent> Selector { get; }

        public override TableName Table { get; }

        public override string[] SelectFields()
        {
            return Selector.SelectFields();
        }

        public override IDocumentSchemaObjects SchemaObjects => new NulloSchemaObject();
    }

    public class NulloSchemaObject : IDocumentSchemaObjects
    {
        public void GenerateSchemaObjectsIfNecessary(AutoCreate autoCreateSchemaObjectsMode, IDocumentSchema schema,
            SchemaPatch patch)
        {
        }

        public void WriteSchemaObjects(IDocumentSchema schema, StringWriter writer)
        {
        }

        public void RemoveSchemaObjects(IManagedConnection connection)
        {
        }

        public void ResetSchemaExistenceChecks()
        {
        }

        public void WritePatch(IDocumentSchema schema, SchemaPatch patch)
        {
        }

        public string Name => "IEvent Query";

        public TableDefinition StorageTable()
        {
            throw new NotSupportedException();
        }
    }
}