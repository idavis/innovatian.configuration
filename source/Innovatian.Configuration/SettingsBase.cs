#region License

// 
// Copyright (c) 2009-2011, Ian Davis <ian.f.davis@gmail.com>
// 
// Dual-licensed under the Apache License, Version 2.0, and the Microsoft Public License (Ms-PL).
// See the file LICENSE.txt for details.
// 

#endregion

#region Using Directives

using System;
using System.Linq.Expressions;
using System.Reflection;

#endregion

namespace Innovatian.Configuration
{
    public abstract class SettingsBase
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "SettingsBase" /> class.
        /// </summary>
        /// <remarks>
        ///   This ctor will initialize itself with the global settings system for the environment that is 
        ///   currently configured.
        /// </remarks>
        protected SettingsBase()
            : this(SettingsManager.ConfigurationSource, SettingsManager.Environment)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "SettingsBase" /> class.
        /// </summary>
        /// <param name = "environment">The environment in which to pull settings from.</param>
        /// <remarks>
        ///   This ctor will initialize itself with the global settings system.
        /// </remarks>
        protected SettingsBase(string environment)
            : this(SettingsManager.ConfigurationSource, environment)
        {
        }


        /// <summary>
        ///   Initializes a new instance of the <see cref = "SettingsBase" /> class.
        /// </summary>
        /// <param name = "configurationSource">The configuration source which contains the section in which this class will pull its settings.</param>
        /// <param name = "environment">The environment in which to pull settings from.</param>
        /// <exception cref = "ArgumentNullException">if <paramref name = "configurationSource" /> is null.</exception>
        /// <exception cref = "ArgumentOutOfRangeException">if <paramref name = "environment" /> is not one of the defined values.</exception>
        protected SettingsBase(IConfigurationSource configurationSource, string environment)
        {
            if (configurationSource == null)
            {
                throw new ArgumentNullException("configurationSource");
            }

            Environment = environment;
            ThisType = GetType();
            Initialize(configurationSource);
        }

        /// <summary>
        ///   Gets or sets the environment.
        /// </summary>
        /// <value>The environment.</value>
        /// <remarks>
        ///   Changing the environment at runtime will effect which default values are used.
        /// </remarks>
        public virtual string Environment { get; set; }


        /// <summary>
        ///   Gets the most derived type of the this instance.
        /// </summary>
        /// <value>The type of the this.</value>
        protected virtual Type ThisType { get; private set; }


        /// <summary>
        ///   Gets the configuration source used to initialize this instance.
        /// </summary>
        /// <value>The configuration source.</value>
        protected virtual IConfigurationSource ConfigurationSource { get; private set; }


        /// <summary>
        ///   Gets the configuration section used to request settings information.
        /// </summary>
        /// <value>The configuration section.</value>
        protected virtual IConfigurationSection ConfigurationSection { get; private set; }


        /// <summary>
        ///   Gets the name of the section used to pull from the <see cref = "IConfigurationSource" />. This
        ///   value will default to the name of the assembly without an extentension.
        /// </summary>
        /// <value>The name of the section.</value>
        /// <remarks>
        ///   If you need to pull all of the settings from another section, you can override this
        ///   property to return the section name of your choice.
        /// </remarks>
        protected virtual string SectionName
        {
            get { return ThisType.Assembly.GetName().Name; }
        }

        /// <summary>
        ///   Initializes this instance with the specified configuration source.
        /// </summary>
        /// <param name = "configurationSource">The configuration source.</param>
        /// <remarks>
        ///   If the <paramref name = "configurationSource" /> does not contain a value for the <see cref = "SectionName" /> supplied,
        ///   a new, empty, section will be created and defaults will be used.
        /// 
        ///   Overriding this method with a Mock call or specifying your own configuration source will allow you to change the 
        ///   values returned during testing.
        /// </remarks>
        protected virtual void Initialize(IConfigurationSource configurationSource)
        {
            ConfigurationSource = configurationSource;
            IConfigurationSection configurationSection;
            if (!ConfigurationSource.Sections.TryGetValue(SectionName, out configurationSection))
            {
                configurationSection = new ConfigurationSection(SectionName);
                ConfigurationSource.Add(configurationSection);
            }
            ConfigurationSection = configurationSection;
        }

        /// <summary>
        ///   Gets the value of the specified property name.
        /// </summary>
        /// <typeparam name = "T">The type of the value being retrieved.</typeparam>
        /// <param name = "propertyName">Name of the property.</param>
        /// <param name = "defaultValue">The default value.</param>
        /// <returns>The value of the property if it was found; else the default value.</returns>
        protected virtual T Get<T>(string propertyName, T defaultValue)
        {
            return ConfigurationSection.Get(propertyName, defaultValue);
        }

        /// <summary>
        ///   Gets the value of the specified property name specified in the property lambda.
        /// </summary>
        /// <typeparam name = "T">The type of the value being retrieved.</typeparam>
        /// <param name = "propertyLambda">The property lambda.</param>
        /// <param name = "defaultValue">The default value.</param>
        /// <returns>The value of the property if it was found; else the default value.</returns>
        protected virtual T Get<T>(Expression<Func<T>> propertyLambda, T defaultValue)
        {
            string propertyName = GetPropertyName(propertyLambda);
            return Get(propertyName, defaultValue);
        }

        /// <summary>
        ///   Gets the name of the property from the property lambda expression.
        /// </summary>
        /// <typeparam name = "TProperty">The type of the property.</typeparam>
        /// <param name = "propertyLambda">The property lambda.</param>
        /// <exception cref = "ArgumentException">if the expression refers to a method, not a property</exception>
        /// <exception cref = "ArgumentException">if the expression refers to a field, not a property</exception>
        /// <exception cref = "ArgumentException">if the expression refers to a property that is not defined on the class supplying the expression.</exception>
        /// <returns>Returns the name of the property from the property lambda expression.</returns>
        protected virtual string GetPropertyName<TProperty>(Expression<Func<TProperty>> propertyLambda)
        {
            var body = propertyLambda.Body as MemberExpression;
            if (body == null)
            {
                const string format = "Expression '{0}' refers to a method, not a property.";
                string message = string.Format(format, propertyLambda);
                throw new ArgumentException(message);
            }

            var propertyInfo = body.Member as PropertyInfo;
            if (propertyInfo == null)
            {
                const string format = "Expression '{0}' refers to a field, not a property.";
                string message = string.Format(format, propertyLambda);
                throw new ArgumentException(message);
            }

            if (ThisType != propertyInfo.ReflectedType &&
                 !ThisType.IsSubclassOf(propertyInfo.ReflectedType))
            {
                const string format = "Expresion '{0}' refers to a property that is not from type {1}.";
                string message = string.Format(format, propertyLambda, ThisType);
                throw new ArgumentException(message);
            }

            return propertyInfo.Name;
        }
    }
}
