using System.Collections.Generic;
using Lumenfall.Data;
using Lumenfall.Services;
using NUnit.Framework;
using UnityEngine;

namespace Lumenfall.Tests.EditMode
{
    public sealed class EndingRuleTests
    {
        [Test]
        public void EvaluateEnding_UsesMatchingRuleBeforeFallback()
        {
            GameObject root = new("Services");
            root.AddComponent<SaveService>();
            GameStateService gameStateService = root.AddComponent<GameStateService>();
            gameStateService.BeginNewGame(0);
            gameStateService.AddCorruption(60);
            gameStateService.SetReleaseChoice(true);

            EndingRuleDefinition releaseRule = ScriptableObject.CreateInstance<EndingRuleDefinition>();
            releaseRule.endingType = EndingType.Release;
            releaseRule.minCorruption = 40;
            releaseRule.maxCorruption = 100;
            releaseRule.requireReleaseChoice = true;

            EndingType ending = gameStateService.EvaluateEnding(new List<EndingRuleDefinition> { releaseRule });

            Assert.That(ending, Is.EqualTo(EndingType.Release));

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(releaseRule);
        }
    }
}
