﻿using Jinaga.Facts;
using Jinaga.Products;
using Jinaga.Projections;
using Jinaga.Services;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Jinaga.Store.SQLite.SQLiteStore;

namespace Jinaga.Store.SQLite
{
    public class SQLiteStore : IStore
    {

        private ConnectionFactory connFactory;

        public SQLiteStore()
        {
            string dbFolderName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            this.connFactory = new ConnectionFactory(dbFolderName + "\\jinaga.db");
        }


        Task<ImmutableList<Fact>> IStore.Save(FactGraph graph, CancellationToken cancellationToken)
        {

            if (graph.FactReferences.IsEmpty)
            {
                return Task.FromResult(ImmutableList<Fact>.Empty);
            }
            else
            {
                ImmutableList<Fact> newFacts = ImmutableList<Fact>.Empty;
                foreach (var factReference in graph.FactReferences)
                {
                    var fact = graph.GetFact(factReference);

                    connFactory.WithTxn(
                        (conn, id) =>
                            {
                                string sql;

                                // Select or insert into FactType table.  Gets a FactTypeId
                                sql = $@"
                                    SELECT fact_type_id 
                                    FROM fact_type 
                                    WHERE name = '{fact.Reference.Type}'
                                ";
                                var factTypeId = conn.ExecuteScalar<String>(sql);
                                if (factTypeId == "")
                                {
                                    sql = $@"
                                        INSERT OR IGNORE INTO fact_type (name) 
                                        VALUES ('{fact.Reference.Type}')
                                    ";
                                    conn.ExecuteNonQuery(sql);
                                    sql = $@"
                                        SELECT fact_type_id 
                                        FROM fact_type 
                                        WHERE name = '{fact.Reference.Type}'
                                    ";
                                    factTypeId = conn.ExecuteScalar<String>(sql);
                                }

                                // Select or insert into Fact table.  Gets a FactId
                                sql = $@"
                                    SELECT fact_id FROM fact 
                                    WHERE hash = '{fact.Reference.Hash}' AND fact_type_id = {factTypeId}
                                ";
                                var factId = conn.ExecuteScalar<String>(sql);
                                if (factId == "")
                                {
                                    newFacts = newFacts.Add(fact);
                                    string data = Fact.Canonicalize(fact.Fields, fact.Predecessors);
                                    sql = $@"
                                        INSERT OR IGNORE INTO fact (fact_type_id, hash, data) 
                                        VALUES ({factTypeId},'{fact.Reference.Hash}', '{data}' )
                                    ";
                                    conn.ExecuteNonQuery(sql);
                                    sql = $@"
                                        SELECT fact_id 
                                        FROM fact 
                                        WHERE hash = '{fact.Reference.Hash}' AND fact_type_id = {factTypeId}
                                    ";
                                    factId = conn.ExecuteScalar<String>(sql);

                                    // For each predecessor of the inserted fact ...
                                    foreach (var predecessor in fact.Predecessors)
                                    {
                                        // Select or insert into Role table.  Gets a RoleId
                                        sql = $@"
                                            SELECT role_id 
                                            FROM role 
                                            WHERE defining_fact_type_id = {factTypeId} AND name = '{predecessor.Role}'
                                        ";
                                        var roleId = conn.ExecuteScalar<String>(sql);
                                        if (roleId == "")
                                        {
                                            sql = $@"
                                                INSERT OR IGNORE INTO role (defining_fact_type_id, name) 
                                                VALUES ({factTypeId},'{predecessor.Role}')
                                            ";
                                            conn.ExecuteNonQuery(sql);
                                            sql = $@"
                                                SELECT role_id 
                                                FROM role 
                                                WHERE defining_fact_type_id = {factTypeId} AND name = '{predecessor.Role}'
                                            ";
                                            roleId = conn.ExecuteScalar<String>(sql);
                                        }

                                        // Insert into Edge and Ancestor tables
                                        string predecessorFactId;
                                        switch (predecessor)
                                        {
                                            case PredecessorSingle s:
                                                predecessorFactId = getFactId(conn, s.Reference);
                                                InsertEdge(conn, roleId, factId, predecessorFactId);
                                                InsertAncestors(conn, factId, predecessorFactId);
                                                break;
                                            case PredecessorMultiple m:
                                                foreach (var predecessorMultipleReference in m.References)
                                                {
                                                    predecessorFactId = getFactId(conn, predecessorMultipleReference);
                                                    InsertEdge(conn, roleId, factId, predecessorFactId);
                                                    InsertAncestors(conn, factId, predecessorFactId);
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                    };
                                }
                                return 0;
                            },
                        false
                    );

                }
                return Task.FromResult(newFacts);
            }

        }


        private string getFactId(ConnectionFactory.Conn conn, FactReference factReference)
        {
            string sql;

            sql = $@"
                SELECT fact_type_id 
                FROM fact_type 
                WHERE name = '{factReference.Type}'
            ";
            var factTypeId = conn.ExecuteScalar<String>(sql);

            sql = $@"
                SELECT fact_id 
                FROM fact 
                WHERE hash = '{factReference.Hash}' AND fact_type_id = {factTypeId}
            ";
            return conn.ExecuteScalar<String>(sql);
        }


        private void InsertAncestors(ConnectionFactory.Conn conn, string factId, string predecessorFactId)
        {
            string sql;

            sql = $@"
                INSERT OR IGNORE INTO ancestor 
                    (fact_id, ancestor_fact_id)
                SELECT '{factId}', '{predecessorFactId}'
                UNION
                SELECT '{factId}', ancestor_fact_id
                FROM ancestor
                WHERE fact_id = '{predecessorFactId}'
            ";
            conn.ExecuteNonQuery(sql);
        }


        private void InsertEdge(ConnectionFactory.Conn conn, string roleId, string successorFactId, string predecessorFactId)
        {
            string sql;

            sql = $@"
                INSERT OR IGNORE INTO edge (role_id, successor_fact_id, predecessor_fact_id) 
                VALUES ({roleId}, {successorFactId}, {predecessorFactId})
            ";
            conn.ExecuteNonQuery(sql);
        }


        Task<FactGraph> IStore.Load(ImmutableList<FactReference> references, CancellationToken cancellationToken)
        {

            if (references.IsEmpty)
            {
                return Task.FromResult(FactGraph.Empty);
            }
            else
            {
                var factsFromDb = connFactory.WithConn(
                    (conn, id) =>
                        {                     
                            string[]  referenceValues = references.Select((f) => "('" + f.Hash + "', '" + f.Type + "')").ToArray();                           
                            string sql;
                            sql = $@"
                                SELECT f.hash, 
                                       f.data,
                                       t.name
                                FROM fact f 
                                JOIN fact_type t 
                                    ON f.fact_type_id = t.fact_type_id    
                                WHERE (f.hash,t.name) 
                                    IN (VALUES {String.Join(",", referenceValues)} )

                             UNION 

                                SELECT f2.hash, 
                                       f2.data,
                                       t2.name
                                FROM fact f1 
                                JOIN fact_type t1 
                                    ON 
                                        f1.fact_type_id = t1.fact_type_id    
                                            AND
                                        (f1.hash,t1.name) IN (VALUES {String.Join(",", referenceValues)} ) 
                                JOIN ancestor a 
                                    ON a.fact_id = f1.fact_id 
                                JOIN fact f2 
                                    ON f2.fact_id = a.ancestor_fact_id 
                                JOIN fact_type t2 
                                    ON t2.fact_type_id = f2.fact_type_id
                            ";

                            return conn.ExecuteQuery<FactFromDb>(sql);
                        },
                    true   //exponentional backoff
                );

                FactGraphBuilder fb = new FactGraphBuilder() ;
            
                foreach (Fact fact in factsFromDb.Deserialise()) 
                {
                    fb.Add(fact);
                }

                return Task.FromResult(fb.Build());
            }
        }


        async Task<ImmutableList<Product>> IStore.Query(ImmutableList<FactReference> startReferences, Specification specification, CancellationToken cancellationToken)
        {
            // Load type and role IDs
            ImmutableDictionary<string, int> factTypes = await LoadFactTypesFromSpecification(specification, cancellationToken);
            ImmutableDictionary<int, ImmutableDictionary<string, int>> roleMap = await LoadRolesFromSpecification(specification, factTypes, cancellationToken);

            // If any of the start facts are not known types, the specification cannot be satisfied
            if (startReferences.Any(r => !factTypes.ContainsKey(r.Type)))
            {
                return ImmutableList<Product>.Empty;
            }

            // Produce a SqlQueryTree from the specification
            SqlQueryTree sqlQueryTree = ResultSqlFromSpecification(startReferences, specification, factTypes, roleMap);

            // Execute all SQL queries and produce a ResultSetTree
            ResultSetTree resultSetTree = await ExecuteSqlQueryTree(sqlQueryTree, cancellationToken);

            // Convert the ResultSetTree to a list of Products
            var givenProduct = specification.Given
                .Select((label, index) =>
                    (label.Name, Element: (Element)new SimpleElement(startReferences[index])))
                .Aggregate(Product.Empty, (product, pair) => product.With(pair.Name, pair.Element));
            ImmutableList<Product> products = sqlQueryTree.ResultsToProducts(resultSetTree, givenProduct);
            return products;
        }

        private Task<ImmutableDictionary<string, int>> LoadFactTypesFromSpecification(Specification specification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private Task<ImmutableDictionary<int, ImmutableDictionary<string, int>>> LoadRolesFromSpecification(Specification specification, ImmutableDictionary<string, int> factTypes, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private SqlQueryTree ResultSqlFromSpecification(ImmutableList<FactReference> startReferences, Specification specification, ImmutableDictionary<string, int> factTypes, ImmutableDictionary<int, ImmutableDictionary<string, int>> roleMap)
        {
            ResultDescriptionBuilder descriptionBuilder = new ResultDescriptionBuilder(factTypes, roleMap);
            ResultDescription description = descriptionBuilder.Build(startReferences, specification);

            if (!description.QueryDescription.IsSatisfiable())
            {
                return null;
            }
            return CreateSqlQueryTree(description, 0);
        }

        private SqlQueryTree CreateSqlQueryTree(ResultDescription description, int parentFactIdLength)
        {
            SpecificationSqlQuery sqlQuery = description.QueryDescription.GenerateResultSqlQuery();
            var childQueries = description.ChildResultDescriptions
                .Select(child => KeyValuePair.Create(
                    child.Key,
                    CreateSqlQueryTree(child.Value, description.QueryDescription.OutputLength())))
                .ToImmutableDictionary();
            return new SqlQueryTree(sqlQuery, parentFactIdLength, childQueries);
        }

        private Task<ResultSetTree> ExecuteSqlQueryTree(SqlQueryTree sqlQueryTree, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> LoadBookmark(string feed)
        {
            throw new NotImplementedException();
        }

        public Task<ImmutableList<FactReference>> ListKnown(ImmutableList<FactReference> factReferences)
        {
            throw new NotImplementedException();
        }

        public Task SaveBookmark(string feed, string bookmark)
        {
            throw new NotImplementedException();
        }

        public class FactFromDb
        {
            public string hash { get; set; }
            public string data { get; set; }
            public string name { get; set; }
        }

    }


    public static class MyExtensions
    {

        public static IEnumerable<Fact> Deserialise(this IEnumerable<FactFromDb> factsFromDb) 
        {

            foreach (var FactFromDb in factsFromDb)
            {
                ImmutableList<Field> fields = ImmutableList<Field>.Empty;
                ImmutableList<Predecessor> predecessors = ImmutableList<Predecessor>.Empty;

                using (JsonDocument document = JsonDocument.Parse(FactFromDb.data))
                {
                    JsonElement root = document.RootElement;

                    JsonElement fieldsElement = root.GetProperty("fields");
                    foreach (var field in fieldsElement.EnumerateObject())
                    {
                        switch (field.Value.ValueKind)
                        {
                            case JsonValueKind.String:
                                fields = fields.Add(new Field(field.Name, new FieldValueString(field.Value.GetString())));
                                break;
                            case JsonValueKind.Number:
                                fields = fields.Add(new Field(field.Name, new FieldValueNumber(field.Value.GetDouble())));
                                break;
                            case JsonValueKind.True:
                            case JsonValueKind.False:
                                fields = fields.Add(new Field(field.Name, new FieldValueBoolean(field.Value.GetBoolean())));
                                break;
                        }
                    }

                    string hash;
                    string type;
                    JsonElement predecessorsElement = root.GetProperty("predecessors");
                    foreach (var predecessor in predecessorsElement.EnumerateObject())
                    {
                        switch (predecessor.Value.ValueKind)
                        {
                            case JsonValueKind.Object:
                                hash = predecessor.Value.GetProperty("hash").GetString();
                                type = predecessor.Value.GetProperty("type").GetString();
                                predecessors = predecessors.Add(new PredecessorSingle(predecessor.Name, new FactReference(type, hash)));
                                break;
                            case JsonValueKind.Array:
                                ImmutableList<FactReference> factReferences = ImmutableList<FactReference>.Empty;
                                foreach (var factReference in predecessor.Value.EnumerateArray())
                                {
                                    hash = factReference.GetProperty("hash").GetString();
                                    type = factReference.GetProperty("type").GetString();
                                    factReferences = factReferences.Add(new FactReference(type, hash));
                                }
                                predecessors = predecessors.Add(new PredecessorMultiple(predecessor.Name, factReferences));
                                break;
                        }
                    }
                }

                yield return Fact.Create(FactFromDb.name, fields, predecessors);
            }            
        }
    }



}
