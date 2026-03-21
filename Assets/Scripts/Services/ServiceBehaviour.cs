using System;
using Lumenfall.Core;
using UnityEngine;

namespace Lumenfall.Services
{
    public abstract class ServiceBehaviour : MonoBehaviour
    {
        protected abstract Type ServiceType { get; }

        protected virtual void Awake()
        {
            ServiceRegistry.Register(ServiceType, this);
        }

        protected virtual void OnDestroy()
        {
            ServiceRegistry.Unregister(ServiceType, this);
        }
    }
}
