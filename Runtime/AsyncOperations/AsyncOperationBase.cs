using System.Collections.Generic;
using System;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// base class for implemented AsyncOperations, implements the needed interfaces and consolidates redundant code
    /// </summary>
    public abstract class AsyncOperationBase<TObject> : IAsyncOperation<TObject>
    {
        protected TObject m_result;
        protected AsyncOperationStatus m_status;
        protected Exception m_error;
        protected object m_context;
        protected object m_key;
        protected bool m_releaseToCacheOnCompletion = false;
        Action<IAsyncOperation> m_completedAction;
        List<Action<IAsyncOperation<TObject>>> m_completedActionT;

        protected AsyncOperationBase()
        {
            IsValid = true;
        }

        public bool IsValid { get; set; }

        public override string ToString()
        {
            var instId = "";
            var or = m_result as Object;
            if (or != null)
                instId = "(" + or.GetInstanceID().ToString() + ")";
            return base.ToString() +  " result = " + m_result + instId + ", status = " + m_status + ", valid = " + IsValid + ", canRelease = " + m_releaseToCacheOnCompletion;
        }

        public virtual void Release()
        {
            Validate();
            m_releaseToCacheOnCompletion = true;
            if (!m_insideCompletionEvent && IsDone)
                AsyncOperationCache.Instance.Release(this);
        }

        public IAsyncOperation<TObject> Retain()
        {
            Validate();
            m_releaseToCacheOnCompletion = false;
            return this;
        }

        public virtual void ResetStatus()
        {
            m_releaseToCacheOnCompletion = true;
            m_status = AsyncOperationStatus.None;
            m_error = null;
            m_result = default(TObject);
            m_context = null;
            m_key = null;
        }

        public bool Validate()
        {
            if (!IsValid)
            {
                Debug.LogError("INVALID OPERATION STATE: " + this);
                return false;
            }
            return true;
        }

        public event Action<IAsyncOperation<TObject>> Completed
        {
            add
            {
                Validate();
                if (IsDone)
                {
                    DelayedActionManager.AddAction(value, 0, this);
                }
                else
                {
                    if (m_completedActionT == null)
                        m_completedActionT = new List<Action<IAsyncOperation<TObject>>>(2);
                    m_completedActionT.Add(value);
                }
            }

            remove
            {
                m_completedActionT.Remove(value);
            }
        }
		
		event Action<IAsyncOperation> IAsyncOperation.Completed
		{
			add
			{
                Validate();
                if (IsDone)
                    DelayedActionManager.AddAction(value, 0, this);
                else
                    m_completedAction += value;
            }

            remove
			{
				m_completedAction -= value;
			}
		}

        object IAsyncOperation.Result
        {
            get
            {
                Validate();
                return m_result;
            }
        }

        public AsyncOperationStatus Status
        {
            get
            {
                Validate();
                return m_status;
            }
            protected set
            {
                Validate();
                m_status = value;
            }
        }

        public Exception OperationException
        {
            get
            {
                Validate();
                return m_error;
            }
            protected set
            {
                m_error = value;
            }
        }

        public bool MoveNext()
        {
            Validate();
            return !IsDone;
        }

        public void Reset()
        {
        }

        public object Current
        {
            get
            {
                Validate();
                return Result;
            }
        }
        public TObject Result
        {
            get
            {
                Validate();
                return m_result;
            }
        }
        public virtual bool IsDone
        {
            get
            {
                Validate();
                return Status == AsyncOperationStatus.Failed || Status == AsyncOperationStatus.Succeeded;
            }
        }
        public virtual float PercentComplete
        {
            get
            {
                Validate();
                return IsDone ? 1f : 0f;
            }
        }
        public object Context
        {
            get
            {
                Validate();
                return m_context;
            }
            protected set
            {
                Validate();
                m_context = value;
            }
        }
        public virtual object Key
        {
            get
            {
                Validate();
                return m_key;
            }
            set
            {
                Validate();
                m_key = value;
            }
        }

        bool m_insideCompletionEvent = false;
        public void InvokeCompletionEvent()
        {
            Validate();
            m_insideCompletionEvent = true;

            if (m_completedAction != null)
            {
                var tmpEvent = m_completedAction;
                m_completedAction = null;
                try
                {
                    tmpEvent(this);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    m_error = e;
                    m_status = AsyncOperationStatus.Failed;
                }
            }

            if (m_completedActionT != null)
            {
                for (int i = 0; i < m_completedActionT.Count; i++)
                {
                    try
                    {
                        m_completedActionT[i](this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        m_error = e;
                        m_status = AsyncOperationStatus.Failed;
                    }
                }
                m_completedActionT.Clear();
            }
            m_insideCompletionEvent = false;
            if (m_releaseToCacheOnCompletion)
                AsyncOperationCache.Instance.Release(this);
        }

        public virtual void SetResult(TObject result)
        {
            Validate();
            m_result = result;
            m_status = (m_result == null) ? AsyncOperationStatus.Failed : AsyncOperationStatus.Succeeded;
        }

    }

    public class CompletedOperation<TObject> : AsyncOperationBase<TObject>
    {
        public CompletedOperation(object context, object key, TObject val, Exception error = null)
        {
            Context = context;
            OperationException = error;
            Key = key;
            SetResult(val);
        }
        public virtual IAsyncOperation<TObject> Start()
        {
            DelayedActionManager.AddAction((Action)InvokeCompletionEvent, 0);
            return this;
        }
    }

    public class EmptyOperation<TObject> : AsyncOperationBase<TObject>
    {
        public virtual IAsyncOperation<TObject> Start(object context, object key, TObject val, Exception error = null)
        {
            Context = context;
            OperationException = error;
            Key = key;
            SetResult(val);
            DelayedActionManager.AddAction((Action)InvokeCompletionEvent, 0);
            return this;
        }
    }

    public class ChainOperation<TObject, TObjectDependency> : AsyncOperationBase<TObject>
    {
        Func<TObjectDependency, IAsyncOperation<TObject>> m_func;
        IAsyncOperation m_dependencyOperation;
        IAsyncOperation m_dependentOperation;
        public virtual IAsyncOperation<TObject> Start(object context, object key, IAsyncOperation<TObjectDependency> dependency, Func<TObjectDependency, IAsyncOperation<TObject>> func)
        {
            m_func = func;
            Context = context;
            Key = key;
            m_dependencyOperation = dependency;
            m_dependentOperation = null;
            dependency.Completed += OnDependencyCompleted;
            return this;
        }

        public override float PercentComplete
        {
            get
            {
                if (m_dependentOperation == null)
                {
                    if (m_dependencyOperation == null)
                        return 0;
                            
                    return m_dependencyOperation.PercentComplete * .5f;
                }
                return m_dependentOperation.PercentComplete * .5f + .5f;
            }
        }

        private void OnDependencyCompleted(IAsyncOperation<TObjectDependency> op)
        {
            m_dependencyOperation = null;
            var funcOp = m_func(op.Result);
            m_dependentOperation = funcOp;
            Context = funcOp.Context;
            funcOp.Key = Key;
            op.Release();
            funcOp.Completed += OnFuncCompleted;
        }

        private void OnFuncCompleted(IAsyncOperation<TObject> op)
        {
            SetResult(op.Result);
            InvokeCompletionEvent();
        }

        public override object Key
        {
            get
            {
                Validate();
                return m_key;
            }
            set
            {
                Validate();
                m_key = value;
                if (m_dependencyOperation != null)
                    m_dependencyOperation.Key = Key;
            }
        }
    }

    public class GroupOperation<TObject> : AsyncOperationBase<IList<TObject>> where TObject : class
    {
        Action<IAsyncOperation<TObject>> m_callback;
        Action<IAsyncOperation<TObject>> m_internalOnComplete;
        List<IAsyncOperation<TObject>> m_operations;
        int m_loadedCount;
        public GroupOperation()
        {
            m_internalOnComplete = OnOperationCompleted;
            m_result = new List<TObject>();
        }

        public override void SetResult(IList<TObject> result)
        {
            Validate();
        }

        public override void ResetStatus()
        {
            m_releaseToCacheOnCompletion = true;
            m_status = AsyncOperationStatus.None;
            m_error = null;
            m_context = null;

            Result.Clear();
            m_operations = null;
        }

        public override object Key
        {
            get
            {
                Validate();
                return m_key;
            }
            set
            {
                Validate();
                m_key = value;
                if (m_operations != null)
                {
                    foreach (var op in m_operations)
                        op.Key = Key;
                }
            }
        }

        public virtual IAsyncOperation<IList<TObject>> Start(IList<IResourceLocation> locations, Action<IAsyncOperation<TObject>> callback, Func<IResourceLocation, IAsyncOperation<TObject>> func)
        {
            m_context = locations;
            m_callback = callback;
            m_loadedCount = 0;
            m_operations = new List<IAsyncOperation<TObject>>(locations.Count);
            foreach (var o in locations)
            {
                Result.Add(default(TObject));
                var op = func(o);
                op.Key = Key;
                m_operations.Add(op);
                op.Completed += m_internalOnComplete;
            }
            return this;
        }

        public virtual IAsyncOperation<IList<TObject>> Start<TParam>(IList<IResourceLocation> locations, Action<IAsyncOperation<TObject>> callback, Func<IResourceLocation, TParam, IAsyncOperation<TObject>> func, TParam funcParams)
        {
            m_context = locations;
            m_callback = callback;
            m_loadedCount = 0;
            m_operations = new List<IAsyncOperation<TObject>>(locations.Count);
            foreach (var o in locations)
            {
                Result.Add(default(TObject));
                var op = func(o, funcParams);
                op.Key = Key;
                m_operations.Add(op);
                op.Completed += m_internalOnComplete;
            }
            return this;
        }

        public override bool IsDone
        {
            get
            {
                Validate();
                return Result.Count == m_loadedCount;
            }
        }

        public override float PercentComplete
        {
            get
            {
                if (IsDone || m_operations.Count < 1)
                    return 1f;
                float total = 0;
                for (int i = 0; i < m_operations.Count; i++)
                    total += m_operations[i].PercentComplete;
                return total / m_operations.Count;
            }
        }

        private void OnOperationCompleted(IAsyncOperation<TObject> op)
        {
            if (m_callback != null)
                m_callback(op);
            m_loadedCount++;
            for (int i = 0; i < m_operations.Count; i++)
            {
                if (m_operations[i] == op)
                {
                    Result[i] = op.Result;
                    if (op.Status != AsyncOperationStatus.Succeeded)
                    {
                        Status = op.Status;
                        m_error = op.OperationException;
                    }
                    break;
                }
            }
            op.Release();
            if (IsDone)
            {
                if (Status != AsyncOperationStatus.Failed)
                    Status = AsyncOperationStatus.Succeeded;
                InvokeCompletionEvent();
            }
        }
    }

}
