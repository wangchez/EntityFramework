// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Internal;
using Microsoft.Data.Entity.Query.ExpressionVisitors;
using Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation;

namespace Microsoft.Data.Entity.Query.Preprocessor.ExpressionVisitors
{
    public class ParameterExtractingExpressionVisitor : ExpressionVisitorBase
    {
        private readonly PartialEvaluationInfo _partialEvaluationInfo;
        private readonly IDictionary<string, object> _parameters;

        public ParameterExtractingExpressionVisitor(
            PartialEvaluationInfo partialEvaluationInfo, IDictionary<string, object> parameters)
        {
            _partialEvaluationInfo = partialEvaluationInfo;
            _parameters = parameters;
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
        {
            if (methodCallExpression.Method.IsGenericMethod)
            {
                var methodInfo = methodCallExpression.Method.GetGenericMethodDefinition();

                if (ReferenceEquals(methodInfo, EntityQueryModelVisitor.PropertyMethodInfo))
                {
                    return methodCallExpression;
                }
            }

            return base.VisitMethodCall(methodCallExpression);
        }

        public override Expression Visit(Expression expression)
        {
            if (expression == null)
            {
                return null;
            }

            if (expression.NodeType == ExpressionType.Lambda
                || !_partialEvaluationInfo.IsEvaluatableExpression(expression))
            {
                return base.Visit(expression);
            }

            var e = expression;

            if (expression.NodeType == ExpressionType.Convert)
            {
                if(expression.RemoveConvert() is ConstantExpression )
                {
                    return expression;
                }

                var unaryExpression = (UnaryExpression)expression;

                if ((unaryExpression.Type.IsNullableType()
                     && !unaryExpression.Operand.Type.IsNullableType())
                    || unaryExpression.Type == typeof(object))
                {
                    e = unaryExpression.Operand;
                }
            }

            if (e.NodeType != ExpressionType.Constant
                && !typeof(IQueryable).GetTypeInfo().IsAssignableFrom(e.Type.GetTypeInfo()))
            {
                try
                {
                    string parameterName;
                    var parameterValue = ExpressionEvaluationHelpers.Evaluate(e, out parameterName);

                    var compilerPrefixIndex = parameterName.LastIndexOf(">", StringComparison.Ordinal);
                    if (compilerPrefixIndex != -1)
                    {
                        parameterName = parameterName.Substring(compilerPrefixIndex + 1);
                    }

                    parameterName
                        = $"{CompiledQueryCache.CompiledQueryParameterPrefix}{parameterName}_{_parameters.Count}";

                    _parameters.Add(parameterName, parameterValue);

                    return e.Type == expression.Type
                        ? Expression.Parameter(e.Type, parameterName)
                        : (Expression)Expression.Convert(
                            Expression.Parameter(e.Type, parameterName),
                            expression.Type);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        Strings.ExpressionParameterizationException(expression),
                        exception);
                }
            }

            return expression;
        }
    }
}
