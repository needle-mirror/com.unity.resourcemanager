using System;
using System.Collections;

namespace ResourceManagement
{
    /// <summary>
    /// Base interface of all async ops
    /// </summary>
    public interface IAsyncOperation : IEnumerator
    {
        /// <summary>
        /// Gets the operation identifier. e.g., an object address.
        /// </summary>
        /// <value>The identifier.</value>
        string id { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:ResourceManagement.IAsyncOperation"/> is done.
        /// </summary>
        /// <value><c>true</c> if is done; otherwise, <c>false</c>.</value>
        bool isDone { get; }

        /// <summary>
        /// Gets the percent complete of this operation.
        /// </summary>
        /// <value>The percent complete.</value>
        float percentComplete { get; }

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <value>The result.</value>
        object result { get; }
    }

    /// <summary>
    /// Templated version of IAsyncOperation, provides templated overrides where possible
    /// </summary>
    public interface IAsyncOperation<T> : IAsyncOperation
    {
        /// <summary>
        /// Gets the result as the templated type.
        /// </summary>
        /// <value>The result.</value>
        new T result { get; }

        /// <summary>
        /// Occurs when completed.
        /// </summary>
        event Action<IAsyncOperation<T>> completed;
    }
}