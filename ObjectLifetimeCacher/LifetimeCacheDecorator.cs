using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace ObjectLifetimeCacher
{
    public class LifetimeCacheDecorator<T> : DispatchProxy
    {
        private T _decorated;
        private Dictionary<string, object> _methodCallResults = new Dictionary<string, object>();

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            try
            {
                if (TryGetScoped(targetMethod, args, out var obj))
                {
                    return obj;
                }

                var result = targetMethod.Invoke(_decorated, args);
                _methodCallResults[CreateKey(targetMethod, args)] = result;

                return result;
            }
            catch (Exception ex) when (ex is TargetInvocationException)
            {
                throw ex.InnerException ?? ex;
            }
        }

        public static T Create(T decorated)
        {
            object proxy = Create<T, LifetimeCacheDecorator<T>>();
            ((LifetimeCacheDecorator<T>)proxy).SetParameters(decorated);

            return (T)proxy;
        }

        private void SetParameters(T decorated)
        {
            if (decorated == null)
            {
                throw new ArgumentNullException(nameof(decorated));
            }
            _decorated = decorated;
        }

        private bool TryGetScoped(MethodInfo methodInfo, object[] args, out object value)
        {
            if (_methodCallResults.TryGetValue(CreateKey(methodInfo, args), out var val))
            {
                value = val;
                return true;
            }

            value = null;
            return false;
        }

        private string CreateKey(MethodInfo methodInfo, object[] args)
        {
            var hash = GetDeterministicHashCode(JsonConvert.SerializeObject(args));

            return methodInfo.Name + hash;
        }

        private int GetDeterministicHashCode(string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
}
