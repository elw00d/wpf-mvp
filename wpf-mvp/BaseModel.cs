using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Wpf.Mvp
{
    public class BaseModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void OnPropertyChanged<T>(Expression<Func<T>> propertyExpression) {
			this.OnPropertyChanged(ExpressionHelpers.GetPropertyName(propertyExpression));
		}
    }

    public static class ExpressionHelpers
    {

        public static string GetPropertyName<T>(Expression<Func<T>> property)
        {
            PropertyInfo propertyInfo = ((MemberExpression)property.Body).Member as PropertyInfo;
            if (propertyInfo == null)
            {
                throw new ArgumentException("The lambda expression 'property' should point to a valid Property");
            }
            return propertyInfo.Name;
        }

        public static PropertyInfo GetPropertyInfo<T>(Expression<Func<T>> property)
        {
            PropertyInfo propertyInfo = ((MemberExpression)property.Body).Member as PropertyInfo;
            if (propertyInfo == null)
            {
                throw new ArgumentException("The lambda expression 'property' should point to a valid Property");
            }
            return propertyInfo;
        }

    }
}
