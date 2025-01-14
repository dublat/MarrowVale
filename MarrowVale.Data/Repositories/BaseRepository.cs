﻿using MarrowVale.Business.Entities.Entities;
using MarrowVale.Data.Contracts;
using MarrowValue.Data;
using Microsoft.Extensions.Logging;
using Neo4jClient;
using Neo4jClient.Cypher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Data.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : GraphNode, new()
    {
        internal readonly IGraphClient _graphClient;
        private readonly string _entityLabel;
        public BaseRepository(IGraphClient graphClient)
        {
            _graphClient = graphClient;
            _entityLabel = new T().EntityLabel;
        }


        public virtual async Task<IEnumerable<T>> All()
        {
            return await _graphClient.Cypher
                .Match($"(x:{_entityLabel})")
                .Return(x => x.As<T>())
                .ResultsAsync;
        }

        public virtual async Task<T> GetById(string id)
        {
            IEnumerable<T> results = await Where(x => x.Id == id);
            return results.FirstOrDefault();
        }

        public virtual async Task<T> GetByName(string name)
        {
            IEnumerable<T> results = await Where(x => x.Name == name);
            return results.FirstOrDefault();
        }

        public virtual async Task<IEnumerable<T>> Where(Expression<Func<T, bool>> query)
        {
            string name = query.Parameters[0].Name;
            T entity = (T)Activator.CreateInstance(query.Parameters[0].Type);
            Expression<Func<T, bool>> newQuery = PredicateRewriter.Rewrite(query, "e");

            return await _graphClient.Cypher
               .Match($"(e:{entity.EntityLabel})")
               .Where(newQuery)
               .Return(e => e.As<T>())
               .ResultsAsync;
        }


        public virtual async Task<T> Single(Expression<Func<T, bool>> query)
        {
            IEnumerable<T> results = await Where(query);
            return results.FirstOrDefault();
        }

        public virtual async Task Add(T item)
        {
            await _graphClient.Cypher
                    .Create("(e:" + item.FormattedLabels() + " $item)")
                    .WithParam("item", item)
                    .ExecuteWithoutResultsAsync();
        }


        public virtual async Task Update(T newItem)
        {
            string name = "e";
            T itemToUpdate = await this.GetById(newItem.Id);
            this.CopyValues(itemToUpdate, newItem);

            var query = _graphClient.Cypher
               .Match($"({name}:{newItem.EntityLabel})")
               .Where((T e) => e.Id == newItem.Id)
               .Set(name + " = $item")
               .WithParam("item", itemToUpdate);

            await query.ExecuteWithoutResultsAsync();
        }

        public virtual async Task Update(Expression<Func<T, bool>> query, T newItem)
        {
            string name = query.Parameters[0].Name;

            T itemToUpdate = await this.Single(query);
            this.CopyValues(itemToUpdate, newItem);

            await _graphClient.Cypher
               .Match($"({name}:{newItem.EntityLabel})")
               .Where(query)
               .Set(name + " = $item")
               .WithParam("item", itemToUpdate)
               .ExecuteWithoutResultsAsync();
        }

        public void CopyValues(T target, T source)
        {
            Type t = typeof(T);

            var properties = t.GetProperties().Where(prop => prop.CanRead && prop.CanWrite);

            foreach (var prop in properties)
            {
                var value = prop.GetValue(source, null);
                if (value != null)
                    prop.SetValue(target, value, null);
            }
        }

        public virtual async Task Delete(Expression<Func<T, bool>> query)
        {
            string name = query.Parameters[0].Name;
            T entity = (T)Activator.CreateInstance(query.Parameters[0].Type);

            await _graphClient.Cypher
                .Match($"({name}:{entity.EntityLabel})")
                .Where(query)
                .Delete(name)
                .ExecuteWithoutResultsAsync();
        }

        public virtual async Task Relate<T2, TRelationship>(Expression<Func<T, bool>> query1, Expression<Func<T2, bool>> query2, TRelationship relationship)
            where T2 : GraphNode, new()
            where TRelationship : GraphRelationship, new()
        {
            string name1 = query1.Parameters[0].Name;
            T entity1 = (T)Activator.CreateInstance(query1.Parameters[0].Type);
            string name2 = query2.Parameters[0].Name;
            T2 entity2 = (T2)Activator.CreateInstance(query2.Parameters[0].Type);

            object properties = new object();


            var cypherQuery = _graphClient.Cypher
                .Match($"({name1}),({name2}:{entity2.EntityLabel})")
                .Where(query1)
                .AndWhere(query2)
                .Create($"({name1}){relationship.ToString()}({name2})");

            await cypherQuery.ExecuteWithoutResultsAsync();
        }


        public virtual async Task AddAndRelate<T2, TRelationship>(Expression<Func<T, bool>> query1, T2 item, TRelationship relationship)
        where T2 : GraphNode, new()
        where TRelationship : GraphRelationship, new()
        {
            string name1 = query1.Parameters[0].Name;
            string name2 = "e";
            T entity1 = (T)Activator.CreateInstance(query1.Parameters[0].Type);

            object properties = new object();

            var query = _graphClient.Cypher
                .Create("(e:" + item.FormattedLabels() + " $item)")
                .WithParam("item", item);

            await query.ExecuteWithoutResultsAsync();

            var query2 = _graphClient.Cypher
                .Match($"({name1}:{entity1.EntityLabel}),({name2}:{item.EntityLabel})")
                .Where(query1)
                .AndWhere((T e) => e.Id == item.Id)
                .Create($"({name1}){relationship.ToString()}({name2})");

            await query2.ExecuteWithoutResultsAsync();
        }


        public virtual ICypherFluentQuery<T2> RelatedTo<T2, TRelationship>(Expression<Func<T, bool>> query1, Expression<Func<T2, bool>> query2, TRelationship relationship, ChainedQuery preQuery = null)
        where T2 : GraphNode, new()
        where TRelationship : GraphRelationship, new()
        {
            string name1 = query1.Parameters[0].Name;
            T entity1 = (T)Activator.CreateInstance(query1.Parameters[0].Type);

            string name2 = "e";
            T2 entity2 = (T2)Activator.CreateInstance(query2.Parameters[0].Type);

            query2 = PredicateRewriter.Rewrite(query2, name2);

            ICypherFluentQuery<T2> query;
            if (preQuery == null)
            {
                query = _graphClient.Cypher
                    .Match($"({name1}:{entity1.EntityLabel}){relationship.ToString()}({name2}:{entity2.EntityLabel})")
                    .Where(query1)
                    .AndWhere(query2)
                    .Return(e => e.As<T2>());
            }
            else
            {
                query2 = PredicateRewriter.Rewrite(query2, preQuery.Alias);
                query = preQuery.Query.Match($"({name1}:{entity1.EntityLabel}){relationship.ToString()}({preQuery.Alias})")
                    .Where(query1)
                    .AndWhere(query2)
                    .Return(preResult => preResult.As<T2>());
            }

            return query;
        }


        public virtual ICypherFluentQuery<T3> RelatedTo<T2, T3, TRelationship>(Expression<Func<T, bool>> query1, Expression<Func<T2, bool>> query2, Expression<Func<T3, bool>> query3, TRelationship relationship, TRelationship relationship2)
        where T2 : GraphNode, new()
        where T3 : GraphNode, new()
        where TRelationship : GraphRelationship, new()
        {
            string name1 = query1.Parameters[0].Name;
            string name2 = query2.Parameters[0].Name;
            string name3 = "e";

            T entity1 = (T)Activator.CreateInstance(query1.Parameters[0].Type);
            T2 entity2 = (T2)Activator.CreateInstance(query2.Parameters[0].Type);
            T3 entity3 = (T3)Activator.CreateInstance(query3.Parameters[0].Type);

            query3 = PredicateRewriter.Rewrite(query3, name2);

            var query = _graphClient.Cypher
                .Match($"({name1}:{entity1.EntityLabel}){relationship.ToString()}({name2}:{entity2.EntityLabel}){relationship2.ToString()}({name3}:{entity3.EntityLabel})")
                .Where(query1)
                .AndWhere(query2)
                .AndWhere(query3)
                .Return(e => e.As<T3>());
            return query;
        }

        public virtual ICypherFluentQuery<T2> RelatedToAndFrom<T2, T3, TRelationship>(Expression<Func<T, bool>> query1, Expression<Func<T2, bool>> query2, Expression<Func<T3, bool>> query3, TRelationship relationship, TRelationship relationship2)
        where T2 : GraphNode, new()
        where T3 : GraphNode, new()
        where TRelationship : GraphRelationship, new()
        {
            string name1 = query1.Parameters[0].Name;
            string name2 = "e";
            string name3 = query3.Parameters[0].Name;

            T entity1 = (T)Activator.CreateInstance(query1.Parameters[0].Type);
            T2 entity2 = (T2)Activator.CreateInstance(query2.Parameters[0].Type);
            T3 entity3 = (T3)Activator.CreateInstance(query3.Parameters[0].Type);

            query2 = PredicateRewriter.Rewrite(query2, name2);


            var query = _graphClient.Cypher
                .Match($"({name1}:{entity1.EntityLabel}){relationship.ToString()}({name2}:{entity2.EntityLabel}){relationship2.ToString()}({name3}:{entity3.EntityLabel})")
                .Where(query1)
                .AndWhere(query2)
                .AndWhere(query3)
                .Return(e => e.As<T2>());
            return query;
        }


        public virtual ICypherFluentQuery<T> RelatedFrom<T2, TRelationship>(Expression<Func<T, bool>> query1, Expression<Func<T2, bool>> query2, TRelationship relationship, ChainedQuery preQuery = null)
        where T2 : GraphNode, new()
        where TRelationship : GraphRelationship, new()
        {
            string name1 = "e";
            T entity1 = (T)Activator.CreateInstance(query1.Parameters[0].Type);

            string name2 = query2.Parameters[0].Name;
            T2 entity2 = (T2)Activator.CreateInstance(query2.Parameters[0].Type);

            query1 = PredicateRewriter.Rewrite(query1, name1);

            ICypherFluentQuery<T> query;
            if (preQuery == null)
            {
                 query = _graphClient.Cypher
                    .Match($"({name2}:{entity2.FormattedLabels()}){relationship.ToString()}({name1}:{entity1.FormattedLabels()})")
                    .Where(query1)
                    .AndWhere(query2)
                    .Return(e => e.As<T>());
            }
            else
            {
                query = preQuery.Query.Match($"({name2}:{entity2.FormattedLabels()}){relationship.ToString()}({preQuery.Alias})")
                    .Where(query1)
                    .AndWhere(query2)
                    .Return(preResult => preResult.As<T>());
            }

            return query;
        }


        public virtual async Task DeleteRelationship<TRelationship>(Expression<Func<T, bool>> query1, TRelationship relationship)
        where TRelationship : GraphRelationship, new()
        {
            string name1 = query1.Parameters[0].Name;
            T entity1 = (T)Activator.CreateInstance(query1.Parameters[0].Type);

            var query = _graphClient.Cypher
                .Match($"(zzz){relationship.ToString()}({name1}:{_entityLabel})")
                .OptionalMatch($"({name1})-[r2:PART_OF]->()")
                .OptionalMatch($"(zzz)-[r3:PART_OF]->()")
                .Where(query1)
                .Delete(relationship.Alias)
                .Delete("r2")
                .Delete("r3");

            await query.ExecuteWithoutResultsAsync();
        }

        public virtual async Task DeleteRelationship<T2, TRelationship>(Expression<Func<T, bool>> query1, Expression<Func<T2, bool>> query2, TRelationship relationship)
            where T2 : GraphNode, new()
            where TRelationship : GraphRelationship, new()
        {
            string name1 = query1.Parameters[0].Name;
            T entity1 = (T)Activator.CreateInstance(query1.Parameters[0].Type);
            string name2 = query2.Parameters[0].Name;
            T2 entity2 = (T2)Activator.CreateInstance(query2.Parameters[0].Type);

            await _graphClient.Cypher
                .Match("(" + name1 + ":" + entity1.FormattedLabels() + ")" + relationship.ToString() + "(" + name2 + ":" + entity2.FormattedLabels() + ")")
                .Where(query1)
                .AndWhere(query2)
                .Delete("r")
                .ExecuteWithoutResultsAsync();
        }


        public ICypherFluentQuery devToolDatabase()
        {
            return _graphClient.Cypher
                .WithDatabase("devtoolbox");
        }


        public virtual async Task<T> GetChildrenById(string id)
        {
            var prequery = FromChildCategory();
            var results = await prequery.Query.Match($"({prequery.Alias})").Where((T preResult) => preResult.Id == id).Return(preResult => preResult.As<T>()).ResultsAsync;
            return results.FirstOrDefault();
        }


        public ChainedQuery FromChildCategory()
        {
            return fromCategory(_entityLabel);
        }

        public ChainedQuery fromCategory(string category)
        {
            var alias = "preResult";
            var preQuery = _graphClient.Cypher.Call($@"n10s.inference.nodesLabelled('{category}',  {{catNameProp: ""Label"",catLabel: ""Topic"",subCatRel: ""SUBCLASS_OF""}})").Yield($"node AS {alias}");
            return new ChainedQuery {Alias = alias, Query = preQuery };
        }



        public virtual async Task DeleteRelationshipById(T baseEntity, GraphRelationship relationship)
        {
            Expression<Func<T, bool>> getById = (T y) => y.Id == baseEntity.Id;
            getById = PredicateRewriter.Rewrite(getById, baseEntity.Alias);

            var query = _graphClient.Cypher
                .Match($"(zzz){relationship.ToString()}({baseEntity.Alias})")
                .Where(getById)
                .Delete(relationship.Alias);

            var query2 = _graphClient.Cypher
                .Match($"(zzz){relationship.ToString()}({baseEntity.Alias})")
                .OptionalMatch($"({baseEntity.Alias})-[r2:PART_OF]->()")
                .OptionalMatch($"(zzz)-[r3:PART_OF]->()")
                .Where(getById)
                .Delete("r2")
                .Delete("r3");

            await query.ExecuteWithoutResultsAsync();
            await query2.ExecuteWithoutResultsAsync();
        }





    }
}
