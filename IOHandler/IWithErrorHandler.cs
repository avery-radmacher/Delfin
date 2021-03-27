using System;

namespace IOHandler
{
    public delegate void ErrorHandler(string errMsg, string errDescription);

    public interface IWithErrorHandler
    {
        protected private static void DefaultErrorHandler(string errMessage, string errDescription)
        {
            throw new Exception($"Error: {errMessage}\n\t{errDescription}");
        }

        public ErrorHandler HandleError { get; }
    }

    public interface ILoader<T> : IWithErrorHandler
    {
        public T Load();
    }

    public interface IHandler<T> : IWithErrorHandler
    {
        public void Handle(T item);
    }
}
