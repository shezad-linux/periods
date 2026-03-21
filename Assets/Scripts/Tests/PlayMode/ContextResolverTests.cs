using System.Collections;
using Lumenfall.Data;
using Lumenfall.Gameplay.Abilities;
using Lumenfall.Gameplay.Player;
using Lumenfall.Services;
using Lumenfall.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lumenfall.Tests.PlayMode
{
    public sealed class ContextResolverTests
    {
        [UnityTest]
        public IEnumerator ContextResolver_PrefersHigherPriorityTargets()
        {
            GameObject services = new("Services");
            services.AddComponent<SaveService>();
            GameStateService gameStateService = services.AddComponent<GameStateService>();
            gameStateService.BeginNewGame(0);
            gameStateService.UnlockAbility(AbilityType.CrystalGrapple);
            gameStateService.UnlockAbility(AbilityType.PhaseShift);

            GameObject player = new("Player");
            player.tag = "Player";
            player.transform.position = Vector3.zero;
            player.AddComponent<CapsuleCollider2D>();
            player.AddComponent<PlayerMotor2D>();
            ContextResolver resolver = player.AddComponent<ContextResolver>();

            GameObject interactable = new("Interactable");
            interactable.transform.position = new Vector3(0.5f, 0f, 0f);
            interactable.AddComponent<CircleCollider2D>();
            interactable.AddComponent<WorldInteractable>();

            GameObject grapple = new("Grapple");
            grapple.transform.position = new Vector3(0.8f, 0f, 0f);
            grapple.AddComponent<CircleCollider2D>();
            grapple.AddComponent<GrappleAnchorTarget>();

            yield return null;

            Assert.That(resolver.CurrentActionType, Is.EqualTo(ContextActionType.Grapple));

            Object.Destroy(services);
            Object.Destroy(player);
            Object.Destroy(interactable);
            Object.Destroy(grapple);
        }
    }
}
