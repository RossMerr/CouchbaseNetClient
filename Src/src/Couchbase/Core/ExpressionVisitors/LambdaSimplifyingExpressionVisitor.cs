﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Couchbase.Core.ExpressionVisitors
{
    /// <summary>
    /// Simplifies an expression tree by evaluating any branches of the tree that do not include
    /// lambda parameter references.  This will remove references to variables external to the lambda
    /// by converting them to constants, perform arithmetic, and execute method calls as needed.
    /// For example, a call to "str.ToUpper()" where string is an external variable would be simplified
    /// to a <see cref="ConstantExpression"/> containing the uppercase version of str.
    /// </summary>
    internal class LambdaSimplifyingExpressionVisitor : ExpressionVisitor
    {
        // Stores the current state of the tree as we're recursing through it
        private bool _isEvaluatable = true;

        /// <summary>
        /// Simplifies an expression tree by evaluating any branches of the tree that do not include
        /// lambda parameter references.  This will remove references to variables external to the lambda
        /// by converting them to constants, perform arithmetic, and execute method calls as needed.
        /// For example, a call to "str.ToUpper()" where string is an external variable would be simplified
        /// to a <see cref="ConstantExpression"/> containing the uppercase version of str.
        /// </summary>
        /// <param name="expression">Expression to simplify.</param>
        /// <returns>The simplified expression.</returns>
        public static Expression Simplify(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            var visitor = new LambdaSimplifyingExpressionVisitor();

            expression = visitor.Visit(expression);

            if (visitor._isEvaluatable)
            {
                // The entire tree can simplified

                return ConvertToConstant(expression);
            }
            else
            {
                return expression;
            }
        }

        /// <summary>
        /// Private constructor, only accessible via static method <see cref="Simplify"/>.
        /// </summary>
        private LambdaSimplifyingExpressionVisitor()
        {
        }

        public override Expression Visit(Expression node)
        {
            while (node.CanReduce)
            {
                node = node.ReduceAndCheck();
            }

            return base.Visit(node);
        }

        /// <summary>
        /// Visits a list of children to see if they are evaluatable or not.  If a branch of the tree
        /// can be evaluated but another cannot, simplifies the branches that can be evaluated to
        /// constants.  Modifies the provided collection with the new expressions.
        /// </summary>
        /// <param name="children">List of children to evaluate.  Null children are skipped.  This list is updated with the new children.</param>
        private void VisitChildren(IList<Expression> children)
        {
            var allChildrenAreEvaluatable = true;
            var evaluatableChildren = new bool[children.Count];

            for (var i=0; i<children.Count; i++)
            {
                if (children[i] == null)
                {
                    // Static method calls may have null children (the Object expression), just skip them

                    evaluatableChildren[i] = true;
                }
                else
                {
                    // Evaluate the child to see if the entire child tree is evaluatable

                    _isEvaluatable = true;

                    children[i] = Visit(children[i]);
                    evaluatableChildren[i] = _isEvaluatable;

                    if (!_isEvaluatable)
                    {
                        allChildrenAreEvaluatable = false;
                    }
                }
            }

            // When moving back up the tree, we are only evaluatable if all children are evaluatable
            _isEvaluatable = allChildrenAreEvaluatable;

            if (!allChildrenAreEvaluatable)
            {
                // Some of the children are evaluatable and others are not, so go ahead and evaluate
                // the children that can be evaluated and convert them to constants

                for (var i = 0; i < children.Count; i++)
                {
                    if (evaluatableChildren[i] && (children[i] != null))
                    {
                        children[i] = ConvertToConstant(children[i]);
                    }
                }
            }
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            // Once we encounter a parameter node, we know this branch of the tree cannot be evaluated to a constant
            _isEvaluatable = false;

            return base.VisitParameter(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var children = new[] {node.Left, node.Right};

            VisitChildren(children);

            return node.Update(children[0], node.Conversion, children[1]);
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            var children = new[] {node.Test, node.IfTrue, node.IfFalse};

            VisitChildren(children);

            return node.Update(children[0], children[1], children[2]);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var children = new[] {node.Object}.Concat(node.Arguments).ToList();

            VisitChildren(children);

            return node.Update(children[0], children.Skip(1));
        }

        private static ConstantExpression ConvertToConstant(Expression node)
        {
            Expression<Func<object>> lambda =
                Expression.Lambda<Func<object>>(Expression.Convert(node, typeof(object)));
            var compiledLambda = lambda.Compile();

            object value = compiledLambda();
            return Expression.Constant(value, node.Type);
        }
    }
}
