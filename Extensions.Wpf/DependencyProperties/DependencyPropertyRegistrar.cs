using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using Extensions.Core.Helpers;
using Expression = System.Linq.Expressions.Expression;

namespace WpfExtensions.DependencyProperties
{
  public delegate void PropertyChangedCallback<TOwner>(TOwner sender, DependencyPropertyChangedEventArgs e) where TOwner : DependencyObject;

  public delegate bool ValidateValueCallback<TValue>(TValue value);

  public delegate TValue CoerceValueCallback<TOwner, TValue>(TOwner sender, TValue value);

  public class DependencyPropertyRegistrar<TOwner> where TOwner : DependencyObject
  {
    public sealed class Property<TValue>
    {
      public delegate void ChangedCallback(TValue oldValue, TValue newValue);

      private readonly string _propertyName;
      private TValue _defaultValue;
      private PropertyChangedCallback<TOwner> _changeCallback;
      private FrameworkPropertyMetadataOptions _propertyMetadataOptions;
      private ValidateValueCallback<TValue> _validateCallback;
      private CoerceValueCallback _coerceCallback;
      private UpdateSourceTrigger? _updateSourceTrigger;

      internal Property(string propertyName)
      {
        this._propertyName = propertyName;
      }

      public Property<TValue> Default(TValue value)
      {
        _defaultValue = value;

        return this;
      }

      public Property<TValue> AffectsRender()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.AffectsRender;

        return this;
      }

      public Property<TValue> AffectsMeasure()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.AffectsMeasure;

        return this;
      }


      public Property<TValue> AffectsArrange()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.AffectsArrange;

        return this;
      }

      public Property<TValue> BindsTwoWayByDefault()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.BindsTwoWayByDefault;

        return this;
      }

      public Property<TValue> NotDataBindable()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.NotDataBindable;

        return this;
      }

      public Property<TValue> Inheritable()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.Inherits;

        return this;
      }


      public Property<TValue> Journal()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.Journal;

        return this;
      }

      public Property<TValue> UpdateSource(UpdateSourceTrigger trigger)
      {
        _updateSourceTrigger = trigger;

        return this;
      }

      public Property<TValue> OnChange(PropertyChangedCallback<TOwner> callback)
      {
        _changeCallback = callback;

        return this;
      }

      public Property<TValue> OnChange(Expression<Func<TOwner, ChangedCallback>> expression)
      {
        var unaryExpression = (UnaryExpression)expression.Body;
        var createDelegateExpression = (MethodCallExpression)unaryExpression.Operand;
        // ReSharper disable once PossibleNullReferenceException
        var method = (MethodInfo)((ConstantExpression)createDelegateExpression.Object).Value;

        var instance = Expression.Parameter(typeof(TOwner), "instance");
        var oldValue = Expression.Parameter(typeof(TValue), "oldValue");
        var newValue = Expression.Parameter(typeof(TValue), "newValue");

        var methodCall = Expression.Call(instance, method, oldValue, newValue);
        var callback = Expression.Lambda<Action<TOwner, TValue, TValue>>(methodCall, instance, oldValue, newValue)
          .Compile();

        _changeCallback = (sender, e) => callback(sender, (TValue)e.OldValue, (TValue)e.NewValue);

        return this;
      }

      public Property<TValue> Coerce(CoerceValueCallback<TOwner, TValue> callback)
      {
        if (typeof(TValue).IsValueType)
        {
          // Create a callback that tries to avoid reboxing the original value if the coercion didn't change it
          _coerceCallback = (sender, baseValue) =>
          {
            var unboxedBaseValue = (TValue)baseValue;
            var coercedValue = callback((TOwner)sender, unboxedBaseValue);

            if (EqualityComparer<TValue>.Default.Equals(coercedValue, unboxedBaseValue))
            {
              // Avoid creating new boxed instance
              return baseValue;
            }

            return Boxing.Box(coercedValue);
          };
        }
        else
        {
          _coerceCallback = (sender, args) => callback((TOwner)sender, (TValue)args);
        }

        return this;
      }

      public Property<TValue> CoerceObject(CoerceValueCallback<TOwner, Object> callback)
      {
        _coerceCallback = (sender, args) => callback((TOwner)sender, args);

        return this;
      }

      public Property<TValue> Validate(ValidateValueCallback<TValue> callback)
      {
        _validateCallback = callback;

        return this;
      }

      public static implicit operator DependencyProperty(Property<TValue> instance)
      {
        var propertyName = instance._propertyName;
        var validateCallback = instance._validateCallback;

        return DependencyProperty.Register(
          propertyName,
          typeof(TValue),
          typeof(TOwner),
          instance.CreateMetadata(),
          validateCallback != null ? new ValidateValueCallback(x => validateCallback((TValue)x)) : null);
      }

      public static implicit operator DependencyPropertyKey(Property<TValue> instance)
      {
        var propertyName = instance._propertyName;
        var validateCallback = instance._validateCallback;

        return DependencyProperty.RegisterReadOnly(
          propertyName,
          typeof(TValue),
          typeof(TOwner),
          instance.CreateMetadata(),
          validateCallback != null ? new ValidateValueCallback(x => validateCallback((TValue)x)) : null);
      }

      private FrameworkPropertyMetadata CreateMetadata()
      {
        // Capture delegates
        var changeCallback = this._changeCallback;
        var coerceCallback = this._coerceCallback;
        var metadata = new FrameworkPropertyMetadata(Boxing.Box(_defaultValue), _propertyMetadataOptions);

        if (changeCallback != null)
        {
          metadata.PropertyChangedCallback = (sender, args) => changeCallback((TOwner)sender, args);
        }

        if (coerceCallback != null)
        {
          metadata.CoerceValueCallback = coerceCallback;
        }

        if (_updateSourceTrigger != null)
        {
          metadata.DefaultUpdateSourceTrigger = _updateSourceTrigger.Value;
        }

        return metadata;
      }
    }


    public sealed class AttachedProperty<TValue>
    {
      private readonly string _propertyName;
      private TValue _defaultValue;
      private PropertyChangedCallback _changeCallback;
      private FrameworkPropertyMetadataOptions _propertyMetadataOptions;
      private ValidateValueCallback _validateCallback;
      private CoerceValueCallback _coerceCallback;
      private UpdateSourceTrigger? _updateSourceTrigger;

      internal AttachedProperty(string propertyName)
      {
        _propertyName = propertyName;
      }

      public AttachedProperty<TValue> Default(TValue value)
      {
        _defaultValue = value;

        return this;
      }

      public AttachedProperty<TValue> AffectsRender()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.AffectsRender;

        return this;
      }

      public AttachedProperty<TValue> AffectsMeasure()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.AffectsMeasure;

        return this;
      }

      public AttachedProperty<TValue> AffectsArrange()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.AffectsArrange;

        return this;
      }

      public AttachedProperty<TValue> BindsTwoWayByDefault()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.BindsTwoWayByDefault;

        return this;
      }

      public AttachedProperty<TValue> NotDataBindable()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.NotDataBindable;

        return this;
      }

      public AttachedProperty<TValue> Inheritable()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.Inherits;

        return this;
      }

      public AttachedProperty<TValue> Journal()
      {
        _propertyMetadataOptions |= FrameworkPropertyMetadataOptions.Journal;

        return this;
      }

      public AttachedProperty<TValue> UpdateSource(UpdateSourceTrigger trigger)
      {
        _updateSourceTrigger = trigger;

        return this;
      }

      public AttachedProperty<TValue> OnChange(PropertyChangedCallback callback)
      {
        _changeCallback = callback;

        return this;
      }

      public AttachedProperty<TValue> Coerce(CoerceValueCallback callback)
      {
        _coerceCallback = callback;

        return this;
      }

      public AttachedProperty<TValue> Validate(ValidateValueCallback callback)
      {
        _validateCallback = callback;

        return this;
      }

      public static implicit operator DependencyProperty(AttachedProperty<TValue> instance)
      {
        var propertyName = instance._propertyName;
        var validateCallback = instance._validateCallback;

        return DependencyProperty.RegisterAttached(propertyName,
          typeof(TValue),
          typeof(TOwner),
          instance.CreateMetadata(),
          validateCallback);
      }

      public static implicit operator DependencyPropertyKey(AttachedProperty<TValue> instance)
      {
        var propertyName = instance._propertyName;
        var validateCallback = instance._validateCallback;

        return DependencyProperty.RegisterAttachedReadOnly(propertyName,
          typeof(TValue),
          typeof(TOwner),
          instance.CreateMetadata(),
          validateCallback);
      }

      private FrameworkPropertyMetadata CreateMetadata()
      {
        var metadata = new FrameworkPropertyMetadata(Boxing.Box(_defaultValue), _propertyMetadataOptions)
        {
          PropertyChangedCallback = _changeCallback,
          CoerceValueCallback = _coerceCallback
        };

        if (_updateSourceTrigger != null)
        {
          metadata.DefaultUpdateSourceTrigger = _updateSourceTrigger.Value;
        }

        return metadata;
      }
    }

    public static Type CurrentType => typeof(TOwner);

    public static Property<TProperty> RegisterProperty<TProperty>(Expression<Func<TOwner, TProperty>> property)
    {
      if (property == null)
      {
        throw new ArgumentNullException(nameof(property));
      }

      var propertyInfo = (PropertyInfo)((MemberExpression)property.Body).Member;

      return new Property<TProperty>(propertyInfo.Name);
    }

    public Property<TProperty> Register<TProperty>(Expression<Func<TOwner, TProperty>> property)
    {
      return RegisterProperty(property);
    }

    public static Property<TProperty> RegisterProperty<TProperty>(string propertyName = null,
      [CallerMemberName] string staticReadOnlyName = null)
    {
      if (propertyName == null)
      {
        // ReSharper disable once PossibleNullReferenceException
        propertyName = staticReadOnlyName.Substring(0, staticReadOnlyName.Length - "Property".Length);
      }
      else
      {
        Debug.Assert(propertyName + "Property" == staticReadOnlyName);
      }

      return new Property<TProperty>(propertyName);
    }

    public static AttachedProperty<TProperty> RegisterAttachedProperty<TProperty>(string propertyName = null,
      [CallerMemberName] string staticReadOnlyName = null)
    {
      if (propertyName == null)
      {
        // ReSharper disable once PossibleNullReferenceException
        propertyName = staticReadOnlyName.Substring(0, staticReadOnlyName.Length - "Property".Length);
      }
      else
      {
        Debug.Assert(propertyName + "Property" == staticReadOnlyName);
      }

      return new AttachedProperty<TProperty>(propertyName);
    }

    public static RoutedEvent RegisterEvent<THandler>(string eventName, RoutingStrategy strategy)
    {
      return EventManager.RegisterRoutedEvent(eventName, strategy, typeof(THandler), CurrentType);
    }
  }
}
