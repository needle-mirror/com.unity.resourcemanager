using System.Collections.Generic;
using System;

namespace UnityEngine.ResourceManagement
{
    /// <summary>
    /// Basic implementation of IResourceLocation
    /// </summary>
    public class ResourceLocationBase : IResourceLocation
    {
        string m_name;
        string m_id;
        string m_providerId;
        List<IResourceLocation> m_dependencies;
        /// <summary>
        /// Internal id.
        /// </summary>
        public string InternalId { get { return m_id; } }
        /// <summary>
        /// Provider Id.  This is usually set to the FullName property of the type of the provider class.
        /// </summary>
        public string ProviderId { get { return m_providerId; } }
        /// <summary>
        /// List of dependencies that must be loaded before this location.  This value may be null.
        /// </summary>
        public IList<IResourceLocation> Dependencies { get { return m_dependencies; } }
        /// <summary>
        /// Convenience method to see if there are any dependencies.
        /// </summary>
        public bool HasDependencies { get { return m_dependencies != null && m_dependencies.Count > 0; } }
        /// <summary>
        /// Data that is intended for the provider.  Objects can be serialized during the build process to be used by the provider.  An example of usage is cache usage data for AssetBundleProvider.
        /// </summary>
        public object Data { get { return null; } }
        /// <summary>
        /// Returns the name of the location. This is usally set to the primary key of the location, or its "address".
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return m_name;
        }
        /// <summary>
        /// Construct a new ResourceLocationBase.
        /// </summary>
        /// <param name="name">The name of the location.  This is usually set to the primary key, or "address" of the location.</param>
        /// <param name="id">The internal id of the location.  This is used by the IResourceProvider to identify the object to provide.  For example this may contain the file path or url of an asset.</param>
        /// <param name="providerId">The provider id.  This is set to the FullName of the type of the provder class.</param>
        /// <param name="dependencies">Locations for the dependencies of this location.</param>
        public ResourceLocationBase(string name, string id, string providerId, params IResourceLocation[] dependencies)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(id);
            if (string.IsNullOrEmpty(providerId))
                throw new ArgumentNullException(providerId);
            m_name = name;
            m_id = id;
            m_providerId = providerId;
            m_dependencies = new List<IResourceLocation>(dependencies);
        }
    }

}
