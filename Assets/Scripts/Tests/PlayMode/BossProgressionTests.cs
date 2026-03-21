using System.Collections;
using Lumenfall.Data;
using Lumenfall.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lumenfall.Tests.PlayMode
{
    public sealed class BossProgressionTests
    {
        [UnityTest]
        public IEnumerator RecordBossDefeat_UnlocksRewardAndFlag()
        {
            GameObject services = new("Services");
            services.AddComponent<SaveService>();
            GameStateService gameStateService = services.AddComponent<GameStateService>();
            gameStateService.BeginNewGame(0);

            BossDefinition bossDefinition = ScriptableObject.CreateInstance<BossDefinition>();
            bossDefinition.bossId = "rust_sentinel";
            bossDefinition.rewardAbility = AbilityType.DashCore;
            bossDefinition.rewardFlag = "silent_gate_cleared";
            bossDefinition.corruptionReward = 7;

            gameStateService.RecordBossDefeat(bossDefinition);
            yield return null;

            Assert.That(gameStateService.ActiveSave.HasAbility(AbilityType.DashCore), Is.True);
            Assert.That(gameStateService.ActiveSave.worldFlags.Contains("silent_gate_cleared"), Is.True);
            Assert.That(gameStateService.ActiveSave.defeatedBossIds.Contains("rust_sentinel"), Is.True);
            Assert.That(gameStateService.ActiveSave.corruptionScore, Is.EqualTo(7));

            Object.Destroy(services);
            Object.Destroy(bossDefinition);
        }
    }
}
