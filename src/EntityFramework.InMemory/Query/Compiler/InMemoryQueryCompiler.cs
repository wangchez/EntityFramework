// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.Data.Entity.ChangeTracking.Internal;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.InMemory;
using Microsoft.Data.Entity.InMemory.Query;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Internal;
using Microsoft.Data.Entity.Query.ExpressionVisitors;
using Microsoft.Data.Entity.Storage;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Framework.Logging;
using Remotion.Linq;

namespace Microsoft.Data.Entity.Query.Compiler
{
    public class InMemoryQueryCompiler : QueryCompiler
    {
        private readonly IQueryContextFactory _queryContextFactory;
        private readonly IModel _model;
        private readonly ILogger _logger;
        private readonly IEntityMaterializerSource _entityMaterializerSource;
        private readonly IEntityKeyFactorySource _entityKeyFactorySource;
        private readonly IClrAccessorSource<IClrPropertyGetter> _clrPropertyGetterSource;

        public InMemoryQueryCompiler(
            [NotNull] ICompiledQueryCache queryCache,
            [NotNull] IQueryContextFactory queryContextFactory,
            [NotNull] IModel model,
            [NotNull] IEntityKeyFactorySource entityKeyFactorySource,
            [NotNull] IEntityMaterializerSource entityMaterializerSource,
            [NotNull] IClrAccessorSource<IClrPropertyGetter> clrPropertyGetterSource,
            [NotNull] IInMemoryStore persistentStore,
            [NotNull] IDbContextOptions options,
            [NotNull] ILoggerFactory loggerFactory)
            : base(queryCache)
        {
            _queryContextFactory = queryContextFactory;
            _model = model;
            _logger = loggerFactory.CreateLogger<Database>();
            _entityMaterializerSource = entityMaterializerSource;
            _entityKeyFactorySource = entityKeyFactorySource;
            _clrPropertyGetterSource = clrPropertyGetterSource;
        }

        protected override CompiledQuery<IEnumerable<TResult>> CompileQueryModel<TResult>(QueryModel queryModel)
        {
            var executor = new InMemoryQueryCompilationContext(
                    _model,
                    _logger,
                    _entityMaterializerSource,
                    _entityKeyFactorySource,
                    _clrPropertyGetterSource)
                    .CreateQueryModelVisitor()
                    .CreateQueryExecutor<TResult>(Check.NotNull(queryModel, nameof(queryModel)));

            return new CompiledQuery<IEnumerable<TResult>>
            {
                Executor = d =>
                {
                    var qc = _queryContextFactory.Create();
                    foreach(var kvp in d)
                    {
                        qc.ParameterValues.Add(kvp);
                    }
                    return executor(qc);
                }
            };
        }

        protected override string GenerateCacheKey(Expression query, bool isAsync)
        {
            return _model.GetHashCode().ToString()
                  + isAsync
                  + new ExpressionStringBuilder()
                      .Build(query);
        }
    }
}
