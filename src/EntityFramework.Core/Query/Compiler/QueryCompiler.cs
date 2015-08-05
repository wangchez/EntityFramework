// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Query.Expressions;
using Microsoft.Data.Entity.Query.ExpressionVisitors;
using Microsoft.Data.Entity.Query.Preprocessor.ExpressionFilters;
using Microsoft.Data.Entity.Query.ResultOperators;
using Microsoft.Data.Entity.Utilities;
using Remotion.Linq;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.StreamedData;
using Remotion.Linq.Parsing.ExpressionVisitors.Transformation;
using Remotion.Linq.Parsing.Structure;
using Remotion.Linq.Parsing.Structure.ExpressionTreeProcessors;
using Remotion.Linq.Parsing.Structure.NodeTypeProviders;

namespace Microsoft.Data.Entity.Query.Compiler
{
    public abstract class QueryCompiler : IQueryCompiler
    {
        private readonly ICompiledQueryCache _queryCache;

        private static readonly Lazy<ReadonlyNodeTypeProvider> _cachedNodeTypeProvider = new Lazy<ReadonlyNodeTypeProvider>(CreateNodeTypeProvider);

        private static MethodInfo CompileQueryMethod { get; }
            = typeof(QueryCompiler).GetTypeInfo().GetDeclaredMethod("CompileQueryNew");

        public QueryCompiler(
            [NotNull] ICompiledQueryCache queryCache)
        {
            Check.NotNull(queryCache, nameof(queryCache));

            _queryCache = queryCache;
        }

        public virtual CompiledQuery CompileQuery<TResult>(Expression query)
        {
            return _queryCache.GetOrAdd(GenerateCacheKey(query, false), () =>
                {
                    var parser = new QueryParser(
                    new ExpressionTreeParser(
                        _cachedNodeTypeProvider.Value,
                        new CompoundExpressionTreeProcessor(new IExpressionTreeProcessor[]
                        {
                            new PartialEvaluatingExpressionTreeProcessor(new NullEvaluatableExpressionFilter()),
                            new FunctionEvaluationEnablingProcessor(),
                            new TransformingExpressionTreeProcessor(ExpressionTransformerRegistry.CreateDefault())
                        })));

                    var queryModel = parser.GetParsedQuery(query);

                    var streamedSequenceInfo
                        = queryModel.GetOutputDataInfo() as StreamedSequenceInfo;

                    var resultItemType
                        = streamedSequenceInfo?.ResultItemType ?? typeof(TResult);

                    return new CompiledQuery
                    {
                        ResultItemType = resultItemType,
                        Executor = (Delegate)CompileQueryMethod
                            .MakeGenericMethod(resultItemType)
                            .Invoke(this, new object[] { queryModel })
                    };
                });
        }

        protected abstract string GenerateCacheKey(Expression query, bool isAsync);

        protected abstract Func<QueryContext, IEnumerable<TResult>> CompileQueryNew<TResult>(QueryModel queryModel);

        private class FunctionEvaluationEnablingProcessor : IExpressionTreeProcessor
        {
            public Expression Process(Expression expressionTree)
            {
                return new FunctionEvaluationEnablingVisitor().Visit(expressionTree);
            }
        }

        private class FunctionEvaluationEnablingVisitor : ExpressionVisitorBase
        {
            protected override Expression VisitExtension(Expression expression)
            {
                var methodCallWrapper = expression as MethodCallEvaluationPreventingExpression;
                if (methodCallWrapper != null)
                {
                    return Visit(methodCallWrapper.MethodCall);
                }

                var propertyWrapper = expression as PropertyEvaluationPreventingExpression;
                if (propertyWrapper != null)
                {
                    return Visit(propertyWrapper.MemberExpression);
                }

                return base.VisitExtension(expression);
            }

            protected override Expression VisitSubQuery(SubQueryExpression expression)
            {
                var clonedModel = expression.QueryModel.Clone();
                clonedModel.TransformExpressions(Visit);

                return new SubQueryExpression(clonedModel);
            }
        }

        private class ReadonlyNodeTypeProvider : INodeTypeProvider
        {
            private readonly INodeTypeProvider _nodeTypeProvider;

            public ReadonlyNodeTypeProvider(INodeTypeProvider nodeTypeProvider)
            {
                _nodeTypeProvider = nodeTypeProvider;
            }

            public bool IsRegistered(MethodInfo method) => _nodeTypeProvider.IsRegistered(method);

            public Type GetNodeType(MethodInfo method) => _nodeTypeProvider.GetNodeType(method);
        }

        private static ReadonlyNodeTypeProvider CreateNodeTypeProvider()
        {
            var methodInfoBasedNodeTypeRegistry = MethodInfoBasedNodeTypeRegistry.CreateFromRelinqAssembly();

            methodInfoBasedNodeTypeRegistry
                .Register(QueryAnnotationExpressionNode.SupportedMethods, typeof(QueryAnnotationExpressionNode));

            methodInfoBasedNodeTypeRegistry
                .Register(IncludeExpressionNode.SupportedMethods, typeof(IncludeExpressionNode));

            methodInfoBasedNodeTypeRegistry
                .Register(ThenIncludeExpressionNode.SupportedMethods, typeof(ThenIncludeExpressionNode));

            var innerProviders
                = new INodeTypeProvider[]
                {
                    methodInfoBasedNodeTypeRegistry,
                    MethodNameBasedNodeTypeRegistry.CreateFromRelinqAssembly()
                };

            return new ReadonlyNodeTypeProvider(new CompoundNodeTypeProvider(innerProviders));
        }
    }
}
