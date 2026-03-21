using System;

namespace Lumenfall.Core
{
    public static class LumenfallConstants
    {
        public const string ProductName = "Ashes of Lumenfall";
        public const string CompanyName = "Lumenfall Studio";
        public const int SaveVersion = 1;
        public const int MaxSaveSlots = 3;
        public const int TargetFrameRate = 60;
        public const string PersistentSystemsRootName = "PersistentSystems";
        public const string PrototypeAreaId = "prototype_area";
        public const string PrototypeRoomId = "prototype_room";
    }

    public static class LumenfallSceneNames
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string PersistentSystems = "PersistentSystems";
        public const string SilentGate = "SilentGate";
        public const string ForgottenMarket = "ForgottenMarket";
        public const string DeepArchives = "DeepArchives";
        public const string RootCaverns = "RootCaverns";
        public const string CoreOfLumenfall = "CoreOfLumenfall";

        public static readonly string[] RequiredScenes =
        {
            Boot,
            MainMenu,
            PersistentSystems,
            SilentGate,
            ForgottenMarket,
            DeepArchives,
            RootCaverns,
            CoreOfLumenfall
        };
    }

    public static class LumenfallLayerNames
    {
        public const string Collision = "Collision";
        public const string OneWay = "OneWay";
        public const string Breakable = "Breakable";
        public const string Phase = "Phase";
        public const string Hazard = "Hazard";
        public const string Decoration = "Decoration";
        public const string Minimap = "Minimap";
        public const string Climbable = "Climbable";
        public const string GrappleAnchor = "GrappleAnchor";
        public const string EnemyBlocker = "EnemyBlocker";

        public static readonly string[] Recommended =
        {
            Collision,
            OneWay,
            Breakable,
            Phase,
            Hazard,
            Decoration,
            Minimap,
            Climbable,
            GrappleAnchor,
            EnemyBlocker
        };
    }

    public static class ServiceRegistry
    {
        private static readonly System.Collections.Generic.Dictionary<Type, UnityEngine.Component> Services = new();

        public static void Register(Type contractType, UnityEngine.Component service)
        {
            Services[contractType] = service;
        }

        public static void Unregister(Type contractType, UnityEngine.Component service)
        {
            if (Services.TryGetValue(contractType, out UnityEngine.Component existing) && existing == service)
            {
                Services.Remove(contractType);
            }
        }

        public static bool TryGet<T>(out T service) where T : UnityEngine.Component
        {
            if (Services.TryGetValue(typeof(T), out UnityEngine.Component component) && component is T cast)
            {
                service = cast;
                return true;
            }

            service = null;
            return false;
        }

        public static T Get<T>() where T : UnityEngine.Component
        {
            if (TryGet(out T service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service '{typeof(T).Name}' has not been registered.");
        }

        public static void Clear()
        {
            Services.Clear();
        }
    }
}
