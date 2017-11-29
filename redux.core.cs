using System;
using System.Linq;

namespace SimpleRedux
{
    public delegate object Reducer(object state, object action);
    public delegate T Reducer<T>(T state, object action);
    public delegate TN Reducer<TP, TN>(TP state, object action);

    public delegate Func<Dispatcher, Dispatcher> Middleware(Dispatcher dispatch, Func<object> getState);
    public delegate Func<Dispatcher, Dispatcher> Middleware<T>(Dispatcher dispatch, Func<T> getState);

    public delegate object Dispatcher(object action);
    public delegate void UnSubscribe();

    public interface IStore
    {
        object GetState();
        object Dispatch(object action);
        UnSubscribe Subscribe(Action listener);
    }

    public partial class Store<T>
    {
        public Store(T initState, Reducer<T> reducer = null)
        {
            currentState = initState;
            currentReducer = reducer ?? ((state, action) => state);
            _dispatcher = InitialDispatch;
        }

        private bool isDispatching;
        private T currentState;
        private Reducer<T> currentReducer;
        private Dispatcher _dispatcher;

        partial void after_dipatch();

        public void ReplaceReducer(Reducer<T> reducer)
        {
            if (reducer != null)
                currentReducer = reducer;
        }

        public T GetState() => currentState;

        public object Dispatch(object action) => _dispatcher?.Invoke(action);

        public void ApplyMiddlewares(params Middleware<T>[] middlewares)
        {
            if (middlewares == null) return;
            //Dispatcher dispatcher = _dispatcher;
            Dispatcher dispatcher = InitialDispatch;
            var fs = middlewares.Select(middleware => middleware(Dispatch, () => GetState())).ToArray();
            for (var i = fs.Length - 1; i >= 0; i--)
            {
                dispatcher = (fs[i]?.Invoke(dispatcher) ?? dispatcher);
            }
            _dispatcher = dispatcher;
        }

        private object InitialDispatch(object action)
        {
            if (isDispatching) throw new InvalidOperationException("reducer is excuting");
            try
            {
                isDispatching = true;
                currentState = currentReducer(currentState, action);
            }
            finally
            {
                isDispatching = false;
            }
            after_dipatch();
            return action;
        }
    }

    public partial class Store<T>
    {
        private Action listeners;

        public bool AllowSubscription { get; set; } = true;

        public UnSubscribe Subscribe(Action listener)
        {
            if (!this.AllowSubscription) throw new InvalidOperationException("do not allow Subscription");
            if (listener != null) listeners += listener;
            return () =>
            {
                if (listener != null)
                {
                    var _ = listener;
                    listener = null;
                    listeners -= _;
                }
            };
        }

        partial void after_dipatch()
        {
            if (!this.AllowSubscription) return;
            listeners?.Invoke();
        }
    }

    public partial class Store<T> : IStore
    {
        object IStore.GetState() => this.GetState();
    }
}