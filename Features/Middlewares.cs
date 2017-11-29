using System;
using System.Threading.Tasks;

namespace SimpleRedux
{
    public static class Middlewares
    {
        private static readonly Task AsyncVoidTask = Task.FromResult<object>(null);

        public static Task DispatchAsTask(this IStore store, object action) => store.Dispatch(action) as Task;
        public static Task<T> DispatchAsTask<T>(this IStore store, object action) => store.Dispatch(action) as Task<T>;

        public static Func<Dispatcher, Dispatcher> AsyncTask<T>(Dispatcher dispatch, Func<T> getState)
        {
            return next => action =>
            {
                object o = af<Func<Dispatcher, Dispatcher, Func<T>, Task>>(action, f => f(next, dispatch, getState));
                o = o ?? af<Func<Dispatcher, Func<T>, Task>>(action, f => f(next, getState));
                o = o ?? af<Func<Dispatcher, Task>>(action, f => f(next));
                return o ?? next(action);
            };
        }

        public static Func<Dispatcher, Dispatcher> Thunk<T>(Dispatcher dispatch, Func<T> getState)
        {
            return next => action =>
            {
                object o = af<Action<Dispatcher, Dispatcher, Func<T>>>(action, f => f(next, dispatch, getState), action);
                o = o ?? af<Action<Dispatcher, Func<T>>>(action, f => f(next, getState), action);
                o = o ?? af<Action<Dispatcher>>(action, f => f(next), action);
                o = o ?? af<Func<Dispatcher, T>>(action, f => f(next));
                o = o ?? af<Func<T, T>>(action, f => f(getState()));
                return o ?? next(action);
            };
        }

        private static object af<TDelegate>(object f, Func<TDelegate, object> ctodo) where TDelegate : class
        {
            if (f is TDelegate d) return ctodo(d) ?? AsyncVoidTask;
            return null;
        }

        private static object af<TDelegate>(object a, Action<TDelegate> ctodo, object r) where TDelegate : class
        {
            if (!(a is TDelegate d)) return null;
            ctodo(d);
            return r;
        }
    }
}