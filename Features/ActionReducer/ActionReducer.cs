using System;
using System.Collections.Generic;

namespace SimpleRedux
{
    public interface IDyAction
    {
        string Type { get; }
        object Payload { get; }
    }

    public class DyAction<T> : IDyAction
    {
        public string Type { get; set; }
        public T Payload { get; set; }

        object IDyAction.Payload => Payload;
    }

    public static class DyAction
    {
        public static DyAction<object> Create(string type) => new DyAction<object> { Type = type };
        public static DyAction<T> Create<T>(string type, T payload = default(T)) => new DyAction<T> { Type = type, Payload = payload };
    }

    public class ActionReducer<T>
    {
        readonly Dictionary<string, List<Reducer<T>>> dict = new Dictionary<string, List<Reducer<T>>>();

        public Action OnAction<TAction>(Func<T, TAction, T> fn) => OnAction(typeof(TAction).FullName, fn);

        public Action OnAction<TPayload>(string type, Func<T, TPayload, T> fn)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (!dict.TryGetValue(type, out var ls))
                dict[type] = ls = new List<Reducer<T>>();

            var h = AddToList(ls, (state, action) =>
            {
                if (fn == null) return state;
                return fn(state, (TPayload)action);
            });

            return () =>
            {
                if (fn == null) return;
                if (dict.TryGetValue(type, out ls))
                {
                    ls.Remove(h);
                    if (ls.Count == 0) dict.Remove(type);
                }
                fn = null;
            };
        }

        public T Reducer(T state, object action)
        {
            if (action != null)
            {
                string ty;
                object a;
                if (action is IDyAction d)
                {
                    ty = d.Type;
                    a = d.Payload;
                }
                else //action should not to be null
                {
                    ty = action?.GetType().FullName;
                    a = action;
                }

                if (ty != null && dict.TryGetValue(ty, out var ls) && ls != null)
                {
                    foreach (var fn in ls.ToArray())
                        if (fn != null)
                            state = fn(state, a);
                }
            }
            return state;
        }

        static Titem AddToList<Titem>(List<Titem> list, Titem item)
        {
            list.Add(item);
            return item;
        }
    }
}