using System;

namespace SpeCLI
{
    public interface IParameter
    {
        int Priority { get; }

        string Name { get; }

        string GetObjectValue(object Value);
    }

    public abstract class IParameter<T> : IParameter
    {
        public Type GenericType => typeof(T);
        public int Priority { get; set; }
        public string Name { get; set; }

        public abstract string GetValue(T Value);

        public string GetObjectValue(object Value)
        {
            if (Value is T || Value != null && typeof(T).IsAssignableFrom(Value.GetType()))
            {
                return GetValue((T)Value);
            }
            return GetValue(default);
        }

        protected abstract void Configure(string Name, object Value);
    }
}