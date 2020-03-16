using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Xyzies.TWC.DisputeService.Tests.Extensions
{
    public static class TypeExtension
    {
        public static Func<T, object> GetExpression<T>(this Type type, string propertyName)
        {
            var propertyInfo = type.GetProperty(propertyName);

            // Create x => portion of lambda expression
            var parameter = Expression.Parameter(type, "x");

            Expression property = null;
            if (propertyInfo == null)
            {
                property = FindInnerObjectProperty(type, propertyName, parameter, null, parameter, parameter, out propertyInfo);
            }
            else
            {
                // create x.{PropertyName} portion of lambda expression
                property = Expression.Property(parameter, propertyInfo.GetMethod);
            }

            // convert property to object
            Expression conversion = Expression.Convert(property, typeof(object));

            // finally create entire expression - entity => entity.Id == 'id'
            var retVal =
                Expression.Lambda<Func<T, object>>(conversion, new[] { parameter });
            return retVal.Compile();
        }

        private static Expression FindInnerObjectProperty(Type type, string propertyName, Expression condition, Expression conditionPrev, Expression property, Expression prevProperty, out PropertyInfo propertyInfo)
        {
            propertyInfo = null;
            foreach (var prop in type.GetProperties())
            {
                if (prop.PropertyType != typeof(string) && prop.PropertyType.IsClass && (!typeof(IEnumerable).IsAssignableFrom(prop.PropertyType)))
                {
                    propertyInfo = prop.PropertyType.GetProperty(propertyName);
                    Expression propertyNext = Expression.Property(property, prop.GetMethod);

                    conditionPrev = condition;
                    condition = GetConditionProperty(condition, propertyNext, prop);

                    if (propertyInfo == null)
                    {
                        property = FindInnerObjectProperty(prop.PropertyType, propertyName, condition, conditionPrev, propertyNext, property, out propertyInfo);

                        if (propertyInfo == null)
                        {
                            condition = conditionPrev;
                            continue;
                        }
                        else break;
                    }
                    else
                    {
                        return GetConditionProperty(condition, Expression.Property(propertyNext, propertyInfo.GetMethod), propertyInfo);
                    }
                }
            }

            return propertyInfo != null ? property : prevProperty;
        }

        private static Expression GetConditionProperty(Expression condition, Expression propertyNext, PropertyInfo prop)
        {
            var constant = Expression.Constant(null, typeof(object));
            var defaultValue = Expression.Constant(null, prop.PropertyType);

            Expression equalExpression = Expression.NotEqual(condition, constant);

            return Expression.Condition(equalExpression, propertyNext, defaultValue);
        }
    }
}
