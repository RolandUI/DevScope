using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace ClassicDiagnostics.Avalonia.Extensions;

internal static class ReactiveExtensions
{
    public static IObservable<TValue> GetObservable<TOwner, TValue>(
        this TOwner vm,
        Expression<Func<TOwner, TValue>> property,
        bool fireImmediately = true)
        where TOwner : INotifyPropertyChanged
    {
        return Observable.Create<TValue>(o =>
        {
            var propertyInfo = property.GetPropertyInfo();

            void Fire()
            {
                o.OnNext((TValue)propertyInfo.GetValue(vm)!);
            }

            void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == propertyInfo.Name)
                {
                    Fire();
                }
            }

            if (fireImmediately)
            {
                Fire();
            }

            vm.PropertyChanged += OnPropertyChanged;

            return Disposable.Create(() => vm.PropertyChanged -= OnPropertyChanged);
        });
    }

    private static PropertyInfo GetPropertyInfo<TOwner, TValue>(this Expression<Func<TOwner, TValue>> property)
    {
        if (property.Body is UnaryExpression unaryExpression)
        {
            return (PropertyInfo)((MemberExpression)unaryExpression.Operand).Member;
        }

        var memExpr = (MemberExpression)property.Body;

        return (PropertyInfo)memExpr.Member;
    }
}