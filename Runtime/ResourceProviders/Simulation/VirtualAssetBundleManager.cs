#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Provides a simulation layer for asset bundles in order to decrease iteration times.
    /// </summary>
    public class VirtualAssetBundleManager : MonoBehaviour
    {
        Dictionary<string, VirtualAssetBundle> m_allBundles = new Dictionary<string, VirtualAssetBundle>();
        Dictionary<string, VirtualAssetBundle> m_activeBundles = new Dictionary<string, VirtualAssetBundle>();

        long m_remoteLoadSpeed = 1024 * 100; //100 KB per second
        long m_localLoadSpeed = 1024 * 1024 * 10; //10 MB per second

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Loads runtime data and creates a VirtualAssetBundleManager object.
        /// </summary>
        /// <param name="bundleNameConverter">Func used to expand variables in bundle names.</param>
        /// <returns>The created VirtualAssetBundleManager.</returns>
        public static VirtualAssetBundleManager CreateManager(Func<string, string> bundleNameConverter)
        {
            var virtualBundleData = VirtualAssetBundleRuntimeData.Load();
            if (virtualBundleData == null)
                return null;

            var mgr = new GameObject("VirtualAssetBundleManager", typeof(VirtualAssetBundleManager)).GetComponent<VirtualAssetBundleManager>();
            mgr.Initialize(virtualBundleData, bundleNameConverter);
            return mgr;
        }

        /// <summary>
        /// Initialize the VirtualAssetBundleManager object
        /// </summary>
        /// <param name="virtualBundleData">Runtime data that contains the virtual bundles.</param>
        /// <param name="bundleNameConverter">Func used to expand variables in bundle names.</param>
        public void Initialize(VirtualAssetBundleRuntimeData virtualBundleData, Func<string, string> bundleNameConverter)
        {
            Debug.Assert(virtualBundleData != null);
            m_localLoadSpeed = virtualBundleData.LocalLoadSpeed;
            m_remoteLoadSpeed = virtualBundleData.RemoteLoadSpeed;
            foreach (var b in virtualBundleData.AssetBundles)
                m_allBundles.Add(bundleNameConverter(b.Name), b);
        }

        internal bool Unload(IResourceLocation location)
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");

            VirtualAssetBundle bundle = null;
            if (!m_allBundles.TryGetValue(location.InternalId, out bundle))
            {
                Debug.LogWarningFormat("Unable to unload virtual bundle {0}.", location);
                return false;
            }
            if (updatingActiveBundles)
                m_pendingOperations.Add(location.InternalId, null);
            else
                m_activeBundles.Remove(location.InternalId);
            return bundle.Unload();
        }

        internal IAsyncOperation<VirtualAssetBundle> LoadAsync(IResourceLocation location)
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");
            VirtualAssetBundle bundle;
            if (!m_allBundles.TryGetValue(location.InternalId, out bundle))
                return new CompletedOperation<VirtualAssetBundle>().Start(location, location, default(VirtualAssetBundle), new ResourceManagerException(string.Format("Unable to unload virtual bundle {0}.", location)));

            if (updatingActiveBundles)
                m_pendingOperations.Add(location.InternalId, bundle);
            else
                m_activeBundles.Add(location.InternalId, bundle);
            return bundle.StartLoad(location);
        }

        bool updatingActiveBundles = false;
        Dictionary<string, VirtualAssetBundle> m_pendingOperations = new Dictionary<string, VirtualAssetBundle>();
        internal void Update()
        {
            long localCount = 0;
            long remoteCount = 0;
            foreach (var b in m_activeBundles)
                b.Value.CountBandwidthUsage(ref localCount, ref remoteCount);

            long localBW = localCount > 1 ? (m_localLoadSpeed / localCount) : m_localLoadSpeed;
            long remoteBW = remoteCount > 1 ? (m_remoteLoadSpeed / remoteCount) : m_remoteLoadSpeed;
            updatingActiveBundles = true;
            foreach (var b in m_activeBundles)
                b.Value.UpdateAsyncOperations(localBW, remoteBW);
            updatingActiveBundles = false;
            if (m_pendingOperations.Count > 0)
            {
                foreach (var o in m_pendingOperations)
                {
                    if (o.Value == null)
                        m_activeBundles.Remove(o.Key);
                    else
                        m_activeBundles.Add(o.Key, o.Value);
                }
                m_pendingOperations.Clear();
            }
        }
    }
}
#endif
