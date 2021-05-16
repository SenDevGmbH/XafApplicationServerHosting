using DevExpress.Xpo.DB;
using DevExpress.Xpo.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using DevExpress.Data.Filtering;

namespace SenDev.Xaf.ApplicationServerHosting
{
    public class MixedDataStore : ICommandChannel, ISqlDataStore, IDisposable
    {


        private abstract class DataStoreEntryBase
        {

            protected DataStoreEntryBase(IDataStore dataStore, DataStoreMode mode)
            {
                DataStore = dataStore;
                Mode = mode;
            }

            internal IDataStore DataStore { get; }
            public DataStoreMode Mode { get; }

            internal abstract bool Matches(DBTable table);
            internal abstract void UpdateStatement(ModificationStatement statement);
            internal abstract void UpdateStatement(SelectStatement statement);
            internal abstract DBTable UpdateTable(DBTable table);
        }

        private class TablesDataStoreEntry : DataStoreEntryBase
        {
            private readonly HashSet<string> tableNames;
            public TablesDataStoreEntry(IDataStore dataStore, DataStoreMode mode, IEnumerable<string> tableNames) : base(dataStore, mode)
            {
                this.tableNames = new HashSet<string>(tableNames, StringComparer.OrdinalIgnoreCase);
            }

            internal override bool Matches(DBTable table)
            {
                return tableNames.Contains(table.Name);
            }

            internal override void UpdateStatement(ModificationStatement statement)
            {
            }

            internal override void UpdateStatement(SelectStatement statement)
            {
            }

            internal override DBTable UpdateTable(DBTable table) => table;
        }

        private class PrefixDataStoreEntry : DataStoreEntryBase
        {

            internal PrefixDataStoreEntry(IDataStore dataStore, DataStoreMode mode, string prefix, bool deletePrefix) : base(dataStore, mode)
            {
                Prefix = prefix;
                DeletePrefix = deletePrefix;
            }


            private string Prefix { get; }

            internal bool DeletePrefix { get; }


            private static DBTable RemovePrefix(DBTable table, string prefix)
            {
                DBTable cloned = new ClonedDbTable(table, prefix);
                cloned.Name = table.Name.Substring(prefix.Length);
                return cloned;
            }

            private void RemovePrefix(JoinNode node, string prefix, Dictionary<DBTable, DBTable> clonedTables)
            {
                if (node.Table.Name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    DBTable cloned;
                    if (!clonedTables.TryGetValue(node.Table, out cloned))
                    {
                        cloned = RemovePrefix(node.Table, prefix);
                        clonedTables[node.Table] = cloned;
                        node.Table = cloned;
                    }
                }
                foreach (var subNode in node.SubNodes)
                    RemovePrefix(subNode, prefix, clonedTables);
            }

            private void RemovePrefix(JoinNode node)
            {
                if (!string.IsNullOrEmpty(Prefix))
                    RemovePrefix(node, Prefix, new Dictionary<DBTable, DBTable>());
            }

            internal override void UpdateStatement(ModificationStatement statement)
            {
                RemovePrefix(statement);
            }



            internal override bool Matches(DBTable table)
            {
                if (table is ClonedDbTable cloned)
                    return Prefix == cloned.Prefix;
                else
                    return !string.IsNullOrEmpty(Prefix) && table.Name.StartsWith(Prefix, StringComparison.Ordinal);
            }

            internal override void UpdateStatement(SelectStatement statement) => RemovePrefix(statement);

            internal override DBTable UpdateTable(DBTable table)
            {
                return RemovePrefix(table, Prefix);
            }
        }


        private class ClonedDbTable : DBTable
        {
            internal ClonedDbTable(DBTable table, string prefix)
            {
                Prefix = prefix;
                Columns.AddRange(table.Columns);
                ForeignKeys.AddRange(table.ForeignKeys);
                Indexes.AddRange(table.Indexes);
                IsView = table.IsView;
                PrimaryKey = table.PrimaryKey;
            }

            public string Prefix { get; }

        }

        private readonly List<DataStoreEntryBase> entries = new List<DataStoreEntryBase>();


        public MixedDataStore(IDataStore dataStore, DataStoreMode mode)
        {
            DefaultDataStoreEntry = new TablesDataStoreEntry(dataStore, mode, Enumerable.Empty<string>());
        }
        public void AddDataStore(IDataStore dataStore, DataStoreMode mode, string prefix, bool deletePrefix)
        {
            entries.Add(new PrefixDataStoreEntry(dataStore, mode, prefix, deletePrefix));
        }
        public void AddDataStore(IDataStore dataStore, DataStoreMode mode, IEnumerable<string> tableNames)
        {
            entries.Add(new TablesDataStoreEntry(dataStore, mode, tableNames));
        }


        private IDataStore MainDataStore => DefaultDataStoreEntry.DataStore;
        public AutoCreateOption AutoCreateOption => MainDataStore.AutoCreateOption;

        public IDbConnection Connection => (MainDataStore as ISqlDataStore)?.Connection ?? throw new NotSupportedException();

        private TablesDataStoreEntry DefaultDataStoreEntry { get; }

        public IDbCommand CreateCommand() => (MainDataStore as ISqlDataStore)?.CreateCommand() ?? throw new NotSupportedException();

        public object Do(string command, object args) => ((ICommandChannel)MainDataStore).Do(command, args);

        public ModificationResult ModifyData(params ModificationStatement[] dmlStatements)
        {
            if (dmlStatements == null)
                throw new ArgumentNullException(nameof(dmlStatements));


            List<ParameterValue> identities = new List<ParameterValue>();
            foreach (var statement in dmlStatements)
            {
                var entry = GetDataStoreEntry(statement, DataStoreMode.ReadWrite);
                entry.UpdateStatement(statement);
                identities.AddRange(entry.DataStore.ModifyData(statement).Identities);

            }

            return new ModificationResult(identities.ToArray());
        }


        private static DBTable[] GetTables(params BaseStatement[] statements)
        {
            HashSet<DBTable> tables = new HashSet<DBTable>();
            foreach (BaseStatement statement in statements)
            {
                List<JoinNode> nodes;
                List<CriteriaOperator> criteria;
                statement.CollectJoinNodesAndCriteria(out nodes, out criteria);
                foreach (JoinNode node in nodes)
                {
                    DBTable table = node.Table;
                    if (table != null)
                    {
                        DBProjection projection = node.Table as DBProjection;
                        if (projection == null)
                        {
                            tables.Add(table);
                        }
                        else
                        {
                            foreach (var tbl in GetTables(projection.Projection))
                            {
                                tables.Add(table);
                            }
                        }
                    }
                }
            }

            return tables.ToArray();
        }

        private DataStoreEntryBase GetDataStoreEntry(BaseStatement statement, DataStoreMode mode)
        {
            var tables = GetTables(statement);

            var dataStores = tables.Select(GetDataStoreEntry).Distinct().ToArray();
            if (dataStores.Length == 1)
            {
                var result = dataStores[0];
                if (result.Mode < mode)
                    throw new InvalidOperationException($"Mode {mode} not allowed on data store {result.DataStore}");
                return result;
            }


            if (dataStores.Length == 0)
                return DefaultDataStoreEntry;

            string joinedNames = string.Join(", ", tables.Select(t => t.Name));
            throw new InvalidOperationException($"Multiple data stores found in sinlge statement. Tables: {joinedNames}");

        }

        private DataStoreEntryBase GetDataStoreEntry(DBTable table) => entries.FirstOrDefault(e => e.Matches(table)) ?? DefaultDataStoreEntry;

        public SelectedData SelectData(params SelectStatement[] selects)
        {
            if (selects == null)
                throw new ArgumentNullException(nameof(selects));

            List<SelectStatementResult> results = new List<SelectStatementResult>();
            foreach (var statement in selects)
            {
                var entry = GetDataStoreEntry(statement, DataStoreMode.ReadOnly);
                entry.UpdateStatement(statement);
                results.AddRange(entry.DataStore.SelectData(statement).ResultSet);
            }

            return new SelectedData(results.ToArray());

        }

        public UpdateSchemaResult UpdateSchema(bool dontCreateIfFirstTableNotExist, params DBTable[] tables)
        {
            HashSet<DBTable> tablesToUpdate = new HashSet<DBTable>(tables);
            UpdateSchemaResult Update(DataStoreEntryBase entry)
            {
                var filteredTables = tablesToUpdate.Where(t => GetDataStoreEntry(t) == entry).Select(t => entry.UpdateTable(t)).ToArray();
                var result = entry.DataStore.UpdateSchema(dontCreateIfFirstTableNotExist, filteredTables);

                foreach (var updatedTable in filteredTables)
                    tablesToUpdate.Remove(updatedTable);

                return result;



            }
            foreach (var entry in entries.Where(e => e.Mode >= DataStoreMode.SchemaUpdate))
            {
                var result = Update(entry);
                if (result == UpdateSchemaResult.FirstTableNotExists)
                    return result;

            }

            if (DefaultDataStoreEntry.Mode == DataStoreMode.SchemaUpdate)
                return Update(DefaultDataStoreEntry);

            return UpdateSchemaResult.SchemaExists;

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var disposable in entries.Select(e => e.DataStore).OfType<IDisposable>())
                    disposable.Dispose();
            }
        }

        ~MixedDataStore()
        {
            Dispose(false);
        }
    }
}
