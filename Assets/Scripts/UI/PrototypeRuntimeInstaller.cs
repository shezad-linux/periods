using Lumenfall.Core;
using Lumenfall.Data;
using Lumenfall.Gameplay.Abilities;
using Lumenfall.Gameplay.Combat;
using Lumenfall.Gameplay.Player;
using Lumenfall.Services;
using Lumenfall.World;
using Lumenfall.World.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lumenfall.UI
{
    public static class PrototypeRuntimeInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototypeScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                return;
            }

            if (Object.FindFirstObjectByType<AreaSceneRoot>() != null || activeScene.name != "SampleScene")
            {
                return;
            }

            if (ServiceRegistry.TryGet(out GameStateService gameStateService))
            {
                gameStateService.BeginNewGame(0);
                gameStateService.UnlockAbility(AbilityType.DashCore);
                gameStateService.UnlockAbility(AbilityType.WallClimb);
                gameStateService.UnlockAbility(AbilityType.LumenPulse);
                gameStateService.UnlockAbility(AbilityType.PhaseShift);
                gameStateService.UnlockAbility(AbilityType.CrystalGrapple);
            }

            GameObject areaObject = new("PrototypeArea");
            AreaSceneRoot areaRoot = areaObject.AddComponent<AreaSceneRoot>();

            AreaDefinition areaDefinition = ScriptableObject.CreateInstance<AreaDefinition>();
            areaDefinition.areaId = LumenfallConstants.PrototypeAreaId;
            areaDefinition.displayName = "Prototype Silent Gate";
            areaDefinition.sceneName = activeScene.name;
            areaRoot.Initialize(areaDefinition);

            RoomDefinition roomDefinition = ScriptableObject.CreateInstance<RoomDefinition>();
            roomDefinition.area = areaDefinition;
            roomDefinition.roomId = LumenfallConstants.PrototypeRoomId;
            roomDefinition.displayName = "Awakening Chamber";
            areaDefinition.rooms.Add(roomDefinition);

            GameObject roomObject = new("PrototypeRoom");
            roomObject.transform.SetParent(areaObject.transform, false);
            BoxCollider2D roomVolume = roomObject.AddComponent<BoxCollider2D>();
            roomVolume.isTrigger = true;
            roomVolume.size = new Vector2(40f, 16f);
            RoomRoot roomRoot = roomObject.AddComponent<RoomRoot>();
            roomRoot.Initialize(roomDefinition, roomVolume);

            CreatePlatform(roomObject.transform, "Ground", new Vector2(0f, -3.75f), new Vector2(18f, 1.5f), new Color(0.1f, 0.14f, 0.18f));
            CreatePlatform(roomObject.transform, "UpperLedge", new Vector2(4.5f, 0.5f), new Vector2(4f, 0.75f), new Color(0.12f, 0.18f, 0.22f));
            CreatePlatform(roomObject.transform, "LeftLedge", new Vector2(-5.5f, -0.2f), new Vector2(3f, 0.75f), new Color(0.12f, 0.18f, 0.22f));

            GameObject shrine = CreateInteractable(roomObject.transform, "CheckpointShrine", new Vector2(-3.5f, -2.7f), new Color(0.4f, 0.9f, 1f));
            CheckpointShrine checkpointShrine = shrine.AddComponent<CheckpointShrine>();
            checkpointShrine.Configure(roomRoot);

            GameObject grappleAnchor = CreateInteractable(roomObject.transform, "GrappleAnchor", new Vector2(6.2f, 2.2f), new Color(0.55f, 0.9f, 1f));
            grappleAnchor.AddComponent<GrappleAnchorTarget>();

            GameObject phaseWall = CreatePlatform(roomObject.transform, "PhaseWall", new Vector2(8f, -1.4f), new Vector2(1f, 3f), new Color(0.7f, 0.3f, 0.9f));
            PhaseWallTarget phaseWallTarget = phaseWall.AddComponent<PhaseWallTarget>();
            phaseWallTarget.Configure(phaseWall.GetComponent<BoxCollider2D>());

            GameObject pulseNode = CreateInteractable(roomObject.transform, "PulseNode", new Vector2(1.8f, -2.6f), new Color(0.3f, 1f, 0.8f));
            PulseTarget pulseTarget = pulseNode.AddComponent<PulseTarget>();
            pulseTarget.Configure(true);

            GameObject player = CreatePlayer(new Vector2(-6f, -2.3f));

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                CameraFollow2D follow = mainCamera.GetComponent<CameraFollow2D>();
                if (follow == null)
                {
                    follow = mainCamera.gameObject.AddComponent<CameraFollow2D>();
                }

                follow.SetTarget(player.transform);
            }

            if (ServiceRegistry.TryGet(out GameStateService finalGameStateService))
            {
                finalGameStateService.RecordCheckpoint("prototype_start", roomDefinition.roomId, areaDefinition.areaId, player.transform.position);
            }

            roomRoot.BindToArea(areaRoot);
        }

        private static GameObject CreatePlayer(Vector2 position)
        {
            GameObject player = new("Player");
            player.tag = "Player";
            player.transform.position = position;

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f));
            renderer.color = new Color(0.75f, 0.85f, 0.95f);

            CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.8f, 1.8f);
            player.AddComponent<PlayerMotor2D>();
            player.AddComponent<ContextResolver>();
            player.AddComponent<AbilityController>();
            player.AddComponent<PlayerCombatController>();
            player.AddComponent<PlayerHealth>();
            player.AddComponent<Hurtbox>();
            return player;
        }

        private static GameObject CreatePlatform(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject platform = new(name);
            platform.transform.SetParent(parent, false);
            platform.transform.position = position;
            platform.layer = LayerMask.NameToLayer(LumenfallLayerNames.Collision) >= 0 ? LayerMask.NameToLayer(LumenfallLayerNames.Collision) : 0;

            SpriteRenderer renderer = platform.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f));
            renderer.color = color;
            platform.transform.localScale = new Vector3(size.x, size.y, 1f);

            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            return platform;
        }

        private static GameObject CreateInteractable(Transform parent, string name, Vector2 position, Color color)
        {
            GameObject interactable = CreatePlatform(parent, name, position, new Vector2(0.8f, 1.4f), color);
            interactable.GetComponent<BoxCollider2D>().isTrigger = false;
            return interactable;
        }
    }
}
