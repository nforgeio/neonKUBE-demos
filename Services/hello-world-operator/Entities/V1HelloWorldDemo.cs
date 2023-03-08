using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Neon.Kube.Resources;

using k8s;
using k8s.Models;

namespace HelloWorldOperator.Entities
{
    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = KubeGroup, Kind = KubeKind, ApiVersion = KubeApiVersion, PluralName = KubePlural)]
    public class V1HelloWorldDemo : IKubernetesObject<V1ObjectMeta>, ISpec<V1HelloWorldDemo.V1HelloWorldSpec>, IStatus<V1HelloWorldDemo.V1HelloWorldStatus>
    {
        /// <summary>
        /// The API version this Kubernetes type belongs to.
        /// </summary>
        public const string KubeApiVersion = "v1alpha1";

        /// <summary>
        /// The Kubernetes named schema this object is based on.
        /// </summary>
        public const string KubeKind = "HelloWorldDemo";

        /// <summary>
        /// The Group this Kubernetes type belongs to.
        /// </summary>
        public const string KubeGroup = "demo.neonkube.io";

        /// <summary>
        /// The plural name of the entity.
        /// </summary>
        public const string KubePlural = "helloworlddemos";

        /// <summary>
        /// Constructor.
        /// </summary>
        public V1HelloWorldDemo()
        {
            ApiVersion = $"{KubeGroup}/{KubeApiVersion}";
            Kind = KubeKind;
        }

        /// <inheritdoc/>
        public string ApiVersion { get; set; }
        /// <inheritdoc/>
        public string Kind { get; set; }
        /// <inheritdoc/>
        public V1ObjectMeta Metadata { get; set; }
        /// <inheritdoc/>
        public V1HelloWorldSpec Spec { get; set; }
        /// <inheritdoc/>
        public V1HelloWorldStatus Status { get; set; }

        public class V1HelloWorldSpec
        {
            public string LogLevel { get; set; }
            public int HelloWorldReplicas { get; set; }
            public int LoadGeneratorReplicas { get; set; }
            public StorageType StorageType { get; set; } = StorageType.Ephemeral;
            public bool SsoEnabled { get; set; } = false;

            public bool NeonDashboardEnabled { get; set; } = false;
        }

        public class V1HelloWorldStatus
        {
            public string Message;
        }

        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumMemberConverter))]
        public enum StorageType
        {
            [EnumMember(Value = "ephemeral")]
            Ephemeral = 0,

            [EnumMember(Value = "nfs")]
            Nfs
        }
    }
}
