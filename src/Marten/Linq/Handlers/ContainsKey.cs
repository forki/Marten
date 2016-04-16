using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Marten.Schema;

namespace Marten.Linq.Handlers
{
    public class ContainsKey : IMethodCallParser
    {
        public bool Matches(MethodCallExpression expression)
        {
            return expression.Method.Name == MartenExpressionParser.CONTAINSKEY &&
                   IsGenericDictionary(expression.Object.Type) &&
                   expression.Arguments.Single().Type == typeof(string);
        }

        private bool IsGenericDictionary(Type type)
        {
            if (type == null) return false;
            
            var genericArguments = type.GetGenericArguments();
            if (genericArguments.Length == 2)
            {
                return typeof(IDictionary<,>).MakeGenericType(genericArguments).IsAssignableFrom(type);
            }
            return false;
        }

        public IWhereFragment Parse(IDocumentMapping mapping, ISerializer serializer, MethodCallExpression expression)
        {
            var finder = new FindMembers();
            finder.Visit(expression);

            var propertyName = expression.Arguments.OfType<ConstantExpression>().FirstOrDefault();
            if (propertyName == null) throw new BadLinqExpressionException($"Could not extract string value from {expression}.", null);

            return new WhereFragment($"d.data ? '{propertyName.Value}'");
        }
    }
}