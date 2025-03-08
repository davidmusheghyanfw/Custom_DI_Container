using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DI
{
    public class DIContainer
    {
        private readonly DIContainer _parentContainer;

        private readonly Dictionary<(string, Type), DIRegistration> _transients = new();
        private readonly Dictionary<(string, Type), DIRegistration> _singletons = new();

        private HashSet<(string, Type)> _resolutions = new();

        public DIContainer(DIContainer parentContainer = null)
        {
            _parentContainer = parentContainer;
        }

        /// <summary>
        /// Registers a singleton instance.
        /// </summary>
        public void RegisterSingleton<T>(Func<DIContainer, T> factory)
        {
            RegisterSingleton(null, factory);
        }

        /// <summary>
        /// Registers a singleton instance with tag.
        /// </summary>
        public void RegisterSingleton<T>(string tag, Func<DIContainer, T> factory)
        {
            var key = (tag, typeof(T));
            Register(key, factory, true);
        }

        /// <summary>
        /// Registers an Instance.
        /// </summary>
        public void RegisterInstance<TInterface, TImplementation>() where TImplementation : TInterface, new()
        {
            RegisterInstance<TInterface>(new TImplementation());
        }


        /// <summary>
        /// Registers a singleton instance.
        /// </summary>
        public void RegisterInstance<T>(T instance)
        {
            RegisterInstance(null, instance);
        }

        /// <summary>
        /// Registers a singleton instance with tag.
        /// </summary>
        public void RegisterInstance<T>(string tag, T instance)
        {
            var key = (tag, typeof(T));
            if (_singletons.ContainsKey(key))
            {
                throw new Exception($"Duplicate key {key}");
            }

            _singletons[key] = new DIRegistration
            {
                Instance = instance,
                IsSingleton = true
            };
        }

        /// <summary>
        /// Registers a type as a singleton.
        /// </summary>
        public void RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface, new()
        {
            RegisterSingleton<TInterface>(_ => new TImplementation());
        }


        /// <summary>
        /// Registers a type with the container.
        /// </summary>
        private void Register<T>((string, Type) key, Func<DIContainer, T> factory, bool isSingleton, string tag = "")
        {
            if (isSingleton)
            {
                if (_singletons.ContainsKey(key))
                    throw new Exception($"Duplicate singleton registration: {typeof(T).Name} with tag '{tag}'.");

                _singletons[key] = new DIRegistration
                {
                    Factory = c => factory(c),
                    IsSingleton = true
                };
            }
            else
            {
                if (_transients.ContainsKey(key))
                    throw new Exception($"Duplicate transient registration: {typeof(T).Name} with tag '{tag}'.");

                _transients[key] = new DIRegistration
                {
                    Factory = c => factory(c),
                    IsSingleton = false
                };
            }
        }

        /// <summary>
        /// Registers a type as transient.
        /// </summary>
        public void RegisterTransient<TInterface, TImplementation>() where TImplementation : TInterface, new()
        {
            RegisterTransient<TInterface, TImplementation>(null);
        }

        /// <summary>
        /// Registers a type as transient with tag.
        /// </summary>
        public void RegisterTransient<TInterface, TImplementation>(string tag) where TImplementation : TInterface, new()
        {
            var key = (tag, typeof(TImplementation));
            Register<TInterface>(key, _ => new TImplementation(), false);
        }

        /// <summary>
        /// Resolves an instance of the requested type.
        /// </summary>
        public T Resolve<T>(string tag = "")
        {
            return (T)Resolve((tag, typeof(T)));
        }

        /// <summary>
        /// Resolves an instance based on a type key.
        /// </summary>
        private object Resolve((string, Type) key)
        {
            // Try to resolve from singletons
            if (_singletons.TryGetValue(key, out var singleton))
            {
                if (singleton.Instance == null)
                {
                    singleton.Instance = singleton.Factory(this);
                    InjectFields(singleton.Instance);
                    InjectProperties(singleton.Instance);
                    InjectMethods(singleton.Instance);
                }
                return singleton.Instance;
            }

            // Try to resolve from transients
            if (_transients.TryGetValue(key, out var transient))
            {
                var instance = transient.Factory(this);
                InjectFields(instance);
                InjectProperties(instance);
                InjectMethods(instance);
                return instance;
            }

            // Check parent container
            if (_parentContainer != null)
            {
                return _parentContainer.Resolve(key);
            }

            // Dynamically create an instance
            var createdInstance = CreateInstance(key.Item2);
            InjectFields(createdInstance);
            InjectProperties(createdInstance);
            InjectMethods(createdInstance);
            return createdInstance;
        }



        /// <summary>
        /// Creates an instance and resolves dependencies automatically.
        /// </summary>
        private object CreateInstance(Type type)
        {
            var constructor = type.GetConstructors().FirstOrDefault() ??
                              throw new Exception($"No constructor found for {type.Name}");

            var parameters = constructor.GetParameters()
                .Select(param => Resolve((string.Empty, param.ParameterType)))
                .ToArray();

            return Activator.CreateInstance(type, parameters);
        }
        
        /// <summary>
        /// Injects dependencies into properties marked with [Inject].
        /// </summary>
        /// <param name="instance"></param>
        private void InjectProperties(object instance)
        {
            if (instance == null) return;

            var properties = instance.GetType().GetProperties()
                .Where(p => p.IsDefined(typeof(InjectAttribute), true) && p.CanWrite);

            foreach (var property in properties)
            {
                var dependency = Resolve((string.Empty, property.PropertyType));
                property.SetValue(instance, dependency);
            }
        }
        
        /// <summary>
        /// Injects dependencies into methods marked with [Inject].
        /// </summary>
        /// <param name="instance"></param>
        private void InjectMethods(object instance)
        {
            if (instance == null) return;

            var methods = instance.GetType().GetMethods()
                .Where(m => m.IsDefined(typeof(InjectAttribute), true));

            foreach (var method in methods)
            {
                var parameters = method.GetParameters()
                    .Select(param => Resolve((string.Empty, param.ParameterType)))
                    .ToArray();

                method.Invoke(instance, parameters);
            }
        }

        /// <summary>
        /// Injects dependencies into fields marked with [Inject].
        /// </summary>
        /// <param name="instance">The object whose fields should be injected.</param>
        private void InjectFields(object instance)
        {
            if (instance == null) return;

            var fields = instance.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                .Where(f => f.IsDefined(typeof(InjectAttribute), true));

            foreach (var field in fields)
            {
                var dependency = Resolve((string.Empty, field.FieldType));
                field.SetValue(instance, dependency);
            }
        }


    }
}